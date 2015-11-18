using CLR_DEV9.PacketReader;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace CLR_DEV9.Sessions
{

    class UDP_DHCPsession : Session
    {
        public static byte[] DHCP_IP = { 192, 0, 2, 1 };
        public static byte[] GATEWAY_IP = DHCP_IP;
        public static byte[] PS2_IP = { 192, 0, 2, 100 };
        public static byte[] NETMASK = { 255, 255, 255, 0 };
        public static byte[] BROADCAST = { 192, 0, 2, 255 };

        List<UDP> recvbuff = new List<UDP>();
        byte HType;
        byte Hlen;
        UInt32 xID = 0;
        byte[] cMac;
        UInt32 cookie = 0;

        UInt16 maMs = 576;
        public override IPPayload recv()
        {
            if (recvbuff.Count == 0)
                return null;
            UDP ret = recvbuff[0];
            recvbuff.RemoveAt(0);
            return ret;
        }
        public override bool send(IPPayload payload)
        {
            #region "Get Network Info"
            //Get comp IP
            IPAddress IPaddress = null;

            //IPAddress NetMask = null;
            //IPAddress GatewayIP = null;
            List<IPAddress> DNS_IP = new List<IPAddress>();
            //IPAddress BroadCastIP = null;
            NetworkInterface[] Interfaces = NetworkInterface.GetAllNetworkInterfaces();

            bool FoundAdapter = false;

            foreach (NetworkInterface adapter in Interfaces)
            {
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }
                if (adapter.OperationalStatus == OperationalStatus.Up)
                {
                    UnicastIPAddressInformationCollection IPInfo = adapter.GetIPProperties().UnicastAddresses;
                    IPInterfaceProperties properties = adapter.GetIPProperties();
                    //GatewayIPAddressInformation myGateways = properties.GatewayAddresses.FirstOrDefault();
                    //if (myGateways.Address.ToString().Equals("0.0.0.0"))
                    //{
                    //    continue;
                    //}
                    foreach (UnicastIPAddressInformation IPAddressInfo in IPInfo)
                    {
                        if (//IPAddressInfo.DuplicateAddressDetectionState == DuplicateAddressDetectionState.Preferred &
                            //IPAddressInfo.AddressPreferredLifetime != UInt32.MaxValue &
                            IPAddressInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            Console.Error.WriteLine("Matched Adapter");
                            IPaddress = IPAddressInfo.Address; ;
                            FoundAdapter = true;
                            break;
                        }
                    }
                    //foreach (GatewayIPAddressInformation Gateway in myGateways) //allow more than one gateway?
                    //{
                    //    if (FoundAdapter == true)
                    //    {
                    //        GatewayIP = Gateway.Address;
                    //        GATEWAY_IP = GatewayIP.GetAddressBytes();
                    //        break;
                    //    }
                    //}
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
                    if (DNS_IP.Count == 0)
                    {
                        //adapter not suitable
                        FoundAdapter = false;
                    }
                }
                if (FoundAdapter == true)
                {
                    Console.Error.WriteLine(adapter.Name);
                    Console.Error.WriteLine(adapter.Description);
                    Console.Error.WriteLine("IP Address :" + IPaddress.ToString());
                    Console.Error.WriteLine("Domain Name :" + Dns.GetHostName());
                    //Console.Error.WriteLine("Subnet Mask :" + NetMask.ToString());
                    //Console.Error.WriteLine("Gateway IP :" + GatewayIP.ToString());
                    Console.Error.WriteLine("DNS 1 : " + DNS_IP[0].ToString());
                    break;
                }
            }
            #endregion

            DHCP dhcp = new DHCP(payload.GetPayload());
            HType = dhcp.HardwareType;
            Hlen = dhcp.HardwareAddressLength;
            xID = dhcp.TransactionID;
            cMac = dhcp.ClientHardwareAddress;
            cookie = dhcp.MagicCookie;

            TCPOption clientID = null;

            byte msg = 0;
            byte[] reqList = null;

            for (int i = 0; i < dhcp.Options.Count; i++)
            {
                switch (dhcp.Options[i].Code)
                {
                    case 0:
                        //Console.Error.WriteLine("Got NOP");
                        continue;
                    case 50:
                        Console.Error.WriteLine("Got Request IP");
                        if (Utils.memcmp(PS2_IP, 0, ((DHCPopREQIP)dhcp.Options[i]).IPaddress, 0, 4) == false)
                            throw new Exception("ReqIP missmatch");
                        break;
                    case 53:
                        msg = ((DHCPopMSG)(dhcp.Options[i])).Message;
                        Console.Error.WriteLine("Got MSG ID = " + msg);
                        break;
                    case 54:
                        Console.Error.WriteLine("Got Server IP");
                        if (Utils.memcmp(DHCP_IP, 0, ((DHCPopSERVIP)dhcp.Options[i]).IPaddress, 0, 4) == false)
                            throw new Exception("ServIP missmatch");
                        break;
                    case 55:
                        reqList = ((DHCPopREQLIST)(dhcp.Options[i])).RequestList;
                        Console.Error.WriteLine("Got Request List of length " + reqList.Length);
                        for (int rID = 0; rID < reqList.Length; rID++)
                        {
                            Console.Error.WriteLine("Requested : " + reqList[rID]);
                        }
                        break;
                    case 56:
                        Console.Error.WriteLine("Got String Message");
                        break;
                    case 57:
                        maMs = ((DHCPopMMSGS)(dhcp.Options[i])).MaxMessageSize;
                        Console.Error.WriteLine("Got Max Message Size of " + maMs);
                        break;
                    case 61:
                        Console.Error.WriteLine("Got Client ID");
                        clientID = dhcp.Options[i];
                        //Ignore
                        break;
                    case 255:
                        Console.Error.WriteLine("Got END");
                        break;
                    default:
                        Console.Error.WriteLine("Got Unknown Option " + dhcp.Options[i].Code);
                        throw new Exception();
                    //break;
                }
            }
            DHCP retPay = new DHCP();
            retPay.OP = 2;
            retPay.HardwareType = HType;
            retPay.HardwareAddressLength = Hlen;
            retPay.TransactionID = xID;

            retPay.YourIP = PS2_IP;//IPaddress.GetAddressBytes();
            retPay.ServerIP = DHCP_IP;

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
                                Console.Error.WriteLine("Sending Subnet");
                                //retPay.Options.Add(new DHCPopSubnet(NetMask.GetAddressBytes()));
                                retPay.Options.Add(new DHCPopSubnet(NETMASK));
                                break;
                            case 3:
                                Console.Error.WriteLine("Sending Router");
                                retPay.Options.Add(new DHCPopRouter(GATEWAY_IP));
                                break;
                            case 6:
                                Console.Error.WriteLine("Sending DNS"); //TODO support more than 1
                                //
                                retPay.Options.Add(new DHCPopDNS(DNS_IP[0]));
                                //retPay.Options.Add(new DHCPopDNS(IPAddress.Parse("1.1.1.1")));
                                break;
                            case 15:
                                Console.Error.WriteLine("Sending Domain Name");
                                //retPay.Options.Add(new DHCPopDNSNAME(Dns.GetHostName()));
                                retPay.Options.Add(new DHCPopDNSNAME("PCSX2-CLRDEV9"));
                                break;
                            case 28:
                                Console.Error.WriteLine("Sending Broadcast Addr");
                                for (int i2 = 0; i2 < 4; i2++)
                                {
                                    BROADCAST[i2] = (byte)((PS2_IP[i2]) | (~NETMASK[i2]));
                                }
                                retPay.Options.Add(new DHCPopBCIP(BROADCAST));
                                break;
                            default:
                                Console.Error.WriteLine("Got Unknown Option " + reqList[i]);
                                throw new Exception();

                        }
                    }
                    retPay.Options.Add(new DHCPopIPLT(86400));
                }
            }

            if (msg == 7)
            {
                Console.Error.WriteLine("PS2 has Disconnected");
                return true;
            }

            retPay.Options.Add(new DHCPopSERVIP(DHCP_IP));
            retPay.Options.Add(new DHCPopEND());

            byte[] udpPayload = retPay.GetBytes((UInt16)(maMs - (8 + 20)));
            UDP retudp = new UDP(udpPayload);
            retudp.SourcePort = 67;
            retudp.DestinationPort = 68;
            recvbuff.Add(retudp);
            return true;
        }

        public override bool isOpen() { return true; }
        public override void Dispose() { }
    }
}
