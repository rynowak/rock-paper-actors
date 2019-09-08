using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Rochambot
{
    public class GameStateService
    {
        private ConcurrentDictionary<string, Entry> _entries;
        private ILogger<GameStateService> _logger;

        public GameStateService(ILogger<GameStateService> logger)
        {
            _entries = new ConcurrentDictionary<string, Entry>();
            _logger = logger;
        }

        public async Task<GameResult> GetCompletedGameAsync(string gameId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Waiting for completion of game {GameId}", gameId);

            var entry = new Entry(gameId);
            using (cancellationToken.Register(Cancel))
            {
                _entries.TryAdd(gameId, entry);

                var state = await entry.Completion.Task;
                _entries.TryRemove(gameId, out _);

                _logger.LogInformation("Game {GameId} is completed", gameId);
                return state;
            }

            void Cancel()
            {
                _logger.LogInformation("Canceling game {GameId}", gameId);
                _entries.TryRemove(gameId, out _);
                entry.Completion.TrySetCanceled(cancellationToken);
            }
        }

        public void Complete(GameResult result)
        {
            if (_entries.TryGetValue(result.GameId, out var entry))
            {
                _logger.LogInformation("Completing game {GameId}", result.GameId);
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