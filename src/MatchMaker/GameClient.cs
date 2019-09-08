using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Actions;
using Microsoft.Extensions.Logging;

namespace MatchMaker
{
    public class GameClient : ServiceClient
    {
        public GameClient(HttpClient httpClient, JsonSerializerOptions options, ILoggerFactory loggerFactory)
            : base(httpClient, options, loggerFactory)
        {
        }

        public ValueTask<string> CreateGameAsync(UserInfo[] players, CancellationToken cancellationToken = default)
        {
            return SendAsync<string>(HttpMethod.Put, "gamemaster", "create", players, cancellationToken);
        }
    }
}