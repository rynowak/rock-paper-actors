using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Hosting;

namespace Rochambot
{
    public class Program
    {
        public static void Main(string[] args) => 
            CreateHostBuilder(args).Build().Run();

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddAzureAppConfiguration(options => 
                    {
                        var cnStr = Environment.GetEnvironmentVariable("AzureAppConfigConnectionString");
                        options.Connect(cnStr);
                    });
                })
                .ConfigureWebHostDefaults(webBuilder => 
                    webBuilder.UseStartup<Startup>());
    }
}   