// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CloudInfrastructureManagerTest.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using Xunit;

    public class CloudInfrastructureManagerTest
    {
        [Fact]
        public static void GetLargestInstanceType_AllSame_ReturnsTheType()
        {
            var instanceType = "m1.small";
            var instanceTypes = new[] { instanceType, instanceType, instanceType, instanceType, instanceType, instanceType };
            var largestType = CloudInfrastructureManager.InferLargestInstanceType(instanceTypes);
            Assert.Equal(instanceType, largestType);
        }

        [Fact]
        public static void GetLargestInstanceType_SamePrefixDifferentNumbers_ReturnsTheLargestNumber()
        {
            var instanceTypes = new[] { "m1.small", "m2.small" };
            var largestType = CloudInfrastructureManager.InferLargestInstanceType(instanceTypes);
            Assert.Equal("m2.small", largestType);
        }

        [Fact]
        public static void GetLargestInstanceType_DifferentPrefix_ReturnsTheLargestPrefix()
        {
            var instanceTypes = new[] { "m1.small", "c1.small" };
            var largestType = CloudInfrastructureManager.InferLargestInstanceType(instanceTypes);
            Assert.Equal("c1.small", largestType);
        }

        [Fact]
        public static void GetLargestInstanceType_SamePrefixAndNumberDifferentSuffix_ReturnsTheLargestSuffix()
        {
            var instanceTypes = new[] { "m1.small", "m1.medium" };
            var largestType = CloudInfrastructureManager.InferLargestInstanceType(instanceTypes);
            Assert.Equal("m1.medium", largestType);
        }
    }
}
