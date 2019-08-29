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
        private readonly ILogger<StateClient> _logger;

        public StateClient(HttpClient httpClient, JsonSerializerOptions options, ILogger<StateClient> logger)
        {
            HttpClient = httpClient;
            Options = options;

            _logger = logger;
        }

        public HttpClient HttpClient { get; }

        public JsonSerializerOptions Options { get; }

        public async Task<T> GetStateAsync<T>(string key, CancellationToken cancellationToken = default)
            where T : class
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"state/{key}");
            var response = await HttpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                return await JsonSerializer.DeserializeAsync<T>(stream, Options, cancellationToken);
            }
        }

        public async Task SetStateAsync(string key, object value, CancellationToken cancellationToken = default)
        {
            var text = JsonSerializer.Serialize(new object[] { new { key = key, value = value, } }, options: Options);
            _logger.LogInformation("THE TEXT YO: {Text}", text);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new object[] { new { key = key, value = value, } }, options: Options);
            var request = new HttpRequestMessage(HttpMethod.Post, "");
            request.Content = new ByteArrayContent(bytes);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await HttpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
    }
}