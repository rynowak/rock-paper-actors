using System;
using System.Threading.Tasks;
using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Actors.Runtime;
using GameMaster;
using MatchMaker;
using Player;

namespace BotPlayer
{
    public class BotPlayerActor : Actor, IBotPlayerActor, IPlayerActor
    {
        private readonly Random random = new Random();

        private PlayerInfo player;

        public BotPlayerActor(ActorService actorService, ActorId actorId) 
            : base(actorService, actorId)
        {
        }

        protected override Task OnActivateAsync()
        {
            player = new PlayerInfo()
            {
                Player = new ActorReference()
                {
                    ActorId = Id,
                    ActorType = ActorService.ActorTypeInfo.ActorTypeName,
                },
                Username = Id.GetId(),
                IsBot = true,
            };

            return Task.CompletedTask;
        }

        public async Task InitializeAsync()
        {
            await JoinGameAsync();
        }

        public Task NotifyGameCompletedAsync(GameResult result)
        {
            // meh
            return Task.CompletedTask;
        }

        public async Task NotifyGameReadyAsync(Player.GameInfo game)
        {
            // wait a random amount of time to simulate thinking...
            await Task.Delay(TimeSpan.FromSeconds(random.Next(3, 5)));

            // just make a random move YO.
            var move = (Shape)random.Next(3);

            var proxy = ActorProxy.Create<IGameActor>(game.Game.ActorId, game.Game.ActorType);
            await proxy.PlayMoveAsync(player, move);

            // do it again
            await JoinGameAsync();
        }

        private async Task JoinGameAsync()
        {
            var proxy = ActorProxy.Create<IMatchMakerActor>(new ActorId("casual"), "MatchMakerActor");
            await proxy.RequestGameAsync(player);
        }
    }
}