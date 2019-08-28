using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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
            services.AddSingleton<PlayerQueue>();
            services.AddHttpClient<GameClient>(client =>
            {
                client.BaseAddress = new Uri(Configuration["gamemaster"] ?? "http://localhost:3500/v1.0/actions/gamemaster/");
            });
            services.AddSingleton<JsonSerializerOptions>(new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, JsonSerializerOptions options)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    var items = new 
                    { 
                        game = "/game",
                    };
                    context.Response.Headers[HeaderNames.ContentType] = "application/json";
                    await JsonSerializer.SerializeAsync(context.Response.Body, items, options);
                });

                endpoints.MapPost("/game", async context =>
                {
                    var user = await JsonSerializer.DeserializeAsync<UserInfo>(context.Request.Body, options);

                    var gameClient = context.RequestServices.GetRequiredService<GameClient>();
                    var queue = context.RequestServices.GetRequiredService<PlayerQueue>();

                    var game = await queue.GetGameAsync(gameClient, user, context.RequestAborted);
                    if (game == null)
                    {
                        // Player hung up.
                        return;
                    }

                    // Signal to the current user that the game is starting
                    context.Response.Headers[HeaderNames.ContentType] = "application/json";
                    await JsonSerializer.SerializeAsync(context.Response.Body, game, options);
                });
            });
        }
    }
}
