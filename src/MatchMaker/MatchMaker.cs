using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.ServiceBus;
using System.Text.Json.Serialization;
using Microsoft.Azure.ServiceBus.Core;

namespace MatchMaker
{
    public class MatchMaker : BackgroundService, IAsyncDisposable
    {
        private readonly ILogger<MatchMaker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IQueueClient _responseQueue;
        private readonly MessageReceiver _gameQueue;

        public MatchMaker(ILogger<MatchMaker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _responseQueue = new QueueClient(_configuration["ConnectionString"], _configuration["ResponseQueueName"]);
            _gameQueue = new MessageReceiver(_configuration["ConnectionString"], _configuration["GameQueueName"]);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var messages = await _gameQueue.ReceiveAsync(2, TimeSpan.FromSeconds(1));

            if(messages.Count() > 1)
            {
                Message firstMessage = null;
                foreach(var message in messages)
                {
                    if(firstMessage == null)
                    {
                        firstMessage = message;
                        continue;
                    }

                    var oponentMessage = new Message();
                    oponentMessage.SessionId = firstMessage.ReplyToSessionId;
                    oponentMessage.ReplyToSessionId = message.ReplyToSessionId;
                    await _responseQueue.SendAsync(oponentMessage);
                    oponentMessage = new Message();
                    oponentMessage.SessionId = message.ReplyToSessionId;
                    oponentMessage.ReplyToSessionId = firstMessage.ReplyToSessionId;
                    await _responseQueue.SendAsync(oponentMessage);

                    await _gameQueue.CompleteAsync(new string[] { 
                        firstMessage.SystemProperties.LockToken, 
                        message.SystemProperties.LockToken 
                    });

                    firstMessage = null;
                }
            }

            await Task.Delay(1000);
        }

        public async ValueTask DisposeAsync()
        {
            if(_responseQueue != null)
            {
                await _responseQueue.CloseAsync();
            }

            if(_gameQueue != null)
            {
                await _gameQueue.CloseAsync();
            }
        }

        // public Task StartAsync(CancellationToken token)
        // {
        //     return Task.CompletedTask;
        // }


        // public async Task StopAsync(CancellationToken token)
        // {
        //     await _requestClient.CloseAsync();
        //     await _responseClient.CloseAsync();
        //     await _gameClient.CloseAsync();
        // }
    }
}
