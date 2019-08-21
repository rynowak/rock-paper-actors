using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RobbyBot
{
    public class GameBackgroundService : BackgroundService
    {
        private readonly GameClient _gameClient;
        private readonly ILogger<GameBackgroundService> _logger;
        private readonly MatchMakerClient _matchMakerClient;
        private readonly Random _random;
        private readonly ConcurrentQueue<Task> _work;

        public GameBackgroundService(GameClient gameClient, MatchMakerClient matchMakerClient, ILogger<GameBackgroundService> logger)
        {
            _gameClient = gameClient;
            _matchMakerClient = matchMakerClient;
            _logger = logger;

            _random = new Random();
            _work = new ConcurrentQueue<Task>(); // Used to drain work on shutdown.
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var user = new UserInfo()
            {
                Username = $"BOT-{Guid.NewGuid()}",
                IsBot = true,
            };

            await Task.Yield();

            while (!stoppingToken.IsCancellationRequested)
            {
                // Remove completed items from the queue.
                while (_work.TryPeek(out var result) && result.IsCompleted)
                {
                    _work.TryDequeue(out _);
                }

                _logger.LogInformation("Waiting for game.");

                GameInfo game;
                try
                {
                    game = await _matchMakerClient.JoinGameAsync(user, stoppingToken);
                } 
                catch(OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Waiting for game timed out.");
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error encountered while waiting for game.", ex);
                    continue;
                }

                _logger.LogInformation("Found game {GameId} against opponent {OpponentUserName}.", game.GameId, game.Opponent.Username);
                _work.Enqueue(PlayGame(game, stoppingToken)); // Fire and forget so we can play again concurrently.
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            while (_work.TryDequeue(out var result))
            {
                try
                {
                    await result;
                }
                catch
                {
                    // we don't care about exceptions, just giving games in a progress a chance to finish.
                }
            }
        }

        private async Task PlayGame(GameInfo game, CancellationToken stoppingToken)
        {
            await Task.Yield();

            try
            {
                // There's no need to observe the results of the game, just make a move.
                var shape = (Shape)_random.Next(3);

                _logger.LogInformation("Playing {Shape} in {GameId} against opponent {OpponentUserName}.", shape, game.GameId, game.Opponent.Username);
                await _gameClient.PlayAsync(game, shape, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception encounterd in game {GameId}", game.GameId);
            }
        }
    }
}