using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MatchMaker
{
    public class GameClient
    {
        private readonly ILogger<GameClient> _logger;

        public GameClient(HttpClient httpClient, JsonSerializerOptions options, ILogger<GameClient> logger)
        {
            HttpClient = httpClient;
            Options = options;
            _logger = logger;
        }

        public HttpClient HttpClient { get; set; }
        public JsonSerializerOptions Options { get; }

        public async Task<string> CreateGameAsync(UserInfo[] players)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, "/game");
            var bytes = JsonSerializer.SerializeToUtf8Bytes(players, Options);
            request.Content = new ByteArrayContent(bytes);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            using (var body = await response.Content.ReadAsStreamAsync())
            {
                return await JsonSerializer.DeserializeAsync<string>(body, Options);
            }
        }
    }
}