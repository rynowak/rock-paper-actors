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
            services.AddHttpClient<GameClient>(client =>
            {
                client.BaseAddress = new Uri(Configuration["gamemaster"] ?? "http://gamemaster/");
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
                endpoints.MapGet("/", async context =>
                {
                    var items = new 
                    { 
                        game = "/game",
                    };
                    context.Response.Headers[HeaderNames.ContentType] = "application/json";
                    await JsonSerializer.SerializeAsync(context.Response.Body, items, options);
                });

                var bots = new ConcurrentQueue<QueueEntry>();
                var players = new ConcurrentQueue<QueueEntry>();
                endpoints.MapPost("/game", async context =>
                {
                    var user = await JsonSerializer.DeserializeAsync<UserInfo>(context.Request.Body, options);
                    logger.LogInformation("User {UserName} is attempting to join a game.", user.Username);

                    // Logic is the same for bots and players, but the queues are flipped.
                    var (queue, opponentQueue) = user.IsBot ? (bots, players) : (players, bots);
                    var gameClient = context.RequestServices.GetRequiredService<GameClient>();

                    // Are there any opponents waiting?
                    if (opponentQueue.TryDequeue(out var opponent))
                    {
                        logger.LogInformation("Found opponent {OpponentUserName} in queue.", opponent.User.Username);

                        // Create a game for both players.
                        var gameId = await gameClient.CreateGameAsync(new[]{ user, opponent.User, });
                        logger.LogInformation("Created game {GameId}.", gameId);

                        // Signal to the waiting user that the game is starting
                        opponent.Completion.SetResult(new GameInfo()
                        {
                            GameId = gameId,
                            Player = opponent.User,
                            Opponent = user,
                        });

                        // Signal to the current user that the game is starting
                        context.Response.Headers[HeaderNames.ContentType] = "application/json";
                        await JsonSerializer.SerializeAsync(context.Response.Body, new GameInfo()
                        {
                            GameId = gameId,
                            Player = user,
                            Opponent = opponent.User,
                        }, options);

                        logger.LogInformation(
                            "Made match for {UserName} against opponent {OpponentUserName} in {GameId}.", 
                            user.Username, 
                            opponent.User.Username,
                            gameId);

                    }
                    else
                    {
                        logger.LogInformation("No opponent available.");

                        // No players are waiting, join the queue and wait for the result.
                        var entry = new QueueEntry(user);
                        bots.Enqueue(entry);

                        var game  = await entry.Completion.Task;
                        
                        // Found an opponent
                        context.Response.Headers[HeaderNames.ContentType] = "application/json";
                        await JsonSerializer.SerializeAsync(context.Response.Body, game, options);

                        logger.LogInformation(
                            "Made match for {UserName} againse opponent {OpponentUserName} in {GameId}.", 
                            user.Username, 
                            game.Opponent.Username,
                            game.GameId);
                    }
                });
            });
        }

        private class QueueEntry
        {
            public QueueEntry(UserInfo user)
            {
                User = user;
                Completion = new TaskCompletionSource<GameInfo>();
            }

            public UserInfo User { get; set; }

            public TaskCompletionSource<GameInfo> Completion { get; }
        }
    }
}
