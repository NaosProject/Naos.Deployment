// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AddPackageAdjuster.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Naos.Deployment.Domain;
    using OBeautifulCode.Assertion.Recipes;
    using OBeautifulCode.Representation.System;

    /// <summary>
    /// Class to implement <see cref="AdjustDeploymentBase"/> to add message bus harness package when needed.
    /// </summary>
    public class AddPackageAdjuster : AdjustDeploymentBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AddPackageAdjuster"/> class.
        /// </summary>
        /// <param name="matchCriterion">Criterion to match on.</param>
        /// <param name="packagesToInject">Packages to inject.</param>
        /// <param name="shouldBundleDependenciesOfPackage">Value indicating whether or not the dependencies of the package should be bundled when injecting the package.</param>
        public AddPackageAdjuster(IReadOnlyCollection<DeploymentAdjustmentMatchCriteria> matchCriterion, IReadOnlyCollection<PackageDescriptionWithOverrides> packagesToInject, bool shouldBundleDependenciesOfPackage)
        {
            this.MatchCriterion = matchCriterion;
            this.PackagesToInject = packagesToInject;
            this.ShouldBundleDependenciesOfPackage = shouldBundleDependenciesOfPackage;
        }

        /// <summary>
        /// Gets the match criterion.
        /// </summary>
        public IReadOnlyCollection<DeploymentAdjustmentMatchCriteria> MatchCriterion { get; private set; }

        /// <summary>
        /// Gets the package to inject.
        /// </summary>
        public IReadOnlyCollection<PackageDescriptionWithOverrides> PackagesToInject { get; private set; }

        /// <summary>
        /// Gets a value indicating whether or not the dependencies of the package should be bundled when injecting the package.
        /// </summary>
        public bool ShouldBundleDependenciesOfPackage { get; private set; }

        /// <inheritdoc cref="AdjustDeploymentBase" />
        public override bool IsMatch(IManageConfigFiles configFileManager, IReadOnlyCollection<PackagedDeploymentConfiguration> packagedDeploymentConfigsWithDefaultsAndOverrides, DeploymentConfiguration configToCreateWith)
        {
            var matches = this.GetMatches(packagedDeploymentConfigsWithDefaultsAndOverrides, configToCreateWith);
            return matches.Any();
        }

        /// <inheritdoc cref="AdjustDeploymentBase" />
        public override IReadOnlyCollection<InjectedPackage> GetAdditionalPackages(
            string environment,
            string instanceName,
            int instanceNumber,
            IManageConfigFiles configFileManager,
            IReadOnlyCollection<PackagedDeploymentConfiguration> packagedDeploymentConfigsWithDefaultsAndOverrides,
            DeploymentConfiguration configToCreateWith,
            PackageHelper packageHelper,
            SetupStepFactorySettings setupStepFactorySettings)
        {
            new { configFileManager }.AsArg().Must().NotBeNull();
            new { packageHelper }.AsArg().Must().NotBeNull();
            new { setupStepFactorySettings }.AsArg().Must().NotBeNull();

            var ret = new List<InjectedPackage>();
            var matches = this.GetMatches(packagedDeploymentConfigsWithDefaultsAndOverrides, configToCreateWith);
            var reason = string.Join(",", matches.Select(_ => _.Name));
            foreach (var packageToInject in this.PackagesToInject)
            {
                var package = packageHelper.GetPackage(packageToInject.PackageDescription, this.ShouldBundleDependenciesOfPackage);

                var packagedConfig = new PackagedDeploymentConfiguration
                             {
                                 PackageWithBundleIdentifier = package,
                                 DeploymentConfiguration = configToCreateWith,
                                 InitializationStrategies = packageToInject.InitializationStrategies,
                                 ItsConfigOverrides = packageToInject.ItsConfigOverrides,
                             };

                ret.Add(new InjectedPackage(reason, packagedConfig));
            }

            return ret;
        }

        private IReadOnlyCollection<DeploymentAdjustmentMatchCriteria> GetMatches(IReadOnlyCollection<PackagedDeploymentConfiguration> packagedDeploymentConfigsWithDefaultsAndOverrides, DeploymentConfiguration configToCreateWith)
        {
            var initializationStrategies =
                packagedDeploymentConfigsWithDefaultsAndOverrides.SelectMany(p => p.InitializationStrategies.Select(i => i.GetType().ToRepresentation())).ToList();

            var ret = this.MatchCriterion.Where(_ => _.Matches(configToCreateWith.InstanceType.OperatingSystem, initializationStrategies));
            return ret.ToList();
        }
    }

    /// <summary>
    /// Criteria to match an adjustment on.
    /// </summary>
    public class DeploymentAdjustmentMatchCriteria
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeploymentAdjustmentMatchCriteria"/> class.
        /// </summary>
        /// <param name="name">Friendly name for match.</param>
        /// <param name="skusToMatch"><see cref="WindowsSku"/>'s to match on.</param>
        /// <param name="distributionsToMatch"><see cref="LinuxDistribution"/>'s to match on.</param>
        /// <param name="initializationStrategiesToMatch"><see cref="TypeRepresentation"/> of the implementers of <see cref="InitializationStrategyBase"/> to match on.</param>
        /// <param name="typeMatchStrategy"><see cref="TypeMatchStrategy"/> to use with <see cref="TypeRepresentation"/>'s.</param>
        /// <param name="criteriaMatchStrategy"><see cref="CriteriaMatchStrategy"/> to use when matching.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "skus", Justification = "Spelling/name is correct.")]
        public DeploymentAdjustmentMatchCriteria(string name, IReadOnlyCollection<WindowsSku> skusToMatch, IReadOnlyCollection<LinuxDistribution> distributionsToMatch, IReadOnlyCollection<TypeRepresentation> initializationStrategiesToMatch, TypeMatchStrategy typeMatchStrategy, CriteriaMatchStrategy criteriaMatchStrategy)
        {
            new { name }.AsArg().Must().NotBeNullNorWhiteSpace();

            this.Name = name;
            this.SkusToMatch = skusToMatch;
            this.DistributionsToMatch = distributionsToMatch;
            this.InitializationStrategiesToMatch = initializationStrategiesToMatch;
            this.TypeMatchStrategy = typeMatchStrategy;
            this.CriteriaMatchStrategy = criteriaMatchStrategy;
        }

        /// <summary>
        /// Gets the friendly name for match.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the <see cref="WindowsSku"/>'s to match on.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Skus", Justification = "Spelling/name is correct.")]
        public IReadOnlyCollection<WindowsSku> SkusToMatch { get; private set; }

        /// <summary>
        /// Gets the <see cref="LinuxDistribution"/>'s to match on.
        /// </summary>
        public IReadOnlyCollection<LinuxDistribution> DistributionsToMatch { get; private set; }

        /// <summary>
        /// Gets the <see cref="TypeRepresentation"/> of the implementers of <see cref="InitializationStrategyBase"/> to match on.
        /// </summary>
        public IReadOnlyCollection<TypeRepresentation> InitializationStrategiesToMatch { get; private set; }

        /// <summary>
        /// Gets the <see cref="TypeMatchStrategy"/> to use with <see cref="TypeRepresentation"/>'s.
        /// </summary>
        public TypeMatchStrategy TypeMatchStrategy { get; private set; }

        /// <summary>
        /// Gets the <see cref="CriteriaMatchStrategy"/> to use when matching.
        /// </summary>
        public CriteriaMatchStrategy CriteriaMatchStrategy { get; private set; }

        /// <summary>
        /// A value indicating whether or not there is a match on the criteria.
        /// </summary>
        /// <param name="operatingSystem"><see cref="OperatingSystemDescriptionBase"/> of the deployment.</param>
        /// <param name="initializationStrategies"><see cref="TypeRepresentation"/> of the implementers of <see cref="InitializationStrategyBase"/> used in the current deployment.</param>
        /// <returns>A value indicating whether or not the criteria is a match.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sku", Justification = "Spelling/name is correct.")]
        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse", Justification = "Don't care about it.")]
        public bool Matches(OperatingSystemDescriptionBase operatingSystem, IReadOnlyCollection<TypeRepresentation> initializationStrategies)
        {
            var match = false;

            var windowsOs = operatingSystem as OperatingSystemDescriptionWindows;
            if (windowsOs != null)
            {
                match = this.SkusToMatch.Contains(windowsOs.Sku);
                if (this.CriteriaMatchStrategy == CriteriaMatchStrategy.MatchAny && match)
                {
                    return match;
                }
            }

            var linuxOs = operatingSystem as OperatingSystemDescriptionLinux;
            if (linuxOs != null)
            {
                match = this.DistributionsToMatch.Contains(linuxOs.Distribution);
                if (this.CriteriaMatchStrategy == CriteriaMatchStrategy.MatchAny && match)
                {
                    return match;
                }
            }

            var typeComparer = new TypeComparer(this.TypeMatchStrategy);
            match = this.InitializationStrategiesToMatch.Intersect(initializationStrategies, typeComparer).Any();
            if (this.CriteriaMatchStrategy == CriteriaMatchStrategy.MatchAny && match)
            {
                return match;
            }

            return match;
        }
    }

    /// <summary>
    /// Enumeration of options when matching.
    /// </summary>
    public enum CriteriaMatchStrategy
    {
        /// <summary>
        /// Invalid default.
        /// </summary>
        Invalid,

        /// <summary>
        /// Match any criteria.
        /// </summary>
        MatchAny,

        /// <summary>
        /// Must match all criteria.
        /// </summary>
        MatchAll,
    }
}
