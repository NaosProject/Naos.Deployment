// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NuGetConfigFile.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Core
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;

    using Naos.Deployment.Contract;

    /// <summary>
    /// Ephemeral class used to XML serialize a config file for NuGet.
    /// </summary>
    [Serializable]
    [XmlRoot("configuration")]
    public class NuGetConfigFile
    {
        /// <summary>
        /// Source name for the public NuGet gallery.
        /// </summary>
        public const string NuGetPublicGalleryName = "nuget.org";

        /// <summary>
        /// Source URL for the public NuGet gallery.
        /// </summary>
        public const string NuGetPublicGalleryUrl = "https://www.nuget.org/api/v2/";

        /// <summary>
        /// Serializes a supplied config into XML.
        /// </summary>
        /// <param name="config">Config to serialize.</param>
        /// <returns>XML representation of the supplied config.</returns>
        public static string Serialize(NuGetConfigFile config)
        {
            var serializer = new XmlSerializer(typeof(NuGetConfigFile));
            var stringBuilder = new StringBuilder();
            var writer = new StringWriter(stringBuilder);
            var ns = new XmlSerializerNamespaces();
            ns.Add(string.Empty, string.Empty);
            serializer.Serialize(writer, config, ns);
            var ret = stringBuilder.ToString();
            ret = ret.Replace(
                "packageSourceCredentialKeys",
                config.ActivePackageSource.Single(_ => _.Key != NuGetPublicGalleryName).Key).Replace("utf-16", "utf-8");
            return ret;
        }

        /// <summary>
        /// Builds a NuGetConfigFile object from the repository config (to be serialized to disk).
        /// </summary>
        /// <param name="packageRepositoryConfiguration">Repository configuration to use to build a NuGetConfigFile.</param>
        /// <returns>NuGetConfigFile object(to be serialized to disk)</returns>
        public static NuGetConfigFile BuildConfigFileFromRepositoryConfiguration(PackageRepositoryConfiguration packageRepositoryConfiguration)
        {
            var packageSources = new AddKeyValue[]
                                                    {
                                                        new AddKeyValue
                                                            {
                                                                Key = NuGetPublicGalleryName,
                                                                Value = NuGetPublicGalleryUrl
                                                            },
                                                        new AddKeyValue
                                                            {
                                                                Key = packageRepositoryConfiguration.SourceName,
                                                                Value = packageRepositoryConfiguration.Source
                                                            }
                                                    };
            var ret = new NuGetConfigFile
            {
                PackageSources = packageSources,
                ActivePackageSource = packageSources,
                PackageSourceCredentialContainer =
                    new PackageSourceCredentialContainer
                    {
                        PackageSourceCredentialKeys =
                            new AddKeyValue[]
                                                  {
                                                      new AddKeyValue
                                                          {
                                                              Key = "Username",
                                                              Value = packageRepositoryConfiguration.Username
                                                          },
                                                      new AddKeyValue
                                                          {
                                                              Key = "ClearTextPassword",
                                                              Value = packageRepositoryConfiguration.ClearTextPassword
                                                          },
                                                      new AddKeyValue
                                                          {
                                                              Key = "Password",
                                                              Value = string.Empty
                                                          }
                                                  }
                    }
            };
            return ret;
        }

        /// <summary>
        /// Gets or sets the list of active packages sources.
        /// </summary>
        [XmlArray("activePackageSource")]
        [XmlArrayItem("add")]
        public AddKeyValue[] ActivePackageSource { get; set; }

        /// <summary>
        /// Gets or sets the list of all packages sources.
        /// </summary>
        [XmlArray("packageSources")]
        [XmlArrayItem("add")]
        public AddKeyValue[] PackageSources { get; set; }

        /// <summary>
        /// Gets or sets the credentials.
        /// </summary>
        [XmlElement("packageSourceCredentials")]
        public PackageSourceCredentialContainer PackageSourceCredentialContainer { get; set; }
    }

    /// <summary>
    /// Ephemeral class used to XML serialize a config file for NuGet.
    /// </summary>
    [Serializable]
    [XmlRoot("add")]
    public class AddKeyValue
    {
        /// <summary>
        /// Gets or sets the key value.
        /// </summary>
        [XmlAttribute("key")]
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the value value.
        /// </summary>
        [XmlAttribute("value")]
        public string Value { get; set; }
    }

    /// <summary>
    /// Ephemeral class used to XML serialize a config file for NuGet.
    /// </summary>
    [Serializable]
    [XmlRoot("packageSourceCredentials")]
    public class PackageSourceCredentialContainer
    {
        /// <summary>
        /// Gets or sets the credentials.
        /// </summary>
        [XmlArray("packageSourceCredentialKeys")]
        [XmlArrayItem("add")]
        public AddKeyValue[] PackageSourceCredentialKeys { get; set; }
    }
}