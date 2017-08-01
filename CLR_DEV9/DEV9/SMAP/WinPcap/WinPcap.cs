using CLRDEV9.DEV9.SMAP.Data;
using CLRDEV9.DEV9.SMAP.Winsock.PacketReader;
using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.ARP;
using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace CLRDEV9.DEV9.SMAP.WinPcap
{
    sealed partial class WinPcapAdapter : DirectAdapter
    {
        IntPtr adHandle;
        bool switched = false;
        bool pcapRunning = false;

        byte[] ps2IP = new byte[4];
        byte[] hostMAC; //host adapter

        public static List<string[]> GetAdapters()
        {
            //Check if we have winPcap
            if (!PcapAvailable())
            {
                Console.Error.WriteLine("WinPcap not found");
                return null;
            }

            List<string[]> pcapDevs = PcapListAdapters();

            if (pcapDevs == null)
            {
                return null;
            }

            List<string[]> names = new List<string[]>();

            foreach (string[] adapter in pcapDevs)
            {
                //return adapter
                if (adapter[1].StartsWith(@"\Device\NPF_"))
                {
                    //We are running under windows
                    if (GetFriendlyName(adapter[1].Substring(@"\Device\NPF_".Length)) != null)
                    {
                        names.Add(new string[] { GetFriendlyName(adapter[1].Substring(@"\Device\NPF_".Length)), adapter[0], adapter[1].Substring(@"\Device\NPF_".Length) });
                    }
                }
                else
                {
                    //We are running under wine
                    names.Add(new string[] { adapter[1], adapter[0], adapter[1] });
                }
            }

            if (names.Count == 0)
                return null;
            return names;
        }

        public WinPcapAdapter(DEV9_State parDev9, string parDevice, bool isSwitch)
            : base(parDev9)
        {
            switched = isSwitch;

            NetworkInterface hostAdapter = GetAdapterFromGuid(parDevice);
            if (hostAdapter == null)
            {
                if (BridgeHelper.IsInBridge(parDevice) == true)
                {
                    hostAdapter = GetAdapterFromGuid(BridgeHelper.GetBridgeGUID());
                }
            }
            if (hostAdapter == null)
            {
                //System.Windows.Forms.MessageBox.Show("Failed to GetAdapter");
                throw new NullReferenceException("Failed to GetAdapter");
            }
            hostMAC = hostAdapter.GetPhysicalAddress().GetAddressBytes();

            //If parDevice starts with "{", assume device is given by GUID (as it would be under windows)
            //else, use the string as is (wine)
            if (!PcapInitIO(parDevice.StartsWith("{") ? @"\Device\NPF_" + parDevice : parDevice))
            {
                Log_Error("Can't Open Device " + parDevice);
                System.Windows.Forms.MessageBox.Show("Can't Open Device " + parDevice);
                return;
            }

            if (DEV9Header.config.DirectConnectionSettings.InterceptDHCP)
            {
                InitDHCP(hostAdapter);
            }

            byte[] wMAC = (byte[])hostMAC.Clone();
            byte temp = wMAC[5];
            wMAC[5] = wMAC[4];
            wMAC[4] = temp;
            SetMAC(wMAC);
        }

        public override bool Blocks()
        {
            return false;	//we use non-blocking io
        }

        public override bool IsInitialised()
        {
            return pcapRunning;
        }

        public override bool Recv(ref NetPacket pkt)
        {
            if (base.Recv(ref pkt)) { return true; }

            int size = PcapRecvIO(pkt.buffer, pkt.buffer.Length);

            if (size <= 0)
            {
                return false;
            }

            //Recive DHCP Intercept Packets

            if (!switched) //TEST
            {
                //Quick and dirty lightweight packet reader
                if (GetEthProtocolHI(pkt.buffer) == 0x08) //ARP or IP
                {
                    if (GetEthProtocolLO(pkt.buffer) == 0x00) //IP
                    {
                        //Compare DEST IP in IP with PS2_IP, if match, change DEST MAC to PS2_MAC
                        //if (Utils.memcmp(pkt.buffer, 14 + 16, ps2_ip, 0, 4))
                        //{
                        //    Utils.memcpy(ref pkt.buffer, 0, ps2_mac, 0, 6); //ETH
                        //}
                        if (Utils.memcmp(GetDestIP_IP(pkt.buffer, 14), 0, ps2IP, 0, 4))
                        {
                            SetDestMAC_Eth(pkt.buffer, ps2MAC); //ETH
                        }
                    }
                    else if (GetEthProtocolLO(pkt.buffer) == 0x06) //ARP
                    {
                        //Compare DEST IP in ARP with PS2_IP, if match, DEST MAC to PS2_MAC
                        //on both ARP and ETH Packet
                        if (Utils.memcmp(GetDestARP_IP(pkt.buffer, 14), 0, ps2IP, 0, 4))
                        {
                            //Utils.memcpy(ref pkt.buffer, 0, ps2_mac, 0, 6); //ETH
                            SetDestMAC_Eth(pkt.buffer, ps2MAC); //ETH
                            //Utils.memcpy(ref pkt.buffer, 14 + 18, ps2_mac, 0, 6); //ARP
                            SetDestMAC_ARP(pkt.buffer, 14, ps2MAC);
                        }
                    }
                }
            }

            if (!Verify(pkt, size))
            {
                return false;
            }

            pkt.size = size;
            return true;
        }

        public override bool Send(NetPacket pkt)
        {
            if (base.Send(pkt)) { return true; }
            //get_eth_protocol_hi(pkt.buffer);
            //get_eth_protocol_lo(pkt.buffer);
            //get_dest_eth_mac(pkt.buffer);
            //get_src_eth_mac(pkt.buffer);
            //get_dest_arp_mac(pkt.buffer,14);
            //get_src_arp_mac(pkt.buffer,14);
            //get_dest_arp_ip(pkt.buffer, 14);
            //get_dest_ip_ip(pkt.buffer, 14);

            EthernetFrame eth = null;

            if (!switched)
            {
                eth = new EthernetFrame(pkt);

                //If intercept DHCP, then get IP from DHCP process
                if (eth.Protocol == (UInt16)EtherFrameType.IPv4)
                {
                    ps2IP = ((IPPacket)eth.Payload).SourceIP;
                    //MAC
                }
                else if (eth.Protocol == (UInt16)EtherFrameType.ARP)
                {
                    ps2IP = ((ARPPacket)eth.Payload).SenderProtocolAddress;
                    //MAC
                    //Need to also set Host MAC (SenderProtocolAddress) 
                    //Utils.memcpy(ref pkt.buffer, 14 + 8, host_mac, 0, 6); //ARP
                    SetSrcMAC_ARP(pkt.buffer, 14, hostMAC);
                }
                //Set Sorce mac to host_mac
                SetSrcMAC_Eth(pkt.buffer, hostMAC);
            }
            else //Switched
            {
                byte[] host_mac_pkt = new byte[pkt.size];
                Array.Copy(pkt.buffer, host_mac_pkt, pkt.size);
                //here we send a boadcast with both host_mac and ps2_mac
                //is destination address broadcast?
                if (Utils.memcmp(GetDestMAC_Eth(host_mac_pkt), 0, broadcastMAC, 0, 6))
                {
                    //Set Dest to host mac
                    SetDestMAC_Eth(host_mac_pkt, hostMAC);
                    PcapSendIO(host_mac_pkt, pkt.size);
                }
            }

            if (PcapSendIO(pkt.buffer, pkt.size))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        #region Basic packet reader/Writer
        //Eth Protocol
        private byte GetEthProtocolHI(byte[] buf)
        {
            //Log_Verb("Eth hi: " + buf[12]);
            return buf[12];

        }
        private byte GetEthProtocolLO(byte[] buf)
        {
            //Log_Verb("Eth lo: " + buf[13]);
            return buf[13];
        }
        //Eth Dest Mac
        private byte[] GetDestMAC_Eth(byte[] buf)
        {
            byte[] ret = new byte[6];
            Array.Copy(buf, 0, ret, 0, 6);
            //Log_Verb("Eth DestMac: " + ret[0].ToString("X") + ":" + ret[1].ToString("X") + ":" + ret[2].ToString("X") + ":" + ret[3].ToString("X") + ":" + ret[4].ToString("X") + ":" + ret[5].ToString("X"));
            return buf;
        }
        private void SetDestMAC_Eth(byte[] buf, byte[] value)
        {
            Array.Copy(value, 0, buf, 0, 6);
        }
        //Eth Sender Mac
        private byte[] GetSrcMAC_Eth(byte[] buf)
        {
            byte[] ret = new byte[6];
            Array.Copy(buf, 6, ret, 0, 6);
            //Log_Verb("Eth DestMac: " + ret[0].ToString("X") + ":" + ret[1].ToString("X") + ":" + ret[2].ToString("X") + ":" + ret[3].ToString("X") + ":" + ret[4].ToString("X") + ":" + ret[5].ToString("X"));
            return buf;
        }
        private void SetSrcMAC_Eth(byte[] buf, byte[] value)
        {
            Array.Copy(value, 0, buf, 6, 6);
        }
        //IP Dest IP
        private byte[] GetDestIP_IP(byte[] buf, int pktOffset)
        {
            //offset is where ip packet starts
            byte[] ret = new byte[4];
            Array.Copy(buf, pktOffset + 16, ret, 0, 4); //16
            //Log_Verb("IP DestIP: " + ret[0] + ":" + ret[1] + ":" + ret[2] + ":" + ret[3]);
            return ret;
        }
        //Arp Dest Mac
        private byte[] GetDestMAC_ARP(byte[] buf, int pktOffset)
        {
            byte[] ret = new byte[6];
            Array.Copy(buf, pktOffset + 18, ret, 0, 6);
            //Log_Verb("Eth DestMac: " + ret[0].ToString("X") + ":" + ret[1].ToString("X") + ":" + ret[2].ToString("X") + ":" + ret[3].ToString("X") + ":" + ret[4].ToString("X") + ":" + ret[5].ToString("X"));
            return buf;
        }
        private void SetDestMAC_ARP(byte[] buf, int pktOffset, byte[] value)
        {
            Array.Copy(value, 0, buf, pktOffset + 18, 6);
        }
        //ARP Sender Mac
        private byte[] GetSrcMAC_ARP(byte[] buf, int pktOffset)
        {
            byte[] ret = new byte[6];
            Array.Copy(buf, pktOffset + 8, ret, 0, 6);
            //Log_Verb("Eth DestMac: " + ret[0].ToString("X") + ":" + ret[1].ToString("X") + ":" + ret[2].ToString("X") + ":" + ret[3].ToString("X") + ":" + ret[4].ToString("X") + ":" + ret[5].ToString("X"));
            return buf;
        }
        private void SetSrcMAC_ARP(byte[] buf, int pktOffset, byte[] value)
        {
            Array.Copy(value, 0, buf, pktOffset + 8, 6);
        }
        //ARP Dest IP
        private byte[] GetDestARP_IP(byte[] buf, int pktOffset)
        {
            byte[] ret = new byte[4];
            Array.Copy(buf, pktOffset + 24, ret, 0, 4); //24
            //Log_Verb("ARP DestIP: " + ret[0] + ":" + ret[1] + ":" + ret[2] + ":" + ret[3]);
            return ret;
        }
        #endregion

        public override void Close()
        {
            //Rx thread still running in close
            //wait untill Rx thread stopped before
            //closing pcap
        }

        public override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                PcapCloseIO();
            }
        }

        protected override void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.WinPcap, str);
        }
        protected override void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.WinPcap, str);
        }
        protected override void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.WinPcap, str);
        }
    }
}
