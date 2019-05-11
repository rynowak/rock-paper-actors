using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text;

namespace GameMaster
{
    public class GameMaster : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GameMaster> _logger;
        IMessageSession _scoringSession;
        QueueClient _sessionResultsClient;
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
                    
                    if(message == null) { continue; }

                    _logger.LogInformation("Message Received {message}", message);
                    var sessionResult = JsonConvert.DeserializeObject<SessionResult>(
                        UTF8Encoding.UTF8.GetString(message.Body)
                    ).Score();
                    
                    var respMessage = new Message(UTF8Encoding.UTF8.GetBytes(
                        JsonConvert.SerializeObject(sessionResult)
                    ));

                    respMessage.SessionId = message.ReplyToSessionId;
                    await _sessionResultsClient.SendAsync(respMessage);
                    await session.CompleteAsync(message.SystemProperties.LockToken);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "GameMaster error");
            }
        }
    }
}
