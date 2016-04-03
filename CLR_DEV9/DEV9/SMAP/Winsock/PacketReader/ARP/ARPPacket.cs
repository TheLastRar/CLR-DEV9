using System;

namespace CLRDEV9.DEV9.SMAP.Winsock.PacketReader.ARP
{
    class ARPPacket : EthernetPayload
    {
        public override ushort Length
        {
            get
            {
                return (UInt16)(8 + (2 * HardwareAddressLength) + (2 * ProtocolAddressLength));
            }
            protected set
            {
                throw new NotImplementedException();
            }
        }
        public override byte[] GetBytes
        {
            get
            {
                byte[] ret = new byte[Length];
                int counter = 0;
                NetLib.WriteUInt16(ref ret, ref counter, HardWareType);
                //
                DataLib.WriteUInt16(ref ret, ref counter, Protocol);
                //
                NetLib.WriteByte08(ref ret, ref counter, HardwareAddressLength);
                NetLib.WriteByte08(ref ret, ref counter, ProtocolAddressLength);
                NetLib.WriteUInt16(ref ret, ref counter, OP);

                NetLib.WriteByteArray(ref ret, ref counter, SenderHardwareAddress);
                NetLib.WriteByteArray(ref ret, ref counter, SenderProtocolAddress);

                NetLib.WriteByteArray(ref ret, ref counter, TargetHardwareAddress);
                NetLib.WriteByteArray(ref ret, ref counter, TargetProtocolAddress);
                return ret;
            }
        }
        public UInt16 HardWareType;
        public UInt16 Protocol; //In Net Order
        public byte HardwareAddressLength = 6;
        public byte ProtocolAddressLength = 4;
        public UInt16 OP;
        public byte[] SenderHardwareAddress;
        public byte[] SenderProtocolAddress;
        public byte[] TargetHardwareAddress;
        public byte[] TargetProtocolAddress;

        public ARPPacket()
        {
            HardWareType = 1;
        }
        public ARPPacket(EthernetFrame Ef)
        {
            int pktOffset = Ef.HeaderLength;
            NetLib.ReadUInt16(Ef.RawPacket.buffer, ref pktOffset, out HardWareType);
            //
            DataLib.ReadUInt16(Ef.RawPacket.buffer, ref pktOffset, out Protocol);
            //
            NetLib.ReadByte08(Ef.RawPacket.buffer, ref pktOffset, out HardwareAddressLength);
            NetLib.ReadByte08(Ef.RawPacket.buffer, ref pktOffset, out ProtocolAddressLength);
            NetLib.ReadUInt16(Ef.RawPacket.buffer, ref pktOffset, out OP);
            //Error.WriteLine("OP" + OP);

            NetLib.ReadByteArray(Ef.RawPacket.buffer, ref pktOffset, HardwareAddressLength, out SenderHardwareAddress);
            //WriteLine("sender MAC :" + SenderHardwareAddress[0] + ":" + SenderHardwareAddress[1] + ":" + SenderHardwareAddress[2] + ":" + SenderHardwareAddress[3] + ":" + SenderHardwareAddress[4] + ":" + SenderHardwareAddress[5]);

            NetLib.ReadByteArray(Ef.RawPacket.buffer, ref pktOffset, HardwareAddressLength, out SenderProtocolAddress);
            //WriteLine("sender IP :" + SenderProtocolAddress[0] + "." + SenderProtocolAddress[1] + "." + SenderProtocolAddress[2] + "." + SenderProtocolAddress[3]);      

            NetLib.ReadByteArray(Ef.RawPacket.buffer, ref pktOffset, HardwareAddressLength, out TargetHardwareAddress);
            //WriteLine("target MAC :" + TargetHardwareAddress[0] + ":" + TargetHardwareAddress[1] + ":" + TargetHardwareAddress[2] + ":" + TargetHardwareAddress[3] + ":" + TargetHardwareAddress[4] + ":" + TargetHardwareAddress[5]);

            NetLib.ReadByteArray(Ef.RawPacket.buffer, ref pktOffset, HardwareAddressLength, out TargetProtocolAddress);
            //WriteLine("target IP :" + TargetProtocolAddress[0] + "." + TargetProtocolAddress[1] + "." + TargetProtocolAddress[2] + "." + TargetProtocolAddress[3]);
        }
    }
}
