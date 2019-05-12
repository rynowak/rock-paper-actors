using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace MatchMaker
{
    public class MatchMaker : BackgroundService, IAsyncDisposable
    {
        readonly ILogger<MatchMaker> _logger;
        readonly IQueueClient _responseQueue;
        readonly IQueueClient _botRequestQueue;
        readonly ISessionClient _botResponseQueue;
        readonly IMessageReceiver _gameQueue;
        readonly string _id = Guid.NewGuid().ToString();

        public MatchMaker(ILogger<MatchMaker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _responseQueue = new QueueClient(configuration["ConnectionString"], configuration["ResponseQueueName"]);
            _botRequestQueue = new QueueClient(configuration["ConnectionString"], configuration["BotRequestQueueName"]);
            _botResponseQueue = new SessionClient(configuration["ConnectionString"], configuration["BotResponseQueueName"]);
            _gameQueue = new MessageReceiver(configuration["ConnectionString"], configuration["GameQueueName"]);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var session = await _botResponseQueue.AcceptMessageSessionAsync(_id);
            while (!stoppingToken.IsCancellationRequested)
            {
                // TODO: Probably should accept a larger even number and loop through in twos
                // matching the remainder with a bot.
                var messages = await _gameQueue.ReceiveAsync(1, TimeSpan.FromSeconds(10));
                if (messages is null || messages.Count == 0)
                {
                    continue;
                }

                var playerOne = messages[0];
                _logger.LogTrace("1 message receiver, asking for bot to play against {opponent}", playerOne.ReplyToSessionId);

                await _botRequestQueue.SendAsync(new Message
                {
                    ReplyToSessionId = _id
                });

                var bot = await session.ReceiveAsync();
                await _responseQueue.SendAsync(new Message
                {
                    SessionId = playerOne.ReplyToSessionId,
                    ReplyToSessionId = bot.ReplyToSessionId
                });
                await _gameQueue.CompleteAsync(playerOne.SystemProperties.LockToken);
                await session.CompleteAsync(bot.SystemProperties.LockToken);
            }

            await session.CloseAsync();
        }

        async Task MakeMatchAsync(Message playerOne, Message playerTwo)
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
            
            await _gameQueue.CompleteAsync(new string[]
            {
                playerOne.SystemProperties.LockToken,
                playerTwo.SystemProperties.LockToken
            });
        }

        public async ValueTask DisposeAsync()
        {
            await _gameQueue?.CloseAsync();
            await _botRequestQueue?.CloseAsync();
            await _responseQueue?.CloseAsync();
        }
    }
}