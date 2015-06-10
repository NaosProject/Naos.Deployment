// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CloudInfrastructureManagerSettings.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Contract
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Settings to be provided to the CloudInfrastructureManager (instance type map, etc.).
    /// </summary>
    public class CloudInfrastructureManagerSettings
    {
        /// <summary>
        /// Gets or sets a map of drive letters to AWS volume descriptors.
        /// </summary>
        public Dictionary<string, string> DriveLetterVolumeDescriptorMap { get; set; }

        /// <summary>
        /// Gets or sets a list (in order) of the AWS instance types and their core/RAM details.
        /// </summary>
        public ICollection<AwsInstanceType> AwsInstanceTypes { get; set; }

        /// <summary>
        /// Gets or sets the user data to use when creating an instance (list allows for keeping multiple lines in JSON format).
        /// </summary>
        public ICollection<string> InstanceCreationUserDataLines { get; set; }

        /// <summary>
        /// Combines the lines of user data and replaces the token '{ComputerName}' with the name provided.
        /// </summary>
        /// <param name="computerName">Name of the computer to use when re-naming in user data script.</param>
        /// <returns>User data as an un-encoded string to provide to AWS for creating an instance.</returns>
        public string GetInstanceCreationUserData(string computerName)
        {
            var userData = string.Join(Environment.NewLine, this.InstanceCreationUserDataLines);
            var ret = userData.Replace("{ComputerName}", computerName);
            return ret;
        }
    }

    /// <summary>
    /// Settings class with an AWS instance type and its core/RAM details.
    /// </summary>
    public class AwsInstanceType
    {
        /// <summary>
        /// Gets or sets the number of cores on the instance type.
        /// </summary>
        public int VirtualCores { get; set; }

        /// <summary>
        /// Gets or sets the amount of RAM on the instance type.
        /// </summary>
        public double RamInGb { get; set; }

        /// <summary>
        /// Gets or sets the AWS instance type descriptor.
        /// </summary>
        public string AwsInstanceTypeDescriptor { get; set; }
    }
}
