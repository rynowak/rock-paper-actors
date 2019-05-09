using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace RobbyBot
{
    public class Worker : IHostedService
    {
        private readonly ILogger<Worker> _logger;
        private IQueueClient _requestClient;
        private IQueueClient _responseClient;
        private IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public Task StartAsync(CancellationToken token)
        {
            _requestClient = new QueueClient(_configuration["ConnectionString"], _configuration["RequestQueueName"]);
            _responseClient = new QueueClient(_configuration["ConnectionString"], _configuration["ResponseQueueName"]);
            _requestClient.RegisterMessageHandler(RecievedRequest, RecievedError);
            return Task.CompletedTask;
        }

        private static Task RecievedError(ExceptionReceivedEventArgs arg)
        {
            throw new NotImplementedException();
        }

        string[] Shapes = new string[] {
            "Rock",
            "Paper",
            "Scissors"
        };

        private async Task RecievedRequest(Message message, CancellationToken cancellationToken)
        {
            Random rand = new Random();

            Console.WriteLine($"Message Recieved {message}");
            var respMessage = new Message(UTF8Encoding.UTF8.GetBytes(Shapes[rand.Next(0,2)]));
            respMessage.SessionId = message.ReplyToSessionId;

            await _responseClient.SendAsync(respMessage);
            await _requestClient.CompleteAsync(message.SystemProperties.LockToken);
        }

        public async Task StopAsync(CancellationToken token)
        {
            await _requestClient.CloseAsync();
            await _responseClient.CloseAsync();
        }
    }
}
