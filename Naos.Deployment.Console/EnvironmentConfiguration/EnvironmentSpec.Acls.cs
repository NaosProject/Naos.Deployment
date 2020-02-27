// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnvironmentSpec.Acls.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Console
{
    using System.Collections.Generic;

    using Naos.AWS.Domain;

    using static System.FormattableString;

    /// <summary>
    /// Consolidated configurations for environments.
    /// </summary>
    public static partial class EnvironmentSpec
    {
        private static ConfigNetworkAcl BuildDefaultNetworkAcl(string environmentName, string regionShortName)
        {
            return new ConfigNetworkAcl
            {
                Name = Invariant($"{nameof(NetworkAcl)}-{environmentName}{Zone.Default}@{regionShortName}"),
                IsDefault = true,
                InboundRules = new ConfigNetworkAclInboundRule[0],
                OutboundRules = new ConfigNetworkAclOutboundRule[0],
            };
        }

        private static ConfigNetworkAcl BuildNatNetworkAcl(string environmentName, string regionShortName, Dictionary<Zone, string> zoneToCidrableNameMap, IReadOnlyCollection<string> subnetNames = null)
        {
            var subnetRef = string.Join(",", subnetNames ?? new string[0]);
            return new ConfigNetworkAcl
            {
                Name = Invariant($"{nameof(NetworkAcl)}-{environmentName}{Zone.Nat}@{regionShortName}"),
                SubnetRef = subnetRef,
                InboundRules =
                               new[]
                                   {
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 90,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.UdpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Ntp],
                                               SourceRef = zoneToCidrableNameMap[Zone.Universe],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 100,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Http],
                                               SourceRef = zoneToCidrableNameMap[Zone.Network],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 110,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Https],
                                               SourceRef = zoneToCidrableNameMap[Zone.Network],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 120,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.SmtpOne],
                                               SourceRef = zoneToCidrableNameMap[Zone.Network],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 130,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.SmtpTwo],
                                               SourceRef = zoneToCidrableNameMap[Zone.Network],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 140,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Dns],
                                               SourceRef = zoneToCidrableNameMap[Zone.Network],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 150,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.UdpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Dns],
                                               SourceRef = zoneToCidrableNameMap[Zone.Network],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 155,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Syslog],
                                               SourceRef = zoneToCidrableNameMap[Zone.Network],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 160,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.AllProtocolsValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.All],
                                               SourceRef = zoneToCidrableNameMap[Zone.Network],
                                               Action = RuleAction.Deny.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 170,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.UdpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Ephemeral],
                                               SourceRef = zoneToCidrableNameMap[Zone.Universe],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 180,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Ephemeral],
                                               SourceRef = zoneToCidrableNameMap[Zone.Universe],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                   },
                OutboundRules = new[]
                                               {
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 90,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.UdpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.Ntp],
                                                           DestinationRef = zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 100,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange =
                                                               ProtocolToPortRangeMap[Protocol.Ephemeral],
                                                           DestinationRef = zoneToCidrableNameMap[Zone.Network],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 110,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.UdpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.Ntp],
                                                           DestinationRef = zoneToCidrableNameMap[Zone.Network],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 120,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.AllProtocolsValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.All],
                                                           DestinationRef = zoneToCidrableNameMap[Zone.Network],
                                                           Action = RuleAction.Deny.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 130,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.UdpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.Dns],
                                                           DestinationRef = zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 140,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.Dns],
                                                           DestinationRef = zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 150,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.SmtpOne],
                                                           DestinationRef = zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 160,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.SmtpTwo],
                                                           DestinationRef = zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 170,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.Http],
                                                           DestinationRef = zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 180,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.Https],
                                                           DestinationRef = zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 190,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.Syslog],
                                                           DestinationRef = zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                               },
            };
        }

        private static ConfigNetworkAcl BuildVpnNetworkAcl(string environmentName, string regionShortName, Dictionary<Zone, string> zoneToCidrableNameMap, IReadOnlyCollection<string> subnetNames = null)
        {
            var subnetRef = string.Join(",", subnetNames ?? new string[0]);
            return new ConfigNetworkAcl
            {
                Name = Invariant($"{nameof(NetworkAcl)}-{environmentName}{Zone.Vpn}@{regionShortName}"),
                SubnetRef = subnetRef,
                InboundRules =
                               new[]
                                   {
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 90,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.UdpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Ntp],
                                               SourceRef = zoneToCidrableNameMap[Zone.Universe],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 100,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Ephemeral],
                                               SourceRef = zoneToCidrableNameMap[Zone.Network],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 105,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.AllProtocolsValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.All],
                                               SourceRef = zoneToCidrableNameMap[Zone.Network],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 110,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.AllProtocolsValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.All],
                                               SourceRef = zoneToCidrableNameMap[Zone.Network],
                                               Action = RuleAction.Deny.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 120,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Ssh],
                                               SourceRef = zoneToCidrableNameMap[Zone.Universe],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 130,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.UdpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Ephemeral],
                                               SourceRef = zoneToCidrableNameMap[Zone.Universe],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 140,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Https],
                                               SourceRef = zoneToCidrableNameMap[Zone.Universe],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 150,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.OpenVpnManage],
                                               SourceRef = zoneToCidrableNameMap[Zone.Universe],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 160,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.UdpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.OpenVpnTunnel],
                                               SourceRef = zoneToCidrableNameMap[Zone.Universe],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 170,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Ephemeral],
                                               SourceRef = zoneToCidrableNameMap[Zone.Universe],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                   },
                OutboundRules = new[]
                                               {
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 90,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.UdpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.Ntp],
                                                           DestinationRef =
                                                               zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 110,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.AllProtocolsValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.All],
                                                           DestinationRef = zoneToCidrableNameMap[Zone.Network],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 115,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.UdpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.Dns],
                                                           DestinationRef =
                                                               zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 120,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.Dns],
                                                           DestinationRef =
                                                               zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 125,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange =
                                                               ProtocolToPortRangeMap[Protocol.Ephemeral],
                                                           DestinationRef =
                                                               zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 130,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange =
                                                               ProtocolToPortRangeMap[Protocol.OpenVpnManage],
                                                           DestinationRef =
                                                               zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 140,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.UdpProtocolValue,
                                                           PortRange =
                                                               ProtocolToPortRangeMap[Protocol.OpenVpnTunnel],
                                                           DestinationRef =
                                                               zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 150,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.Http],
                                                           DestinationRef =
                                                               zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 160,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.Https],
                                                           DestinationRef =
                                                               zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                               },
            };
        }

        private static ConfigNetworkAcl BuildPrivateNetworkAcl(string environmentName, string regionShortName, Dictionary<Zone, string> zoneToCidrableNameMap, IReadOnlyCollection<string> subnetNames = null)
        {
            var subnetRef = string.Join(",", subnetNames ?? new string[0]);
            return new ConfigNetworkAcl
            {
                Name = Invariant($"{nameof(NetworkAcl)}-{environmentName}{Zone.Private}@{regionShortName}"),
                SubnetRef = subnetRef,
                InboundRules =
                               new[]
                                   {
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 90,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.UdpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Ntp],
                                               SourceRef = zoneToCidrableNameMap[Zone.Universe],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 100,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Rdp],
                                               SourceRef = zoneToCidrableNameMap[Zone.Vpn],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 110,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.WinRm],
                                               SourceRef = zoneToCidrableNameMap[Zone.Vpn],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 130,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.MsSql],
                                               SourceRef = zoneToCidrableNameMap[Zone.Vpn],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 140,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.SmbOne],
                                               SourceRef = zoneToCidrableNameMap[Zone.Vpn],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 150,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.UdpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.SmbTwo],
                                               SourceRef = zoneToCidrableNameMap[Zone.Vpn],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 160,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.SmbTwo],
                                               SourceRef = zoneToCidrableNameMap[Zone.Vpn],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 170,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Http],
                                               SourceRef = zoneToCidrableNameMap[Zone.Vpn],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 180,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Https],
                                               SourceRef = zoneToCidrableNameMap[Zone.Vpn],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 190,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.AllProtocolsValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.All],
                                               SourceRef = zoneToCidrableNameMap[Zone.Private],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 200,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.AllProtocolsValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.All],
                                               SourceRef = zoneToCidrableNameMap[Zone.Public],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 210,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Ephemeral],
                                               SourceRef = zoneToCidrableNameMap[Zone.Universe],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 220,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.UdpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Ephemeral],
                                               SourceRef = zoneToCidrableNameMap[Zone.Universe],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                   },
                OutboundRules = new[]
                                               {
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 90,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.UdpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.Ntp],
                                                           DestinationRef =
                                                               zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 100,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange =
                                                               ProtocolToPortRangeMap[Protocol.Ephemeral],
                                                           DestinationRef = zoneToCidrableNameMap[Zone.Vpn],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 110,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.AllProtocolsValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.All],
                                                           DestinationRef = zoneToCidrableNameMap[Zone.Private],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 120,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.AllProtocolsValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.All],
                                                           DestinationRef = zoneToCidrableNameMap[Zone.Public],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 130,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.Http],
                                                           DestinationRef =
                                                               zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 140,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.Https],
                                                           DestinationRef =
                                                               zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 150,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.SmtpOne],
                                                           DestinationRef =
                                                               zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 160,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.SmtpTwo],
                                                           DestinationRef =
                                                               zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 170,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.Dns],
                                                           DestinationRef =
                                                               zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 180,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.UdpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.Dns],
                                                           DestinationRef =
                                                               zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 190,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.Syslog],
                                                           DestinationRef = zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                               },
            };
        }

        private static ConfigNetworkAcl BuildPublicNetworkAcl(string environmentName, string regionShortName, Dictionary<Zone, string> zoneToCidrableNameMap, IReadOnlyCollection<string> subnetNames = null)
        {
            var subnetRef = string.Join(",", subnetNames ?? new string[0]);
            return new ConfigNetworkAcl
            {
                Name = Invariant($"{nameof(NetworkAcl)}-{environmentName}{Zone.Public}@{regionShortName}"),
                SubnetRef = subnetRef,
                InboundRules =
                               new[]
                                   {
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 90,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.UdpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Ntp],
                                               SourceRef = zoneToCidrableNameMap[Zone.Universe],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 100,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Rdp],
                                               SourceRef = zoneToCidrableNameMap[Zone.Vpn],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 110,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Rdp],
                                               SourceRef = zoneToCidrableNameMap[Zone.Universe],
                                               Action = RuleAction.Deny.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 120,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.WinRm],
                                               SourceRef = zoneToCidrableNameMap[Zone.Vpn],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 130,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.WinRm],
                                               SourceRef = zoneToCidrableNameMap[Zone.Universe],
                                               Action = RuleAction.Deny.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 140,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.SmbOne],
                                               SourceRef = zoneToCidrableNameMap[Zone.Vpn],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 150,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.UdpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.SmbTwo],
                                               SourceRef = zoneToCidrableNameMap[Zone.Vpn],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 160,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.SmbTwo],
                                               SourceRef = zoneToCidrableNameMap[Zone.Vpn],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 170,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.AllProtocolsValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.All],
                                               SourceRef = zoneToCidrableNameMap[Zone.Private],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 180,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Http],
                                               SourceRef = zoneToCidrableNameMap[Zone.Universe],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 190,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Https],
                                               SourceRef = zoneToCidrableNameMap[Zone.Universe],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                       new ConfigNetworkAclInboundRule
                                           {
                                               RuleNumber = 200,
                                               Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                               Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                               PortRange = ProtocolToPortRangeMap[Protocol.Ephemeral],
                                               SourceRef = zoneToCidrableNameMap[Zone.Universe],
                                               Action = RuleAction.Allow.ToString(),
                                           },
                                   },
                OutboundRules = new[]
                                               {
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 90,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.UdpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.Ntp],
                                                           DestinationRef =
                                                               zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 100,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange =
                                                               ProtocolToPortRangeMap[Protocol.Ephemeral],
                                                           DestinationRef = zoneToCidrableNameMap[Zone.Vpn],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 110,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.AllProtocolsValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.All],
                                                           DestinationRef = zoneToCidrableNameMap[Zone.Private],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 120,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.Http],
                                                           DestinationRef =
                                                               zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 130,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.Https],
                                                           DestinationRef =
                                                               zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 140,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange =
                                                               ProtocolToPortRangeMap[Protocol.Ephemeral],
                                                           DestinationRef =
                                                               zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 150,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.SmtpOne],
                                                           DestinationRef =
                                                               zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                                   new ConfigNetworkAclOutboundRule
                                                       {
                                                           RuleNumber = 160,
                                                           Type = ConfigNetworkAclRuleBase.AllTypesValue,
                                                           Protocol = ConfigTrafficRuleBase.TcpProtocolValue,
                                                           PortRange = ProtocolToPortRangeMap[Protocol.SmtpTwo],
                                                           DestinationRef =
                                                               zoneToCidrableNameMap[Zone.Universe],
                                                           Action = RuleAction.Allow.ToString(),
                                                       },
                                               },
            };
        }

        private static readonly IReadOnlyDictionary<Protocol, string> ProtocolToPortRangeMap =
            new Dictionary<Protocol, string>
                {
                    { Protocol.All, ConfigTrafficRuleBase.AllPortsValue },
                    { Protocol.Ntp, "123" },
                    { Protocol.Dns, "53" },
                    { Protocol.Http, "80" },
                    { Protocol.Https, "443" },
                    { Protocol.SmtpOne, "25" },
                    { Protocol.SmtpTwo, "587" },
                    { Protocol.Syslog, "6514" },
                    { Protocol.Ephemeral, "1024-65535" },
                    { Protocol.Rdp, "3389" },
                    { Protocol.WinRm, "5985-5986" },
                    { Protocol.MsSql, "1433" },
                    { Protocol.Mongo, "27017" },
                    { Protocol.Ssh, "22" },
                    { Protocol.OpenVpnManage, "943" },
                    { Protocol.OpenVpnTunnel, "1194" },
                    { Protocol.SmbOne, "445" },
                    { Protocol.SmbTwo, "137-139" },
                };
    }

    /// <summary>
    /// Common protocols.
    /// </summary>
    public enum Protocol
    {
        /// <summary>
        /// Open VPN management.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Vpn", Justification = "Spelling/name is correct.")]
        OpenVpnManage,

        /// <summary>
        /// Open VPN tunnel.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Vpn", Justification = "Spelling/name is correct.")]
        OpenVpnTunnel,

        /// <summary>
        /// Return traffic.
        /// </summary>
        Ephemeral,

        /// <summary>
        /// SES email protocol one.
        /// </summary>
        SmtpOne,

        /// <summary>
        /// SES email protocol two.
        /// </summary>
        SmtpTwo,

        /// <summary>
        /// Syslog protocol.
        /// </summary>
        Syslog,

        /// <summary>
        /// Time protocol.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ntp", Justification = "Spelling/name is correct.")]
        Ntp,

        /// <summary>
        /// Name lookup protocol.
        /// </summary>
        Dns,

        /// <summary>
        /// File sharing protocol one.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Smb", Justification = "Spelling/name is correct.")]
        SmbOne,

        /// <summary>
        /// File sharing protocol two.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Smb", Justification = "Spelling/name is correct.")]
        SmbTwo,

        /// <summary>
        /// SSH protocol.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ssh", Justification = "Spelling/name is correct.")]
        Ssh,

        /// <summary>
        /// Web traffic.
        /// </summary>
        Http,

        /// <summary>
        /// Secure web traffic.
        /// </summary>
        Https,

        /// <summary>
        /// Remote desktop protocol.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Rdp", Justification = "Spelling/name is correct.")]
        Rdp,

        /// <summary>
        /// MS SQL Server.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ms", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ms", Justification = "Spelling/name is correct.")]
        MsSql,

        /// <summary>
        /// Mongo database.
        /// </summary>
        Mongo,

        /// <summary>
        /// Remote command protocol.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Rm", Justification = "Spelling/name is correct.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Rm", Justification = "Spelling/name is correct.")]
        WinRm,

        /// <summary>
        /// All ports.
        /// </summary>
        All,
    }
}
