using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
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

        public async Task<Game> CreateGame(string gameId)
        {
            await _gamesContainer.Items.CreateItemAsync<Game>(gameId, new Game { GameId = gameId });
            return await GetGame(gameId);
        }

        public async Task<Game> GetGame(string gameId)
        {
            var game = await _gamesContainer.Items.ReadItemAsync<Game>(gameId, gameId);
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
            await _gamesContainer.Items.ReplaceItemAsync<Game>(gameId, gameId, game);
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

            await _gamesContainer.Items.ReplaceItemAsync<Game>(gameId, gameId, game);
            return game;
        }
    }
}