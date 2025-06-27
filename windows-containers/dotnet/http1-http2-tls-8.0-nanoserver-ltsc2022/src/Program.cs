using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace dotnetapp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(serverOptions =>
                    {
                        serverOptions.ListenAnyIP(port: 443, listenOptions =>
                        {
                            var cert = new X509Certificate2("app.pfx", "changeit", X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

                            listenOptions.UseHttps(cert);
                            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                        });
                    });

                    webBuilder.UseStartup<Startup>();
                });
    }
}
