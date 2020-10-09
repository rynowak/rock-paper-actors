using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using Dapr.Client.Http;

namespace Frontend
{
    public class ScoreboardClient
    {
        private readonly DaprClient client;
        public ScoreboardClient(DaprClient client)
        {
            this.client = client;
        }

        public async ValueTask<IEnumerable<PlayerRecord>> GetScoreboardAsync(int? count = default, CancellationToken cancellationToken = default)
        {
            var records = await client.InvokeMethodAsync<Dictionary<string, PlayerRecord>>("scoreboard", "get-stats", new HTTPExtension(){ Verb = HTTPVerb.Get, }, cancellationToken);
            return records.Values.OrderBy(r => r.Wins).Take(count ?? 10);
        }
    }
}