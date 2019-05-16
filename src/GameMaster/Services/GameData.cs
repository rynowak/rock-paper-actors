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
            var result = await GamesContainer.Items.ReadItemAsync<Game>(gameId, gameId);
            return result.StatusCode == HttpStatusCode.Found;
        }

        public async Task<Game> CreateGame(string gameId)
        {
            await GamesContainer.Items.CreateItemAsync<Game>(gameId, new Game { GameId = gameId });
            return await GetGame(gameId);
        }

        public async Task<Game> GetGame(string gameId)
        {
            CosmosItemResponse<Game> game = (await this.GamesContainer.Items.ReadItemAsync<Game>(gameId, gameId));
            return game.Resource;
        }

        public bool IsGameComplete(Game game)
        {
            if(game.Turns == null || game.Turns.Count() == 0) return false;

            var player1wins = game.Turns.Where(x => x.Player1.IsWinner).Count();
            var player2wins = game.Turns.Where(x => x.Player2.IsWinner).Count();
            return (player1wins >= game.NumberOfTurnsNeededToWin) || (player2wins >= game.NumberOfTurnsNeededToWin);
        }

        public async Task<Game> StartTurn(string gameId, Play play)
        {
            var game = await GetGame(gameId);
            if(game.Turns == null || game.Turns.Count() == 0)
                game.Turns = new List<Turn>().ToArray();
            var turns = game.Turns.ToList();
            turns.Add(new Turn { Player1 = play });
            game.Turns = turns.ToArray();
            await GamesContainer.Items.ReplaceItemAsync<Game>(gameId, gameId, game);
            return game;
        }

        public bool IsTurnComplete(Game game)
        {
            if(game.Turns == null || !game.Turns.Any()) return true; // this is a new game, start a new turn
            return (game.Turns.Last().Player1 != null 
                && game.Turns.Last().Player2 == null); // player 1 went, player 2 didn't, return false
        }

        public async Task<Game> CompleteTurn(string gameId, Play play)
        {
            var game = await GetGame(gameId);
            var turns = game.Turns.ToList();
            
            turns.Last().Player2 = play;
            turns.Last().TurnEnded = DateTime.Now;
            turns.Last().DetermineScore();
            game.Turns = turns.ToArray();

            if(IsGameComplete(game))
            {
                var player1wins = game.Turns.Where(x => x.Player1.IsWinner).Count();
                var player2wins = game.Turns.Where(x => x.Player2.IsWinner).Count();
                if(player1wins > player2wins) game.WinnerPlayerId = game.Turns[0].Player1.PlayerId;
                else game.WinnerPlayerId = game.Turns[0].Player2.PlayerId;
            }

            await GamesContainer.Items.ReplaceItemAsync<Game>(gameId, gameId, game);
            return game;
        }
    }
}