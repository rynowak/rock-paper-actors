using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapr.Actors.AspNetCore;
using Dapr.Actors.Runtime;
using Dapr.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Json;

namespace HumanPlayer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddDaprClient();
                    services.RegisterActorWithDI<HumanPlayerActor>();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseActors(actors => { });
                })
                .UseSerilog((hostingContext, loggerConfiguration) => 
                {
                    loggerConfiguration
                    .ReadFrom.Configuration(hostingContext.Configuration)
                    .Enrich.FromLogContext()
                    .WriteTo.Console(formatter: new JsonFormatter(renderMessage: true));
                });
    }
}
