// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Computing.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Console
{
    using System.Collections.Generic;

    /// <summary>
    /// Utility class to help with mapping to cloud resources.
    /// </summary>
    public static class Computing
    {
        /// <summary>
        /// Map of <see cref="string" /> to <see cref="ComputingProviderDetails" />.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "Is not mutable.")]
        public static readonly IReadOnlyDictionary<string, ComputingProviderDetails> Details =
            new Dictionary<string, ComputingProviderDetails>
                {
                    {
                        "production-1",
                        new ComputingProviderDetails
                            {
                                SecondCidrComponent = "31",
                                LocationName = "us-east-2",
                                LocationAbbreviation = "use2",
                                ContainerLocationName = "us-east-2a",
                                ContainerLocationAbbreviation = "az1-use2",
                            }
                    },
                    {
                        "production-2",
                        new ComputingProviderDetails
                            {
                                SecondCidrComponent = "32",
                                LocationName = "us-west-2",
                                LocationAbbreviation = "usw2",
                                ContainerLocationName = "us-west-2b",
                                ContainerLocationAbbreviation = "az2-usw2",
                            }
                    },
                };
    }

    /// <summary>
    /// Details about cloud.
    /// </summary>
    public class ComputingProviderDetails
    {
        /// <summary>
        /// Gets or sets the second component of the CIDR (i.e. 10.XX.0.0/16).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cidr", Justification = "Spelling/name is correct.")]
        public string SecondCidrComponent { get; set; }

        /// <summary>
        /// Gets or sets the location of resources (i.e. us-east-1).
        /// </summary>
        public string LocationName { get; set; }

        /// <summary>
        /// Gets or sets the abbreviation of the location for naming (i.e. use1).
        /// </summary>
        public string LocationAbbreviation { get; set; }

        /// <summary>
        /// Gets or sets the container to store resources in (i.e. us-east-1a).
        /// </summary>
        public string ContainerLocationName { get; set; }

        /// <summary>
        /// Gets or sets the abbreviation of the container (i.e. az1-use1).
        /// </summary>
        public string ContainerLocationAbbreviation { get; set; }
    }
}
