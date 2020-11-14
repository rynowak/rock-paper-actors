using System;
using System.Text.Json;
using Dapr;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BotPlayer
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

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/healthz");
            });
        }
    }
}
