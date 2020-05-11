using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            string localPort = "8080"; 
            var hostName = Dns.GetHostName();
            foreach (var item in Dns.GetHostEntry(hostName).AddressList)
            {
                if (item.AddressFamily == AddressFamily.InterNetwork)
                    localIP = item.ToString();
            }

            Console.WriteLine("Host:\t\t" + hostName);
            Console.WriteLine("IP Address:\t" + localIP);
            Console.WriteLine("Port:\t\t" +localPort);
            Console.WriteLine();

            WebHost.CreateDefaultBuilder()
                .UseUrls($"http://{localIP}:{localPort}")
                .ConfigureServices(services=> services.AddMvc())
                .ConfigureLogging(log=>log.ClearProviders())
                .Configure(app => { app.UseMvc(); app.UseCors(c => c.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()); })
                .Build()
                .Run();
        }
    }
}
