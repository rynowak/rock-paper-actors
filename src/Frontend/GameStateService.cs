using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Frontend
{
    public class GameStateService
    {
        private ConcurrentDictionary<string, Entry> entries;
        private ILogger<GameStateService> logger;

        public GameStateService(ILogger<GameStateService> logger)
        {
            entries = new ConcurrentDictionary<string, Entry>();
            this.logger = logger;
        }

        public async Task<GameResult> GetCompletedGameAsync(string gameId, CancellationToken cancellationToken)
        {
            logger.LogInformation("Waiting for completion of game {GameId}", gameId);

            var entry = new Entry(gameId);
            using (cancellationToken.Register(Cancel))
            {
                entries.TryAdd(gameId, entry);

                var state = await entry.Completion.Task;
                entries.TryRemove(gameId, out _);

                logger.LogInformation("Game {GameId} is completed", gameId);
                return state;
            }

            void Cancel()
            {
                logger.LogInformation("Canceling game {GameId}", gameId);
                entries.TryRemove(gameId, out _);
                entry.Completion.TrySetCanceled(cancellationToken);
            }
        }

        public void Complete(GameResult result)
        {
            if (entries.TryGetValue(result.GameId, out var entry))
            {
                logger.LogInformation("Completing game {GameId}", result.GameId);
                entry.Completion.TrySetResult(result);
            }
        }

        private class Entry
        {
            public Entry(string gameId)
            {
                GameId = gameId;

                Completion = new TaskCompletionSource<GameResult>();
            }

            public string GameId { get; }

            public TaskCompletionSource<GameResult> Completion { get; }
        }
    }
}