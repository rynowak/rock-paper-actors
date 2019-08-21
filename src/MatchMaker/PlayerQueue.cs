using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MatchMaker
{
    public class PlayerQueue
    {
        private readonly ILogger<PlayerQueue> _logger;

        private readonly ConcurrentQueue<QueueEntry> _bots;
        private readonly ConcurrentQueue<QueueEntry> _players;

        public PlayerQueue(ILogger<PlayerQueue> logger)
        {
            _logger = logger;

            _bots = new ConcurrentQueue<QueueEntry>();
            _players = new ConcurrentQueue<QueueEntry>();
        }

        public async Task<GameInfo> GetGameAsync(GameClient gameClient, UserInfo user, CancellationToken cancellationToken)
        {
            _logger.LogInformation("User {UserName} is attempting to join a game.", user.Username);
            var player = new QueueEntry(user);

            // Logic is the same for bots and players, but the queues are flipped.
            var (queue, opponentQueue) = user.IsBot ? (_bots, _players) : (_players, _bots);
            while (opponentQueue.TryDequeue(out var opponent))
            {
                if (!opponent.Completion.Task.IsCanceled)
                {
                    _logger.LogInformation("Found opponent {OpponentUserName} in queue.", opponent.User.Username);

                    await CreateGameAsync(gameClient, new[] { player, opponent, });
                    return await player.Completion.Task;
                }

                // keep draining the queue if these opponents cancelled.
            }

            // No players are waiting, join the queue and wait for the result.
            _logger.LogInformation("No opponent available.");
            queue.Enqueue(player);

            return await player.Completion.Task;
        }

        private async Task CreateGameAsync(GameClient gameClient, QueueEntry[] entries)
        {
            // Create a game for both players.
            var gameId = await gameClient.CreateGameAsync(new[]{ entries[0].User, entries[1].User, });
            _logger.LogInformation("Created game {GameId}.", gameId);

            // Signal to both players that the game is starting.
            entries[0].Completion.SetResult(new GameInfo()
            {
                GameId = gameId,
                Player = entries[0].User,
                Opponent = entries[1].User,
            });

            entries[1].Completion.SetResult(new GameInfo()
            {
                GameId = gameId,
                Player = entries[1].User,
                Opponent = entries[0].User,
            });

            _logger.LogInformation(
                "Made match for {Users} in {GameId}.", 
                string.Join(", ", entries.Select(e => e.User.Username)),
                gameId);
        }
    }
}