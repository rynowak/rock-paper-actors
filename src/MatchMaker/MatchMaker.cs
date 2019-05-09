using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.ServiceBus;
using System.Text.Json;

namespace MatchMaker
{
    public class MatchMaker : IHostedService
    {
        private readonly ILogger<MatchMaker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IQueueClient _requestClient;
        private readonly IQueueClient _responseClient;
        private readonly IQueueClient _gameClient;

        private readonly List<GameRequest> _gameRequests;

        public MatchMaker(ILogger<MatchMaker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _requestClient = new QueueClient(_configuration["ConnectionString"], _configuration["RequestQueueName"]);
            _responseClient = new QueueClient(_configuration["ConnectionString"], _configuration["ResponseQueueName"]);
            _gameClient = new QueueClient(_configuration["ConnectionString"], _configuration["GameQueueName"]);
        }

        public Task StartAsync(CancellationToken token)
        {
            _requestClient.RegisterMessageHandler(RecievedRequest, RecievedError);
            return Task.CompletedTask;
        }

        private async Task RecievedRequest(Message message, CancellationToken cancellationToken)
        {
            
        }

        private static Task RecievedError(ExceptionReceivedEventArgs arg)
        {
            throw new NotImplementedException();
        }

        public async Task StopAsync(CancellationToken token)
        {
            await _requestClient.CloseAsync();
            await _responseClient.CloseAsync();
            await _gameClient.CloseAsync();
        }
    }
}
