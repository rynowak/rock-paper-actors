using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Actions;
using Microsoft.Extensions.Logging;

namespace Rochambot
{
    public class ScoreboardClient : ServiceClient
    {
        public ScoreboardClient(HttpClient httpClient, JsonSerializerOptions options, ILoggerFactory loggerFactory)
            : base(httpClient, options, loggerFactory)
        {
        }

        public async ValueTask<IEnumerable<PlayerRecord>> GetScoreboardAsync(int? count = default, CancellationToken cancellationToken = default)
        {
            var records = await GetAsync<Dictionary<string, PlayerRecord>>(HttpMethod.Get, "scoreboard", "get-stats", cancellationToken);
            return records.Values.OrderBy(r => r.Wins).Take(count ?? 10);
        }
    }
}