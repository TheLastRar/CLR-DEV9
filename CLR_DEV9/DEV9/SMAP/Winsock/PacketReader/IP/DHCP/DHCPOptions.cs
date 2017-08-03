using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
using System.Collections.Generic;
using System.Text;

namespace CLRDEV9.DEV9.SMAP.Winsock.PacketReader.DHCP
{
    class DHCPopNOP : TCPOption //unlike TCP options, DCHP length feild does not count the option header
    {
        public DHCPopNOP()
        {

        }
        public override byte Length { get { return 1; } }
        public override byte Code { get { return 0; } }

        public override byte[] GetBytes()
        {
            return new byte[] { Code };
        }
    }
    class DHCPopSubnet : TCPOption
    {
        //Subnet Mask
        byte[] mask = new byte[4];
        public byte[] SubnetMask
        {
            get
            {
                return mask;
            }
        }
        public DHCPopSubnet(byte[] data)
        {
            mask = data;
        }
        public DHCPopSubnet(byte[] data, int offset) //Offset will include Kind and Len
        {
            offset += 2;
            NetLib.ReadByteArray(data, ref offset, 4, out mask);
        }
        public override byte Length { get { return 6; } }
        public override byte Code { get { return 1; } }

        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteByte08(ret, ref counter, Code);
            NetLib.WriteByte08(ret, ref counter, (byte)(Length - 2));
            NetLib.WriteByteArray(ret, ref counter, mask);
            return ret;
        }
    }
    class DHCPopRouter : TCPOption //can be longer then 1 address
    {
        //byte[] routerIP = new byte[4];
        List<byte[]> routers = new List<byte[]>();
        public List<byte[]> RouterIPs
        {
            get
            {
                return routers;
            }
        }
        public DHCPopRouter(List<byte[]> routerIPs)
        {
            routers = routerIPs;
        }
        public DHCPopRouter(byte[] data, int offset) //Offset will include Kind and Len
        {
            offset += 1;
            byte len;
            NetLib.ReadByte08(data, ref offset, out len);
            len = (byte)((len) / 4);
            byte[] rIP;
            for (int x = 0; x < len; x++)
            {
                NetLib.ReadByteArray(data, ref offset, 4, out rIP);
                routers.Add(rIP);
            }
        }
        public override byte Length { get { return (byte)(2 + 4 * RouterIPs.Count); } }
        public override byte Code { get { return 3; } }

        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteByte08(ret, ref counter, Code);
            NetLib.WriteByte08(ret, ref counter, (byte)(Length - 2));
            foreach (byte[] rIP in routers)
            {
                NetLib.WriteByteArray(ret, ref counter, rIP);
            }
            return ret;
        }
    }
    class DHCPopDNS : TCPOption //can be longer then 1 address
    {
        List<byte[]> dnsServers = new List<byte[]>();
        public List<byte[]> DNSServers
        {
            get
            {
                return dnsServers;
            }
        }
        public DHCPopDNS(List<byte[]> dnsIPs)
        {
            dnsServers = dnsIPs;
        }

        public DHCPopDNS(byte[] data, int offset) //Offset will include Kind and Len
        {
            offset += 1;
            byte len;
            NetLib.ReadByte08(data, ref offset, out len);
            len = (byte)((len) / 4);
            byte[] dIP;
            for (int x = 0; x < len; x++)
            {
                NetLib.ReadByteArray(data, ref offset, 4, out dIP);
                dnsServers.Add(dIP);
            }
        }
        public override byte Length { get { return (byte)(2 + 4 * dnsServers.Count); } }
        public override byte Code { get { return 6; } }

        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteByte08(ret, ref counter, Code);
            NetLib.WriteByte08(ret, ref counter, (byte)(Length - 2));
            foreach (byte[] dIP in dnsServers)
            {
                NetLib.WriteByteArray(ret, ref counter, dIP);
            }
            return ret;
        }
    }
    class DHCPopDNSNAME : TCPOption
    {
        byte len;
        byte[] domainNameBytes;
        public string Name
        {
            get
            {
                int x = 0;
                NetLib.ReadCString(domainNameBytes, ref x, domainNameBytes.Length, out string value);
                return value;
            }
        }
        public DHCPopDNSNAME(string name)
        {
            domainNameBytes = Encoding.ASCII.GetBytes(name);
            len = (byte)domainNameBytes.Length;
            if (domainNameBytes.Length > len)
            {
                throw new Exception("Domain Name Overflow");
            }
        }
        public DHCPopDNSNAME(byte[] data, int offset) //Offset will include Kind and Len
        {
            len = data[offset + 1];
            domainNameBytes = new byte[len];
            Utils.memcpy(domainNameBytes, 0, data, offset + 2, len);
        }
        public override byte Length { get { return (byte)(2 + len); } }
        public override byte Code { get { return 15; } }

        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            ret[0] = Code;
            ret[1] = (byte)(Length - 2);
            Utils.memcpy(ret, 2, domainNameBytes, 0, len);
            return ret;
        }
    }
    class DHCPopBCIP : TCPOption //The IP to send broadcasts to
    {
        byte[] ip = new byte[4];
        public byte[] BroadcastIP
        {
            get
            {
                return ip;
            }
        }
        public DHCPopBCIP(byte[] data) //ip provided as byte array
        {
            ip = data;
        }
        public DHCPopBCIP(byte[] data, int offset) //Offset will include Kind and Len
        {
            offset += 2;
            NetLib.ReadByteArray(data, ref offset, 4, out ip);
        }
        public override byte Length { get { return 6; } }
        public override byte Code { get { return 28; } }

        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteByte08(ret, ref counter, Code);
            NetLib.WriteByte08(ret, ref counter, (byte)(Length - 2));
            NetLib.WriteByteArray(ret, ref counter, ip);
            return ret;
        }
    }
    class DHCPopNBOIPType : TCPOption
    {
        byte type;
        public bool HNode
        {
            get { return ((type & (1 << 3)) != 0); }
            set
            {
                if (value) { type |= (1 << 3); }
                else { type &= unchecked((byte)(~(1 << 3))); }
            }
        }
        public bool MNode
        {
            get { return ((type & (1 << 2)) != 0); }
            set
            {
                if (value) { type |= (1 << 2); }
                else { type &= unchecked((byte)(~(1 << 2))); }
            }
        }
        public bool PNode
        {
            get { return ((type & (1 << 1)) != 0); }
            set
            {
                if (value) { type |= (1 << 1); }
                else { type &= unchecked((byte)(~(1 << 1))); }
            }
        }
        public bool BNode
        {
            get { return ((type & (1)) != 0); }
            set
            {
                if (value) { type |= (1); }
                else { type &= unchecked((byte)(~(1))); }
            }
        }
        public DHCPopNBOIPType(byte[] data, int offset) //Offset will include Kind and Len
        {
            offset += 2;
            NetLib.ReadByte08(data, ref offset, out type);
        }
        public override byte Length { get { return 3; } }
        public override byte Code { get { return 46; } }

        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteByte08(ret, ref counter, Code);
            NetLib.WriteByte08(ret, ref counter, (byte)(Length - 2));
            NetLib.WriteByte08(ret, ref counter, type);
            return ret;
        }
    }
    class DHCPopREQIP : TCPOption
    {
        byte[] ip = new byte[4];
        public byte[] IPaddress
        {
            get
            {
                return ip;
            }
        }
        public DHCPopREQIP(byte[] data) //ip provided as byte array
        {
            ip = data;
        }
        public DHCPopREQIP(byte[] data, int offset) //Offset will include Kind and Len
        {
            offset += 2;
            NetLib.ReadByteArray(data, ref offset, 4, out ip);
        }
        public override byte Length { get { return 6; } }
        public override byte Code { get { return 50; } }

        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteByte08(ret, ref counter, Code);
            NetLib.WriteByte08(ret, ref counter, (byte)(Length - 2));
            NetLib.WriteByteArray(ret, ref counter, ip);
            return ret;
        }
    }
    class DHCPopIPLT : TCPOption
    {
        public UInt32 IPLeaseTime;
        public DHCPopIPLT(UInt32 LeaseTime)
        {
            IPLeaseTime = LeaseTime;
        }
        public DHCPopIPLT(byte[] data, int offset) //Offset will include Kind and Len
        {
            offset += 2;
            NetLib.ReadUInt32(data, ref offset, out IPLeaseTime);
        }
        public override byte Length { get { return 6; } }
        public override byte Code { get { return 51; } }

        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteByte08(ret, ref counter, Code);
            NetLib.WriteByte08(ret, ref counter, (byte)(Length - 2));
            NetLib.WriteUInt32(ret, ref counter, IPLeaseTime);
            return ret;
        }
    }
    class DHCPopMSG : TCPOption
    {
        byte msg;
        public byte Message
        {
            get
            {
                return msg;
            }
        }
        public DHCPopMSG(byte parMsg)
        {
            msg = parMsg;
        }
        public DHCPopMSG(byte[] data, int offset) //Offset will include Kind and Len
        {
            offset += 2;
            NetLib.ReadByte08(data, ref offset, out msg);
        }
        public override byte Length { get { return 3; } }
        public override byte Code { get { return 53; } }

        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteByte08(ret, ref counter, Code);
            NetLib.WriteByte08(ret, ref counter, (byte)(Length - 2));
            NetLib.WriteByte08(ret, ref counter, msg);
            return ret;
        }
    }
    class DHCPopSERVIP : TCPOption //DHCP server ip
    {
        byte[] ip = new byte[4];
        public byte[] ServerIP
        {
            get
            {
                return ip;
            }
        }
        public DHCPopSERVIP(byte[] data) //ip provided as byte array
        {
            ip = data;
        }
        public DHCPopSERVIP(byte[] data, int offset) //Offset will include Kind and Len
        {
            offset += 2;
            NetLib.ReadByteArray(data, ref offset, 4, out ip);
        }
        public override byte Length { get { return 6; } }
        public override byte Code { get { return 54; } }

        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteByte08(ret, ref counter, Code);
            NetLib.WriteByte08(ret, ref counter, (byte)(Length - 2));
            NetLib.WriteByteArray(ret, ref counter, ip);
            return ret;
        }
    }
    class DHCPopREQLIST : TCPOption
    {
        byte len;
        byte[] requests;
        public byte[] RequestList
        {
            get
            {
                return requests;
            }
        }
        public DHCPopREQLIST(byte[] data, int offset) //Offset will include Kind and Len
        {
            offset += 1;
            NetLib.ReadByte08(data, ref offset, out len);
            NetLib.ReadByteArray(data, ref offset, len, out requests);
        }
        public DHCPopREQLIST(byte[] requestList)
        {
            len = (byte)requestList.Length;
            requests = requestList;
        }
        public override byte Length { get { return (byte)(2 + len); } }
        public override byte Code { get { return 55; } }

        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteByte08(ret, ref counter, Code);
            NetLib.WriteByte08(ret, ref counter, (byte)(Length - 2));
            NetLib.WriteByteArray(ret, ref counter, requests);
            return ret;
        }
    }
    class DHCPopMSGStr : TCPOption
    {
        byte len;
        byte[] msgBytes;
        public string Message
        {
            get
            {
                Encoding enc = Encoding.ASCII;
                return enc.GetString(msgBytes);
            }
        }
        public DHCPopMSGStr(byte[] data, int offset) //Offset will include Kind and Len
        {
            offset += 1;
            NetLib.ReadByte08(data, ref offset, out len);
            NetLib.ReadByteArray(data, ref offset, len, out msgBytes);
        }
        public override byte Length { get { return (byte)(2 + len); } }
        public override byte Code { get { return 56; } }

        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteByte08(ret, ref counter, Code);
            NetLib.WriteByte08(ret, ref counter, (byte)(Length - 2));
            NetLib.WriteByteArray(ret, ref counter, msgBytes);
            return ret;
        }
    }
    class DHCPopMMSGS : TCPOption
    {
        public UInt16 MaxMessageSize;
        public DHCPopMMSGS(byte[] data, int offset) //Offset will include Kind and Len
        {
            offset += 2;
            NetLib.ReadUInt16(data, ref offset, out MaxMessageSize);
        }
        public override byte Length { get { return 4; } }
        public override byte Code { get { return 57; } }
        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteByte08(ret, ref counter, Code);
            NetLib.WriteByte08(ret, ref counter, (byte)(Length - 2));
            NetLib.WriteUInt16(ret, ref counter, MaxMessageSize);
            return ret;
        }
    }
    class DHCPopT1 : TCPOption
    {
        public UInt32 IPRenewalTimeT1;
        public DHCPopT1(UInt32 T1)
        {
            IPRenewalTimeT1 = T1;
        }
        public DHCPopT1(byte[] data, int offset) //Offset will include Kind and Len
        {
            offset += 2;
            NetLib.ReadUInt32(data, ref offset, out IPRenewalTimeT1);
        }
        public override byte Length { get { return 6; } }
        public override byte Code { get { return 58; } }

        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteByte08(ret, ref counter, Code);
            NetLib.WriteByte08(ret, ref counter, (byte)(Length - 2));
            NetLib.WriteUInt32(ret, ref counter, IPRenewalTimeT1);
            return ret;
        }
    }
    class DHCPopT2 : TCPOption
    {
        public UInt32 IPRebindingTimeT2;
        public DHCPopT2(UInt32 LeaseTime)
        {
            IPRebindingTimeT2 = LeaseTime;
        }
        public DHCPopT2(byte[] data, int offset) //Offset will include Kind and Len
        {
            offset += 2;
            NetLib.ReadUInt32(data, ref offset, out IPRebindingTimeT2);
        }
        public override byte Length { get { return 6; } }
        public override byte Code { get { return 59; } }

        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteByte08(ret, ref counter, Code);
            NetLib.WriteByte08(ret, ref counter, (byte)(Length - 2));
            NetLib.WriteUInt32(ret, ref counter, IPRebindingTimeT2);
            return ret;
        }
    }
    class DHCPopClassID : TCPOption
    {
        byte len;
        byte[] classBytes;
        public string ClassID
        {
            get
            {
                Encoding enc = Encoding.ASCII;
                return enc.GetString(classBytes);
            }
        }
        public DHCPopClassID(byte[] data, int offset) //Offset will include Kind and Len
        {
            offset += 1;
            NetLib.ReadByte08(data, ref offset, out len);
            NetLib.ReadByteArray(data, ref offset, len, out classBytes);
        }
        public override byte Length { get { return (byte)(2 + len); } }
        public override byte Code { get { return 60; } }

        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteByte08(ret, ref counter, Code);
            NetLib.WriteByte08(ret, ref counter, (byte)(Length - 2));
            NetLib.WriteByteArray(ret, ref counter, classBytes);
            return ret;
        }
    }
    class DHCPopClientID : TCPOption
    {
        byte len;
        byte[] clientID;
        public DHCPopClientID(byte[] data, int offset) //Offset will include Kind and Len
        {
            offset += 1;
            NetLib.ReadByte08(data, ref offset, out len);
            //ClientID = new byte[len];
            NetLib.ReadByteArray(data, ref offset, len, out clientID);
        }
        public override byte Length { get { return (byte)(2 + len); } }
        public override byte Code { get { return 61; } }

        public override byte[] GetBytes()
        {
            byte[] ret = new byte[Length];
            int counter = 0;
            NetLib.WriteByte08(ret, ref counter, Code);
            NetLib.WriteByte08(ret, ref counter, (byte)(Length - 2));
            NetLib.WriteByteArray(ret, ref counter, clientID);
            return ret;
        }
    }
    class DHCPopEND : TCPOption
    {
        public DHCPopEND()
        {

        }
        public override byte Length { get { return 1; } }
        public override byte Code { get { return 255; } }

        public override byte[] GetBytes()
        {
            return new byte[] { Code };
        }
    }
}
