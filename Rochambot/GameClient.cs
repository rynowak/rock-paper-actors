using System;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace Rochambot
{
    public class GameClient
    {
        private IConfiguration _configuration;

        IQueueClient _requestClient;
        ISessionClient _responseClient;

        public string GameId {get;private set;}

        IMessageSession _session;

        public GameClient(IConfiguration configuration)
        {
            _configuration = configuration;
            _requestClient = new QueueClient(_configuration["ConnectionString"], _configuration["RequestQueueName"]);
            _responseClient = new SessionClient(_configuration["ConnectionString"], _configuration["ResponseQueueName"]);
            GameId = Guid.NewGuid().ToString();
        }

        public async Task RequestShape(Action<Shape> responseAction)
        {
            if(_session == null)
            {
                _session = await _responseClient.AcceptMessageSessionAsync(GameId);
            }

            //TODO: Make this less synchronous.
            var requestMessage = new Message();
            requestMessage.ReplyToSessionId = GameId;
            await _requestClient.SendAsync(requestMessage);
            var message = await _session.ReceiveAsync();
            var shape = UTF8Encoding.UTF8.GetString(message.Body);
            responseAction(Enum.Parse<Shape>(shape));
            await _session.CompleteAsync(message.SystemProperties.LockToken);
        }
    }
}