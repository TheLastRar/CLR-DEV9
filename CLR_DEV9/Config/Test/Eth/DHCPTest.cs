//using CLRDEV9.DEV9.SMAP.Data;
//using CLRDEV9.DEV9.SMAP.Winsock.PacketReader;
//using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.DHCP;
//using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
//using System;
//using System.Diagnostics;
//using System.Net;
//using System.Windows.Forms;

//namespace CLRDEV9.Config.Test.Eth
//{
//    class DHCPTest
//    {
//        ushort mms = 576 - (8 + 20);


//        public byte[] PS2IP = new byte[] { 0, 0, 0, 0 };
//        public byte[] GatewayIP;
//        public byte[] Mask;
//        public byte[] DNS1;
//        public byte[] DNS2;

//        public byte[] BroadcastIP = new byte[] { 255, 255, 255, 255 };

//        byte[] dhcpIP;
//        byte[] dhcpIPOption;

//        public bool Connect(SMAP_Test smap, Func<SMAP_Test, int, EtherFrameType, byte, uint, EthernetFrame> GetFrameOfType)
//        {
//            uint xID = (uint)(new Random()).Next();
//            //DHCP session cannot act as client, do it ourself.
//            //3.1 Create and Send DHCP packet
//            #region "S3.1"
//            Log_Info("Sending DHCP Discover");
//            DHCP dhcp = new DHCP();
//            dhcp.OP = 1;
//            dhcp.HardwareType = 1;
//            dhcp.HardwareAddressLength = 6;
//            dhcp.TransactionID = xID;
//            //This should be updated when we resend
//            //but that would need recreating the NetPacket
//            dhcp.Seconds = 0;
//            dhcp.Flags = 0x8000;
//            byte[] chaddr = new byte[16];
//            Utils.memcpy(chaddr, 0, smap.GetHWAddress(), 0, 6);
//            dhcp.ClientHardwareAddress = chaddr;
//            dhcp.Options.Add(new DHCPopMSG(1));
//            dhcp.Options.Add(new DHCPopREQLIST(new byte[]
//                { 1, 3, 15, 6, 28 }));
//            dhcp.Options.Add(new DHCPopEND());

//            UDP uP = new UDP(dhcp.GetBytes((UInt16)mms));
//            uP.SourcePort = 68;
//            uP.DestinationPort = 67;

//            IPPacket ipp = new IPPacket(uP);
//            ipp.SourceIP = new byte[] { 0, 0, 0, 0 };
//            ipp.DestinationIP = new byte[] { 255, 255, 255, 255 };

//            EthernetFrame ef = new EthernetFrame(ipp);
//            ef.SourceMAC = smap.GetHWAddress();
//            ef.DestinationMAC = new byte[] { 255, 255, 255, 255, 255, 255 };
//            ef.Protocol = (UInt16)EtherFrameType.IPv4;

//            NetPacket pkt = ef.CreatePacket();

//            int tries = 0;
//            DHCP dhcpret = null;
//            while (tries < 3)
//            {
//                tries++;
//                smap.TxProcess(ref pkt);
//                EthernetFrame retef = GetFrameOfType(smap, 10, EtherFrameType.IPv4, (byte)IPType.UDP, 68);
//                while (retef != null)
//                {
//                    //Must match TransactionID
//                    dhcpret = new DHCP(((UDP)((IPPacket)retef.Payload).Payload).GetPayload());
//                    if (dhcpret.TransactionID == xID &
//                        dhcpret.OP == 2 &
//                        Utils.memcmp(dhcp.ClientHardwareAddress, 0, smap.GetHWAddress(), 0, 6))
//                    {
//                        break;
//                    }
//                    dhcpret = null;
//                    retef = GetFrameOfType(smap, 1, EtherFrameType.IPv4, (byte)IPType.UDP, 68);
//                }
//                if (dhcpret != null) { break; }
//            }

//            if (dhcpret == null)
//            {
//                MessageBox.Show("DHCP Failed (No Offer)");
//                Log_Error("DHCP Failed (No offer)");
//                return false;
//            }
//            #endregion
//            //3.2 Read Data
//            Log_Info("Got DHCP Offer");
//            #region "S3.2"
//            byte[] tmpps2IP = dhcpret.YourIP;
//            dhcpIP = dhcpret.ServerIP;
//            dhcpIPOption = dhcpIP;
//            //gwIP = dhcpret.GatewayIP;

//            foreach (TCPOption dop in dhcpret.Options)
//            {
//                switch (dop.Code)
//                {
//                    case 1:
//                        Mask = ((DHCPopSubnet)dop).SubnetMask;
//                        break;
//                    case 3:
//                        GatewayIP = ((DHCPopRouter)dop).RouterIPs[0];
//                        break;
//                    case 6:
//                        DHCPopDNS dns = ((DHCPopDNS)dop);
//                        DNS1 = dns.DNSServers[0];
//                        if (dns.DNSServers.Count > 1)
//                        {
//                            DNS2 = dns.DNSServers[1];
//                        }
//                        break;
//                    case 28:
//                        BroadcastIP = ((DHCPopBCIP)dop).BroadcastIP;
//                        break;
//                    case 51:
//                        //Lease time
//                        break;
//                    case 53:
//                        if (((DHCPopMSG)dop).Message != 2)
//                        {
//                            MessageBox.Show("DHCP Failed (Not An Offer)");
//                            Log_Error("DHCP Failed, Unexpeted packet");
//                            return false;
//                        }
//                        break;
//                    case 54:
//                        dhcpIPOption = ((DHCPopSERVIP)dop).ServerIP;
//                        break;
//                }
//            }
//            #endregion
//            //3.3 Accept Offer
//            #region "S3.3"
//            Log_Info("Sending DHCP Request");
//            dhcp.ServerIP = dhcpIP;

//            dhcp.Options.Clear();

//            dhcp.Options.Add(new DHCPopMSG(3));
//            dhcp.Options.Add(new DHCPopREQIP(tmpps2IP));
//            dhcp.Options.Add(new DHCPopSERVIP(dhcpIPOption));
//            dhcp.Options.Add(new DHCPopREQLIST(new byte[]
//                { 1, 3, 6, 15, 28 }));
//            dhcp.Options.Add(new DHCPopEND());

//            uP = new UDP(dhcp.GetBytes(mms));
//            uP.SourcePort = 68;
//            uP.DestinationPort = 67;

//            ipp = new IPPacket(uP);
//            ipp.SourceIP = new byte[] { 0, 0, 0, 0 };
//            ipp.DestinationIP = new byte[] { 255, 255, 255, 255 };

//            ef = new EthernetFrame(ipp);
//            ef.SourceMAC = smap.GetHWAddress();
//            ef.DestinationMAC = new byte[] { 255, 255, 255, 255, 255, 255 };
//            ef.Protocol = (UInt16)EtherFrameType.IPv4;

//            pkt = ef.CreatePacket();

//            tries = 0;
//            dhcpret = null;
//            while (tries < 3)
//            {
//                tries++;
//                smap.TxProcess(ref pkt);
//                EthernetFrame retef = GetFrameOfType(smap, 10, EtherFrameType.IPv4, (byte)IPType.UDP, 68);
//                while (retef != null)
//                {
//                    //Must match TransactionID
//                    dhcpret = new DHCP(((UDP)((IPPacket)retef.Payload).Payload).GetPayload());
//                    if (dhcpret.TransactionID == xID &
//                        dhcpret.OP == 2 &
//                        Utils.memcmp(dhcp.ClientHardwareAddress, 0, smap.GetHWAddress(), 0, 6))
//                    {
//                        break;
//                    }
//                    dhcpret = null;
//                    retef = GetFrameOfType(smap, 1, EtherFrameType.IPv4, (byte)IPType.UDP, 68);
//                }
//                if (dhcpret != null) { break; }
//            }

//            if (dhcpret == null)
//            {
//                MessageBox.Show("DHCP Failed (No ACK)");
//                Log_Error("DHCP Failed (No ACK)");
//                return false;
//            }
//            #endregion
//            //3.4 Inspect ACK
//            #region "S3.4"
//            foreach (TCPOption dop in dhcpret.Options)
//            {
//                switch (dop.Code)
//                {
//                    case 53:
//                        if (((DHCPopMSG)dop).Message != 5)
//                        {
//                            MessageBox.Show("DHCP Failed (Got NAK)");
//                            Log_Error("DHCP Failed (Got NAK)");
//                            return false;
//                        }
//                        break;
//                }
//            }
//            PS2IP = tmpps2IP;
//            Log_Info("DHCP OK");
//            Log_Info("IP: " + (new IPAddress(PS2IP)).ToString());
//            Log_Info("GatewayIP: " + (new IPAddress(GatewayIP)).ToString());
//            Log_Info("Mask: " + (new IPAddress(Mask)).ToString());
//            Log_Info("DNS1: " + (new IPAddress(DNS1)).ToString());
//            if (DNS2 != null)
//            {
//                Log_Info("DNS2: " + (new IPAddress(DNS2)).ToString());
//            }
//            Log_Info("BroadcastIP: " + (new IPAddress(BroadcastIP)).ToString());
//            #endregion
//            return true;
//        }

//        private void Log_Error(string str)
//        {
//            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.Test, str);
//        }
//        private void Log_Info(string str)
//        {
//            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.Test, str);
//        }
//        private void Log_Verb(string str)
//        {
//            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.Test, str);
//        }
//    }
//}
