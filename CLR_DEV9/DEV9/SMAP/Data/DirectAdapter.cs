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
        public DirectAdapter(DEV9_State parDev9)
            : base(parDev9)
        { }

        private bool dhcpActive = false;
        private UDP_DHCPsession dhcp = null;

        protected void InitDHCP(NetworkInterface parAdapter)
        {
            //Cleanup to pass options as paramaters instead of accessing the config directly?
            dhcpActive = true;
            byte[] ps2IP = IPAddress.Parse(DEV9Header.config.DirectConnectionSettings.PS2IP).GetAddressBytes();
            byte[] netMask = null;
            byte[] gateway = null;

            byte[] dns1 = null;
            byte[] dns2 = null;

            if (!DEV9Header.config.DirectConnectionSettings.AutoSubNet)
            {
                netMask = IPAddress.Parse(DEV9Header.config.DirectConnectionSettings.SubNet).GetAddressBytes();
            }

            if (!DEV9Header.config.DirectConnectionSettings.AutoGateway)
            {
                gateway = IPAddress.Parse(DEV9Header.config.DirectConnectionSettings.Gateway).GetAddressBytes();
            }

            if (!DEV9Header.config.DirectConnectionSettings.AutoDNS1)
            {
                dns1 = IPAddress.Parse(DEV9Header.config.DirectConnectionSettings.DNS1).GetAddressBytes();
            }
            if (!DEV9Header.config.DirectConnectionSettings.AutoDNS2)
            {
                dns2 = IPAddress.Parse(DEV9Header.config.DirectConnectionSettings.DNS2).GetAddressBytes();
            }
            //Create DHCP Session
            dhcp = new UDP_DHCPsession(parAdapter, ps2IP, netMask, gateway, dns1, dns2);
            dhcpActive = true;
        }

        public override bool Recv(ref NetPacket pkt)
        {
            if (dhcpActive)
            {
                IPPayload retDHCP = dhcp.Recv();
                if (retDHCP != null)
                {
                    IPPacket retIP = new IPPacket(retDHCP);
                    retIP.DestinationIP = new byte[] { 255, 255, 255, 255 };
                    retIP.SourceIP = DefaultDHCPConfig.DHCP_IP;

                    EthernetFrame ef = new EthernetFrame(retIP);
                    ef.SourceMAC = virturalDHCPMAC;
                    ef.DestinationMAC = ps2MAC;
                    ef.Protocol = (UInt16)EtherFrameType.IPv4;
                    pkt = ef.CreatePacket();
                    return true;
                }
            }
            return false;
        }

        public override bool Send(NetPacket pkt)
        {
            if (dhcpActive)
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
                            dhcp.Send(udppkt);
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
            if (dhcpActive & disposing)
            {
                dhcpActive = false;
                dhcp.Dispose();
                dhcp = null;
            }
        }
    }
}
