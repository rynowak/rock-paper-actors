using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Hosting;
using System;

namespace GameMaster
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
                .ConfigureServices(services => 
                {
                    services.AddHostedService<GameMaster>();
                    services.AddSingleton<GameData>();
                });
    }
}