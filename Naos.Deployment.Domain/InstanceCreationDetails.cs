// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InstanceCreationDetails.cs" company="Naos">
//    Copyright (c) Naos 2017. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    /// <summary>
    /// Carries the information necessary to create an instance.
    /// </summary>
    public class InstanceCreationDetails
    {
        /// <summary>
        /// Gets or sets an object holding the information for looking up an image.
        /// </summary>
        public ImageDetails ImageDetails { get; set; }

        /// <summary>
        /// Gets or sets information about the container the instance lives in.
        /// </summary>
        public ComputingContainerDescription ComputingContainerDescription { get; set; }

        /// <summary>
        /// Gets or sets the location to use.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the private IP address.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ip", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ip", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Ip", Justification = "Spelling/name is correct.")]
        public string PrivateIpAddress { get; set; }

        /// <summary>
        /// Gets or sets the name of the encryption key to use.
        /// </summary>
        public string KeyName { get; set; }

        /// <summary>
        /// Gets or sets the ID of the security group to assign it to.
        /// </summary>
        public string SecurityGroupId { get; set; }
    }
}
