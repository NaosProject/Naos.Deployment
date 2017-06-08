// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AddPackageAdjuster.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;

    using Naos.Deployment.Domain;
    using Naos.MessageBus.Domain;

    using OBeautifulCode.TypeRepresentation;

    /// <summary>
    /// Class to implement <see cref="AdjustDeploymentBase"/> to add message bus harness package when needed.
    /// </summary>
    public class AddPackageAdjuster : AdjustDeploymentBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AddPackageAdjuster"/> class.
        /// </summary>
        /// <param name="matchCriterion">Criterion to match on.</param>
        /// <param name="configToInject">The config to inject for a match.</param>
        public AddPackageAdjuster(IReadOnlyCollection<DeploymentAdjustmentMatchCriteria> matchCriterion, PackagedDeploymentConfiguration configToInject)
        {
            this.MatchCriterion = matchCriterion;
            this.ConfigToInject = configToInject;
        }

        /// <summary>
        /// Gets the match criterion.
        /// </summary>
        public IReadOnlyCollection<DeploymentAdjustmentMatchCriteria> MatchCriterion { get; private set; }

        /// <summary>
        /// Gets the config to inject.
        /// </summary>
        public PackagedDeploymentConfiguration ConfigToInject { get; private set; }

        /// <inheritdoc cref="AdjustDeploymentBase" />
        public override bool IsMatch(ICollection<PackagedDeploymentConfiguration> packagedDeploymentConfigsWithDefaultsAndOverrides, DeploymentConfiguration configToCreateWith)
        {
            var initializationStrategies =
                packagedDeploymentConfigsWithDefaultsAndOverrides.SelectMany(p => p.InitializationStrategies.Select(i => i.GetType().ToTypeDescription()))
                    .ToList();

            var ret = this.MatchCriterion.Any(_ => _.Matches(configToCreateWith.InstanceType.WindowsSku, initializationStrategies));
            return ret;
        }

        /// <inheritdoc cref="AdjustDeploymentBase" />
        public override IReadOnlyCollection<InjectedPackage> GetAdditionalPackages(
            string environment,
            string instanceName,
            int instanceNumber,
            ICollection<PackagedDeploymentConfiguration> packagedDeploymentConfigsWithDefaultsAndOverrides,
            DeploymentConfiguration configToCreateWith,
            PackageHelper packageHelper,
            string[] itsConfigPrecedenceAfterEnvironment,
            string rootDeploymentPath)
        {
            return null;
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
        /// <param name="initializationStrategiesToMatch"><see cref="TypeDescription"/> of the implementers of <see cref="InitializationStrategyBase"/> to match on.</param>
        /// <param name="typeMatchStrategy"><see cref="TypeMatchStrategy"/> to use with <see cref="TypeDescription"/>'s.</param>
        /// <param name="criteriaMatchStrategy"><see cref="CriteriaMatchStrategy"/> to use when matching.</param>
        public DeploymentAdjustmentMatchCriteria(string name, IReadOnlyCollection<WindowsSku> skusToMatch, IReadOnlyCollection<TypeDescription> initializationStrategiesToMatch, TypeMatchStrategy typeMatchStrategy, CriteriaMatchStrategy criteriaMatchStrategy)
        {
            this.Name = name;
            this.SkusToMatch = skusToMatch;
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
        public IReadOnlyCollection<WindowsSku> SkusToMatch { get; private set; }

        /// <summary>
        /// Gets the <see cref="TypeDescription"/> of the implementers of <see cref="InitializationStrategyBase"/> to match on.
        /// </summary>
        public IReadOnlyCollection<TypeDescription> InitializationStrategiesToMatch { get; private set; }

        /// <summary>
        /// Gets the <see cref="TypeMatchStrategy"/> to use with <see cref="TypeDescription"/>'s.
        /// </summary>
        public TypeMatchStrategy TypeMatchStrategy { get; private set; }

        /// <summary>
        /// Gets the <see cref="CriteriaMatchStrategy"/> to use when matching.
        /// </summary>
        public CriteriaMatchStrategy CriteriaMatchStrategy { get; private set; }

        /// <summary>
        /// A value indicating whether or not there is a match on the criteria.
        /// </summary>
        /// <param name="windowsSku"><see cref="WindowsSku"/> of the deployment.</param>
        /// <param name="initializationStrategies"><see cref="TypeDescription"/> of the implementers of <see cref="InitializationStrategyBase"/> used in the current deployment.</param>
        /// <returns>A value indicating whether or not the criteria is a match.</returns>
        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse", Justification = "Don't care about it.")]
        public bool Matches(WindowsSku windowsSku, IReadOnlyCollection<TypeDescription> initializationStrategies)
        {
            var match = false;

            match = this.SkusToMatch.Contains(windowsSku);
            if (this.CriteriaMatchStrategy == CriteriaMatchStrategy.MatchAny && match)
            {
                return match;
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
        MatchAll
    }
}