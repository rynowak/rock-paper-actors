using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using System.Text;
using Microsoft.Azure.ServiceBus.Core;

namespace RobbyBot
{
    public class GameRequestHandler : IHostedService
    {
        private readonly ILogger<GameRequestHandler> _logger;
        private IQueueClient _botRequestQueue;
        private IQueueClient _botResponseQueue;
        private IConfiguration _configuration;
        private readonly string _botId;

        public GameRequestHandler(ILogger<GameRequestHandler> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _botId = _configuration["BotId"];
        }

        public Task StartAsync(CancellationToken token)
        {
            _botRequestQueue = new QueueClient(_configuration["ConnectionString"], _configuration["BotRequestQueueName"]);
            _botResponseQueue = new QueueClient(_configuration["ConnectionString"], _configuration["BotResponseQueueName"]);

            _botRequestQueue.RegisterMessageHandler(RecievedGameRequest, RecievedGameRequestError);
            return Task.CompletedTask;
        }

        private Task RecievedGameRequestError(ExceptionReceivedEventArgs arg)
        {
            _logger.LogError(arg.Exception, "Error recieving game request");
            return Task.CompletedTask;
        }

        private async Task RecievedGameRequest(Message message, CancellationToken arg2)
        {
            var gameRequest = new Message
            {
                SessionId = message.ReplyToSessionId,
                ReplyToSessionId = _botId
            };
            await _botResponseQueue.SendAsync(gameRequest);
        }

        public async Task StopAsync(CancellationToken token)
        {
            await _botRequestQueue.CloseAsync();
            await _botResponseQueue.CloseAsync();
        }
    }
}
