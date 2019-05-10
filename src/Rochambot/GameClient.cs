using System;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace Rochambot
{
    public class GameClient : IAsyncDisposable
    {
        readonly IConfiguration _configuration;
        readonly IQueueClient _requestClient;
        readonly ISessionClient _responseClient;
        readonly IQueueClient _gameRequest;

        IMessageSession _session;

        public string Id { get; private set; }

        public Opponent Opponent { get; private set; }

        public GameClient(IConfiguration configuration)
        {
            _configuration = configuration;
            _requestClient = new QueueClient(_configuration["ConnectionString"], _configuration["RequestQueueName"]);
            _responseClient = new SessionClient(_configuration["ConnectionString"], _configuration["ResponseQueueName"]);
            _gameRequest = new QueueClient(_configuration["ConnectionString"], _configuration["GameQueueName"]);
            Id = Guid.NewGuid().ToString();
        }

        public async Task<Shape> SelectShape()
        {
            await StartSession();

            var requestMessage = new Message
            {
                SessionId = Opponent.Id,
                ReplyToSessionId = Id
            };

            await _requestClient.SendAsync(requestMessage);
            var message = await _session.ReceiveAsync();

            var shape = UTF8Encoding.UTF8.GetString(message.Body);

            await _session.CompleteAsync(message.SystemProperties.LockToken);

            return Enum.Parse<Shape>(shape);
        }

        public async Task RequestGame()
        {
            await StartSession();

            var gameRequestMessage = new Message
            {
                ReplyToSessionId = Id
            };

            await _gameRequest.SendAsync(gameRequestMessage);

            var gameData = await _session.ReceiveAsync();
            Opponent = new Opponent(gameData.ReplyToSessionId);
            await _session.CompleteAsync(gameData.SystemProperties.LockToken);
        }

        public async Task StartSession()
        {
            if (_session == null)
            {
                _session = await _responseClient.AcceptMessageSessionAsync(Id);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if(_requestClient != null)
            {
                await _requestClient.CloseAsync();
            }

            if (_session != null)
            {
                await _session.CloseAsync();
            }

            if ( _responseClient != null)
            {
                await _responseClient.CloseAsync();
            }
        }
    }
}