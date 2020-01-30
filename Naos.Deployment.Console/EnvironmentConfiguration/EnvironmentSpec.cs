// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnvironmentSpec.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Console
{
    using System.Collections.Generic;

    using Naos.AWS.Domain;

    using OBeautifulCode.Validation.Recipes;

    using static System.FormattableString;

    /// <summary>
    /// Consolidated configurations for environments.
    /// </summary>
    public static partial class EnvironmentSpec
    {
        /// <summary>
        /// Builds environment specification for creation.
        /// </summary>
        /// <param name="environment">Environment to build for.</param>
        /// <returns>Configuration to use.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Want lowercase here.")]
        public static ConfigEnvironment BuildEnvironmentSpec(string environment)
        {
            var details = Computing.Details[environment.ToLowerInvariant()];

            var zoneToCidrableNameMap = new Dictionary<Zone, string>
                                            {
                                                {
                                                    Zone.Network, Invariant($"{nameof(Vpc)}-{environment}@{details.LocationAbbreviation}")
                                                },
                                                {
                                                    Zone.Vpn, Invariant($"{nameof(Subnet)}-{environment}{nameof(Zone.Vpn)}@{details.ContainerLocationAbbreviation}")
                                                },
                                                {
                                                    Zone.Nat, Invariant($"{nameof(Subnet)}-{environment}{nameof(Zone.Nat)}@{details.ContainerLocationAbbreviation}")
                                                },
                                                {
                                                    Zone.Private, Invariant($"{nameof(Subnet)}-{environment}{nameof(Zone.Private)}@{details.ContainerLocationAbbreviation}")
                                                },
                                                {
                                                    Zone.Public, Invariant($"{nameof(Subnet)}-{environment}{nameof(Zone.Public)}@{details.ContainerLocationAbbreviation}")
                                                },
                                                {
                                                    Zone.Universe, ConfigCidr.AllTrafficCidrName
                                                },
                                            };

            var internetGatewayName = Invariant($"{nameof(InternetGateway)}-{environment}@{details.ContainerLocationAbbreviation}");

            var natElasticIpName = Invariant($"{nameof(ElasticIp)}-{environment}{nameof(Zone.Nat)}@{details.ContainerLocationAbbreviation}");

            var natGatewayName = Invariant($"{nameof(NatGateway)}-{environment}{nameof(Zone.Nat)}@{details.ContainerLocationAbbreviation}");

            var defaultRouteTable = BuildDefaultRouteTable(environment, details.LocationAbbreviation);
            var privateRouteTable = BuildPrivateRouteTable(environment, details.LocationAbbreviation, natGatewayName);
            var publicRouteTable = BuildPublicRouteTable(environment, details.LocationAbbreviation, internetGatewayName);

            var defaultSecurityGroup = BuildDefaultSecurityGroup(environment, details.LocationAbbreviation);

            var privateSubnet = new ConfigSubnet
            {
                Name = zoneToCidrableNameMap[Zone.Private],
                AvailabilityZone = details.ContainerLocationName,
                Cidr = Invariant($"10.{details.SecondCidrComponent}.1.0/24"),
                RouteTableRef = privateRouteTable.Name,
            };

            var vpnSubnet = new ConfigSubnet
            {
                Name = zoneToCidrableNameMap[Zone.Vpn],
                AvailabilityZone = details.ContainerLocationName,
                Cidr = Invariant($"10.{details.SecondCidrComponent}.10.0/24"),
                RouteTableRef = publicRouteTable.Name,
            };

            var natSubnet = new ConfigSubnet
            {
                Name = zoneToCidrableNameMap[Zone.Nat],
                AvailabilityZone = details.ContainerLocationName,
                Cidr = Invariant($"10.{details.SecondCidrComponent}.11.0/24"),
                RouteTableRef = publicRouteTable.Name,
            };

            var publicSubnet = new ConfigSubnet
            {
                Name = zoneToCidrableNameMap[Zone.Public],
                AvailabilityZone = details.ContainerLocationName,
                Cidr = Invariant($"10.{details.SecondCidrComponent}.12.0/24"),
                RouteTableRef = publicRouteTable.Name,
            };

            var defaultNetworkAcl = BuildDefaultNetworkAcl(environment, details.LocationAbbreviation);
            var privateNetworkAcl = BuildPrivateNetworkAcl(environment, details.LocationAbbreviation, zoneToCidrableNameMap, new[] { privateSubnet.Name });
            var publicNetworkAcl = BuildPublicNetworkAcl(environment, details.LocationAbbreviation, zoneToCidrableNameMap, new[] { publicSubnet.Name });
            var natNetworkAcl = BuildNatNetworkAcl(environment, details.LocationAbbreviation, zoneToCidrableNameMap, new[] { natSubnet.Name });
            var vpnNetworkAcl = BuildVpnNetworkAcl(environment, details.LocationAbbreviation, zoneToCidrableNameMap, new[] { vpnSubnet.Name });

            var vpc = new ConfigVpc
            {
                Name = zoneToCidrableNameMap[Zone.Network],
                Cidr = Invariant($"10.{details.SecondCidrComponent}.0.0/16"),
                Tenancy = "default",
                InternetGatewayRef = internetGatewayName,
                RouteTables = new[] { defaultRouteTable, privateRouteTable, publicRouteTable, },
                Subnets = new[] { vpnSubnet, natSubnet, publicSubnet, privateSubnet, },
                NatGateways =
                                  new[]
                                      {
                                          new ConfigNatGateway
                                              {
                                                  Name = natGatewayName,
                                                  ElasticIpRef = natElasticIpName,
                                                  SubnetRef = natSubnet.Name,
                                              },
                                      },
                NetworkAcls = new[] { defaultNetworkAcl, natNetworkAcl, vpnNetworkAcl, publicNetworkAcl, privateNetworkAcl, },
                SecurityGroups = new[] { defaultSecurityGroup, },
            };

            var environmentConfig = new ConfigEnvironment
            {
                Name = environment,
                RegionName = details.LocationName,
                ElasticIps = new[] { new ConfigElasticIp { Name = natElasticIpName } },
                InternetGateways = new[] { new ConfigInternetGateway { Name = internetGatewayName } },
                Vpcs = new[] { vpc, },
            };

            return environmentConfig;
        }

        private static ConfigRouteTable BuildPublicRouteTable(string environmentName, string locationAbbreviation, string internetGatewayName)
        {
            return new ConfigRouteTable
            {
                Name = Invariant($"{nameof(RouteTable)}-{environmentName}{nameof(Zone.Public)}@{locationAbbreviation}"),
                IsDefault = false,
                Routes = new[]
                                        {
                                            new ConfigRoute
                                                {
                                                    DestinationRef = ConfigCidr.AllTrafficCidrName,
                                                    TargetRef = internetGatewayName,
                                                    Comment = "Send traffic bound for internet to Internet Gateway",
                                                },
                                        },
            };
        }

        private static ConfigRouteTable BuildDefaultRouteTable(string environmentName, string locationAbbreviation)
        {
            return new ConfigRouteTable
            {
                Name = Invariant($"{nameof(RouteTable)}-{environmentName}{nameof(Zone.Default)}@{locationAbbreviation}"),
                IsDefault = true,
                Routes = new ConfigRoute[0],
            };
        }

        private static ConfigRouteTable BuildPrivateRouteTable(string environmentName, string locationAbbreviation, string natGatewayName)
        {
            return new ConfigRouteTable
            {
                Name = Invariant($"{nameof(RouteTable)}-{environmentName}{nameof(Zone.Private)}@{locationAbbreviation}"),
                IsDefault = false,
                Routes = new[]
                                        {
                                            new ConfigRoute
                                                {
                                                    DestinationRef = ConfigCidr.AllTrafficCidrName,
                                                    TargetRef = natGatewayName,
                                                    Comment = "Send traffic bound for internet to NAT",
                                                },
                                        },
            };
        }

        private static ConfigSecurityGroup BuildDefaultSecurityGroup(string environmentName, string locationAbbreviation)
        {
            return new ConfigSecurityGroup
            {
                Name = Invariant($"{nameof(SecurityGroup)}-{environmentName}{nameof(Zone.Default)}@{locationAbbreviation}"),
                IsDefault = true,
                InboundRules =
                               new[]
                                   {
                                       new ConfigSecurityGroupInboundRule
                                           {
                                               Protocol = ConfigTrafficRuleBase.AllProtocolsValue,
                                               PortRange = ConfigTrafficRuleBase.AllPortsValue,
                                               SourceRef = ConfigCidr.AllTrafficCidrName,
                                               Comment = "Allow all traffic and let Network Acls block.",
                                           },
                                   },
                OutboundRules = new[]
                                               {
                                                   new ConfigSecurityGroupOutboundRule
                                                       {
                                                           Protocol =
                                                               ConfigTrafficRuleBase.AllProtocolsValue,
                                                           PortRange =
                                                               ConfigTrafficRuleBase.AllPortsValue,
                                                           DestinationRef = ConfigCidr.AllTrafficCidrName,
                                                           Comment =
                                                               "Allow all traffic and let Network Acls block.",
                                                       },
                                               },
            };
        }
    }

    /// <summary>
    /// Zones of computing.
    /// </summary>
    public enum Zone
    {
        /// <summary>
        /// Invalid default enumeration value.
        /// </summary>
        Invalid,

        /// <summary>
        /// Default zone, should generally be locked down and not used.
        /// </summary>
        Default,

        /// <summary>
        /// NAT translated private traffic to open internet.
        /// </summary>
        Nat,

        /// <summary>
        /// Tunneled traffic.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Vpn", Justification = "Spelling/name is correct.")]
        Vpn,

        /// <summary>
        /// Private traffic.
        /// </summary>
        Private,

        /// <summary>
        /// Open to internet public traffic.
        /// </summary>
        Public,

        /// <summary>
        /// All zones (VPC/parent level).
        /// </summary>
        Network,

        /// <summary>
        /// All networks (internet).
        /// </summary>
        Universe,
    }
}