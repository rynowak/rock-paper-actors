using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Actions;
using Microsoft.Extensions.Logging;

namespace Rochambot
{
    public class GameClient : ServiceClient
    {
        public GameClient(HttpClient httpClient, JsonSerializerOptions options, ILoggerFactory loggerFactory)
            : base(httpClient, options, loggerFactory)
        {
        }

        public ValueTask<GameState> PlayAsync(GameInfo game, Shape move, CancellationToken cancellationToken = default)
        {
            return SendAsync<GameState>(HttpMethod.Post, "gamemaster", game.GameId, new PlayerMove() { Player = game.Player, Move = move, }, cancellationToken);
        }
    }
}