//using CLRDEV9.DEV9.SMAP.Winsock.PacketReader;
//using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.DNS;
//using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Net;
//using System.Text;
//using System.Windows.Forms;

//namespace CLRDEV9.Config.Test.Eth
//{
//    class DNSTest
//    {
//        //TODO finish
//        public byte[] GetIP(SMAP_Test smap, string address, Action<SMAP_Test, IPPayload> SendIP, Func<SMAP_Test, int, EtherFrameType, byte, uint, EthernetFrame> GetFrameOfType)
//        {
//            ushort xID = (ushort)(new Random()).Next();

//            Log_Info("Sending DNS Lookup for " + address);

//            DNS dns = new DNS();
//            dns.ID = xID;
//            dns.OPCode = (byte)DNSOPCode.Query;
//            dns.RD = true;

//            dns.Questions.Add(new DNSQuestionEntry(address, 1, 1));

//            UDP uP = new UDP(dns.GetBytes());
//            uP.DestinationPort = 53;
//            uP.SourcePort = 51234;

//            int tries = 0;
//            DNS dnsret = null;
//            while (tries < 3)
//            {
//                tries++;
//                SendIP(smap, uP);
//                EthernetFrame retef = GetFrameOfType(smap, 10, EtherFrameType.IPv4, (byte)IPType.UDP, 68);
//                while (retef != null)
//                {
//                    //Must match TransactionID
//                    dnsret = new DNS(((UDP)((IPPacket)retef.Payload).Payload).GetPayload());
//                    if (dnsret.ID == xID)
//                    {
//                        break;
//                    }
//                    dnsret = null;
//                    retef = GetFrameOfType(smap, 1, EtherFrameType.IPv4, (byte)IPType.UDP, 68);
//                }
//                if (dnsret != null) { break; }
//            }

//            if (dnsret != null)
//            {
//                MessageBox.Show("DNS Lookup Failed (No Reply)");
//                Log_Error("DNS Lookup Failed (No Reply)");
//                return null;
//            }

//            Log_Info("Got DNS Reply");

//            if (dnsret.RCode != (byte)DNSRCode.NoError)
//            {
//                MessageBox.Show("DNS Lookup Failed (" + ((DNSRCode)dnsret.RCode).ToString() + ")");
//                Log_Error("DNS Lookup Failed (" + ((DNSRCode)dnsret.RCode).ToString() + ")");
//                return null;
//            }

//            if (dnsret.AnswerCount == 1)
//            {
//                Log_Info("IP: " + (new IPAddress(dnsret.Answers[0].Data)).ToString());
//                return dnsret.Answers[0].Data;
//            }

//            Log_Info("Got Invalid DNS Reply");

//            return null;
//        }

//        private void Log_Error(string str)
//        {
//            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.Test, str);
//        }
//        private void Log_Info(string str)
//        {
//            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.Test, str);
//        }
//        private void Log_Verb(string str)
//        {
//            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.Test, str);
//        }
//    }
//}
