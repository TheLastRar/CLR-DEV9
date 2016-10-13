using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UPNP_Test_Thing
{
    class Program
    {
        static void Main(string[] args)
        {
            Encoding targetEncoding = Encoding.ASCII;
            UdpClient c = new UdpClient(1900);

            IPEndPoint sp = new IPEndPoint(new IPAddress(new byte[] { 239, 255, 255, 250 }),1900);

            c.JoinMulticastGroup(new IPAddress(new byte[] { 239, 255, 255, 250 }));
            //byte[] payload = targetEncoding.GetBytes("M-SEARCH * HTTP/1.1\r\nHOST: 239.255.255.250:1900\r\nMAN: \"ssdp:discover\"\r\nMX: 2\r\nST: urn:schemas-upnp-org:service:InternetGatewayDevice:1\r\n\r\n");
            //c.Send(payload, payload.Length, sp);

            while (true)
            {
                IPEndPoint rp = new IPEndPoint(new IPAddress(new byte[] { 192, 168, 1, 75 }), 1900);

                byte[] ret = c.Receive(ref rp);
                Console.WriteLine(rp.ToString());
                Console.WriteLine(targetEncoding.GetString(ret));
            }
        }
    }
}
