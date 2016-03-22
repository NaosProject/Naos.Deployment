// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArcologyTest.cs" company="Naos">
//   Copyright 2015 Naos
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

    public class ArcologyTest
    {
        [Fact]
        public static void GetInstancesByDeployedPackages_Match_ReturnsInstance()
        {
            // arrange
            var environment = "envo";
            var packageExisting = new PackageDescription { Id = "MyTestPackage", Version = "1" };
            var packageSearching = new PackageDescription { Id = "MyTestPackage", Version = "2" };

            var arcology = new Arcology
                               {
                                   Environment = environment,
                                   Instances = new List<InstanceWrapper>(),
                                   CloudContainers =
                                       new[]
                                           {
                                               new ComputingContainerDescription
                                                   {
                                                       InstanceAccessibility =
                                                           InstanceAccessibility
                                                           .Private,
                                                       Cidr = "10.0.0.0/24"
                                                   }
                                           },
                                   WindowsSkuSearchPatternMap =
                                       new Dictionary<WindowsSku, string> { { WindowsSku.Base, "matchy" } }
                               };

            var deploymentConfiguration = new DeploymentConfiguration
                                              {
                                                  InstanceAccessibility =
                                                      InstanceAccessibility.Private,
                                                  InstanceType =
                                                      new InstanceType
                                                          {
                                                              WindowsSku =
                                                                  WindowsSku.Base
                                                          }
                                              };

            var newInstance = arcology.MakeNewInstanceCreationDetails(
                deploymentConfiguration,
                new[] { packageExisting });

            // act
            var needDeletingList = arcology.GetInstancesByDeployedPackages(new[] { packageSearching });

            // assert
            Assert.NotNull(needDeletingList);
            var instanceDescription = needDeletingList.Single();
            Assert.Equal(environment, instanceDescription.Environment);
            Assert.Equal(packageSearching.Id, instanceDescription.DeployedPackages.Single().Value.Id);
        }
    }
}
