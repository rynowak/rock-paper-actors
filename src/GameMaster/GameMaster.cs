using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Rochambot;
using Microsoft.Azure.ServiceBus.Management;

namespace GameMaster
{
    public class GameMaster : BackgroundService
    {
        private static string Name { get; } = "GameMaster";
        private readonly IConfiguration _configuration;
        private readonly ILogger<GameMaster> _logger;
        private ManagementClient _managementClient;
        private ISubscriptionClient _playSubscriptionClient;
        private TopicClient _playTopicClient;

        public GameMaster(ILogger<GameMaster> logger, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken token)
        {
            await VerifyGameMasterSubscriptionExists();

            _playSubscriptionClient = new SubscriptionClient(
                _configuration["AzureServiceBusConnectionString"],
                _configuration["PlayTopic"],
                GameMaster.Name);

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
            await _playSubscriptionClient.CloseAsync();
            await _managementClient.CloseAsync();
            await _playTopicClient.CloseAsync();
            await base.StopAsync(token);
        }

        private async Task OnMessageReceived(Message message, CancellationToken token)
        {
            _logger.LogInformation($"Received message: {message.SystemProperties.SequenceNumber}");

            // todo: save the data
            var shape = message.UserProperties["Shape"];
            var gameId = message.UserProperties["GameId"];

            var messageFromGameMaster = new Message();
            messageFromGameMaster.UserProperties["GameId"] = message.UserProperties["GameId"];
            messageFromGameMaster.UserProperties["To"] = message.UserProperties["Opponent"];
            messageFromGameMaster.UserProperties["From"] = "GameMaster";
            messageFromGameMaster.UserProperties["Opponent"] = message.UserProperties["From"];
            await _playTopicClient.SendAsync(messageFromGameMaster);

            await _playSubscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
        }

        private Task OnMessageHandlingException(ExceptionReceivedEventArgs args)
        {
            _logger.LogError(args.Exception, $"Message handler error: {Environment.NewLine}Endpoint: {args.ExceptionReceivedContext.Endpoint}{Environment.NewLine}Client ID: {args.ExceptionReceivedContext.ClientId}{Environment.NewLine}Entity Path: {args.ExceptionReceivedContext.EntityPath}");
            return Task.CompletedTask;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("GameMaster running at {time}", DateTimeOffset.Now);
                await Task.Delay(10000);
            }
        }

        private async Task VerifyGameMasterSubscriptionExists()
        {
            _managementClient = new ManagementClient(_configuration["AzureServiceBusConnectionString"]);

            if(!(await _managementClient.SubscriptionExistsAsync(_configuration["PlayTopic"], GameMaster.Name)))
            {
                await _managementClient.CreateSubscriptionAsync
                (
                    new SubscriptionDescription(_configuration["PlayTopic"], GameMaster.Name),
                    new RuleDescription($"gamemasterrule", new SqlFilter($"To = 'GameMaster'"))
                );
            }
        }
    }
}