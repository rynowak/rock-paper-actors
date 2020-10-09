using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using Dapr.Client.Http;

namespace MatchMaker
{
    public class GameClient
    {
        private readonly DaprClient client;
        public GameClient(DaprClient client)
        {
            this.client = client;
        }

        public ValueTask<string> CreateGameAsync(UserInfo[] players, CancellationToken cancellationToken = default)
        {
            return client.InvokeMethodAsync<UserInfo[], string>("gamemaster", "create", players, new HTTPExtension(){ Verb = HTTPVerb.Put, }, cancellationToken);
        }
    }
}