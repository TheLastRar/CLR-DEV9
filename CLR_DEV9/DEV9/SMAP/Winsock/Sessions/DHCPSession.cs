using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.DHCP;
using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace CLRDEV9.DEV9.SMAP.Winsock.Sessions
{
    class DefaultDHCPConfig
    {
        public static byte[] DHCP_IP = { 192, 0, 2, 1 };
        public static byte[] GATEWAY_IP = DHCP_IP;
        public static byte[] PS2_IP = { 192, 0, 2, 100 };
        public static byte[] NETMASK = { 255, 255, 255, 0 };
    }

    class UDP_DHCPSession : Session
    {
#region CurrentConfig
        public byte[] PS2IP;
        private byte[] NetMask;
        public byte[] Gateway;

        private byte[] DNS1;
        private byte[] DNS2;
        public byte[] Broadcast;
        //Treat Limited Broadcast as directed broadcast
        public byte[] LimitedBroadcast = new byte[] { 255, 255, 255, 255 };
#endregion

        ConcurrentQueue<UDP> recvBuff = new ConcurrentQueue<UDP>();
        byte hType;
        byte hLen;
        UInt32 xID = 0;
        byte[] cMac;
        UInt32 cookie = 0;

        UInt16 maxMs = 576;

        public UDP_DHCPSession(ConnectionKey parKey, NetworkInterface parAdapter, byte[] parDNS1, byte[] parDNS2, bool parLANMode)
            : base(parKey, IPAddress.Any)
        {
            //Socket

            ////Load DNS from Adapter
            //if (parAdapter == null)
            //{
            //    parAdapter = AutoAdapter();
            //}
            //Set IP, NetMask and Gateway based on if LANMode is set
            if (parLANMode)
            {
                if (parAdapter != null)
                {
                    //Give PS2 same IP as host to allow LAN games to work
                    IPInterfaceProperties properties = parAdapter.GetIPProperties();
                    UnicastIPAddressInformationCollection IPInfoCollection = properties.UnicastAddresses;

                    foreach (UnicastIPAddressInformation IPInfo in IPInfoCollection)
                    {
                        if (IPInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            PS2IP = IPInfo.Address.GetAddressBytes();
                            break;
                        }
                    }
                }
                HandleNetMask(parAdapter, null);
                HandleGateway(parAdapter, null);
            }
            else
            {
                //Give PS2 different IP from host to allow host-PS2 comunications
                PS2IP = DefaultDHCPConfig.PS2_IP;
                HandleNetMask(parAdapter, DefaultDHCPConfig.NETMASK);
                HandleGateway(parAdapter, DefaultDHCPConfig.GATEWAY_IP);
            }
            //Set DNS
            HandleDNS(parAdapter, parDNS1, parDNS2);
            //Broadcast Address
            HandleBroadcast(PS2IP, NetMask);
        }

        public UDP_DHCPSession(ConnectionKey parKey, NetworkInterface parAdapter, byte[] parIP, byte[] parNetmask, byte[] parGateway,
            byte[] parDNS1, byte[] parDNS2)
            : base(parKey, IPAddress.Any)
        {
            //DirectAdapter
            PS2IP = parIP;

            HandleNetMask(parAdapter, parNetmask);
            HandleGateway(parAdapter, parGateway);
            HandleDNS(parAdapter, parDNS1, parDNS2);
            HandleBroadcast(PS2IP, NetMask);
#region ICS
            //Special case for ICS
            if (Gateway == null & PSE.CLR_PSE_Utils.IsWindows())
            {
                //Retrieve ICS IP from Regs
                byte[] icsIP;
                try
                {
                    using (Microsoft.Win32.RegistryKey localKey = Microsoft.Win32.RegistryKey.OpenBaseKey(
                                                                    Microsoft.Win32.RegistryHive.LocalMachine,
                                                                    Microsoft.Win32.RegistryView.Registry64))
                    {
                        using (Microsoft.Win32.RegistryKey icsKey = localKey.OpenSubKey("System\\CurrentControlSet\\Services\\SharedAccess\\Parameters"))
                        {
                            icsIP = IPAddress.Parse((string)icsKey.GetValue("ScopeAddress")).GetAddressBytes();
                        }
                    }
                }
                catch
                {
                    icsIP = new byte[] { 192, 168, 137, 1 };
                }
                //Check if adapter has ICS IP
                //If so, then adapter is being shared to via ICS
                IPInterfaceProperties properties = parAdapter.GetIPProperties();
                UnicastIPAddressInformationCollection IPInfoCollection = properties.UnicastAddresses;

                foreach (UnicastIPAddressInformation IPInfo in IPInfoCollection)
                {
                    if (IPInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        byte[] addrB = IPInfo.Address.GetAddressBytes();
                        if (Utils.memcmp(addrB, 0, icsIP, 0, 4))
                        {
                            Gateway = icsIP;
                            if (parDNS1 == null)
                            {
                                DNS1 = icsIP;
                            }
                        }
                    }
                }
            }
#endregion
        }

        private void HandleNetMask(NetworkInterface parAdapter, byte[] parNetMask)
        {
            NetMask = parNetMask;

            if (NetMask == null &
                parAdapter != null)
            {
                IPInterfaceProperties properties = parAdapter.GetIPProperties();
                UnicastIPAddressInformationCollection IPInfoCollection = properties.UnicastAddresses;

                foreach (UnicastIPAddressInformation IPInfo in IPInfoCollection)
                {
                    if (IPInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        NetMask = IPInfo.IPv4Mask.GetAddressBytes();
                        break;
                    }
                }
            }
        }

        private void HandleGateway(NetworkInterface parAdapter, byte[] parGateway)
        {
            Gateway = parGateway;

            if (Gateway == null &
                parAdapter != null &
                NetMask != null)
            {
                IPInterfaceProperties properties = parAdapter.GetIPProperties();
                GatewayIPAddressInformationCollection GatewayInfoCollection = properties.GatewayAddresses;

                foreach (GatewayIPAddressInformation GatewayInfo in GatewayInfoCollection)
                {
                    if (GatewayInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        Gateway = GatewayInfo.Address.GetAddressBytes();
                        break;
                    }
                }
            }
        }

        private void HandleDNS(NetworkInterface parAdapter, byte[] parDNS1, byte[] parDNS2)
        {
            List<IPAddress> DNS_IP = new List<IPAddress>();
            if (parAdapter != null)
            {
                IPInterfaceProperties properties = parAdapter.GetIPProperties();
                foreach (IPAddress DNSaddress in properties.DnsAddresses) //allow more than one DNS address?
                {
                    if (!(DNSaddress.AddressFamily == AddressFamily.InterNetworkV6))
                    {
                        DNS_IP.Add(DNSaddress);
                    }
                }
            }
            DNS1 = parDNS1;
            DNS2 = parDNS2;

            if (DNS1 == null)
            {
                //DNS1 is null
                //If Adapter has DNS, add 1st entry
                if (DNS_IP.Count >= 1)
                {
                    DNS1 = DNS_IP[0].GetAddressBytes();
                }
                //If adapter dosn't have 1st entry
                //And we have been given a value for DNS2
                //(but not 1) then set DNS1 to 0.0.0.0
                else if (DNS2 != null)
                {
                    DNS1 = new byte[] { 0, 0, 0, 0 };
                }

                //Is both DNS1 and 2 null?
                if (DNS2 == null)
                {
                    //If so, add adapter's second DNS record
                    //if present.
                    if (DNS_IP.Count >= 2)
                    {
                        DNS2 = DNS_IP[1].GetAddressBytes();
                    }
                }
            }
            else
            {
                //DNS1 is not null
                if (DNS2 == null)
                {
                    //DNS2 is null
                    //If Adapter has DNS, add entry to DNS2.
                    //if adapter has 2+ entries, we add the
                    //second entry to DNS2.
                    //If not, add the 1st entry
                    if (DNS_IP.Count >= 2)
                    {
                        DNS2 = DNS_IP[1].GetAddressBytes();
                    }
                    else if (DNS_IP.Count >= 1)
                    {
                        DNS2 = DNS_IP[0].GetAddressBytes();
                    }
                }
            }
        }

        private void HandleBroadcast(byte[] parPS2IP, byte[] parNetMask)
        {
            if (parNetMask != null)
            {
                Broadcast = new byte[4];
                for (int i2 = 0; i2 < 4; i2++)
                {
                    Broadcast[i2] = (byte)((parPS2IP[i2]) | (~parNetMask[i2]));
                }
            }
        }

        public static NetworkInterface AutoAdapter()
        {
            IPAddress IPaddress = null;
            List<IPAddress> DNS_IP = new List<IPAddress>();
            NetworkInterface[] Interfaces = NetworkInterface.GetAllNetworkInterfaces();

            bool hasGateway = false;
            bool FoundAdapter = false;

            foreach (NetworkInterface adapter in Interfaces)
            {
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }
                if (adapter.OperationalStatus == OperationalStatus.Up)
                {
                    UnicastIPAddressInformationCollection IPInfoCollection = adapter.GetIPProperties().UnicastAddresses;
                    IPInterfaceProperties properties = adapter.GetIPProperties();

                    foreach (UnicastIPAddressInformation IPAddressInfo in IPInfoCollection)
                    {
                        if (//IPAddressInfo.DuplicateAddressDetectionState == DuplicateAddressDetectionState.Preferred &
                            //IPAddressInfo.AddressPreferredLifetime != UInt32.MaxValue &
                            IPAddressInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            Log_Info("Matched Adapter");
                            IPaddress = IPAddressInfo.Address;
                            FoundAdapter = true;
                            break;
                        }
                    }
                    foreach (IPAddress DNSaddress in properties.DnsAddresses) //allow more than one DNS address?
                    {
                        if (FoundAdapter == true)
                        {
                            if (!(DNSaddress.AddressFamily == AddressFamily.InterNetworkV6))
                            {
                                DNS_IP.Add(DNSaddress);
                            }
                        }
                    }

                    GatewayIPAddressInformationCollection GatewayInfoCollection = properties.GatewayAddresses;

                    foreach (GatewayIPAddressInformation GatewayInfo in GatewayInfoCollection)
                    {
                        if (GatewayInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            hasGateway = true;
                            break;
                        }
                    }

                    if (DNS_IP.Count == 0 | hasGateway == false)
                    {
                        //adapter not suitable
                        hasGateway = false;
                        DNS_IP.Clear();
                        FoundAdapter = false;
                    }
                }
                if (FoundAdapter == true)
                {
                    Log_Info(adapter.Name);
                    Log_Info(adapter.Description);
                    Log_Verb("IP Address :" + IPaddress.ToString());
                    Log_Verb("Domain Name :" + Dns.GetHostName());
                    //Error.WriteLine("Subnet Mask :" + NetMask.ToString());
                    //Error.WriteLine("Gateway IP :" + GatewayIP.ToString());
                    Log_Verb("DNS 1 : " + DNS_IP[0].ToString());
                    return adapter;
                }
            }
            return null;
        }

        public override IPPayload Recv()
        {
            if (recvBuff.TryDequeue(out UDP ret))
                return ret;
            return null;
        }
        public override bool Send(IPPayload payload)
        {
            DHCP dhcp = new DHCP(payload.GetPayload());
            hType = dhcp.HardwareType;
            hLen = dhcp.HardwareAddressLength;
            xID = dhcp.TransactionID;
            cMac = dhcp.ClientHardwareAddress;
            cookie = dhcp.MagicCookie;

            DHCPopClientID clientID = null;

            byte msg = 0;
            byte[] reqList = null;

            uint leaseTime = 86400;

            for (int i = 0; i < dhcp.Options.Count; i++)
            {
                switch (dhcp.Options[i].Code)
                {
                    case 0:
                        //Error.WriteLine("Got NOP");
                        continue;
                    case 1:
                        Log_Info("Got SubnetMask?");
                        if (Utils.memcmp(NetMask, 0, ((DHCPopSubnet)dhcp.Options[i]).SubnetMask, 0, 4) == false)
                            throw new Exception("SubnetMask missmatch");
                        break;
                    case 3:
                        Log_Info("Got Router?");
                        if (((DHCPopRouter)dhcp.Options[i]).RouterIPs.Count != 1)
                        {
                            throw new Exception("RouterIPs count missmatch");
                        }
                        if (Utils.memcmp(Gateway, 0, ((DHCPopRouter)dhcp.Options[i]).RouterIPs[0], 0, 4) == false)
                            throw new Exception("RouterIPs missmatch");
                        break;
                    case 6:
                        Log_Info("Got DNS?");
                        if ((((DHCPopDNS)dhcp.Options[i]).DNSServers.Count != 0 & DNS1 == null) ||
                            (((DHCPopDNS)dhcp.Options[i]).DNSServers.Count != 1 & DNS2 == null) ||
                            (((DHCPopDNS)dhcp.Options[i]).DNSServers.Count != 2 & DNS2 != null))
                        {
                            throw new Exception("DNS count missmatch");
                        }
                        if ((DNS1 != null && Utils.memcmp(DNS1, 0, ((DHCPopDNS)dhcp.Options[i]).DNSServers[0], 0, 4) == false) ||
                            (DNS2 != null && Utils.memcmp(DNS2, 0, ((DHCPopDNS)dhcp.Options[i]).DNSServers[1], 0, 4) == false))
                            throw new Exception("DNS missmatch");
                        break;
                    case 12:
                        Log_Info("Got HostName");
                        //TODO use name?
                        break;
                    case 50:
                        Log_Info("Got Request IP");
                        if (Utils.memcmp(PS2IP, 0, ((DHCPopREQIP)dhcp.Options[i]).IPaddress, 0, 4) == false)
                            throw new Exception("ReqIP missmatch");
                        break;
                    case 51:
                        Log_Info("Got Requested Lease Time");
                        leaseTime = ((DHCPopIPLT)(dhcp.Options[i])).IPLeaseTime;
                        break;
                    case 53:
                        msg = ((DHCPopMSG)(dhcp.Options[i])).Message;
                        Log_Info("Got MSG ID = " + msg);
                        break;
                    case 54:
                        Log_Info("Got Server IP");
                        if (Utils.memcmp(DefaultDHCPConfig.DHCP_IP, 0, ((DHCPopSERVIP)dhcp.Options[i]).ServerIP, 0, 4) == false)
                            throw new Exception("ServIP missmatch");
                        break;
                    case 55:
                        reqList = ((DHCPopREQLIST)(dhcp.Options[i])).RequestList;
                        Log_Verb("Got Request List of length " + reqList.Length);
                        for (int rID = 0; rID < reqList.Length; rID++)
                        {
                            Log_Verb("Requested : " + reqList[rID]);
                        }
                        break;
                    case 56:
                        Log_Verb("Got String Message of " + ((DHCPopMSGStr)dhcp.Options[i]).Message);
                        break;
                    case 57:
                        maxMs = ((DHCPopMMSGS)(dhcp.Options[i])).MaxMessageSize;
                        Log_Verb("Got Max Message Size of " + maxMs);
                        break;
                    case 60:
                        Log_Verb("Got Class Id of " + ((DHCPopClassID)dhcp.Options[i]).ClassID);
                        break;
                    case 61:
                        Log_Verb("Got Client ID");
                        clientID = (DHCPopClientID)dhcp.Options[i];
                        //Ignore
                        break;
                    case 255:
                        Log_Verb("Got END");
                        break;
                    default:
                        Log_Error("Got Unknown Option " + dhcp.Options[i].Code);
                        throw new Exception("Got Unknown Option " + dhcp.Options[i].Code);
                        //break;
                }
            }
            DHCP retPay = new DHCP();
            retPay.OP = 2;
            retPay.HardwareType = hType;
            retPay.HardwareAddressLength = hLen;
            retPay.TransactionID = xID;

            retPay.YourIP = PS2IP;//IPaddress.GetAddressBytes();
            retPay.ServerIP = DefaultDHCPConfig.DHCP_IP;

            retPay.ClientHardwareAddress = cMac;
            retPay.MagicCookie = cookie;

            if (msg == 1 || msg == 3) //Fill out Requests
            {
                if (msg == 1)
                {
                    retPay.Options.Add(new DHCPopMSG(2));
                }
                if (msg == 3)
                {
                    retPay.Options.Add(new DHCPopMSG(5));
                }

                if (reqList != null)
                {
                    for (int i = 0; i < reqList.Length; i++)
                    {
                        switch (reqList[i])
                        {
                            case 1:
                                Log_Verb("Sending Subnet");
                                //retPay.Options.Add(new DHCPopSubnet(NetMask.GetAddressBytes()));
                                retPay.Options.Add(new DHCPopSubnet(NetMask));
                                break;
                            case 3:
                                Log_Verb("Sending Router");
                                if (Gateway != null)
                                {
                                    retPay.Options.Add(new DHCPopRouter(new List<byte[]>() { Gateway }));
                                }
                                break;
                            case 6:
                                Log_Verb("Sending DNS");
                                if (DNS1 != null)
                                {
                                    if (DNS2 != null)
                                    {
                                        retPay.Options.Add(new DHCPopDNS(new List<byte[]>() { DNS1, DNS2 }));
                                    }
                                    else
                                    {
                                        retPay.Options.Add(new DHCPopDNS(new List<byte[]>() { DNS1 }));
                                    }
                                }
                                break;
                            case 15:
                                Log_Verb("Sending Domain Name");
                                //retPay.Options.Add(new DHCPopDNSNAME(Dns.GetHostName()));
                                retPay.Options.Add(new DHCPopDNSNAME("PCSX2-CLRDEV9"));
                                break;
                            case 28:
                                Log_Verb("Sending Broadcast Addr");
                                retPay.Options.Add(new DHCPopBCIP(Broadcast));
                                break;
                            case 50:
                                Log_Verb("Sending PS2 IP Addr");
                                retPay.Options.Add(new DHCPopREQIP(PS2IP));
                                break;
                            case 53:
                                Log_Verb("Sending MSG (Already Added)");
                                break;
                            case 54:
                                Log_Verb("Sending Server Identifier (Already Added)");
                                break;
                            case 77:
                                //Isn't this surpossed to be sent by the client?
                                Log_Error("Request for User-Class, Ignoring");
                                break;
                            default:
                                Log_Error("Got Unknown Req " + reqList[i]);
                                throw new Exception("Got Unknown Req " + reqList[i]);
                        }
                    }
                    retPay.Options.Add(new DHCPopIPLT(leaseTime));
                }
            }

            if (msg == 7)
            {
                Log_Info("PS2 has Disconnected");
                return true;
            }

            retPay.Options.Add(new DHCPopSERVIP(DefaultDHCPConfig.DHCP_IP));
            retPay.Options.Add(new DHCPopEND());

            byte[] udpPayload = retPay.GetBytes((UInt16)(maxMs - (8 + 20)));
            UDP retudp = new UDP(udpPayload);
            retudp.SourcePort = 67;
            retudp.DestinationPort = 68;
            recvBuff.Enqueue(retudp);
            return true;
        }
        public override void Reset()
        {
            //throw new NotImplementedException();
        }

        //public override bool isOpen() { return true; }
        public override void Dispose() { }

        private static void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.DHCPSession, str);
        }
        private static void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.DHCPSession, str);
        }
        private static void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.DHCPSession, str);
        }
    }
}
