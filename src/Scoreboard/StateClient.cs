using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GameMaster
{
    public class StateClient
    {
        public StateClient(HttpClient httpClient, JsonSerializerOptions options, ILoggerFactory loggerFactory)
        {
            HttpClient = httpClient;
            Options = options;

            Logger = loggerFactory.CreateLogger(GetType());
        }

        public HttpClient HttpClient { get; }

        public JsonSerializerOptions Options { get; }

        protected ILogger Logger { get; }

        public async ValueTask<T> GetStateAsync<T>(string key, CancellationToken cancellationToken = default)
            where T : class
        {
            Logger.LogInformation("Getting state for key {StateKey} of type {StateType}.", key, typeof(T));

            var url = HttpClient.BaseAddress == null ? $"http://locahost:3500/v1.0/state/{key}" : $"/v1.0/state/{key}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await HttpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Logger.LogInformation("State was not found for key {StateKey}", key);
                return null;
            }

            try
            {
                response.EnsureSuccessStatusCode();
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    return await JsonSerializer.DeserializeAsync<T>(stream, Options, cancellationToken);
                }
            }
            catch (HttpRequestException ex)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to get state: {error}", ex);
            }
        }

        public async Task SetStateAsync(string key, object value, CancellationToken cancellationToken = default)
        {
            Logger.LogInformation("Setting state for key {StateKey} of type {StateType}.", key, value?.GetType());

            var obj = new object[] { new { key = key, value = value, } };
            var url = HttpClient.BaseAddress == null ? $"http://locahost:3500/v1.0/state" : $"/v1.0/state";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(obj, options: Options));
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
                throw new HttpRequestException($"Failed to set state: {error}", ex);
            }        
        }
    }
}