//using CLRDEV9.DEV9.SMAP.Data;
//using CLRDEV9.DEV9.SMAP.Winsock.PacketReader;
//using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.ARP;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using System.Windows.Forms;

//namespace CLRDEV9.Config.Test.Eth
//{
//    class ARPTest
//    {
//        byte[] ps2IP = new byte[4];
//        byte[] mask = new byte[4];
//        public byte[] GatewayMAC;

//        public void SetPS2IP(byte[] ip)
//        {
//            ps2IP = ip;
//        }
//        public void SetSubnet(byte[] subnet)
//        {
//            mask = subnet;
//        }

//        public bool GetGatewayMAC(SMAP_Test parSMAP, byte[] parGWIP, Func<SMAP_Test, int, EtherFrameType, byte, uint, EthernetFrame> GetFrameOfType)
//        {
//            Log_Info("Getting GatewayMAC");
//            GatewayMAC = GetMAC(parSMAP, parGWIP, GetFrameOfType);
//            if (GatewayMAC == null)
//            {
//                MessageBox.Show("Can't Reach Gateway");
//                Log_Info("No Reply To Gateway ARP Request");
//                //TODO release
//                return false;
//            }
//            Log_Info("Found GatewayMAC");
//            return true;
//        }

//        public bool CheckConflict(SMAP_Test parSMAP, byte[] parPS2IP, Func<SMAP_Test, int, EtherFrameType, byte, uint, EthernetFrame> GetFrameOfType)
//        {
//            Log_Info("Sending ARP Probe For PS2 IP");
//            byte[] tmp = ps2IP;
//            ps2IP = new byte[4];

//            if (GetMAC(parSMAP, parPS2IP, GetFrameOfType) != null)
//            {
//                MessageBox.Show("Conflicting IP Address");
//                Log_Info("Got Reply, Conflicting IP Address");
//                //TODO release
//                ps2IP = tmp;
//                return false;
//            }
//            Log_Info("No Reply, IP Address OK");
//            ps2IP = tmp;
//            return true;
//        }

//        public void AnnonceIP(SMAP_Test parSMAP)
//        {
//            Log_Info("ARP Annonce IP");
//            ARPPacket arp = new ARPPacket();
//            arp.HardWareType = 1;
//            arp.Protocol = (UInt16)EtherFrameType.IPv4;
//            arp.HardwareAddressLength = 6;
//            arp.ProtocolAddressLength = 4;
//            arp.OP = 1;
//            arp.SenderHardwareAddress = parSMAP.GetHWAddress();
//            arp.SenderProtocolAddress = ps2IP;
//            arp.TargetHardwareAddress = new byte[] { 0, 0, 0, 0, 0, 0 };
//            arp.TargetProtocolAddress = ps2IP;

//            EthernetFrame ef = new EthernetFrame(arp);
//            ef.Protocol = (UInt16)EtherFrameType.ARP;
//            ef.SourceMAC = parSMAP.GetHWAddress();
//            ef.DestinationMAC = new byte[] { 255, 255, 255, 255, 255, 255 };

//            NetPacket pkt = ef.CreatePacket();
//            parSMAP.TxProcess(ref pkt);
//            Log_Info("ARP Annonce IP Done");
//        }

//        public byte[] GetMAC(SMAP_Test parSMAP, byte[] parIP, Func<SMAP_Test, int, EtherFrameType, byte, uint, EthernetFrame> GetFrameOfType)
//        {
//            //Is IP in network?
//            if (!Utils.memcmp(ps2IP, 0, new byte[] { 0, 0, 0, 0 }, 0, 4))
//            {
//                int o = 0;
//                DataLib.ReadUInt32(ps2IP, ref o, out uint pInt); o = 0;
//                DataLib.ReadUInt32(mask, ref o, out uint mInt); o = 0;
//                DataLib.ReadUInt32(parIP, ref o, out uint iInt);
//                if ((pInt & mInt) != (iInt & mInt))
//                {
//                    return GatewayMAC;
//                }
//            }

//            ARPPacket arp = new ARPPacket();
//            arp.HardWareType = 1;
//            arp.Protocol = (UInt16)EtherFrameType.IPv4;
//            arp.HardwareAddressLength = 6;
//            arp.ProtocolAddressLength = 4;
//            arp.OP = 1;
//            arp.SenderHardwareAddress = parSMAP.GetHWAddress();
//            arp.SenderProtocolAddress = ps2IP;
//            arp.TargetHardwareAddress = new byte[] { 255, 255, 255, 255, 255, 255 };
//            arp.TargetProtocolAddress = parIP;

//            EthernetFrame ef = new EthernetFrame(arp);
//            ef.Protocol = (UInt16)EtherFrameType.ARP;
//            ef.SourceMAC = parSMAP.GetHWAddress();
//            ef.DestinationMAC = new byte[] { 255, 255, 255, 255, 255, 255 };

//            int x = 0;
//            DataLib.ReadUInt32(parIP, ref x, out uint ipInt);

//            NetPacket pkt = ef.CreatePacket();

//            int tries = 0;

//            while (tries < 2)
//            {
//                tries++;
//                parSMAP.TxProcess(ref pkt);
//                EthernetFrame ret = GetFrameOfType(parSMAP, 3, EtherFrameType.ARP, 2, ipInt);
//                if (ret != null)
//                {
//                    return ((ARPPacket)ret.Payload).SenderHardwareAddress;
//                }
//            }
//            return null;
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
