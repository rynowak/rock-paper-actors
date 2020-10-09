using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using Microsoft.Extensions.Logging;

namespace MatchMaker
{
    public class PlayerQueue
    {
        private readonly DaprClient daprClient;
        private readonly ILogger<PlayerQueue> logger;

        public PlayerQueue(DaprClient publishClient, ILogger<PlayerQueue> logger)
        {
            this.daprClient = publishClient;
            this.logger = logger;
        }

        public async ValueTask<GameInfo> GetGameAsync(GameClient gameClient, UserInfo player, CancellationToken cancellationToken)
        {
            logger.LogInformation("User {UserName} is attempting to join a game.", player.Username);
            
            // Assign a random bot to play against the player.
            var bot = new UserInfo()
            {
                Username = "Bot-" + Guid.NewGuid().ToString(),
                IsBot = true,
            };

            var gameId = await CreateGameAsync(gameClient, new[] { player, bot, });
            await daprClient.PublishEventAsync<GameInfo>("pubsub", "bot-game-starting", new GameInfo(){ GameId = gameId, Player = bot, Opponent = player, });

            return new GameInfo(){ GameId = gameId, Player = player, Opponent = bot, };
        }

        private async ValueTask<string> CreateGameAsync(GameClient gameClient, UserInfo[] players)
        {
            // Create a game for both players.
            var gameId = await gameClient.CreateGameAsync(new[]{ players[0], players[1], });
            logger.LogInformation(
                "Made match for {Users} in {GameId}.", 
                string.Join(", ", players.Select(e => e.Username)),
                gameId);

            return gameId;
        }
    }
}