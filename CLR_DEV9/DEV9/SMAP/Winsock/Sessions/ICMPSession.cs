using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.IP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;

namespace CLRDEV9.DEV9.SMAP.Winsock.Sessions
{
    class ICMPSession : Session
    {
        Object sentry = new Object();

        List<ICMP> recvBuff = new List<ICMP>();

        List<Ping> pings = new List<Ping>();

        Dictionary<ConnectionKey, Session> connections;

        public ICMPSession(Dictionary<ConnectionKey, Session> parConnections)
            : base(IPAddress.Any)
        {
            connections = parConnections;
        }

        struct PingData
        {
            public byte[] HeaderData;
            public byte[] Data;
        }

        public void PingCompleate(object sender, PingCompletedEventArgs e)
        {
            Log_Verb("Ping Complete");
            PingData seq = (PingData)e.UserState;
            PingReply rep = e.Reply;
            Ping ping = (Ping)sender;

            lock (sentry)
            {
                pings.Remove(ping);
                ping.Dispose();

                switch (rep.Status)
                {
                    case IPStatus.Success:
                        ICMP retICMP = new ICMP(seq.Data);
                        retICMP.HeaderData = seq.HeaderData;
                        retICMP.Type = 0; //echo reply
                        recvBuff.Add(retICMP);
                        break;
                    default:
                        open -= 1;
                        break;
                }
            }
        }

        public override IPPayload Recv()
        {
            //Error.WriteLine("UDP Recive");
            lock (sentry)
            {
                if (recvBuff.Count != 0)
                {
                    ICMP ret;
                    ret = recvBuff[0];
                    recvBuff.RemoveAt(0);
                    open -= 1;
                    return ret;
                }
            }

            return null;
        }
        public override bool Send(IPPayload payload)
        {
            ICMP icmp = (ICMP)payload;

            switch (icmp.Type)
            {
                case 8: //Echo
                    //Code == zero
                    Log_Verb("Send Ping");
                    lock (sentry)
                    {
                        open += 1;
                    }
                    PingData pd;
                    pd.Data = icmp.Data;
                    pd.HeaderData = icmp.HeaderData;
                    Ping nPing = new Ping();
                    nPing.PingCompleted += PingCompleate;
                    lock (sentry)
                    {
                        pings.Add(nPing);
                    }
                    nPing.SendAsync(new IPAddress(DestIP), pd);
                    System.Threading.Thread.Sleep(1); //Hack Fix
                    break;
                case 3: //
                    switch (icmp.Code)
                    {
                        case 3:
                            Log_Error("Recived Packet Rejected, Port Closed");
                            IPPacket retPkt = new IPPacket(icmp);
                            byte[] srvIP = retPkt.SourceIP;
                            byte prot = retPkt.Protocol;
                            UInt16 srvPort = 0;
                            UInt16 ps2Port = 0;
                            switch (prot)
                            {
                                case (byte)IPType.TCP:
                                    TCP tcp = (TCP)retPkt.Payload;
                                    srvPort = tcp.SourcePort;
                                    ps2Port = tcp.DestinationPort;
                                    break;
                                case (byte)IPType.UDP:
                                    UDP udp = (UDP)retPkt.Payload;
                                    srvPort = udp.SourcePort;
                                    ps2Port = udp.DestinationPort;
                                    break;
                            }
                            ConnectionKey Key = new ConnectionKey();
                            Key.IP0 = srvIP[0]; Key.IP1 = srvIP[1]; Key.IP2 = srvIP[2]; Key.IP3 = srvIP[3];
                            Key.Protocol = prot;
                            Key.PS2Port = ps2Port;
                            Key.SRVPort = srvPort;
                            if (connections.ContainsKey(Key)) //TODO, Prevent this from removing permanent sessions
                            {
                                connections[Key].Reset();
                                Log_Info("Reset Rejected Connection");
                            }
                            else
                            {
                                Log_Error("Failed To Reset Rejected Connection");
                            }
                            break;
                        default:
                            throw new NotImplementedException("Unsupported ICMP Code For Destination Unreachable" + icmp.Code);
                    }
                    break;
                default:
                    throw new NotImplementedException("Unsupported ICMP Type" + icmp.Type);
            }

            return true;
        }
        public override void Reset()
        {
            Dispose();
        }

        int open = 0;
        public override bool isOpen()
        {
            lock (sentry)
            {
                return (open > 0);
            }
        }
        public override void Dispose()
        {
            lock (sentry)
            {
                open = 0;
                foreach (Ping ping in pings)
                {
                    ping.SendAsyncCancel();
                    ping.Dispose();
                }
            }
        }

        private void Log_Error(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Error, (int)DEV9LogSources.IMCPSession, str);
        }
        private void Log_Info(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (int)DEV9LogSources.IMCPSession, str);
        }
        private void Log_Verb(string str)
        {
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (int)DEV9LogSources.IMCPSession, str);
        }
    }
}
