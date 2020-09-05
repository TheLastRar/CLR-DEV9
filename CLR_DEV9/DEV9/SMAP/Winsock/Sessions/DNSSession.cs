using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.DNS;
using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CLRDEV9.DEV9.SMAP.Winsock.Sessions
{
    class UDP_DNSSession : Session
    {
        public class DNSState
        {
            private int counter;
            public readonly string[] Questions;
            public readonly DNS DNS;
            public readonly UInt16 ClientPort;

            private ConcurrentDictionary<string, byte[]> answers = new ConcurrentDictionary<string, byte[]>();

            public DNSState(int count, string[] questions, DNS dns, UInt16 port)
            {
                DNS = dns;
                counter = count;
                Questions = questions;
                ClientPort = port;
            }

            public int DecCounter()
            {
                return Interlocked.Decrement(ref counter);
            }

            public void AddAnswer(string answer, byte[] address)
            {
                if (!answers.TryAdd(answer, address))
                {
                    throw new Exception("Answer Add Failed");
                }
            }

            public ConcurrentDictionary<string, byte[]> GetAnswers()
            {
                if (Interlocked.CompareExchange(ref counter, 0, 0) != 0)
                    throw new InvalidOperationException("Counter not zero");
                return answers;
            }
        }

        byte[] localhostIP;
        Dictionary<string, byte[]> hosts;
        ConcurrentQueue<UDP> _recvBuff = new ConcurrentQueue<UDP>();

        object errSentry = new object();
        Exception lastTaskError = null;

        public UDP_DNSSession(ConnectionKey parKey, Dictionary<string, byte[]> parHosts, byte[] parLocalhostIP) : base(parKey, IPAddress.Any)
        {
            hosts = parHosts;
            localhostIP = parLocalhostIP;
        }

        public override IPPayload Recv()
        {
            UDP udp;

            lock (errSentry)
            {
                if (lastTaskError != null)
                    throw lastTaskError;
            }

            //Check for DNS replies
            if (_recvBuff.TryDequeue(out udp))
                return udp;
            else
                return null;
        }

        public override bool Send(IPPayload payload)
        {
            Log_Info("DNS Packet Sent To CLR_DEV9 DNS server");
            Log_Info("Contents");
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
                if (dns.TC == true) { throw new NotImplementedException("Truncated DNS packet"); }
                //Interlocked.Increment(ref open);
                DNS ret = new DNS
                {
                    ID = dns.ID, //TODO, drop duplicate requests based on ID
                    QR = true,
                    OPCode = (byte)DNSOPCode.Query,
                    AA = false,
                    TC = false,
                    RD = true,
                    RA = true,
                    AD = false,
                    CD = false,
                    RCode = (byte)DNSRCode.NoError,
                    //Counts
                    Questions = dns.Questions
                };
                DNSState state = new DNSState(reqs.Count, reqs.ToArray(), ret, ((UDP)payload).SourcePort);

                foreach (string req in reqs)
                {
                    if (CheckHost(req, state))
                        continue;
                    Task<bool> res = GetHost(req, state);
                }
                return true;
            }
            else
            {
                Log_Error("Unexpected OPCode, Code:" + dns.OPCode);
                return true;
            }
        }

        public bool CheckHost(string url, DNSState state)
        {
            if (hosts.ContainsKey(url.ToLower()))
            {
                state.AddAnswer(url, hosts[url]);
                Log_Info(url + " found in hosts");
                //Add entry to DNS state
                if (state.DecCounter() == 0)
                    FinDNS(state);
                return true;
            }
            return false;
        }

        public async Task<bool> GetHost(string url, DNSState state)
        {
            try
            {
                IPHostEntry ret = await Dns.GetHostEntryAsync(url).ConfigureAwait(false);
                Log_Info("Success Lookup of " + url);
                state.AddAnswer(url, ret.AddressList[0].GetAddressBytes());
            }
            catch (SocketException ex)
            {
                Log_Error("Failed Lookup of " + url);
                if (ex.NativeErrorCode != 11001 & //Host not found (Authoritative)
                    ex.NativeErrorCode != 11002)  //Host not found (Non-Authoritative)
                {
                    Log_Error("Unexpected error of " + ex.NativeErrorCode);
                    lock (errSentry)
                        lastTaskError = ex;
                    return false;
                }
            }
            if (state.DecCounter() == 0)
                FinDNS(state);

            return true;
        }

        public void FinDNS(DNSState state)
        {
            Log_Info("DNS Packet Sent From CLR_DEV9 DNS server");
            DNS retPay = state.DNS;
            string[] reqs = state.Questions;
            ConcurrentDictionary<string, byte[]> answers = state.GetAnswers();

            foreach (string req in reqs)
            {
                if (answers.ContainsKey(req))
                {
                    byte[] retIP = answers[req];
                    if (Utils.memcmp(retIP, 0, new byte[] { 127, 0, 0, 1 }, 0, 4))
                        retIP = localhostIP;

                    DNSResponseEntry ans = new DNSResponseEntry(req, 1, 1, retIP, 10800);
                    retPay.Answers.Add(ans);
                }
                else
                {
                    //Log_Error("Missing an Answer");
                    retPay.RCode = 2; //ServerFailure
                }
            }
            byte[] udpPayload = retPay.GetBytes();
            if (udpPayload.Length > 512)
                throw new NotImplementedException("DNS packet larger than 512bytes");

            UDP retudp = new UDP(udpPayload);
            retudp.SourcePort = 53;
            retudp.DestinationPort = state.ClientPort;

            _recvBuff.Enqueue(retudp);
            //Interlocked.Decrement(ref open);
        }

        public override void Reset()
        {
            //Needs to kill all requests?
            //Some games spam DNS requests and then reject replies after the 1st message
            //This will result in a call to Reset, however, this session can handle multiple
            //seperate DNS requests, so just eat reset requests to play it safe
        }

        public override void Dispose()
        {
            //DNS requests can't be cancelled
            //We could wait on all running tasks to ensure
            //DHSsession is idle, but that would require
            //tracking all running tasks
        }

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
