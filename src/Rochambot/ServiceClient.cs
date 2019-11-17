using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Actions
{
    public class ServiceClient
    {
        public ServiceClient(HttpClient httpClient, JsonSerializerOptions options, ILoggerFactory loggerFactory)
        {
            HttpClient = httpClient;
            Options = options;

            Logger = loggerFactory.CreateLogger(GetType());
        }

        public HttpClient HttpClient { get; }

        public JsonSerializerOptions Options { get; }

        protected ILogger Logger { get; }

        public async ValueTask<TResponse> GetAsync<TResponse>(HttpMethod httpMethod, string service, string operation, CancellationToken cancellationToken = default)
        {
            Logger.LogInformation("Sending message {HttpMethod} {Service}::{Operation}.", httpMethod, service, operation);

            var url = HttpClient.BaseAddress == null ? $"http://locahost:3500/v1.0/invoke/{service}/method/{operation}" : $"/v1.0/invoke/{service}/method/{operation}";
            var request = new HttpRequestMessage(httpMethod, url);

            var response = await HttpClient.SendAsync(request, cancellationToken);
            try
            {
                response.EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentType.MediaType == "application/json")
                {
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        return await JsonSerializer.DeserializeAsync<TResponse>(stream, Options, cancellationToken);
                    }
                }

                return default(TResponse);
            }
            catch (HttpRequestException ex)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to send message: {error}", ex);
            }
        }

        public async ValueTask<TResponse> SendAsync<TResponse>(HttpMethod httpMethod, string service, string operation, object data, CancellationToken cancellationToken = default)
        {
            Logger.LogInformation("Sending message {HttpMethod} {Service}::{Operation} of type {DataType}.", httpMethod, service, operation, data?.GetType());

            var url = HttpClient.BaseAddress == null ? $"http://locahost:3500/v1.0/invoke/{service}/method/{operation}" : $"/v1.0/invoke/{service}/method/{operation}";
            var request = new HttpRequestMessage(httpMethod, url);
            request.Content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(data, options: Options));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await HttpClient.SendAsync(request, cancellationToken);
            try
            {
                response.EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentType.MediaType == "application/json")
                {
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        return await JsonSerializer.DeserializeAsync<TResponse>(stream, Options, cancellationToken);
                    }
                }

                return default(TResponse);
            }
            catch (HttpRequestException ex)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to send message: {error}", ex);
            }
        }
    }
}
