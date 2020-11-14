using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Actors.Runtime;
using GameMaster;
using Player;

namespace MatchMaker
{
    public class MatchMakerActor : Actor, IMatchMakerActor, IRemindable
    {
        public MatchMakerActor(ActorService actorService, ActorId actorId) 
            : base(actorService, actorId)
        {
        }

        public async Task RequestGameAsync(PlayerInfo player)
        {
            if (player.Player.ActorId == null || player.Player.ActorId.GetId() == null)
            {
                throw new InvalidOperationException("Players must have an ID");
            }

            if (player.IsBot)
            {
                // We have a separate queue for bot players, and match bots with players on
                // a background timer.
                var bots = await StateManager.GetOrAddStateAsync("bots", new List<PlayerInfo>());
                bots.Add(player);
                await StateManager.SetStateAsync("bots", bots);
                return;
            }
            
            // Due to the natural backpressure of actors, we don't need to maintain a queue
            // just the "next".
            var next = (await StateManager.TryGetStateAsync<PlayerInfo>("next")).Value;
            if (next != null)
            {
                // someone is waiting, join them up.
                await CreateGameAsync(new[]{ player, next, });

                // the "next" player is in a game now.
                await StateManager.RemoveStateAsync("next");
                return;
            }

            // No one is waiting, make this player the next one in line.
            await StateManager.SetStateAsync("next", player);

            // Check on them in 5 seconds and see if they are still waiting.
            var bytes = JsonSerializer.SerializeToUtf8Bytes(player);
            await RegisterReminderAsync("bot-timer",  bytes, TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            if (reminderName == "bot-timer")
            {
                var next = (await StateManager.TryGetStateAsync<PlayerInfo>("next")).Value;
                var player = JsonSerializer.Deserialize<PlayerInfo>(state);

                if (next?.Username != player.Username)
                {
                    // this player is no longer waiting. nothing to do.
                    return;
                }

                // player is still waiting, join them to a bot if possible.
                var bots = await StateManager.GetOrAddStateAsync("bots", new List<PlayerInfo>());
                if (bots.Count == 0)
                {
                    // no bots, try again in a bit...
                    await RegisterReminderAsync("bot-timer", state, TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
                    return;
                }

                // "pop" the first bot in line
                var bot = bots[0];
                bots.RemoveAt(0);
                await CreateGameAsync(new[]{ player, bot, });

                // the "next" player is in a game now.
                await StateManager.RemoveStateAsync("next");

                await StateManager.SetStateAsync("bots", bots);
            }
        }

        private async Task CreateGameAsync(PlayerInfo[] players)
        {
            var proxy = ActorProxy.Create<IGameActor>(ActorId.CreateRandom(), "GameActor");
            await proxy.InitializeAsync(players);
        }
    }
}