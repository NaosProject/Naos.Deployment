// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArcologyTest.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System.Collections.Generic;
    using System.Linq;

    using Naos.Deployment.Domain;
    using Naos.Deployment.Tracking;
    using Naos.Packaging.Domain;

    using Xunit;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Arcology", Justification = "Spelling/name is correct.")]
    public static class ArcologyTest
    {
        [Fact]
        public static void GetInstancesByDeployedPackages_Match_ReturnsInstance()
        {
            // arrange
            var environment = "envo";
            var packageExisting = new PackageDescriptionWithOverrides { PackageDescription = new PackageDescription { Id = "MyTestPackage", Version = "1" } };
            var packageSearching = new PackageDescriptionWithOverrides { PackageDescription = new PackageDescription { Id = "MyTestPackage", Version = "2" } };

            var computingContainers = new[]
                                                     {
                                                         new ComputingContainerDescription
                                                             {
                                                                 InstanceAccessibility = InstanceAccessibility.Private,
                                                                 Cidr = "10.0.0.0/24",
                                                             },
                                                     };
            var arcologyInfo = new ArcologyInfo
                                   {
                                       ComputingContainers = computingContainers,
                                       Location = "some-location",
                                       RootDomainHostingIdMap = new Dictionary<string, string>(),
                                       WindowsSkuSearchPatternMap =
                                           new Dictionary<WindowsSku, string>
                                               {
                                                   { WindowsSku.Base, "matchy" },
                                               },
                                   };

            var arcology = new Arcology(environment, arcologyInfo, null);
            var deploymentConfiguration = new DeploymentConfiguration
                                              {
                                                  InstanceAccessibility =
                                                      InstanceAccessibility.Private,
                                                  InstanceType =
                                                      new InstanceType
                                                          {
                                                              OperatingSystem = new OperatingSystemDescriptionWindows { Sku = WindowsSku.Base },
                                                          },
                                              };

            var newInstance = arcology.CreateNewDeployedInstance(
                deploymentConfiguration,
                new[] { packageExisting });

            // act
            arcology.MutateInstancesAdd(newInstance);
            var needDeletingList = arcology.GetInstancesByDeployedPackages(new[] { packageSearching.PackageDescription });

            // assert
            Assert.NotNull(needDeletingList);
            var instanceDescription = needDeletingList.Single();
            Assert.Equal(environment, instanceDescription.Environment);
            Assert.Equal(packageSearching.PackageDescription.Id, instanceDescription.DeployedPackages.Single().Value.PackageDescription.Id);
        }
    }
}
