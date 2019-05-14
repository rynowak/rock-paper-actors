using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace Rochambot
{
    public class GameClient : IAsyncDisposable
    {
        readonly IQueueClient _requestClient;
        readonly ISessionClient _responseClient;
        readonly IQueueClient _gameRequest;
        readonly ISessionClient _sessionResultsClient;
        readonly IQueueClient _playerShapeSelectionClient;

        IMessageSession _session;
        IMessageSession _scoringSession;

        public string Id { get; } = Guid.NewGuid().ToString();

        public Opponent Opponent { get; private set; }

        public GameClient(IConfiguration configuration)
        {
            _requestClient = new QueueClient(configuration["AzureServiceBusConnectionString"], configuration["RequestQueueName"]);
            _responseClient = new SessionClient(configuration["AzureServiceBusConnectionString"], configuration["ResponseQueueName"]);
            _gameRequest = new QueueClient(configuration["AzureServiceBusConnectionString"], configuration["GameQueueName"]);
            _playerShapeSelectionClient = new QueueClient(configuration["AzureServiceBusConnectionString"], configuration["PlayerShapeSelectionQueueName"]);
            _sessionResultsClient = new SessionClient(configuration["AzureServiceBusConnectionString"], configuration["SessionResultsQueueName"]);
        }

        public async Task<Shape> RequestShapeAsync(Shape playerPick)
        {
            await StartSessionAsync();
            await _requestClient.SendAsync(new Message
            {
                SessionId = Opponent.Id,
                ReplyToSessionId = Id
            });

            var message = await _session.ReceiveAsync();
            var shape = message.Body.FromUTF8Bytes();

            await _session.CompleteAsync(message.SystemProperties.LockToken);

            return shape.ToEnum<Shape>();
        }

        public async Task<SessionResult> SendShapesToGameMasterAsync(Shape playerPick, Shape botPick)
        {
            await StartScoringSessionAsync();

            var sessionResult = new SessionResult
            {
                Player1 = new PlayerResult
                {
                    PlayerId = Id,
                    ShapeSelected = playerPick
                },
                Player2 = new PlayerResult
                {
                    PlayerId = Opponent.Id,
                    ShapeSelected = botPick
                },
                SessionEnd = DateTime.Now
            };

            await _playerShapeSelectionClient.SendAsync(new Message
            {
                SessionId = Id,
                ReplyToSessionId = Id,
                Body = sessionResult.ToJson().ToUTF8Bytes()
            });

            var scoringResultMessage = await _scoringSession.ReceiveAsync();
            var scoringResult = scoringResultMessage.Body.FromUTF8Bytes();

            await _scoringSession.CompleteAsync(scoringResultMessage.SystemProperties.LockToken);

            return scoringResult.To<SessionResult>();
        }

        public async Task RequestGameAsync()
        {
            await StartSessionAsync();
            await _gameRequest.SendAsync(new Message
            {
                ReplyToSessionId = Id
            });

            var gameData = await _session.ReceiveAsync();

            Opponent = new Opponent(gameData.ReplyToSessionId);

            await _session.CompleteAsync(gameData.SystemProperties.LockToken);
        }

        public async Task StartSessionAsync()
        {
            if (_session is null)
            {
                _session = await _responseClient.AcceptMessageSessionAsync(Id);
            }
        }

        public async Task StartScoringSessionAsync()
        {
            if (_scoringSession is null)
            {
                _scoringSession = await _sessionResultsClient.AcceptMessageSessionAsync(Id);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _requestClient?.CloseAsync();
            await _session?.CloseAsync();
            await _responseClient?.CloseAsync();
            await _playerShapeSelectionClient?.CloseAsync();
        }
    }
}