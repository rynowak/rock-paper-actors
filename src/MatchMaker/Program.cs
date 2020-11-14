using Dapr.Actors.AspNetCore;
using Dapr.Actors.Runtime;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Json;

namespace MatchMaker
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
                    services.RegisterActorWithDI<MatchMakerActor>();
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
