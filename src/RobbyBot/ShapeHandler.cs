using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
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
        static readonly Shape[] Shapes = new Shape[] 
        {
            Shape.Rock, Shape.Paper, Shape.Scissors
        };

        readonly ILogger<ShapeHandler> _logger;
        readonly IConfiguration _configuration;
        readonly string _botId;
        private TopicClient _playTopicClient;
        private ISubscriptionClient _playSubscriptionClient;
        private ManagementClient _managementClient;
        private string _playerSubscriptionName;

        public ShapeHandler(ILogger<ShapeHandler> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _botId = _configuration["BotId"];
        }

        public override async Task StartAsync(CancellationToken token)
        {
            await VerifySubscriptionExistsForPlayerAsync();

            _playSubscriptionClient = new SubscriptionClient(
                _configuration["AzureServiceBusConnectionString"],
                _configuration["PlayTopic"],
                _playerSubscriptionName);

            _playSubscriptionClient.RegisterMessageHandler(OnMessageReceived, 
                new MessageHandlerOptions(OnMessageHandlingException) 
                {
                    AutoComplete = false,
                    MaxConcurrentCalls = 1
                });

            _playTopicClient = new TopicClient(_configuration["AzureServiceBusConnectionString"], _configuration["PlayTopic"]);

            await base.StartAsync(token);
        }

        public override async Task StopAsync(CancellationToken token)
        {
            await _managementClient.CloseAsync();
            await _playTopicClient.CloseAsync();
            await _playSubscriptionClient.CloseAsync();
            await base.StopAsync(token);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"{_botId} running at {DateTimeOffset.Now}");
                await Task.Delay(10000);
            }
        }

        private async Task OnMessageReceived(Message message, CancellationToken token)
        {
            _logger.LogInformation($"Received message: {message.SystemProperties.SequenceNumber}");

            var messageToGameMaster = new Message();
            messageToGameMaster.UserProperties["GameId"] = message.UserProperties["GameId"];
            messageToGameMaster.UserProperties["To"] = "GameMaster";
            messageToGameMaster.UserProperties["From"] = _botId;
            messageToGameMaster.UserProperties["Opponent"] = message.UserProperties["Opponent"];
            messageToGameMaster.UserProperties["Shape"] = Shapes[Random.Next(0, 2)].ToString();
                
            await _playTopicClient.SendAsync(messageToGameMaster);

            await _playSubscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
        }

        private Task OnMessageHandlingException(ExceptionReceivedEventArgs args)
        {
            _logger.LogError(args.Exception, $"Message handler error: {Environment.NewLine}Endpoint: {args.ExceptionReceivedContext.Endpoint}{Environment.NewLine}Client ID: {args.ExceptionReceivedContext.ClientId}{Environment.NewLine}Entity Path: {args.ExceptionReceivedContext.EntityPath}");
            return Task.CompletedTask;
        }

        public async Task VerifySubscriptionExistsForPlayerAsync()
        {
            _managementClient = new ManagementClient(_configuration["AzureServiceBusConnectionString"]);
            _playerSubscriptionName = $"player-{_botId}";

            if(!(await _managementClient.SubscriptionExistsAsync(_configuration["PlayTopic"], _playerSubscriptionName)))
            {
                await _managementClient.CreateSubscriptionAsync
                (
                    new SubscriptionDescription(_configuration["PlayTopic"], _playerSubscriptionName),
                    new RuleDescription($"player{_botId}rule", new SqlFilter($"To = '{_botId}'"))
                );
            }
        }
    }
}