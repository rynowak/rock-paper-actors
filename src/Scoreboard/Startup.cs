using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GameMaster;
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
            services.AddHttpClient<StateClient>(c =>
            {
                c.BaseAddress = new Uri("http://localhost:3500");
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

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/actions/subscribe", async context =>
                {
                    context.Response.ContentType = "application/json";
                    await JsonSerializer.SerializeAsync(context.Response.Body, new[] { "game-complete", }, options: options);
                });

                var random = new Random();
                endpoints.MapPost("/game-complete", async context =>
                {
                    var stateClient = context.RequestServices.GetRequiredService<StateClient>();

                    var game = await JsonSerializer.DeserializeAsync<GameResult>(context.Request.Body, options: options);
                    logger.LogInformation("Processing results of game {GameId}.", game.GameId);
                });
            });
        }
    }
}
