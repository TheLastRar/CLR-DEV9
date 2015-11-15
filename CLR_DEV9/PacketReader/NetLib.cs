using System;
using System.Net;
using System.Text;

namespace CLR_DEV9.PacketReader
{
    //Performes NetOrder changes
    class NetLib
    {
        //TODO make shit use this

        //Perform convert to Net order and write to buffer
        public static void WriteUInt32(ref Byte[] buffer, ref int offset, UInt32 value)
        {
            DataLib.WriteUInt32(ref buffer, ref offset, (UInt32)IPAddress.HostToNetworkOrder((Int32)value));
        }
        public static void WriteUInt16(ref Byte[] buffer, ref int offset, UInt16 value)
        {
            DataLib.WriteUInt16(ref buffer, ref offset, (UInt16)IPAddress.HostToNetworkOrder((Int16)value));
        }
        public static void WriteByte08(ref Byte[] buffer, ref int offset, Byte value)
        {
            DataLib.WriteByte08(ref buffer, ref offset, value);
        }
        //Special
        public static void WriteCString(ref Byte[] buffer, ref int offset, String value)
        {
            DataLib.WriteCString(ref buffer, ref offset, value);
        }
        public static void WriteByteArray(ref Byte[] buffer, ref int offset, Byte[] value)
        {
            DataLib.WriteByteArray(ref buffer, ref offset, value);
        }
        //read
        public static void ReadUInt32(Byte[] buffer, ref int offset, out UInt32 value)
        {
            DataLib.ReadUInt32(buffer, ref offset, out value);
            value = (UInt32)IPAddress.NetworkToHostOrder((Int32)value);
        }
        public static void ReadUInt16(Byte[] buffer, ref int offset, out UInt16 value)
        {
            DataLib.ReadUInt16(buffer, ref offset, out value);
            value = (UInt16)IPAddress.NetworkToHostOrder((Int16)value);
        }
        public static void ReadByte08(Byte[] buffer, ref int offset, out Byte value)
        {
            DataLib.ReadByte08(buffer, ref offset, out value);
        }
        //Special
        public static void ReadCString(Byte[] buffer, ref int offset, int maxLength, out String value)
        {
            DataLib.ReadCString(buffer, ref offset, maxLength, out value);
        }
        public static void ReadByteArray(Byte[] buffer, ref int offset, int length, out Byte[] value)
        {
            DataLib.ReadByteArray(buffer, ref offset, length, out value);
        }
    }
    //No NetOrder changes
    class DataLib
    {
        //write to buffer without NO convert
        public static void WriteUInt32(ref Byte[] buffer, ref int offset, UInt32 value)
        {
            Array.Copy(BitConverter.GetBytes(
                value),
                0,
                buffer,
                offset,
                sizeof(UInt32));
            offset += sizeof(UInt32);
        }
        public static void WriteUInt16(ref Byte[] buffer, ref int offset, UInt16 value)
        {
            Array.Copy(BitConverter.GetBytes(
                value),
                0,
                buffer,
                offset,
                sizeof(UInt16));
            offset += sizeof(UInt16);
        }
        public static void WriteByte08(ref Byte[] buffer, ref int offset, Byte value)
        {
            buffer[offset] = value;
            offset += sizeof(Byte);
        }
        //Special
        public static void WriteCString(ref Byte[] buffer, ref int offset, String value)
        {
            Byte[] strBytes = ASCIIEncoding.ASCII.GetBytes(value);
            Array.Copy(strBytes, 0, buffer, offset, strBytes.Length);
            //C# arrays are initialised to zero, so no need to write
            //null char, just skip the byte
            offset += strBytes.Length + 1;
        }
        public static void WriteByteArray(ref Byte[] buffer, ref int offset, Byte[] value)
        {
            Array.Copy(value, 0, buffer, offset, value.Length);
            offset += value.Length;
        }
        //read
        public static void ReadUInt32(Byte[] buffer, ref int offset, out UInt32 value)
        {
            value = BitConverter.ToUInt32(buffer, offset);
            offset += sizeof(UInt32);
        }
        public static void ReadUInt16(Byte[] buffer, ref int offset, out UInt16 value)
        {
            value = BitConverter.ToUInt16(buffer, offset);
            offset += sizeof(UInt16);
        }
        public static void ReadByte08(Byte[] buffer, ref int offset, out Byte value)
        {
            value = buffer[offset];
            offset += sizeof(Byte);
        }
        //Special
        //A little bit more complex then the rest
        //Soure http://stackoverflow.com/q/5964718
        public static void ReadCString(Byte[] buffer, ref int offset, int maxLength, out String value)
        {
            Encoding targetEncoding = ASCIIEncoding.ASCII;

            int length = 0;
            int remainingLen = buffer.Length - offset;
            //Is buffer shorter than max len?
            int realMax = remainingLen < maxLength ? remainingLen : maxLength;

            for (
                 ; 0 != buffer[offset + length] && length < realMax
                 ; ++length)
            { }
            value = targetEncoding.GetString(buffer, offset, length);
            offset += length + 1;
        }
        public static void ReadByteArray(Byte[] buffer, ref int offset, int length, out Byte[] value)
        {
            //Check Input value
            if ((length + offset) > buffer.Length)
                throw new ArgumentOutOfRangeException();
            value = new Byte[length];
            Array.Copy(buffer, offset, value, 0, length);
            offset += value.Length;
        }
    }
}
