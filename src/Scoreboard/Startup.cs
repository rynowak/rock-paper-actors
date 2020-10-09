using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Scoreboard
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
            
            services.AddDaprClient(client =>
            {
                client.UseJsonSerializationOptions(new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                });
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

                endpoints.MapGet("/get-stats", async context =>
                {
                    var stateClient = context.RequestServices.GetRequiredService<DaprClient>();

                    var records = await stateClient.GetStateAsync<Dictionary<string, PlayerRecord>>("statestore", "records");

                    context.Response.ContentType = "application/json";
                    await JsonSerializer.SerializeAsync(context.Response.Body, records, options: options);
                });

                var random = new Random();
                endpoints.MapPost("/game-complete", async context =>
                {
                    var stateClient = context.RequestServices.GetRequiredService<DaprClient>();

                    var game = await JsonSerializer.DeserializeAsync<GameResult>(context.Request.Body, options: options);
                    logger.LogInformation("Processing results of game {GameId}.", game.GameId);

                    var records = await stateClient.GetStateEntryAsync<Dictionary<string, PlayerRecord>>("statestore", "records");
                    records.Value ??= new Dictionary<string, PlayerRecord>();

                    for (var i = 0; i < game.Players.Length; i++)
                    {
                        var player = game.Players[i];
                        if (player.IsBot)
                        {
                            continue;
                        }

                        if (!records.Value.TryGetValue(player.Username, out var record))
                        {
                            record = new PlayerRecord() { Username = player.Username, };
                            records.Value.Add(player.Username, record);
                        }

                        if (game.IsDraw == true)
                        {
                            record.Draws++;
                        }
                        else if (game.IsVictory(player) == true)
                        {
                            record.Wins++;
                        }
                        else
                        {
                            record.Losses++;
                        }
                    }

                    await records.SaveAsync();
                })
                .WithMetadata(new TopicAttribute("pubsub", "game-complete"));
            });
        }
    }
}
