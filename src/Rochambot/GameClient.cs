using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Rochambot
{
    public class GameClient
    {
        public GameClient(HttpClient httpClient, JsonSerializerOptions options)
        {
            HttpClient = httpClient;
            Options = options;
        }

        public HttpClient HttpClient { get; set; }
        public JsonSerializerOptions Options { get; }

        public async Task<GameState> GetStateAsync(GameInfo game, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:3500/v1.0/actions/gamemaster/{game.GameId}");

            var response = await HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            using (var body = await response.Content.ReadAsStreamAsync())
            {
                return await JsonSerializer.DeserializeAsync<GameState>(body, Options);
            }
        }

        public async Task<GameState> PlayAsync(GameInfo game, Shape move, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"http://localhost:3500/v1.0/actions/gamemaster/{game.GameId}");
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new PlayerMove() { Player = game.Player, Move = move, }, Options);
            request.Content = new ByteArrayContent(bytes);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            using (var body = await response.Content.ReadAsStreamAsync())
            {
                return await JsonSerializer.DeserializeAsync<GameState>(body, Options);
            }
        }
    }
}