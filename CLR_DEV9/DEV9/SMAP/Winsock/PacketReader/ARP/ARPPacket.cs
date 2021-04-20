using System;
using System.Diagnostics;

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
        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteUInt16(ret, ref counter, HardWareType);
            //
            DataLib.WriteUInt16(ret, ref counter, Protocol);
            //
            NetLib.WriteByte08(ret, ref counter, HardwareAddressLength);
            NetLib.WriteByte08(ret, ref counter, ProtocolAddressLength);
            NetLib.WriteUInt16(ret, ref counter, OP);

            NetLib.WriteByteArray(ret, ref counter, SenderHardwareAddress);
            NetLib.WriteByteArray(ret, ref counter, SenderProtocolAddress);

            NetLib.WriteByteArray(ret, ref counter, TargetHardwareAddress);
            NetLib.WriteByteArray(ret, ref counter, TargetProtocolAddress);
            return ret;
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
            //Log_Error("OP" + OP);

            NetLib.ReadByteArray(Ef.RawPacket.buffer, ref pktOffset, HardwareAddressLength, out SenderHardwareAddress);
            //Log_Error("sender MAC :" + SenderHardwareAddress[0] + ":" + SenderHardwareAddress[1] + ":" + SenderHardwareAddress[2] + ":" + SenderHardwareAddress[3] + ":" + SenderHardwareAddress[4] + ":" + SenderHardwareAddress[5]);

            NetLib.ReadByteArray(Ef.RawPacket.buffer, ref pktOffset, ProtocolAddressLength, out SenderProtocolAddress);
            //Log_Error("sender IP :" + SenderProtocolAddress[0] + "." + SenderProtocolAddress[1] + "." + SenderProtocolAddress[2] + "." + SenderProtocolAddress[3]);      
            
            NetLib.ReadByteArray(Ef.RawPacket.buffer, ref pktOffset, HardwareAddressLength, out TargetHardwareAddress);
            //Log_Error("target MAC :" + TargetHardwareAddress[0] + ":" + TargetHardwareAddress[1] + ":" + TargetHardwareAddress[2] + ":" + TargetHardwareAddress[3] + ":" + TargetHardwareAddress[4] + ":" + TargetHardwareAddress[5]);

            NetLib.ReadByteArray(Ef.RawPacket.buffer, ref pktOffset, ProtocolAddressLength, out TargetProtocolAddress);
            //Log_Error("target IP :" + TargetProtocolAddress[0] + "." + TargetProtocolAddress[1] + "." + TargetProtocolAddress[2] + "." + TargetProtocolAddress[3]);
        }

        protected void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.Winsock, str);
        }
        protected void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.Winsock, str);
        }
        protected void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.Winsock, str);
        }
    }
}
