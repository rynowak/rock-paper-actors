using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;

namespace Frontend
{
    public class GameClient
    {
        private readonly DaprClient client;
        public GameClient(DaprClient client)
        {
            this.client = client;
        }

        public ValueTask<GameResult> PlayAsync(GameInfo game, Shape move, CancellationToken cancellationToken = default)
        {
            return client.InvokeMethodAsync<PlayerMove, GameResult>("gamemaster", game.GameId, new PlayerMove() { Player = game.Player, Move = move, }, cancellationToken: cancellationToken);
        }
    }
}