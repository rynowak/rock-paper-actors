using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace GameMaster
{
    public class GameData
    {
        public GameData(IConfiguration configuration)
        {
            Configuration = configuration;
            CosmosClient = new CosmosClient(Configuration["CosmosEndpointUri"], Configuration["CosmosAccountKey"]);
            GamesContainer = CosmosClient.Databases["Rochambot"].Containers["Games"];
        }

        public IConfiguration Configuration { get; }
        public CosmosClient CosmosClient { get; }
        public CosmosContainer GamesContainer { get; }

        public async Task<bool> GameExists(string gameId)
        {
            return ((await GamesContainer.Items.ReadItemAsync<Game>(gameId, gameId)).StatusCode == HttpStatusCode.Found);
        }

        public async Task<Game> CreateGame(Game game)
        {
            await GamesContainer.Items.CreateItemAsync<Game>(game.GameId, game);
            return await GetGame(game.GameId);
        }

        public async Task<Game> GetGame(string gameId)
        {
            CosmosItemResponse<Game> game = (await this.GamesContainer.Items.ReadItemAsync<Game>(gameId, gameId));
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
            await GamesContainer.Items.ReplaceItemAsync<Game>(gameId, gameId, game);
        }

        public bool IsTurnComplete(Game game)
        {
            return game.Turns.Any(x => x.Player2 == null);
        }

        public async Task<Game> CompleteTurn(string gameId, Play play)
        {
            var game = await GetGame(gameId);
            var turns = game.Turns.ToList();
            
            turns.Last().Player2 = play;
            turns.Last().DetermineScore();

            game.Turns = turns.ToArray();
            await GamesContainer.Items.ReplaceItemAsync<Game>(gameId, gameId, game);
            return game;
        }
    }
}