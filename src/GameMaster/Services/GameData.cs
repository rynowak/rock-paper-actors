using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace GameMaster
{
    public class GameData
    {
        private readonly IConfiguration _configuration;
        private readonly CosmosClient _cosmosClient;
        private readonly CosmosContainer _gamesContainer;

        public GameData(IConfiguration configuration)
        {
            _configuration = configuration;
            _cosmosClient = new CosmosClient(_configuration["CosmosEndpointUri"], _configuration["CosmosAccountKey"]);
            _gamesContainer = _cosmosClient.Databases["Rochambot"].Containers["Games"];
        }

        public async Task<bool> GameExists(string gameId)
        {
            var response = await _gamesContainer.Items.ReadItemAsync<Game>(gameId, gameId);
            return response.StatusCode == HttpStatusCode.Found;
        }

        public async Task<Game> CreateGame(Game game)
        {
            await _gamesContainer.Items.CreateItemAsync<Game>(game.GameId, game);
            return await GetGame(game.GameId);
        }

        public async Task<Game> GetGame(string gameId)
        {
            var game = await _gamesContainer.Items.ReadItemAsync<Game>(gameId, gameId);
            return game.Resource;
        }

        public bool IsGameComplete(Game game)
        {
            var player1wins = game.Turns.Where(x => x.Player1.IsWinner).Count();
            var player2wins = game.Turns.Where(x => x.Player2.IsWinner).Count();

            return (player1wins >= game.NumberOfTurnsNeededToWin) || (player2wins >= game.NumberOfTurnsNeededToWin);
        }

        public async Task StartTurn(string gameId, Play play)
        {
            var game = await GetGame(gameId);
            var turns = game.Turns.ToList();
            turns.Add(new Turn { Player1 = play });
            game.Turns = turns.ToArray();

            await _gamesContainer.Items.ReplaceItemAsync<Game>(gameId, gameId, game);
        }

        public bool IsTurnComplete(Game game) => game.Turns.Any(x => x.Player2 is null);

        public async Task<Game> CompleteTurn(string gameId, Play play)
        {
            var game = await GetGame(gameId);
            var turns = game.Turns.ToList();
            
            turns.Last().Player2 = play;
            turns.Last().DetermineScore();

            game.Turns = turns.ToArray();
            await _gamesContainer.Items.ReplaceItemAsync<Game>(gameId, gameId, game);
            return game;
        }
    }
}