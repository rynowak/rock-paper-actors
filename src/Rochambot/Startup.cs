using System;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Rochambot
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }

        public IHostEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                })
                .AddDapr();

            services.AddHealthChecks();
            
            services.AddRazorPages();
            services.AddServerSideBlazor(options =>
            {
                options.DetailedErrors = Environment.IsDevelopment();
            });

            services.AddScoped<AuthenticationStateProvider, NameAuthenticationStateProvider>();

            services.AddScoped<GameService>();
            services.AddSingleton<GameStateService>();

            services.AddSingleton<JsonSerializerOptions>(new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });

            services.AddHttpClient<GameClient>(client =>
            {
                client.BaseAddress = new Uri($"http://localhost:{System.Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500"}");
            });
            services.AddHttpClient<MatchMakerClient>(client =>
            {
                client.BaseAddress = new Uri($"http://localhost:{System.Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500"}");
            });
            services.AddHttpClient<ScoreboardClient>(client =>
            {
                client.BaseAddress = new Uri($"http://localhost:{System.Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500"}");
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseCloudEvents();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapSubscribeHandler();
                endpoints.MapHealthChecks("/healthz");
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
