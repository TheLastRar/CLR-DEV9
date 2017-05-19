using CLRDEV9.DEV9.SMAP.Winsock.PacketReader.DNS;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace DNS_Test_Thing
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Sending");
            //Create DNS Packet
            DNS p = new DNS();
            p.ID = 0;
            p.OPCode = (byte)DNSOPCode.Query;
            p.RD = true;

            p.Questions.Add(new DNSQuestionEntry("www01.kddi-mmbb.jp", 1, 1));

            DNS p2 = new DNS(p.GetBytes());
            Console.WriteLine(); Console.WriteLine(); 
            UdpClient udp = new UdpClient();

            byte[] data = p.GetBytes();

            udp.Send(data, data.Length, new IPEndPoint(new IPAddress(new byte[] { 173, 198, 207, 99 }), 53));
            IPEndPoint retIP = new IPEndPoint(IPAddress.Any, 0);
            Console.WriteLine("Waiting for response");
            data = udp.Receive(ref retIP);
            Console.WriteLine("Recived From " + retIP.Address.ToString());
            p2 = new DNS(data);
            

            if (p2.AnswerCount == 1 && 
                (p2.Answers[0].Data[0] == 173 & p2.Answers[0].Data[1] == 198 & p2.Answers[0].Data[2] == 207 & p2.Answers[0].Data[3] == 99))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Response Seems OK");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("BAD Response");
            }
            Console.ReadKey();
        }
    }
}
