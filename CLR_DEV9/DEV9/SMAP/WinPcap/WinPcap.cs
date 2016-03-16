using CLRDEV9.DEV9.SMAP.Data;
using CLRDEV9.DEV9.SMAP.Winsock.PacketReader;
using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.ARP;
using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using CLRDEV9.DEV9.SMAP.Winsock.Sessions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;

namespace CLRDEV9.DEV9.SMAP.WinPcap
{
    partial class WinPcapAdapter : NetAdapter
    {
        IntPtr adhandle;
        bool switched = false;
        bool pcap_io_running = false;

        byte[] ps2_ip = new byte[4];
        byte[] host_mac;

        public static List<string[]> GetAdapters()
        {
            //Check if we have winPcap
            if (!pcap_io_available())
            {
                Console.Error.WriteLine("WinPcap not found");
                return null;
            }
            //Duplicate code bettween this and winsock
            NetworkInterface[] Interfaces = NetworkInterface.GetAllNetworkInterfaces();
            List<string[]> names = new List<string[]>();

            foreach (NetworkInterface adapter in Interfaces)
            {
                //if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                //{
                //    continue;
                //}
                //Don't know it we support all of these
                if (adapter.NetworkInterfaceType != NetworkInterfaceType.Ethernet &
                    adapter.NetworkInterfaceType != NetworkInterfaceType.Ethernet3Megabit &
                    adapter.NetworkInterfaceType != NetworkInterfaceType.FastEthernetFx &
                    adapter.NetworkInterfaceType != NetworkInterfaceType.FastEthernetT &
                    adapter.NetworkInterfaceType != NetworkInterfaceType.GigabitEthernet)
                {
                    continue;
                }
                //if (adapter.OperationalStatus == OperationalStatus.Up)
                //{
                    UnicastIPAddressInformationCollection IPInfo = adapter.GetIPProperties().UnicastAddresses;
                    IPInterfaceProperties properties = adapter.GetIPProperties();

                    foreach (UnicastIPAddressInformation IPAddressInfo in IPInfo)
                    {
                        if (IPAddressInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            //return adapter
                            names.Add(new string[] { adapter.Name, adapter.Description, adapter.Id });
                            break;
                        }
                    //}
                }
            }

            if (names.Count == 0)
                return null;
            return names;
        }

        public WinPcapAdapter(DEV9_State pardev9, string parDevice, bool isSwitch)
            : base(pardev9)
        {
            switched = isSwitch;

            NetworkInterface host_adapter = GetAdapterFromGuid(parDevice);
            host_mac = host_adapter.GetPhysicalAddress().GetAddressBytes();

            //DEV9Header.config.Eth.Substring(12, DEV9Header.config.Eth.Length - 12)
            if (!pcap_io_init(@"\Device\NPF_" + parDevice))
            {
                Log_Error("Can't Open Device " + DEV9Header.config.Eth);
                System.Windows.Forms.MessageBox.Show("Can't Open Device " + DEV9Header.config.Eth);
                return;
            }

            if (DEV9Header.config.DirectConnectionSettings.InterceptDHCP)
            {
                InitDHCP(host_adapter);
            }
        }

        #region DHCP
        private bool DHCP_Active = false;
        UDP_DHCPsession DHCP = null;
        private void InitDHCP(NetworkInterface parAdapter)
        {
            //Cleanup to pass options as paramaters instead of accessing the config directly?
            DHCP_Active = true;
            byte[] PS2IP = IPAddress.Parse(DEV9Header.config.DirectConnectionSettings.PS2IP).GetAddressBytes();
            byte[] NetMask = null;
            byte[] Gateway = null;

            byte[] DNS1 = null;
            byte[] DNS2 = null;

            if (!DEV9Header.config.DirectConnectionSettings.AutoSubNet)
            {
                NetMask = IPAddress.Parse(DEV9Header.config.DirectConnectionSettings.SubNet).GetAddressBytes();
            }

            if (!DEV9Header.config.DirectConnectionSettings.AutoGateway)
            {
                Gateway = IPAddress.Parse(DEV9Header.config.DirectConnectionSettings.Gateway).GetAddressBytes();
            }

            if (!DEV9Header.config.DirectConnectionSettings.AutoDNS1)
            {
                DNS1 = IPAddress.Parse(DEV9Header.config.DirectConnectionSettings.DNS1).GetAddressBytes();
            }
            if (!DEV9Header.config.DirectConnectionSettings.AutoDNS2)
            {
                DNS2 = IPAddress.Parse(DEV9Header.config.DirectConnectionSettings.DNS2).GetAddressBytes();
            }
            //Create DHCP Session
            DHCP = new UDP_DHCPsession(parAdapter, PS2IP, NetMask, Gateway, DNS1, DNS2);
            DHCP_Active = true;
        }
        #endregion

        public override bool Blocks()
        {
            return false;	//we use non-blocking io
        }

        public override bool IsInitialised()
        {
            return pcap_io_running;
        }

        public override bool Recv(ref NetPacket pkt)
        {
            if (DHCP_Active)
            {
                IPPayload retDHCP = DHCP.recv();
                if (retDHCP != null)
                {
                    IPPacket retIP = new IPPacket(retDHCP);
                    retIP.DestinationIP = new byte[] { 255, 255, 255, 255 };
                    retIP.SourceIP = DefaultDHCPConfig.DHCP_IP;

                    EthernetFrame eF = new EthernetFrame(retIP);
                    eF.SourceMAC = virtural_gateway_mac;
                    eF.DestinationMAC = ps2_mac;
                    eF.Protocol = (UInt16)EtherFrameType.IPv4;
                    pkt = eF.CreatePacket();
                    return true;
                }
            }


            int size = pcap_io_recv(pkt.buffer, pkt.buffer.Length);

            if (size <= 0)
            {
                return false;
            }

            //Recive DHCP Intercept Packets

            if (!switched) //TEST
            {
                //Quick and dirty lightweight packet reader
                if (get_eth_protocol_hi(pkt.buffer) == 0x08) //ARP or IP
                {
                    if (get_eth_protocol_lo(pkt.buffer) == 0x00) //IP
                    {
                        //Compare DEST IP in IP with PS2_IP, if match, change DEST MAC to PS2_MAC
                        //if (Utils.memcmp(pkt.buffer, 14 + 16, ps2_ip, 0, 4))
                        //{
                        //    Utils.memcpy(ref pkt.buffer, 0, ps2_mac, 0, 6); //ETH
                        //}
                        if (Utils.memcmp(get_dest_ip_ip(pkt.buffer, 14), 0, ps2_ip, 0, 4))
                        {
                            set_dest_eth_mac(pkt.buffer, ps2_mac); //ETH
                        }
                    }
                    else if (get_eth_protocol_lo(pkt.buffer) == 0x06) //ARP
                    {
                        //Compare DEST IP in ARP with PS2_IP, if match, DEST MAC to PS2_MAC
                        //on both ARP and ETH Packet
                        if (Utils.memcmp(get_dest_arp_ip(pkt.buffer, 14), 0, ps2_ip, 0, 4))
                        {
                            //Utils.memcpy(ref pkt.buffer, 0, ps2_mac, 0, 6); //ETH
                            set_dest_eth_mac(pkt.buffer, ps2_mac); //ETH
                            //Utils.memcpy(ref pkt.buffer, 14 + 18, ps2_mac, 0, 6); //ARP
                            set_dest_arp_mac(pkt.buffer, 14, ps2_mac);
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
            //get_eth_protocol_hi(pkt.buffer);
            //get_eth_protocol_lo(pkt.buffer);
            //get_dest_eth_mac(pkt.buffer);
            //get_src_eth_mac(pkt.buffer);
            //get_dest_arp_mac(pkt.buffer,14);
            //get_src_arp_mac(pkt.buffer,14);
            //get_dest_arp_ip(pkt.buffer, 14);
            //get_dest_ip_ip(pkt.buffer, 14);

            EthernetFrame eth = null;

            if (DHCP_Active)
            {
                eth = new EthernetFrame(pkt);
                if (eth.Protocol == (UInt16)EtherFrameType.IPv4)
                {
                    IPPacket ipp = (IPPacket)eth.Payload;
                    if (ipp.Protocol == (byte)IPType.UDP)
                    {
                        UDP udppkt = (UDP)ipp.Payload;
                        if (udppkt.DestinationPort == 67)
                        {
                            DHCP.send(udppkt);
                            return true;
                        }
                    }
                }
            }

            if (!switched)
            {
                if (eth == null) { eth = new EthernetFrame(pkt); }

                //If intercept DHCP, then get IP from DHCP process
                if (eth.Protocol == (UInt16)EtherFrameType.IPv4)
                {
                    ps2_ip = ((IPPacket)eth.Payload).SourceIP;
                    //MAC
                }
                else if (eth.Protocol == (UInt16)EtherFrameType.ARP)
                {
                    ps2_ip = ((ARPPacket)eth.Payload).SenderProtocolAddress;
                    //MAC
                    //Need to also set Host MAC (SenderProtocolAddress) 
                    //Utils.memcpy(ref pkt.buffer, 14 + 8, host_mac, 0, 6); //ARP
                    set_src_arp_mac(pkt.buffer, 14, host_mac);
                }
                //Set Sorce mac to host_mac
                set_src_eth_mac(pkt.buffer, host_mac);
            }
            else //Switched
            {
                byte[] host_mac_pkt = new byte[pkt.size];
                Array.Copy(pkt.buffer, host_mac_pkt, pkt.size);
                //here we send a boadcast with both host_mac and ps2_mac
                //is destination address broadcast?
                if (Utils.memcmp(get_dest_eth_mac(host_mac_pkt), 0, broadcast_mac, 0, 6))
                {
                    //Set Dest to host mac
                    set_dest_eth_mac(host_mac_pkt, host_mac);
                    pcap_io_send(host_mac_pkt, pkt.size);
                }
            }

            if (pcap_io_send(pkt.buffer, pkt.size))
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
        private byte get_eth_protocol_hi(byte[] buf)
        {
            Log_Verb("Eth hi: " + buf[12]);
            return buf[12];

        }
        private byte get_eth_protocol_lo(byte[] buf)
        {
            Log_Verb("Eth lo: " + buf[13]);
            return buf[13];
        }
        //Eth Dest Mac
        private byte[] get_dest_eth_mac(byte[] buf)
        {
            byte[] ret = new byte[6];
            Array.Copy(buf, 0, ret, 0, 6);
            Log_Verb("Eth DestMac: " + ret[0].ToString("X") + ":" + ret[1].ToString("X") + ":" + ret[2].ToString("X") + ":" + ret[3].ToString("X") + ":" + ret[4].ToString("X") + ":" + ret[5].ToString("X"));
            return buf;
        }
        private void set_dest_eth_mac(byte[] buf, byte[] value)
        {
            Array.Copy(value, 0, buf, 0, 6);
        }
        //Eth Sender Mac
        private byte[] get_src_eth_mac(byte[] buf)
        {
            byte[] ret = new byte[6];
            Array.Copy(buf, 6, ret, 0, 6);
            Log_Verb("Eth DestMac: " + ret[0].ToString("X") + ":" + ret[1].ToString("X") + ":" + ret[2].ToString("X") + ":" + ret[3].ToString("X") + ":" + ret[4].ToString("X") + ":" + ret[5].ToString("X"));
            return buf;
        }
        private void set_src_eth_mac(byte[] buf, byte[] value)
        {
            Array.Copy(value, 0, buf, 6, 6);
        }
        //IP Dest IP
        private byte[] get_dest_ip_ip(byte[] buf, int pktoffset)
        {
            //offset is where ip packet starts
            byte[] ret = new byte[4];
            Array.Copy(buf, pktoffset + 16, ret, 0, 4); //16
            Log_Verb("IP DestIP: " + ret[0] + ":" + ret[1] + ":" + ret[2] + ":" + ret[3]);
            return ret;
        }
        //Arp Dest Mac
        private byte[] get_dest_arp_mac(byte[] buf, int pktoffset)
        {
            byte[] ret = new byte[6];
            Array.Copy(buf, pktoffset + 18, ret, 0, 6);
            Log_Verb("Eth DestMac: " + ret[0].ToString("X") + ":" + ret[1].ToString("X") + ":" + ret[2].ToString("X") + ":" + ret[3].ToString("X") + ":" + ret[4].ToString("X") + ":" + ret[5].ToString("X"));
            return buf;
        }
        private void set_dest_arp_mac(byte[] buf, int pktoffset, byte[] value)
        {
            Array.Copy(value, 0, buf, pktoffset + 18, 6);
        }
        //ARP Sender Mac
        private byte[] get_src_arp_mac(byte[] buf, int pktoffset)
        {
            byte[] ret = new byte[6];
            Array.Copy(buf, pktoffset + 8, ret, 0, 6);
            Log_Verb("Eth DestMac: " + ret[0].ToString("X") + ":" + ret[1].ToString("X") + ":" + ret[2].ToString("X") + ":" + ret[3].ToString("X") + ":" + ret[4].ToString("X") + ":" + ret[5].ToString("X"));
            return buf;
        }
        private void set_src_arp_mac(byte[] buf, int pktoffset, byte[] value)
        {
            Array.Copy(value, 0, buf, pktoffset + 8, 6);
        }
        //ARP Dest IP
        private byte[] get_dest_arp_ip(byte[] buf, int pktoffset)
        {
            byte[] ret = new byte[4];
            Array.Copy(buf, pktoffset + 24, ret, 0, 4); //24
            Log_Verb("ARP DestIP: " + ret[0] + ":" + ret[1] + ":" + ret[2] + ":" + ret[3]);
            return ret;
        }
        #endregion

        public override void Dispose()
        {
            pcap_io_close();
            if (DHCP_Active)
            {
                DHCP_Active = false;
                DHCP.Dispose();
                DHCP = null;
            }
        }

        protected override void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.WinPcap, "WinPcap", str);
        }
        protected override void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.WinPcap, "WinPcap", str);
        }
        protected override void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.WinPcap, "WinPcap", str);
        }
    }
}
