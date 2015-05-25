// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageManagerTest.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using Naos.Deployment.Contract;

    using Xunit;

    public class PackageManagerTest
    {
        [Fact]
        public static void DistinctPackageIdsMatchExactly_SameCollection_ReturnsTrue()
        {
            var a = new[] { new PackageDescription() { Id = "monkey", Version = null } };
            var b = new[] { new PackageDescription() { Id = "monkey", Version = null } };
            var actual = PackageManager.DistinctPackageIdsMatchExactly(a, b);
            var expected = true;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void DistinctPackageIdsMatchExactly_SameDuplicatesInFirst_ReturnsTrue()
        {
            var a = new[] { new PackageDescription() { Id = "monkey", Version = "1.0.0" }, new PackageDescription() { Id = "monkey", Version = "1.1.0" } };
            var b = new[] { new PackageDescription() { Id = "monkey", Version = null } };
            var actual = PackageManager.DistinctPackageIdsMatchExactly(a, b);
            var expected = true;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void DistinctPackageIdsMatchExactly_SameDuplicatesInSecond_ReturnsTrue()
        {
            var a = new[] { new PackageDescription() { Id = "monkey", Version = null } };
            var b = new[] { new PackageDescription() { Id = "monkey", Version = "1.1.0" }, new PackageDescription() { Id = "monkey", Version = "1.0.0" } };
            var actual = PackageManager.DistinctPackageIdsMatchExactly(a, b);
            var expected = true;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void DistinctPackageIdsMatchExactly_SameDuplicatesInBoth_ReturnsTrue()
        {
            var a = new[] { new PackageDescription() { Id = "monkey", Version = "1.0.0" }, new PackageDescription() { Id = "monkey", Version = "1.1.0" } };
            var b = new[] { new PackageDescription() { Id = "monkey", Version = "1.1.0" }, new PackageDescription() { Id = "monkey", Version = "1.0.0" } };
            var actual = PackageManager.DistinctPackageIdsMatchExactly(a, b);
            var expected = true;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void DistinctPackageIdsMatchExactly_Different_ReturnsFalse()
        {
            var a = new[] { new PackageDescription() { Id = "monkey", Version = null }, new PackageDescription() { Id = "ape", Version = null } };
            var b = new[] { new PackageDescription() { Id = "monkey", Version = null } };
            var actual = PackageManager.DistinctPackageIdsMatchExactly(a, b);
            var expected = false;
            Assert.Equal(expected, actual);
        }
    }
}
