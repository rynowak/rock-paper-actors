using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Rochambot
{
    public class GameClient : IAsyncDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly IQueueClient _requestClient;
        private readonly ISessionClient _responseClient;
        private readonly IQueueClient _gameRequest;
        private readonly ILogger<GameClient> _logger;
        private IMessageSession _session;
        private ITopicClient _playTopicClient;
        private SubscriptionClient _playSubscriptionClient;

        public GameClient(IConfiguration configuration, ILogger<GameClient> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _requestClient = new QueueClient(_configuration["AzureServiceBusConnectionString"], _configuration["RequestQueueName"]);
            _responseClient = new SessionClient(_configuration["AzureServiceBusConnectionString"], _configuration["ResponseQueueName"]);
            _gameRequest = new QueueClient(_configuration["AzureServiceBusConnectionString"], _configuration["GameQueueName"]);
            _playTopicClient = new TopicClient(_configuration["AzureServiceBusConnectionString"], _configuration["PlayTopic"]);
        }

        public string GameId { get; } = Guid.NewGuid().ToString();
        public string PlayerId { get; } = "somehuman"; // todo: add auth and use authz here instead of a static string
        public Opponent Opponent { get; private set; }
        public string PlayerSubscriptionName { get; private set; }

        public async Task<Shape> PlayShapeAsync(Shape playerPick)
        {
            await VerifySubscriptionExistsForPlayerAsync();

            var message = new Message();
            message.UserProperties["To"] = "GameMaster";
            message.UserProperties["From"] = PlayerId;
            message.UserProperties["Opponent"] = Opponent.Id;
            message.UserProperties["Shape"] = playerPick.ToString();
            message.UserProperties["GameId"] = GameId;

            await _playTopicClient.SendAsync(message);
            return playerPick;
        }

        public async Task VerifySubscriptionExistsForPlayerAsync()
        {
            var managementClient = new ManagementClient(_configuration["AzureServiceBusConnectionString"]);
            PlayerSubscriptionName = $"player-{PlayerId}";

            if (!await managementClient.SubscriptionExistsAsync(_configuration["PlayTopic"], PlayerSubscriptionName))
            {
                await managementClient.CreateSubscriptionAsync
                (
                    new SubscriptionDescription(_configuration["PlayTopic"], PlayerSubscriptionName),
                    new RuleDescription($"player{PlayerId}rule", new SqlFilter($"To = '{PlayerId}'"))
                );
            }
        }

        private async Task OnMessageReceived(Message message, CancellationToken token)
        {
            _logger.LogInformation($"Received message: {message.SystemProperties.SequenceNumber}");

            var messageToBot = new Message();
            var gameId = message.UserProperties["GameId"];
            var opponent = message.UserProperties["Opponent"];

            await _playSubscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
        }

        private Task OnMessageHandlingException(ExceptionReceivedEventArgs args)
        {
            _logger.LogError(args.Exception, $"Message handler error: {Environment.NewLine}Endpoint: {args.ExceptionReceivedContext.Endpoint}{Environment.NewLine}Client ID: {args.ExceptionReceivedContext.ClientId}{Environment.NewLine}Entity Path: {args.ExceptionReceivedContext.EntityPath}");
            return Task.CompletedTask;
        }
        public async Task StartSessionAsync()
        {
            if (_session is null)
            {
                _session = await _responseClient.AcceptMessageSessionAsync(GameId);
            }
        }

        private void Subscribe()
        {
            _playSubscriptionClient = new SubscriptionClient(
                _configuration["AzureServiceBusConnectionString"],
                _configuration["PlayTopic"],
                PlayerSubscriptionName);

            _playSubscriptionClient.RegisterMessageHandler(OnMessageReceived,
                new MessageHandlerOptions(OnMessageHandlingException)
                {
                    AutoComplete = false,
                    MaxConcurrentCalls = 1
                });

            _playTopicClient = new TopicClient(_configuration["AzureServiceBusConnectionString"], _configuration["PlayTopic"]);
        }

        public async Task RequestGameAsync()
        {
            await StartSessionAsync();
            await _gameRequest.SendAsync(new Message
            {
                ReplyToSessionId = GameId
            });

            var gameData = await _session.ReceiveAsync();

            Opponent = new Opponent(gameData.ReplyToSessionId);

            await _session.CompleteAsync(gameData.SystemProperties.LockToken);
        }

        public async ValueTask DisposeAsync()
        {
            await _requestClient?.CloseAsync();
            await _session?.CloseAsync();
            await _responseClient?.CloseAsync();
            await _playTopicClient?.CloseAsync();
            await _playSubscriptionClient?.CloseAsync();
        }
    }
}