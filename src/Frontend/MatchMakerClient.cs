using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using Grpc.Core;

namespace Frontend
{
    public class MatchMakerClient
    {
        private readonly DaprClient client;
        public MatchMakerClient(DaprClient client)
        {
            this.client = client;
        }

        public async ValueTask<GameInfo> JoinGameAsync(UserInfo user, CancellationToken cancellationToken = default)
        {
            try
            {
                var game = await client.InvokeMethodAsync<UserInfo, GameInfo>("matchmaker", "join", user, cancellationToken: cancellationToken);
                return game;
            }
            catch (RpcException rpc) when (rpc.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(rpc.Message, rpc, cancellationToken);
            }
        }
    }
}