﻿using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CLRDEV9.DEV9.SMAP.Winsock.PacketReader.DHCP
{
    class DHCP
    {
        public byte OP;
        public byte HardwareType;
        public byte HardwareAddressLength;
        public byte Hops;
        public UInt32 TransactionID; //xid
        //Int16 NO_sec; //seconds
        public UInt16 Seconds;
        public UInt16 Flags;
        public byte[] ClientIP = new byte[4];
        public byte[] YourIP = new byte[4];
        public byte[] ServerIP = new byte[4];
        public byte[] GatewayIP = new byte[4]; //NOT the router IP
        public byte[] ClientHardwareAddress = new byte[16]; //always 16 bytes, regardless of HardwareAddressLength
        //192 bytes of padding
        public UInt32 MagicCookie;
        public List<TCPOption> Options = new List<TCPOption>();
        public DHCP()
        {
            int x = 0;
            NetLib.ReadUInt32(new byte[] { 99, 130, 83, 99 }, ref x, out MagicCookie);
        }
        public DHCP(byte[] data)
        {
            int offset = 0;
            //Bits 0-31 //Bytes 0-3
            NetLib.ReadByte08(data, ref offset, out OP);
            Log_Verb("OP " + OP);
            NetLib.ReadByte08(data, ref offset, out HardwareType);
            //Error.WriteLine("HWt " + HardwareType);
            NetLib.ReadByte08(data, ref offset, out HardwareAddressLength);
            //Error.WriteLine("HWaddrlen " + HardwareAddressLength);
            NetLib.ReadByte08(data, ref offset, out Hops);
            //Error.WriteLine("Hops " + Hops);

            //Bits 32-63 //Bytes 4-7
            //TransactionID = BitConverter.ToInt32(data, 4);
            NetLib.ReadUInt32(data, ref offset, out TransactionID);
            Log_Verb("xid " + TransactionID);

            //Bits 64-95 //Bytes 8-11
            NetLib.ReadUInt16(data, ref offset, out Seconds);
            //Error.WriteLine("sec " + Seconds);
            NetLib.ReadUInt16(data, ref offset, out Flags);
            //Error.WriteLine("Flags " + Flags);

            //Bits 96-127 //Bytes 12-15
            NetLib.ReadByteArray(data, ref offset, 4, out ClientIP);
            Log_Info("CIP " + ClientIP[0] + "." + ClientIP[1] + "." + ClientIP[2] + "." + ClientIP[3]);

            //Bits 128-159 //Bytes 16-19
            NetLib.ReadByteArray(data, ref offset, 4, out YourIP);
            Log_Info("YIP " + YourIP[0] + "." + YourIP[1] + "." + YourIP[2] + "." + YourIP[3]);

            //Bits 160-191 //Bytes 20-23
            NetLib.ReadByteArray(data, ref offset, 4, out ServerIP);
            Log_Info("SIP " + ServerIP[0] + "." + ServerIP[1] + "." + ServerIP[2] + "." + ServerIP[3]);

            //Bits 192-223 //Bytes 24-27
            NetLib.ReadByteArray(data, ref offset, 4, out GatewayIP);
            Log_Info("GIP " + GatewayIP[0] + "." + GatewayIP[1] + "." + GatewayIP[2] + "." + GatewayIP[3]);

            //Bits 192+ //Bytes 28-43
            NetLib.ReadByteArray(data, ref offset, 16, out ClientHardwareAddress);

            //Bytes 44-107

            byte[] sNamebytes;
            NetLib.ReadByteArray(data, ref offset, 64, out sNamebytes);

            //Bytes 108-235
            byte[] filebytes;
            NetLib.ReadByteArray(data, ref offset, 128, out filebytes);

            //Bytes 236-239
            NetLib.ReadUInt32(data, ref offset, out MagicCookie);
            //Error.WriteLine("Cookie " + MagicCookie);
            bool opReadFin = false;
            //int op_offset = 240;
            do
            {
                byte opKind = data[offset];
                if (opKind == 255)
                {
                    Options.Add(new DHCPopEND());
                    opReadFin = true;
                    offset += 1;
                    continue;
                }
                if ((offset + 1) >= data.Length)
                {
                    Log_Error("Unexpected end of packet");
                    Options.Add(new DHCPopEND());
                    opReadFin = true;
                    continue;
                }
                byte opLen = data[offset + 1];
                switch (opKind)
                {
                    case 0:
                        //Error.WriteLine("Got NOP");
                        Options.Add(new DHCPopNOP());
                        offset += 1;
                        continue;
                    case 1:
                        //Error.WriteLine("Got Subnet");
                        Options.Add(new DHCPopSubnet(data, offset));
                        break;
                    case 3:
                        //Error.WriteLine("Got Router");
                        Options.Add(new DHCPopRouter(data, offset));
                        break;
                    case 6:
                        //Error.WriteLine("Got DNS Servers");
                        Options.Add(new DHCPopDNS(data, offset));
                        break;
                    case 12:
                        Options.Add(new DHCPopHOSTNAME(data, offset));
                        break;
                    case 15:
                        //Error.WriteLine("Got Domain Name (Not supported)");
                        Options.Add(new DHCPopDNSNAME(data, offset));
                        break;
                    case 28:
                        //Error.WriteLine("Got broadcast");
                        Options.Add(new DHCPopBCIP(data, offset));
                        break;
                    case 46:
                        //Error.WriteLine("Got Request IP");
                        Options.Add(new DHCPopNBOIPType(data, offset));
                        break;
                    case 50:
                        //Error.WriteLine("Got Request IP");
                        Options.Add(new DHCPopREQIP(data, offset));
                        break;
                    case 51:
                        //Error.WriteLine("Got IP Address Lease Time");
                        Options.Add(new DHCPopIPLT(data, offset));
                        break;
                    case 53:
                        //Error.WriteLine("Got MSG");
                        Options.Add(new DHCPopMSG(data, offset));
                        break;
                    case 54:
                        //Error.WriteLine("Got Server IP");
                        Options.Add(new DHCPopSERVIP(data, offset));
                        break;
                    case 55:
                        //Error.WriteLine("Got Request List");
                        Options.Add(new DHCPopREQLIST(data, offset));
                        break;
                    case 56:
                        Options.Add(new DHCPopMSGStr(data, offset));
                        break;
                    case 57:
                        //Error.WriteLine("Got Max Message Size");
                        Options.Add(new DHCPopMMSGS(data, offset));
                        break;
                    case 58:
                        //Error.WriteLine("Got Renewal (T1) Time");
                        Options.Add(new DHCPopT1(data, offset));
                        break;
                    case 59:
                        //Error.WriteLine("Got Rebinding (T2) Time");
                        Options.Add(new DHCPopT2(data, offset));
                        break;
                    case 60:
                        //Error.WriteLine("Got Max Message Size");
                        Options.Add(new DHCPopClassID(data, offset));
                        break;
                    case 61:
                        //Error.WriteLine("Got Client ID");
                        Options.Add(new DHCPopClientID(data, offset));
                        break;
                    default:
                        Log_Error("Got Unknown Option " + opKind + " with len " + opLen);
                        break;
                }
                offset += opLen + 2;
                if (offset >= data.Length)
                {
                    Log_Error("Unexpected end of packet");
                    Options.Add(new DHCPopEND());
                    opReadFin = true;
                }
            } while (opReadFin == false);
        }
        public byte[] GetBytes(UInt16 MaxLen)
        {
            //int len = 576; //Min size;
            //We will create a message of the min size and hop it fits.
            //byte[] ret = new byte[240]; //Fixed size section
            byte[] ret = new byte[MaxLen];
            int counter = 0;
            NetLib.WriteByte08(ret, ref counter, OP);
            NetLib.WriteByte08(ret, ref counter, HardwareType);
            NetLib.WriteByte08(ret, ref counter, HardwareAddressLength);
            NetLib.WriteByte08(ret, ref counter, Hops);

            NetLib.WriteUInt32(ret, ref counter, TransactionID);

            NetLib.WriteUInt16(ret, ref counter, Seconds);
            NetLib.WriteUInt16(ret, ref counter, Flags);

            NetLib.WriteByteArray(ret, ref counter, ClientIP);
            NetLib.WriteByteArray(ret, ref counter, YourIP);
            NetLib.WriteByteArray(ret, ref counter, ServerIP);
            NetLib.WriteByteArray(ret, ref counter, GatewayIP);

            NetLib.WriteByteArray(ret, ref counter, ClientHardwareAddress);
            //empty bytes
            NetLib.WriteByteArray(ret, ref counter, new byte[64]);
            NetLib.WriteByteArray(ret, ref counter, new byte[128]);

            NetLib.WriteUInt32(ret, ref counter, MagicCookie);

            //const UInt16 minOpLength = 64;
            //UInt16 OpLength = minOpLength;
            //byte[] retOp = new byte[minOpLength];
            //int opOffset = 0;
            for (int i = 0; i < Options.Count; i++)
            {
                NetLib.WriteByteArray(ret, ref counter, Options[i].GetBytes());
            }

            ////byte[] RetFinal = new byte[OpLength+240];
            //byte[] RetFinal = new byte[MaxLen];
            //Utils.memcpy(ref RetFinal, 0, ret, 0, 240);
            //Utils.memcpy(ref RetFinal, 240, retOp, 0, OpLength);
            //return RetFinal;
            return ret;
        }

        private void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.DHCPPacket, str);
        }
        private void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.DHCPPacket, str);
        }
        private void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.DHCPPacket, str);
        }
    }
}
