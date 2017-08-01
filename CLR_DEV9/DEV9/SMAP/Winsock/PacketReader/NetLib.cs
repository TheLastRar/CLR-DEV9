using System;
using System.Net;
using System.Text;

namespace CLRDEV9.DEV9.SMAP.Winsock.PacketReader
{
    //Performes NetOrder changes
    class NetLib
    {
        //TODO make shit use this

        //Perform convert to Net order and write to buffer
        public static void WriteUInt32(byte[] buffer, ref int offset, UInt32 value)
        {
            DataLib.WriteUInt32(buffer, ref offset, (UInt32)IPAddress.HostToNetworkOrder((Int32)value));
        }
        public static void WriteUInt16(byte[] buffer, ref int offset, UInt16 value)
        {
            DataLib.WriteUInt16(buffer, ref offset, (UInt16)IPAddress.HostToNetworkOrder((Int16)value));
        }
        public static void WriteByte08(byte[] buffer, ref int offset, byte value)
        {
            DataLib.WriteByte08(buffer, ref offset, value);
        }
        //Special
        public static void WriteCString(byte[] buffer, ref int offset, String value)
        {
            DataLib.WriteCString(buffer, ref offset, value);
        }
        public static void WriteByteArray(byte[] buffer, ref int offset, byte[] value)
        {
            DataLib.WriteByteArray(buffer, ref offset, value);
        }
        //read
        public static void ReadUInt32(byte[] buffer, ref int offset, out UInt32 value)
        {
            DataLib.ReadUInt32(buffer, ref offset, out value);
            value = (UInt32)IPAddress.NetworkToHostOrder((Int32)value);
        }
        public static void ReadUInt16(byte[] buffer, ref int offset, out UInt16 value)
        {
            DataLib.ReadUInt16(buffer, ref offset, out value);
            value = (UInt16)IPAddress.NetworkToHostOrder((Int16)value);
        }
        public static void ReadByte08(byte[] buffer, ref int offset, out byte value)
        {
            DataLib.ReadByte08(buffer, ref offset, out value);
        }
        //Special
        public static void ReadCString(byte[] buffer, ref int offset, int maxLength, out String value)
        {
            DataLib.ReadCString(buffer, ref offset, maxLength, out value);
        }
        public static void ReadByteArray(byte[] buffer, ref int offset, int length, out byte[] value)
        {
            DataLib.ReadByteArray(buffer, ref offset, length, out value);
        }
    }
    //No NetOrder changes
    class DataLib
    {
        //write to buffer without NO convert
        public static void WriteUInt64(byte[] buffer, ref int offset, UInt64 value)
        {
            Array.Copy(BitConverter.GetBytes(
                value),
                0,
                buffer,
                offset,
                sizeof(UInt64));
            offset += sizeof(UInt64);
        }
        public static void WriteUInt32(byte[] buffer, ref int offset, UInt32 value)
        {
            Array.Copy(BitConverter.GetBytes(
                value),
                0,
                buffer,
                offset,
                sizeof(UInt32));
            offset += sizeof(UInt32);
        }
        public static void WriteUInt16(byte[] buffer, ref int offset, UInt16 value)
        {
            Array.Copy(BitConverter.GetBytes(
                value),
                0,
                buffer,
                offset,
                sizeof(UInt16));
            offset += sizeof(UInt16);
        }
        public static void WriteByte08(byte[] buffer, ref int offset, byte value)
        {
            buffer[offset] = value;
            offset += sizeof(byte);
        }
        //Special
        public static void WriteCString(byte[] buffer, ref int offset, String value)
        {
            byte[] strbytes = Encoding.ASCII.GetBytes(value);
            Array.Copy(strbytes, 0, buffer, offset, strbytes.Length);
            //C# arrays are initialised to zero, so no need to write
            //null char, just skip the byte
            offset += strbytes.Length + 1;
        }
        public static void WriteByteArray(byte[] buffer, ref int offset, byte[] value)
        {
            Array.Copy(value, 0, buffer, offset, value.Length);
            offset += value.Length;
        }
        //read
        public static void ReadUInt32(byte[] buffer, ref int offset, out UInt32 value)
        {
            value = BitConverter.ToUInt32(buffer, offset);
            offset += sizeof(UInt32);
        }
        public static void ReadUInt16(byte[] buffer, ref int offset, out UInt16 value)
        {
            value = BitConverter.ToUInt16(buffer, offset);
            offset += sizeof(UInt16);
        }
        public static void ReadByte08(byte[] buffer, ref int offset, out byte value)
        {
            value = buffer[offset];
            offset += sizeof(byte);
        }
        //Special
        //A little bit more complex then the rest
        //Soure http://stackoverflow.com/q/5964718
        public static void ReadCString(byte[] buffer, ref int offset, int maxLength, out String value)
        {
            Encoding targetEncoding = Encoding.ASCII;

            int length = 0;
            int remainingLen = buffer.Length - offset;
            //Is buffer shorter than max len?
            int realMax = remainingLen < maxLength ? remainingLen : maxLength;

            for (
                 ; length < realMax && 0 != buffer[offset + length]
                 ; ++length)
            { }
            value = targetEncoding.GetString(buffer, offset, length);
            offset += length + 1;
        }
        public static void ReadByteArray(byte[] buffer, ref int offset, int length, out byte[] value)
        {
            //Check Input value
            if ((length + offset) > buffer.Length)
                throw new ArgumentOutOfRangeException();
            value = new byte[length];
            Array.Copy(buffer, offset, value, 0, length);
            offset += value.Length;
        }
    }
}
