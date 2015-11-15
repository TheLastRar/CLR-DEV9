using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CLR_DEV9.PacketReader
{
    class EthernetFrame
    {
        private netHeader.NetPacket _pkt;
        public netHeader.NetPacket RawPacket
        {
            get{
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
        public netHeader.NetPacket CreatePacket()
        {
            netHeader.NetPacket nPK = new netHeader.NetPacket();
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

        public EthernetFrame(netHeader.NetPacket pkt)
        {
            _pkt = pkt;
            int offset = 0;
            NetLib.ReadByteArray(pkt.buffer, ref offset, 6, out DestinationMAC);
            //Console.WriteLine("eth dst MAC :" + DestinationMAC[0] + ":" + DestinationMAC[1] + ":" + DestinationMAC[2] + ":" + DestinationMAC[3] + ":" + DestinationMAC[4] + ":" + DestinationMAC[5]);
            NetLib.ReadByteArray(pkt.buffer, ref offset, 6, out SourceMAC);
            //Console.WriteLine("src MAC :" + SourceMAC[0] + ":" + SourceMAC[1] + ":" + SourceMAC[2] + ":" + SourceMAC[3] + ":" + SourceMAC[4] + ":" + SourceMAC[5]);
            
            hlen = 14; //(6+6+2)
            
            //NOTE: we don't have to worry about the Ethernet Frame CRC as it is not included in the packet

            DataLib.ReadUInt16(pkt.buffer, ref offset, out proto);
            switch (proto) //Note, Diffrent Edian
            {
                case (UInt16)EtherFrameType.NULL:
                    break;
                case (UInt16)EtherFrameType.IPv4:
                    _pl = new IPPacket(this);
                    break;
                case (UInt16)EtherFrameType.ARP:
                    _pl = new ARPPacket(this);
                    break;
                case (UInt16)EtherFrameType.VLAN_TAGGED_FRAME:
                    //Console.Error.WriteLine("VLAN-tagged frame (IEEE 802.1Q)");
                    throw new NotImplementedException();
                    //break;
                default:
                    Console.Error.WriteLine("Unkown Ethernet Protocol " + proto.ToString("X4"));
                    break;
            }
        }
    }
}
