using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace RobbyBot
{
    public class GameRequestHandler : IHostedService
    {
        readonly ILogger<GameRequestHandler> _logger;
        readonly IConfiguration _configuration;
        readonly string _botId;

        IQueueClient _botRequestQueue;
        IQueueClient _botResponseQueue;

        public GameRequestHandler(ILogger<GameRequestHandler> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _botId = _configuration["BotId"];
        }

        public Task StartAsync(CancellationToken token)
        {
            _botRequestQueue = new QueueClient(_configuration["AzureServiceBusConnectionString"], _configuration["BotRequestQueueName"]);
            _botResponseQueue = new QueueClient(_configuration["AzureServiceBusConnectionString"], _configuration["BotResponseQueueName"]);
            _botRequestQueue.RegisterMessageHandler(ReceivedGameRequestAsync, ReceivedGameRequestErrorAsync);

            return Task.CompletedTask;
        }

        Task ReceivedGameRequestAsync(Message message, CancellationToken _) =>
            _botResponseQueue.SendAsync(new Message
            {
                SessionId = message.ReplyToSessionId,
                ReplyToSessionId = _botId
            });

        Task ReceivedGameRequestErrorAsync(ExceptionReceivedEventArgs arg)
        {
            _logger.LogError(arg.Exception, "Error receiving game request");
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken token)
        {
            await _botRequestQueue?.CloseAsync();
            await _botResponseQueue?.CloseAsync();
        }
    }
}