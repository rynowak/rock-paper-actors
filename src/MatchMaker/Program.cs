using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using System;

namespace MatchMaker
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
                .ConfigureServices(services => services.AddHostedService<MatchMaker>());
    }
}