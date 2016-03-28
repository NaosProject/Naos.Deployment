// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ComputingInfrastructureNamerTest.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System;
    using System.IO;
    using System.Linq;

    using Naos.Deployment.ComputingManagement;

    using Xunit;

    public class ComputingInfrastructureNamerTest
    {
        [Fact]
        public static void GetInstanceName_ValidInputs_ValidName()
        {
            var expected = "instance-demo-theComputer@use-east-1a";
            var actual =
                new ComputingInfrastructureNamer("theComputer", "demo", "use-east-1a").GetInstanceName();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void GetVolumeName_ValidInputs_ValidName()
        {
            var expected = "ebs-demo-theComputer-G@use-east-1a";
            var actual =
                new ComputingInfrastructureNamer("theComputer", "demo", "use-east-1a").GetVolumeName(
                    "G");
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void Constructor_InvalidSubDomainName_ThrowsArgumentException()
        {
            var manualInvalidNamesToTest = new[]
                                         {
                                             null,
                                             string.Empty,
                                             "the.computer", 
                                             "-thecomputer", 
                                             "thecomputer-",
                                             "symbols!",
                                             "symbols@",
                                             "symbols#",
                                             "symbols$",
                                             "symbols%",
                                             "symbols^",
                                             "symbols&",
                                             "symbols*",
                                             "symbols(",
                                             "symbols)",
                                             "symbols=",
                                             "symbols+",
                                         };

            var invalidNamesToTest = Path.GetInvalidPathChars().Select(_ => _.ToString()).ToList();
            invalidNamesToTest.AddRange(Path.GetInvalidFileNameChars().Select(_ => _.ToString()));
            invalidNamesToTest.AddRange(manualInvalidNamesToTest);

            foreach (var invalidName in invalidNamesToTest)
            {
                try
                {
                    var neverGotten = new ComputingInfrastructureNamer(
                        invalidName,
                        "demo",
                        "use-east-1a");

                    throw new NotSupportedException("An invalid name was allowed. Name: " + invalidName);
                }
                catch (ArgumentException)
                {
                    /* no-op as this is inteneded... */
                }
            }
        }
    }
}
