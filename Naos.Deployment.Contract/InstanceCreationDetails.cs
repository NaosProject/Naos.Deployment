// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InstanceCreationDetails.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
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
        public ContainerDetails ContainerDetails { get; set; }

        /// <summary>
        /// Gets or sets the location to use.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the 
        /// </summary>
        public string PrivateIpAddress { get; set; }

        /// <summary>
        /// Gets or sets the name of the encryption key to use.
        /// </summary>
        public string KeyName { get; set; }

        /// <summary>
        /// Gets or sets the ID of the security group to assign it to.
        /// </summary>
        public string SecurityGroupId { get; set; }

        /// <summary>
        /// Gets or sets the default drive type to use.
        /// </summary>
        public string DefaultDriveType { get; set; }
    }
}
