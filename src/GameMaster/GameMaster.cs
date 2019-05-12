using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Rochambot;

namespace GameMaster
{
    public class GameMaster : BackgroundService
    {
        readonly IConfiguration _configuration;
        readonly ILogger<GameMaster> _logger;
        readonly IMessageSession _scoringSession;

        IQueueClient _sessionResultsClient;
        ISessionClient _playerShapeSelectionClient;

        public GameMaster(ILogger<GameMaster> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken token)
        {
            _playerShapeSelectionClient = new SessionClient(_configuration["ConnectionString"], _configuration["PlayerShapeSelectionQueueName"]);
            _sessionResultsClient = new QueueClient(_configuration["ConnectionString"], _configuration["SessionResultsQueueName"]);

            return base.StartAsync(token);
        }

        public override async Task StopAsync(CancellationToken token)
        {
            await _playerShapeSelectionClient.CloseAsync();
            await _sessionResultsClient.CloseAsync();

            await base.StopAsync(token);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var session = await _playerShapeSelectionClient.AcceptMessageSessionAsync();

                while (!stoppingToken.IsCancellationRequested)
                {
                    var message = await session.ReceiveAsync();
                    if (message is null)
                    {
                        continue;
                    }

                    _logger.LogInformation($"Message Received {message}");

                    var sessionResult =
                        message.Body
                               .FromUTF8Bytes()
                               .To<SessionResult>()
                               .Score();

                    var respMessage =
                        new Message(sessionResult.ToJson().ToUTF8Bytes())
                        {
                            SessionId = message.ReplyToSessionId
                        };

                    await _sessionResultsClient.SendAsync(respMessage);
                    await session.CompleteAsync(message.SystemProperties.LockToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GameMaster error");
            }
        }
    }
}