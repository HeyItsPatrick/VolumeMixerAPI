using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QRCoder;
using System;
using System.Net;
using System.Net.Sockets;

namespace VolumeMixerAPISelfHost
{
    class Program
    {
        static void Main()
        {
            string localIP = "";
            var hostName = Dns.GetHostName();
            foreach (var item in Dns.GetHostEntry(hostName).AddressList)
            {
                if (item.AddressFamily == AddressFamily.InterNetwork)
                    localIP = item.ToString();
            }

            //TcpListener finds a free port for us, so there is no risk of conflicting with another application
            TcpListener server = new TcpListener(IPAddress.Loopback, 0);
            server.Start();
            string localPort = ((IPEndPoint)server.LocalEndpoint).Port.ToString();
            server.Stop();

            Console.WindowHeight = 40;
            Console.WriteLine("Host:\t\t" + hostName);
            Console.WriteLine("IP Address:\t" + localIP);
            Console.WriteLine("Port:\t\t" + localPort);
            Console.WriteLine();

            var generator = new QRCodeGenerator();
            var data = generator.CreateQrCode(localIP + ":" + localPort, QRCodeGenerator.ECCLevel.Q);
            var code = new AsciiQRCode(data);
            var image = code.GetGraphic(1);
            Console.WriteLine(image);
            Console.WriteLine();


            WebHost.CreateDefaultBuilder()
                .UseUrls($"http://{localIP}:{localPort}")
                .ConfigureServices(services => services.AddMvc())
                .ConfigureLogging(log => log.ClearProviders())
                .Configure(app => { app.UseMvc(); app.UseCors(c => c.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()); })
                .Build()
                .Run();
        }
    }
}
