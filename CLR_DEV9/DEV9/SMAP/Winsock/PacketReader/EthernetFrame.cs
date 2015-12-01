using CLRDEV9.DEV9.SMAP.Data;
using System;
using System.Diagnostics;

namespace CLRDEV9.DEV9.SMAP.Winsock.PacketReader
{
    class EthernetFrame
    {
        private NetPacket _pkt;
        public NetPacket RawPacket
        {
            get
            {
                return _pkt;
            }
        }
        public byte[] DestinationMAC = new byte[6];
        public byte[] SourceMAC = new byte[6];

        UInt16 proto;
        public UInt16 Protocol
        {
            get
            {
                return proto;
            }
            set { proto = value; }
        }
        private int hlen = 0;
        public int HeaderLength
        {
            get
            {
                return hlen;
            }
        }
        public int Length
        {
            get
            {
                return _pkt.size;//Frame Check is not added to the frame
            }
        }
        EthernetPayload _pl;
        public EthernetPayload Payload
        {
            get
            {
                return _pl;
            }
        }

        public EthernetFrame(EthernetPayload ePL)
        {
            hlen = 14;
            _pl = ePL;
        }
        public NetPacket CreatePacket()
        {
            NetPacket nPK = new NetPacket();
            byte[] PLbytes = _pl.GetBytes;
            int counter = 0;

            //byte[] rawbytes = new byte[PLbytes.Length + hlen];
            nPK.size = PLbytes.Length + hlen;
            NetLib.WriteByteArray(ref nPK.buffer, ref counter, DestinationMAC);
            NetLib.WriteByteArray(ref nPK.buffer, ref counter, SourceMAC);
            //
            DataLib.WriteUInt16(ref nPK.buffer, ref counter, proto);
            //
            NetLib.WriteByteArray(ref nPK.buffer, ref counter, PLbytes);
            return nPK;
        }

        public EthernetFrame(NetPacket pkt)
        {
            _pkt = pkt;
            int offset = 0;
            NetLib.ReadByteArray(pkt.buffer, ref offset, 6, out DestinationMAC);
            //WriteLine("eth dst MAC :" + DestinationMAC[0] + ":" + DestinationMAC[1] + ":" + DestinationMAC[2] + ":" + DestinationMAC[3] + ":" + DestinationMAC[4] + ":" + DestinationMAC[5]);
            NetLib.ReadByteArray(pkt.buffer, ref offset, 6, out SourceMAC);
            //WriteLine("src MAC :" + SourceMAC[0] + ":" + SourceMAC[1] + ":" + SourceMAC[2] + ":" + SourceMAC[3] + ":" + SourceMAC[4] + ":" + SourceMAC[5]);

            hlen = 14; //(6+6+2)

            //NOTE: we don't have to worry about the Ethernet Frame CRC as it is not included in the packet

            DataLib.ReadUInt16(pkt.buffer, ref offset, out proto);
            switch (proto) //Note, Diffrent Edian
            {
                case (UInt16)EtherFrameType.NULL:
                    break;
                case (UInt16)EtherFrameType.IPv4:
                    _pl = new IP.IPPacket(this);
                    break;
                case (UInt16)EtherFrameType.ARP:
                    _pl = new ARP.ARPPacket(this);
                    break;
                case (UInt16)EtherFrameType.VLAN_TAGGED_FRAME:
                    //Error.WriteLine("VLAN-tagged frame (IEEE 802.1Q)");
                    throw new NotImplementedException();
                //break;
                default:
                    PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.Winsock, "ETH", "Unkown Ethernet Protocol " + proto.ToString("X4"));
                    break;
            }
        }
    }
}
