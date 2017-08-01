//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Windows.Forms;
//using CLRDEV9.DEV9.SMAP.Winsock.PacketReader;
//using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.DHCP;
//using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
//using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.DNS;
//using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.ARP;
//using CLRDEV9.DEV9.SMAP.Data;
//using System.Diagnostics;
//using System.Net;

//namespace CLRDEV9.Config.Test.Eth
//{
//    class EthTester
//    {
//        DHCPTest dhcp = new DHCPTest();
//        ARPTest arp = new ARPTest();

//        public void Test()
//        {
//            //STEP 1, CHECK TESTER CAN RUN WITH SETTINGS
//            #region "S1"
//            bool ok = false;
//            if (DEV9Header.config.EthType == ConfigFile.EthAPI.Winsock)
//            {
//                ok = true;
//            }
//            else if (DEV9Header.config.EthType == ConfigFile.EthAPI.Null) { }
//            else
//            {
//                ok = true;//DEV9Header.config.DirectConnectionSettings.InterceptDHCP;
//            }
//            if (!ok)
//            {
//                MessageBox.Show("Cannot test using current settings. Test ingame instead");
//                return;
//            }
//            #endregion
//            //STEP 2, CREATE SMAP_Test
//            #region "S2"
//            SMAP_Test smap = new SMAP_Test();
//            if (!(smap.Open() == 0))
//            {
//                MessageBox.Show("Failed to open adapter");
//                return;
//            }
//            #endregion
//            //STEP 3, CONTACT DHCP
//            #region "S3"
//            if (!dhcp.Connect(smap, GetFrameOfType))
//            {
//                return;
//            }
//            #endregion
//            //STEP 4, INITAL ARP
//            #region "S3.5"
//            if (!arp.CheckConflict(smap, dhcp.PS2IP, GetFrameOfType))
//            {
//                return;
//            }
//            arp.SetPS2IP(dhcp.PS2IP);
//            arp.SetSubnet(dhcp.Mask);
//            if (!arp.GetGatewayMAC(smap, dhcp.GatewayIP, GetFrameOfType))
//            {
//                return;
//            }
//            #endregion
//            //STEP 4, CONTACT DNS
//            #region "S4"

//            #endregion
//        }

//        private void SendIP(SMAP_Test parSMAP, IPPayload perPayload, byte[] destIP)
//        {
//            IPPacket ippkt = new IPPacket(perPayload);
//            ippkt.DestinationIP = destIP;
//            ippkt.SourceIP = dhcp.PS2IP;

//            EthernetFrame ef = new EthernetFrame(ippkt);
//            ef.DestinationMAC = arp.GetMAC(parSMAP, destIP, GetFrameOfType);
//            if (ef.DestinationMAC == null) { return; }
//            ef.SourceMAC = parSMAP.GetHWAddress();
//            ef.Protocol = (ushort)EtherFrameType.IPv4;

//            NetPacket pkt = ef.CreatePacket();
//            parSMAP.TxProcess(ref pkt);
//        }

//        private EthernetFrame GetFrameOfType(SMAP_Test parSMAP, int parTimeout, EtherFrameType parType, byte parType2, uint parType3)
//        {
//            //Drop all packets we don't want
//            Stopwatch timer = new Stopwatch();
//            timer.Start();
//            while (timer.Elapsed.TotalSeconds < parTimeout)
//            {
//                parSMAP.ReceivedEvent.WaitOne(parTimeout * 1000 - (int)timer.ElapsedMilliseconds);
//                while (true)
//                {
//                    if (!parSMAP.ReceivedPackets.TryDequeue(out NetPacket pkt)) { break; }

//                    EthernetFrame ef = new EthernetFrame(pkt);
//                    if (!Utils.memcmp(ef.DestinationMAC, 0, parSMAP.GetHWAddress(), 0, 6) &
//                        !Utils.memcmp(ef.DestinationMAC, 0, new byte[] { 255, 255, 255, 255, 255, 255 }, 0, 6))
//                    {
//                        continue;
//                    }

//                    if (ef.Protocol == (ushort)parType)
//                    {
//                        switch (parType)
//                        {
//                            case EtherFrameType.ARP:
//                                ARPPacket arpptk = (ARPPacket)ef.Payload;
//                                //Don't care if Destination IP matches
//                                //Info may still be usefull
//                                uint ip;
//                                int x = 0;
//                                DataLib.ReadUInt32(arpptk.SenderProtocolAddress, ref x, out ip);
//                                if (arpptk.OP == parType2 & ip == parType3)
//                                {
//                                    timer.Stop();
//                                    return ef;
//                                }
//                                break;
//                            case EtherFrameType.IPv4:
//                                IPPacket ippkt = (IPPacket)ef.Payload;
//                                //Check IP Matches
//                                if (!Utils.memcmp(dhcp.PS2IP, 0, ippkt.DestinationIP, 0, 4) &
//                                    !Utils.memcmp(new byte[] { 255, 255, 255, 255 }, 0, ippkt.DestinationIP, 0, 4) &
//                                    !Utils.memcmp(dhcp.BroadcastIP, 0, ippkt.DestinationIP, 0, 4))
//                                {
//                                    break;
//                                }

//                                if (ippkt.Protocol == parType2)
//                                {
//                                    switch ((IPType)parType2)
//                                    {
//                                        case IPType.ICMP:
//                                            break;
//                                        case IPType.IGMP:
//                                            break;
//                                        case IPType.TCP:
//                                            break;
//                                        case IPType.UDP:
//                                            UDP upkt = (UDP)ippkt.Payload;
//                                            if (upkt.DestinationPort == parType3)
//                                            {
//                                                timer.Stop();
//                                                return ef;
//                                            }
//                                            break;
//                                    }
//                                }
//                                break;
//                        }
//                    }
//                }
//            }
//            timer.Stop();
//            return null;
//        }

//        //Send Message
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
