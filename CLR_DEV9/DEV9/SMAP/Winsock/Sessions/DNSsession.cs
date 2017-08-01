using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.DNS;
using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;

namespace CLRDEV9.DEV9.SMAP.Winsock.Sessions
{
    class DNSsession : Session
    {
        object sentry = new object();
        int open = 0;

        public DNSsession(ConnectionKey parKey, IPAddress parAdapterIP) : base(parKey, parAdapterIP)
        {

        }

        ConcurrentQueue<UDP> _recvBuff = new ConcurrentQueue<UDP>();
        public override IPPayload Recv()
        {
            UDP udp;
            if (_recvBuff.TryDequeue(out udp))
            {
                return udp;
            }
            else
            {
                if (open == 0)
                {
                    RaiseEventConnectionClosed();
                }
                return null;
            }
        }
        public override bool Send(IPPayload payload)
        {
            DNS dns = new DNS(payload.GetPayload());

            if (dns.OPCode == (byte)DNSOPCode.Query & dns.QuestionCount > 0 & dns.QR == false)
            {
                List<string> reqs = new List<string>();
                foreach (DNSQuestionEntry q in dns.Questions)
                {
                    if (q.Type == 1 & q.Class == 1)
                    {
                        reqs.Add(q.Name);
                    }
                    else
                    {
                        Log_Error("Unexpected question type of class, T:" + q.Type.ToString() + " C:" + q.Class.ToString());
                    }
                }
                if (reqs.Count == 0) { return true; }
                lock (sentry)
                {
                    open += 1;
                }
                DNS ret = new DNS();
                ret.ID = dns.ID;
                ret.QR = true;
                ret.OPCode = (byte)DNSOPCode.Query;
                ret.AA = false;
                ret.TC = false;
                ret.RD = true;
                ret.RA = true;
                ret.AD = false;
                ret.CD = false;
                ret.RCode = (byte)DNSRCode.NoError;
                //foreach (string req in reqs)
                //{
                //    Dns.BeginGetHostEntry(req, , null);
                //    IPHostEntry host = Dns.GetHostEntry(req);
                //}
                return true;
            }
            else
            {
                Log_Error("Unexpected OPCode, Code:" + dns.OPCode);
                return true;
            }
        }
        public override void Reset()
        {
            //throw new NotImplementedException();
        }

        public override void Dispose() { }

        protected void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.DNSSession, str);
        }
        protected void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.DNSSession, str);
        }
        protected void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.DNSSession, str);
        }
    }
}
