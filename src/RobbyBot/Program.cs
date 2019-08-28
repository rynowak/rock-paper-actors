using System;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RobbyBot
{
    public class Program
    {
        public static void Main(string[] args) =>
            CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddHostedService<GameBackgroundService>();
                    services.AddHttpClient<GameClient>(client =>
                    {
                        client.BaseAddress = new Uri(context.Configuration["gamemaster"] ?? "http://localhost:3500/v1.0/actions/gamemaster/");
                    });
                    services.AddHttpClient<MatchMakerClient>(client =>
                    {
                        client.BaseAddress = new Uri(context.Configuration["matchmaker"] ?? "http://localhost:3500/v1.0/actions/matchmaker/");
                    });
                    services.AddSingleton<JsonSerializerOptions>(new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = false,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    });
                });
    }
}