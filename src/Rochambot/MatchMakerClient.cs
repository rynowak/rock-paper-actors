using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Actions;
using Microsoft.Extensions.Logging;

namespace Rochambot
{
    public class MatchMakerClient : ServiceClient
    {
        public MatchMakerClient(HttpClient httpClient, JsonSerializerOptions options, ILoggerFactory loggerFactory)
            : base(httpClient, options, loggerFactory)
        {
        }

        public ValueTask<GameInfo> JoinGameAsync(UserInfo user, CancellationToken cancellationToken = default)
        {
            return SendAsync<GameInfo>(HttpMethod.Post, "matchmaker", "join", user, cancellationToken);
        }
    }
}