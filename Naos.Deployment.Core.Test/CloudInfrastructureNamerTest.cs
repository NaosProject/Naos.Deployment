// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CloudInfrastructureNamerTest.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using Xunit;

    public class CloudInfrastructureNamerTest
    {
        [Fact]
        public static void GetInstanceName_ValidInputs_ValidName()
        {
            var expected = "instance-theComputer@use-east-1a";
            var actual = new CloudInfrastructureNamer("theComputer", "use-east-1a").GetInstanceName();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void GetVolumeName_ValidInputs_ValidName()
        {
            var expected = "ebs-G-theComputer@use-east-1a";
            var actual = new CloudInfrastructureNamer("theComputer", "use-east-1a").GetVolumeName("G");
            Assert.Equal(expected, actual);
        }
    }
}
