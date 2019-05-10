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
        private readonly IQueueClient _botRequestQueue;
        private readonly ISessionClient _botResponseQueue;
        private readonly string _id;

        public MatchMaker(ILogger<MatchMaker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _id = Guid.NewGuid().ToString();
            _responseQueue = new QueueClient(_configuration["ConnectionString"], _configuration["ResponseQueueName"]);
            _botRequestQueue = new QueueClient(_configuration["ConnectionString"], _configuration["BotRequestQueueName"]);
            _botResponseQueue = new SessionClient(_configuration["ConnectionString"], _configuration["BotResponseQueueName"]);
            _gameQueue = new MessageReceiver(_configuration["ConnectionString"], _configuration["GameQueueName"]);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var session = await _botResponseQueue.AcceptMessageSessionAsync(_id);
            while(!stoppingToken.IsCancellationRequested)
            {
                //TODO: Probably should accept a larger even number and loop through in twos
                //matching the remainder with a bot.
                var messages = await _gameQueue.ReceiveAsync(2, TimeSpan.FromSeconds(10));

                if(messages == null) { continue; }

                if(messages.Count() == 1)
                {
                    var playerOne = messages[0];
                    _logger.LogTrace("1 message receiver, asking for bot to play against {opponent}", playerOne.ReplyToSessionId);

                    var botRequestMessage = new Message
                    {
                        ReplyToSessionId = _id
                    };

                    await _botRequestQueue.SendAsync(botRequestMessage);
                    var bot = await session.ReceiveAsync();
                    var playerMatchResponse = new Message
                    {
                        SessionId = playerOne.ReplyToSessionId,
                        ReplyToSessionId = bot.ReplyToSessionId
                    };
                    await _responseQueue.SendAsync(playerMatchResponse);
                    await _gameQueue.CompleteAsync(playerOne.SystemProperties.LockToken);
                    await session.CompleteAsync(bot.SystemProperties.LockToken);
                }
                else
                {
                    //We have two messages on the queue at the same
                    //time waiting for a game so match them up.
                    await MakeMatch(messages[0], messages[1]);
                }
            }
            await session.CloseAsync();
        }

        private async Task MakeMatch(Message playerOne, Message playerTwo)
        {
            _logger.LogTrace("Creating match between", playerOne.ReplyToSessionId, playerTwo.ReplyToSessionId);

            var playerOneGameMessage = new Message
            {
                SessionId = playerOne.ReplyToSessionId,
                ReplyToSessionId = playerTwo.ReplyToSessionId
            };

            var playerTwoGameMessage = new Message
            {
                SessionId = playerTwo.ReplyToSessionId,
                ReplyToSessionId = playerOne.ReplyToSessionId
            };

            await Task.WhenAll(_responseQueue.SendAsync(playerOneGameMessage),
                               _responseQueue.SendAsync(playerTwoGameMessage));

            await _gameQueue.CompleteAsync(new string[] {
                        playerOne.SystemProperties.LockToken,
                        playerTwo.SystemProperties.LockToken
                    });
        }

        public async ValueTask DisposeAsync()
        {
            if(_gameQueue != null)
            {
                await _gameQueue.CloseAsync();
            }

            if(_botRequestQueue != null)
            {
                await _botRequestQueue.CloseAsync();
            }

            if(_responseQueue != null)
            {
                await _responseQueue.CloseAsync();
            }
        }
    }
}
