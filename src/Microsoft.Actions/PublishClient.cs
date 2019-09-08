using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Actions
{
    public class PublishClient
    {
        public PublishClient(HttpClient httpClient, JsonSerializerOptions options, ILoggerFactory loggerFactory)
        {
            HttpClient = httpClient;
            Options = options;

            Logger = loggerFactory.CreateLogger(GetType());
        }

        public HttpClient HttpClient { get; }

        public JsonSerializerOptions Options { get; }

        protected ILogger Logger { get; }

        public async Task PublishAsync(string topic, object data, CancellationToken cancellationToken = default)
        {
            Logger.LogInformation("Publishing message to topic {Topic} of type {MessageType}.", topic, data?.GetType());

            var url = HttpClient.BaseAddress == null ? $"http://locahost:3500/v1.0/publish/{topic}" : $"/v1.0/publish/{topic}";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(data, options: Options));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await HttpClient.SendAsync(request, cancellationToken);
            try
            {
                response.EnsureSuccessStatusCode();
                return;
            }
            catch (HttpRequestException ex)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to publish message: {error}", ex);
            }
        }
    }
}
