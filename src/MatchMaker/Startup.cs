using System;
using System.Text.Json;
using Microsoft.Actions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace MatchMaker
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
            
            services.AddSingleton<PlayerQueue>();
            services.AddHttpClient<GameClient>(client =>
            {
                client.BaseAddress = new Uri($"http://localhost:{Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500"}");
            });
            services.AddHttpClient<PublishClient>(client =>
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

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/healthz");

                endpoints.MapPost("/join", async context =>
                {
                    var user = await JsonSerializer.DeserializeAsync<UserInfo>(context.Request.Body, options);

                    var gameClient = context.RequestServices.GetRequiredService<GameClient>();
                    var queue = context.RequestServices.GetRequiredService<PlayerQueue>();

                    var game = await queue.GetGameAsync(gameClient, user, context.RequestAborted);
                    if (game == null)
                    {
                        // Player hung up.
                        logger.LogInformation("Player {UserId} hung up", user.Username);
                        return;
                    }

                    // Signal to the current user that the game is starting
                    context.Response.Headers[HeaderNames.ContentType] = "application/json";
                    await JsonSerializer.SerializeAsync(context.Response.Body, game, options);
                    logger.LogInformation("Player {UserId} has been connected to game {GameId} ", user.Username, game.GameId);
                });
            });
        }
    }
}
