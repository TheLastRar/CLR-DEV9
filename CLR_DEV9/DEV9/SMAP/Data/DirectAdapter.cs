using CLRDEV9.DEV9.SMAP.Winsock.PacketReader;
using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using CLRDEV9.DEV9.SMAP.Winsock.Sessions;
using System;
using System.Net;
using System.Net.NetworkInformation;

namespace CLRDEV9.DEV9.SMAP.Data
{
    abstract class DirectAdapter : NetAdapter
    {
        public DirectAdapter(DEV9_State pardev9)
            : base(pardev9)
        { }

        private bool DHCP_Active = false;
        private UDP_DHCPsession DHCP = null;

        protected void InitDHCP(NetworkInterface parAdapter)
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

        public override bool Recv(ref NetPacket pkt)
        {
            if (DHCP_Active)
            {
                IPPayload retDHCP = DHCP.Recv();
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
            return false;
        }

        public override bool Send(NetPacket pkt)
        {
            if (DHCP_Active)
            {
                EthernetFrame eth = new EthernetFrame(pkt);
                if (eth.Protocol == (UInt16)EtherFrameType.IPv4)
                {
                    IPPacket ipp = (IPPacket)eth.Payload;
                    if (ipp.Protocol == (byte)IPType.UDP)
                    {
                        UDP udppkt = (UDP)ipp.Payload;
                        if (udppkt.DestinationPort == 67)
                        {
                            DHCP.Send(udppkt);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (DHCP_Active & disposing)
            {
                DHCP_Active = false;
                DHCP.Dispose();
                DHCP = null;
            }
        }
    }
}
