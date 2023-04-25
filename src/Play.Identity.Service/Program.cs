using System;
using Azure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Play.Identity.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((ctx, cfgBuilder) =>
                {
                    if (ctx.HostingEnvironment.IsProduction())
                    {
                        var uri = new Uri("https://playeconomy1384.vault.azure.net/");
                        var credentials = new DefaultAzureCredential();
                        cfgBuilder.AddAzureKeyVault(uri, credentials);
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
