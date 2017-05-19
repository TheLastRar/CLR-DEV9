using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CLRDEV9.DEV9.SMAP.Winsock.PacketReader.DNS
{
    class DNS
    {
        public UInt16 ID;
        protected byte flags1;
        protected byte flags2;
        #region 'Flags'
        public bool QR
        {
            get { return ((flags1 & (1 << 7)) != 0); }
            set
            {
                if (value) { flags1 |= (1 << 7); }
                else { flags1 &= unchecked((byte)(~(1 << 7))); }
            }
        }
        public byte OPCode
        {
            get { return (byte)((flags1 >> 3) & 0xF); }
            set { flags1 = (byte)((flags1 & ~(0xF << 3)) | ((value & 0xF) << 3)); }
        }
        public bool AA
        {
            get { return ((flags1 & (1 << 2)) != 0); }
            set
            {
                if (value) { flags1 |= (1 << 2); }
                else { flags1 &= unchecked((byte)(~(1 << 2))); }
            }
        }
        public bool TC
        {
            get { return ((flags1 & (1 << 1)) != 0); }
            set
            {
                if (value) { flags1 |= (1 << 1); }
                else { flags1 &= unchecked((byte)(~(1 << 1))); }
            }
        }
        public bool RD
        {
            get { return ((flags1 & 1) != 0); }
            set
            {
                if (value) { flags1 |= 1; }
                else { flags1 &= unchecked((byte)(~1)); }
            }
        }
        public bool RA
        {
            get { return ((flags2 & (1 << 7)) != 0); }
            set
            {
                if (value) { flags2 |= (1 << 7); }
                else { flags2 &= unchecked((byte)(~(1 << 7))); }
            }
        }
        //Reserved 0
        public byte ZO
        {
            get { return ((byte)(flags2 & (1 << 6))); }
            set
            {
                if (value != 0) { flags2 |= (1 << 6); }
                else { flags2 &= unchecked((byte)(~(1 << 6))); }
            }
        }
        public bool AD
        {
            get { return ((flags2 & (1 << 5)) != 0); }
            set
            {
                if (value) { flags2 |= (1 << 5); }
                else { flags2 &= unchecked((byte)(~(1 << 5))); }
            }
        }
        public bool CD
        {
            get { return ((flags2 & (1 << 4)) != 0); }
            set
            {
                if (value) { flags2 |= (1 << 4); }
                else { flags2 &= unchecked((byte)(~(1 << 4))); }
            }
        }
        public byte RCode
        {
            get { return (byte)((flags2) & 0xF); }
            set { flags2 = (byte)((flags2 & ~(0xF)) | ((value & 0xF))); }
        }
        #endregion 'Flags'
        public UInt16 QuestionCount { get { return (UInt16)Questions.Count; } }
        public UInt16 AnswerCount { get { return (UInt16)Answers.Count; } }
        public UInt16 AuthorityCount { get { return (UInt16)Authorities.Count; } } //Other NameServers
        public UInt16 AdditionalCount { get { return (UInt16)Additional.Count; } }

        public List<DNSQuestionEntry> Questions = new List<DNSQuestionEntry>();
        public List<DNSResponseEntry> Answers = new List<DNSResponseEntry>();
        public List<DNSResponseEntry> Authorities = new List<DNSResponseEntry>(); //Other NameServers
        public List<DNSResponseEntry> Additional = new List<DNSResponseEntry>();

        public DNS()
        {

        }
        public DNS(byte[] data)
        {
            int offset = 0;
            //Bits 0-31 //Bytes 0-3
            NetLib.ReadUInt16(data, ref offset, out ID);
            Log_Info("ID " + ID);
            NetLib.ReadByte08(data, ref offset, out flags1);
            NetLib.ReadByte08(data, ref offset, out flags2);
            Log_Info("Is Response " + QR);
            Log_Info("OpCode " + (DNSOPCode)OPCode);
            Log_Info("Is Authoritative (not cached) " + AA);
            Log_Info("Is Truncated " + TC);
            Log_Info("Recursion Desired " + RD);
            Log_Info("Recursion Available " + RA);
            Log_Info("Zero " + ZO);
            Log_Info("Authenticated Data? " + AD);
            Log_Info("Checking Disabled? " + CD);
            Log_Info("Result " + (DNSRCode)RCode);
            //Bits 32-63 //Bytes 4-7
            UInt16 qCount;
            UInt16 aCount;
            UInt16 auCount;
            UInt16 adCount;
            NetLib.ReadUInt16(data, ref offset, out qCount);
            Log_Info("QuestionCount " + qCount);
            NetLib.ReadUInt16(data, ref offset, out aCount);
            Log_Info("AnswerCount " + aCount);
            //Bits 64-95 //Bytes 8-11
            NetLib.ReadUInt16(data, ref offset, out auCount);
            Log_Info("Authority Count " + auCount);
            NetLib.ReadUInt16(data, ref offset, out adCount);
            Log_Info("Additional Count " + adCount);
            //Bits 96+   //Bytes 8+
            for (int i = 0; i < qCount; i++)
            {
                DNSQuestionEntry entry = new DNSQuestionEntry(data, offset);
                Log_Info("Q" + i + " Name " + entry.Name);
                Log_Info("Q" + i + " Type " + entry.Type);
                Log_Info("Q" + i + " Class " + entry.Class);
                Log_Info("Q" + i + " Length " + entry.Length);
                offset += entry.Length;
                Questions.Add(entry);
            }
            for (int i = 0; i < aCount; i++)
            {
                DNSResponseEntry entry = new DNSResponseEntry(data, offset);
                Log_Info("Ans" + i + " Name " + entry.Name);
                Log_Info("Ans" + i + " Type " + entry.Type);
                Log_Info("Ans" + i + " Class " + entry.Class);
                Log_Info("Ans" + i + " TTL " + entry.TTL);
                string str = "";
                for (int y = 0; y < entry.Data.Length; y++)
                {
                    str += entry.Data[y] + ":";
                }
                Log_Info("Ans" + i + " Data " + str.Substring(0, str.Length - 1));
                Log_Info("ANS" + i + " Length " + entry.Length);
                offset += entry.Length;
                Answers.Add(entry);
            }
            for (int i = 0; i < auCount; i++)
            {
                DNSResponseEntry entry = new DNSResponseEntry(data, offset);
                Log_Info("Auth" + i + " Name " + entry.Name);
                Log_Info("Auth" + i + " Type " + entry.Type);
                Log_Info("Auth" + i + " Class " + entry.Class);
                Log_Info("Auth" + i + " TTL " + entry.TTL);
                string str = "";
                for (int y = 0; y < entry.Data.Length; y++)
                {
                    str += entry.Data[y] + ":";
                }
                Log_Info("Auth" + i + " Data " + str.Substring(0, str.Length - 1));
                Log_Info("Auth" + i + " Length " + entry.Length);
                offset += entry.Length;
                Authorities.Add(entry);
            }
            for (int i = 0; i < adCount; i++)
            {
                DNSResponseEntry entry = new DNSResponseEntry(data, offset);
                Log_Info("Add" + i + " Name " + entry.Name);
                Log_Info("Add" + i + " Type " + entry.Type);
                Log_Info("Add" + i + " Class " + entry.Class);
                Log_Info("Add" + i + " TTL " + entry.TTL);
                string str = "";
                for (int y = 0; y < entry.Data.Length; y++)
                {
                    str += entry.Data[y] + ":";
                }
                Log_Info("Add" + i + " Data " + str.Substring(0, str.Length - 1));
                Log_Info("Add" + i + " Length " + entry.Length);
                offset += entry.Length;
                Additional.Add(entry);
            }
        }
        public byte[] GetBytes()
        {
            //Calc length
            int length = 2 * 2 + 4 * 2;

            for (int i = 0; i < Questions.Count; i++)
            {
                length += Questions[i].Length;
            }
            for (int i = 0; i < Answers.Count; i++)
            {
                length += Answers[i].Length;
            }
            for (int i = 0; i < Authorities.Count; i++)
            {
                length += Authorities[i].Length;
            }
            for (int i = 0; i < Additional.Count; i++)
            {
                length += Additional[i].Length;
            }

            byte[] ret = new byte[length];
            int counter = 0;
            NetLib.WriteUInt16(ref ret, ref counter, ID);
            NetLib.WriteByte08(ref ret, ref counter, flags1);
            NetLib.WriteByte08(ref ret, ref counter, flags2);
            NetLib.WriteUInt16(ref ret, ref counter, (ushort)Questions.Count);
            NetLib.WriteUInt16(ref ret, ref counter, (ushort)Answers.Count);
            NetLib.WriteUInt16(ref ret, ref counter, (ushort)Authorities.Count);
            NetLib.WriteUInt16(ref ret, ref counter, (ushort)Additional.Count);

            for (int i = 0; i < Questions.Count; i++)
            {
                NetLib.WriteByteArray(ref ret, ref counter, Questions[i].GetBytes());
            }
            for (int i = 0; i < Answers.Count; i++)
            {
                NetLib.WriteByteArray(ref ret, ref counter, Answers[i].GetBytes());
            }
            for (int i = 0; i < Authorities.Count; i++)
            {
                NetLib.WriteByteArray(ref ret, ref counter, Authorities[i].GetBytes());
            }
            for (int i = 0; i < Additional.Count; i++)
            {
                NetLib.WriteByteArray(ref ret, ref counter, Additional[i].GetBytes());
            }
            return ret;
        }

        private void Log_Error(string str)
        {
            //PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.DNSPacket, str);
            Console.Error.WriteLine(str);
        }
        private void Log_Info(string str)
        {
            //PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.DNSPacket, str);
            Console.WriteLine(str);
        }
        private void Log_Verb(string str)
        {
            //PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.DNSPacket, str);
            Console.WriteLine(str);
        }
    }
}
