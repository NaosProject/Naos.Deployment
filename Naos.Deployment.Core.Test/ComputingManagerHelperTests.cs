// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ComputingManagerHelperTests.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core.Test
{
    using System.Collections.Generic;

    using FluentAssertions;

    using Naos.Deployment.Domain;
    using Naos.Deployment.MessageBus.Handler;

    using Xunit;

    public static class ComputingManagerHelperTests
    {
        [Fact]
        public static void IsTagMatch___With_match_any_and_no_matches___Returns_false()
        {
            // Arrange
            var tagsToMatch = new Dictionary<string, string> { { "match1", "value1" }, { "match2", "value2" }, };
            var strategy = TagMatchStrategy.Any;
            var tags = new Dictionary<string, string> { { "match3", "value3" }, };
            var expected = false;

            // Act
            var actual = ComputingManagerHelper.IsTagMatch(tags, tagsToMatch, strategy);

            // Assert
            actual.Should().Be(expected);
        }

        [Fact]
        public static void IsTagMatch___With_match_any_and_at_least_one_match___Returns_true()
        {
            // Arrange
            var tagsToMatch = new Dictionary<string, string> { { "match1", "value1" }, { "match2", "value2" }, };
            var strategy = TagMatchStrategy.Any;
            var tags = new Dictionary<string, string> { { "match1", "value1" }, };
            var expected = true;

            // Act
            var actual = ComputingManagerHelper.IsTagMatch(tags, tagsToMatch, strategy);

            // Assert
            actual.Should().Be(expected);
        }

        [Fact]
        public static void IsTagMatch___With_match_all_and_at_least_one_not_match___Returns_false()
        {
            // Arrange
            var tagsToMatch = new Dictionary<string, string> { { "match1", "value1" }, { "match2", "value2" }, };
            var strategy = TagMatchStrategy.All;
            var tags = new Dictionary<string, string> { { "match1", "value1" }, };
            var expected = false;

            // Act
            var actual = ComputingManagerHelper.IsTagMatch(tags, tagsToMatch, strategy);

            // Assert
            actual.Should().Be(expected);
        }

        [Fact]
        public static void IsTagMatch___With_match_all_and_all_match___Returns_true()
        {
            // Arrange
            var tagsToMatch = new Dictionary<string, string> { { "match1", "value1" }, { "match2", "value2" }, };
            var strategy = TagMatchStrategy.All;
            var tags = new Dictionary<string, string> { { "match1", "value1" }, { "match2", "value2" }, { "match3", "value3" }, };
            var expected = true;

            // Act
            var actual = ComputingManagerHelper.IsTagMatch(tags, tagsToMatch, strategy);

            // Assert
            actual.Should().Be(expected);
        }
    }
}
