using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Actors.Runtime;
using Microsoft.Extensions.Logging;
using Player;

namespace GameMaster
{
    public class GameActor : Actor, IGameActor, IRemindable
    {
        private readonly ILogger logger;

        public GameActor(ActorService actorService, ActorId actorId, ILogger<GameActor> logger) 
            : base(actorService, actorId)
        {
            this.logger = logger;
        }

        public async Task InitializeAsync(PlayerInfo[] players)
        {
            if (players.Length != 2)
            {
                throw new InvalidOperationException("Two players are required.");
            }

            var state = new GameState()
            {
                GameId = this.Id.GetId(),
                Players = players,
                Moves = new Dictionary<string, Shape?>()
                {
                    { players[0].Username, null },
                    { players[1].Username, null },
                },
            };

            await StateManager.AddStateAsync("state", state);

            foreach (var (player, view) in state.GetViews())
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(view.Info);
                await RegisterReminderAsync("notify-game-ready", bytes, TimeSpan.Zero, Timeout.InfiniteTimeSpan);
            }
        }

        public async Task PlayMoveAsync(PlayerInfo player, Player.Shape move)
        {
            var state = await StateManager.GetStateAsync<GameState>("state");

            if (!state.Players.Any(p => p.Username == player.Username))
            {
                logger.LogInformation("Player {UserId} is not part of game {GameId}.", player.Username, state.GameId);
                throw new InvalidOperationException("Player is not part of this game.");
            }

            if (state.Moves[player.Username] != null)
            {
                logger.LogInformation("Player {UserId} has already made a move in {GameId}.", player.Username, state.GameId);
                throw new InvalidOperationException("Player has already made a move.");
            }

            logger.LogInformation("Player {UserId} has make move {Move} in {GameId}.", player.Username, move, state.GameId);
            state.Moves[player.Username] = move;
            if (state.Moves.All(kvp => kvp.Value != null))
            {
                var (shape0, shape1) = (state.Moves[state.Players[0].Username], state.Moves[state.Players[1].Username]);
                if (shape0 == shape1)
                {
                    // Draw
                    logger.LogInformation("Game {GameId} is a draw.", state.GameId);
                } 
                else if ((((int)shape0 - (int)shape1) % 3) == 2)
                {
                    // Player0 wins!
                    state.Winner = state.Players[0].Username;
                    logger.LogInformation("Player {UserId} wins {GameId}.", state.Players[0].Username, state.GameId);

                }
                else
                {
                    // Player1 wins!
                    state.Winner = state.Players[1].Username;
                    logger.LogInformation("Player {UserId} wins {GameId}.", state.Players[1].Username, state.GameId);
                }

                await StateManager.SetStateAsync("state", state);

                foreach (var (p, view) in state.GetViews())
                {
                    var bytes = JsonSerializer.SerializeToUtf8Bytes(view);
                    await RegisterReminderAsync("notify-game-complete", bytes, TimeSpan.Zero, Timeout.InfiniteTimeSpan);
                }

                return;
            }

            await StateManager.SetStateAsync("state", state);
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            if (reminderName == "notify-game-ready")
            {
                var info = JsonSerializer.Deserialize<GameInfo>(state);

                var proxy = ActorProxy.Create<IPlayerActor>(info.Player.Player.ActorId, info.Player.Player.ActorType);
                await proxy.NotifyGameReadyAsync(info);
            }
            else if (reminderName == "notify-game-complete")
            {
                var result = JsonSerializer.Deserialize<GameResult>(state);

                var proxy = ActorProxy.Create<IPlayerActor>(result.Info.Player.Player.ActorId, result.Info.Player.Player.ActorType);
                await proxy.NotifyGameCompletedAsync(result);
            }
        }
    }
}