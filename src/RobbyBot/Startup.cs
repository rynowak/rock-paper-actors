using System;
using System.Text.Json;
using Dapr;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RobbyBot
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthChecks();
            
            services.AddHttpClient<GameClient>(client =>
            {
                client.BaseAddress = new Uri($"http://localhost:{Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500"}");
            });

            services.AddSingleton<JsonSerializerOptions>(new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, JsonSerializerOptions options, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCloudEvents();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/healthz");
                endpoints.MapSubscribeHandler();

                var random = new Random();
                endpoints.MapPost("/bot-game-starting", async context =>
                {
                    var gameClient = context.RequestServices.GetRequiredService<GameClient>();

                    var game = await JsonSerializer.DeserializeAsync<GameInfo>(context.Request.Body, options: options);
                    logger.LogInformation("Joined game {GameId}.", game.GameId);

                    // There's no need to observe the results of the game, just make a move.
                    var shape = (Shape)random.Next(3);

                    logger.LogInformation("Playing {Shape} in {GameId} against opponent {OpponentUserName}.", shape, game.GameId, game.Opponent.Username);
                    await gameClient.PlayAsync(game, shape);
                })
                .WithMetadata(new TopicAttribute("bot-game-starting"));
            });
        }
    }
}
