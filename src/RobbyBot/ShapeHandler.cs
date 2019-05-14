using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rochambot;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RobbyBot
{
    public class ShapeHandler : BackgroundService
    {
        static readonly Random Random = new Random((int)DateTime.Now.Ticks);
        static readonly string[] Shapes = new string[] 
        {
            nameof(Shape.Rock),
            nameof(Shape.Paper),
            nameof(Shape.Scissors)
        };

        readonly ILogger<ShapeHandler> _logger;
        readonly IConfiguration _configuration;
        readonly string _botId;

        ISessionClient _requestQueue;
        IQueueClient _responseQueue;

        public ShapeHandler(ILogger<ShapeHandler> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _botId = _configuration["BotId"];
        }

        public override Task StartAsync(CancellationToken token)
        {
            _requestQueue = new SessionClient(_configuration["AzureServiceBusConnectionString"], _configuration["RequestQueueName"]);
            _responseQueue = new QueueClient(_configuration["AzureServiceBusConnectionString"], _configuration["ResponseQueueName"]);

            return base.StartAsync(token);
        }

        public override async Task StopAsync(CancellationToken token)
        {
            await _requestQueue.CloseAsync();
            await _responseQueue.CloseAsync();

            await base.StopAsync(token);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var session = await _requestQueue.AcceptMessageSessionAsync(_botId);
            while (!stoppingToken.IsCancellationRequested)
            {
                var message = await session.ReceiveAsync();
                if (message is null)
                {
                    continue;
                }

                _logger.LogInformation($"Message Received {message}");

                var responseMessage = new Message(Encoding.UTF8.GetBytes(Shapes[Random.Next(0, 2)]))
                {
                    SessionId = message.ReplyToSessionId
                };

                await _responseQueue.SendAsync(responseMessage);
                await session.CompleteAsync(message.SystemProperties.LockToken);
            }

            await session.CloseAsync();
        }
    }
}