using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Rochambot
{
    public class MatchMakerClient
    {
        public MatchMakerClient(HttpClient httpClient, JsonSerializerOptions options)
        {
            HttpClient = httpClient;
            Options = options;
        }

        public HttpClient HttpClient { get; }
        public JsonSerializerOptions Options { get; }

        public async Task<GameInfo> JoinGameAsync(UserInfo user, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/game");
            var bytes = JsonSerializer.SerializeToUtf8Bytes(user, Options);
            request.Content = new ByteArrayContent(bytes);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            using (var body = await response.Content.ReadAsStreamAsync())
            {
                return await JsonSerializer.DeserializeAsync<GameInfo>(body, Options);
            }
        }
    }
}