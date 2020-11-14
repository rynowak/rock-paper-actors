using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Player;

namespace Frontend
{
    public class GameStateService
    {
        private ConcurrentDictionary<string, TaskCompletionSource<GameInfo>> games;
        private ConcurrentDictionary<string, TaskCompletionSource<GameResult>> results;
        private ILogger<GameStateService> logger;

        public GameStateService(ILogger<GameStateService> logger)
        {
            this.logger = logger;

            games = new ConcurrentDictionary<string, TaskCompletionSource<GameInfo>>();
            results = new ConcurrentDictionary<string, TaskCompletionSource<GameResult>>();
        }

        public async Task<GameInfo> GetReadyGameAsync(string playerId, CancellationToken cancellationToken)
        {
            logger.LogInformation("Waiting to join game of game {PlayerId}", playerId);

            var entry = new TaskCompletionSource<GameInfo>();
            using (cancellationToken.Register(Cancel))
            {
                games.TryAdd(playerId, entry);

                var state = await entry.Task;
                games.TryRemove(playerId, out _);

                logger.LogInformation("Player {PlayerId} has joined Game {GameId}", playerId, state.GameId);
                return state;
            }

            void Cancel()
            {
                logger.LogInformation("Canceling wait for game {PlayerId}", playerId);
                games.TryRemove(playerId, out _);
                entry.TrySetCanceled(cancellationToken);
            }
        }

        public async Task<GameResult> GetCompletedGameAsync(string gameId, CancellationToken cancellationToken)
        {
            logger.LogInformation("Waiting for completion of game {GameId}", gameId);

            var entry = new TaskCompletionSource<GameResult>();
            using (cancellationToken.Register(Cancel))
            {
                results.TryAdd(gameId, entry);

                var state = await entry.Task;
                results.TryRemove(gameId, out _);

                logger.LogInformation("Game {GameId} is completed", gameId);
                return state;
            }

            void Cancel()
            {
                logger.LogInformation("Canceling game {GameId}", gameId);
                games.TryRemove(gameId, out _);
                entry.TrySetCanceled(cancellationToken);
            }
        }

        public void Complete(GameInfo game)
        {
            if (games.TryGetValue(game.Player.Player.ActorId.GetId(), out var entry))
            {
                entry.TrySetResult(game);
            }
        }

        public void Complete(GameResult result)
        {
            if (results.TryGetValue(result.Info.GameId, out var entry))
            {
                entry.TrySetResult(result);
            }
        }
    }
}