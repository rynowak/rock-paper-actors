using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Rochambot
{
    public class GameClient : IAsyncDisposable
    {
        private readonly IQueueClient _requestClient;
        private readonly ISessionClient _responseClient;
        private readonly IQueueClient _gameRequest;
        private IMessageSession _session;
        private ITopicClient _playTopicClient;
        private ManagementClient _managementClient;
        private string _playerSubscriptionName;
        public string GameId { get; } = Guid.NewGuid().ToString();
        public string PlayerId { get; } = "bradyg";
        public Opponent Opponent { get; private set; }
        IConfiguration _configuration;

        public GameClient(IConfiguration configuration)
        {
            _configuration = configuration;
            _requestClient = new QueueClient(_configuration["AzureServiceBusConnectionString"], _configuration["RequestQueueName"]);
            _responseClient = new SessionClient(_configuration["AzureServiceBusConnectionString"], _configuration["ResponseQueueName"]);
            _gameRequest = new QueueClient(_configuration["AzureServiceBusConnectionString"], _configuration["GameQueueName"]);
            _playTopicClient = new TopicClient(_configuration["AzureServiceBusConnectionString"], _configuration["PlayTopic"]);
        }

        public async Task<Shape> RequestShapeAsync(Shape playerPick)
        {
            await VerifySubscriptionExistsForPlayer();

            var message = new Message();
            message.UserProperties["PlayerId"] = "webclient";
            message.UserProperties["Opponent"] = Opponent.Id;
            message.UserProperties["GameId"] = GameId;
            message.UserProperties["Shape"] = playerPick;

            await _playTopicClient.SendAsync(message);
            return playerPick;
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

        public async Task VerifySubscriptionExistsForPlayer()
        {
            _managementClient = new ManagementClient(_configuration["AzureServiceBusConnectionString"]);
            _playerSubscriptionName = $"player-{PlayerId}";

            if(!(await _managementClient.SubscriptionExistsAsync(_configuration["PlayTopic"], _playerSubscriptionName)))
            {
                await _managementClient.CreateSubscriptionAsync(
                    new SubscriptionDescription(_configuration["PlayTopic"], _playerSubscriptionName),
                    new RuleDescription($"player{PlayerId}rule", new SqlFilter($"PlayerId = '{PlayerId}'")));
            }
        }

        public async Task StartSessionAsync()
        {
            if (_session is null)
            {
                _session = await _responseClient.AcceptMessageSessionAsync(GameId);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _requestClient?.CloseAsync();
            await _session?.CloseAsync();
            await _responseClient?.CloseAsync();
        }
    }
}