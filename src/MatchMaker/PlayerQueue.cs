using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Actions;
using Microsoft.Extensions.Logging;

namespace MatchMaker
{
    public class PlayerQueue
    {
        private readonly PublishClient _publishClient;
        private readonly ILogger<PlayerQueue> _logger;

        public PlayerQueue(PublishClient publishClient, ILogger<PlayerQueue> logger)
        {
            _publishClient = publishClient;
            _logger = logger;
        }

        public async ValueTask<GameInfo> GetGameAsync(GameClient gameClient, UserInfo player, CancellationToken cancellationToken)
        {
            _logger.LogInformation("User {UserName} is attempting to join a game.", player.Username);
            
            // Assign a random bot to play against the player.
            var bot = new UserInfo()
            {
                Username = "Bot-" + Guid.NewGuid().ToString(),
                IsBot = true,
            };

            var gameId = await CreateGameAsync(gameClient, new[] { player, bot, });
            await _publishClient.PublishAsync("bot-game-starting", new GameInfo(){ GameId = gameId, Player = bot, Opponent = player, });

            return new GameInfo(){ GameId = gameId, Player = player, Opponent = bot, };
        }

        private async ValueTask<string> CreateGameAsync(GameClient gameClient, UserInfo[] players)
        {
            // Create a game for both players.
            var gameId = await gameClient.CreateGameAsync(new[]{ players[0], players[1], });
            _logger.LogInformation(
                "Made match for {Users} in {GameId}.", 
                string.Join(", ", players.Select(e => e.Username)),
                gameId);

            return gameId;
        }
    }
}