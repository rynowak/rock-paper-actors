using System.Threading.Tasks;
using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Actors.Runtime;
using Dapr.Client;
using GameMaster;
using MatchMaker;
using Player;

namespace HumanPlayer
{
    public class HumanPlayerActor : Actor, IHumanPlayerActor, IPlayerActor
    {
        private readonly DaprClient client;

        private PlayerInfo player;

        public HumanPlayerActor(ActorService actorService, ActorId actorId, DaprClient client) 
            : base(actorService, actorId)
        {
            this.client = client;
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
                IsBot = false,
            };

            return Task.CompletedTask;
        }
        public async Task RequestGameAsync()
        {
            var proxy = ActorProxy.Create<IMatchMakerActor>(new ActorId("casual"), "MatchMakerActor");
            await proxy.RequestGameAsync(player);
        }

        public async Task PlayMoveAsync(Shape move)
        {
            var game = await StateManager.GetStateAsync<GameInfo>("game");

            var proxy = ActorProxy.Create<IGameActor>(game.Game.ActorId, game.Game.ActorType);
            await proxy.PlayMoveAsync(player, move);
        }

        public async Task NotifyGameReadyAsync(GameInfo game)
        {
            await StateManager.SetStateAsync("game", game);
            await client.PublishEventAsync("pubsub", "game-ready", game);
        }

        public async Task NotifyGameCompletedAsync(GameResult result)
        {
            await client.PublishEventAsync("pubsub", "game-complete", result);
        }
    }
}