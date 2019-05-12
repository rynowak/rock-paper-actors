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
    public class ShapeHandler : BackgroundService
    {
        private readonly ILogger<ShapeHandler> _logger;
        private ISessionClient _requestQueue;
        private IQueueClient _responseQueue;
        private IConfiguration _configuration;
        private readonly string _botId;

        public ShapeHandler(ILogger<ShapeHandler> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _botId = _configuration["BotId"];
        }

        public override Task StartAsync(CancellationToken token)
        {
            _requestQueue = new SessionClient(_configuration["ConnectionString"], _configuration["RequestQueueName"]);
            _responseQueue = new QueueClient(_configuration["ConnectionString"], _configuration["ResponseQueueName"]);

            return base.StartAsync(token);
        }

        public override async Task StopAsync(CancellationToken token)
        {
            await _requestQueue.CloseAsync();
            await _responseQueue.CloseAsync();
            await base.StopAsync(token);
        }

        string[] Shapes = new string[] {
            "Rock",
            "Paper",
            "Scissors"
        };

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var session = await _requestQueue.AcceptMessageSessionAsync(_botId);

            while(!stoppingToken.IsCancellationRequested)
            {
                var message = await session.ReceiveAsync();
                
                if(message == null) { continue; }
                
                Random rand = new Random();

                _logger.LogInformation("Message Received {message}", message);
                var respMessage = new Message(UTF8Encoding.UTF8.GetBytes(Shapes[rand.Next(0,2)]));
                respMessage.SessionId = message.ReplyToSessionId;

                await _responseQueue.SendAsync(respMessage);
                await session.CompleteAsync(message.SystemProperties.LockToken);
            }

            await session.CloseAsync();
        }
    }
}
