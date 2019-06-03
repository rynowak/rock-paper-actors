using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.ServiceBus.Management;
using System.Text;
using Rochambot;

namespace GameMaster
{
    public class GameMaster : IHostedService
    {
        private const string Name = nameof(GameMaster);
        private readonly IConfiguration _configuration;
        private readonly ILogger<GameMaster> _logger;
        private readonly GameData _gameData;
        private ManagementClient _managementClient;
        private ISubscriptionClient _playSubscriptionClient;
        private TopicClient _playTopicClient;

        public GameMaster(ILogger<GameMaster> logger, 
            IConfiguration configuration,
            GameData gameData)
        {
            _configuration = configuration;
            _logger = logger;
            _gameData = gameData;
        }

        public async Task StartAsync(CancellationToken token)
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

            _playTopicClient = 
                new TopicClient(
                    _configuration["AzureServiceBusConnectionString"], 
                    _configuration["PlayTopic"]);
        }

        public async Task StopAsync(CancellationToken token)
        {
            await _playSubscriptionClient.CloseAsync();
            await _managementClient.CloseAsync();
            await _playTopicClient.CloseAsync();
        }

        private async Task OnMessageReceived(Message message, CancellationToken token)
        {
            _logger.LogInformation($"Received message: {message.SystemProperties.SequenceNumber}");

            var shape = message.UserProperties["Shape"].ToString();
            var gameId = message.UserProperties["GameId"].ToString();
            var playerId = message.UserProperties["From"].ToString();
            Game game = null;

            if(!(await _gameData.GameExists(gameId)))
                game = await _gameData.CreateGame(gameId);
            else
                game = await _gameData.GetGame(gameId);

            if(_gameData.IsTurnComplete(game))
            {
                game = await _gameData.StartTurn(game.GameId, new Play
                {
                    PlayerId = playerId, 
                    ShapeSelected = Enum.Parse<Shape>(shape)
                });
            }
            else
            {
                game = await _gameData.CompleteTurn(game.GameId, new Play
                {
                    PlayerId = playerId, 
                    ShapeSelected = Enum.Parse<Shape>(shape)
                });
            }

            await ReplayOpponentPlay(message, game);
        }

        private async Task ReplayOpponentPlay(Message message, Game game)
        {
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

        private async Task VerifyGameMasterSubscriptionExists()
        {
            _managementClient = new ManagementClient(_configuration["AzureServiceBusConnectionString"]);

            if (!await _managementClient.SubscriptionExistsAsync(_configuration["PlayTopic"], GameMaster.Name))
            {
                await _managementClient.CreateSubscriptionAsync
                (
                    // todo: set up ttl and auto-delete on idle so the subscriptions die when unused
                    new SubscriptionDescription(_configuration["PlayTopic"], GameMaster.Name),
                    new RuleDescription($"gamemasterrule", new SqlFilter($"To = 'GameMaster'"))
                );
            }
        }
    }
}