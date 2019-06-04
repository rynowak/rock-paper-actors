using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace MatchMaker
{
    public class PlayerData
    {
        private readonly IConfiguration _configuration;
        private readonly CosmosClient _cosmosClient;
        private readonly CosmosContainer _playerContainer;

        public PlayerData(IConfiguration configuration)
        {
            _configuration = configuration;
            _cosmosClient = new CosmosClient(_configuration["CosmosEndpointUri"], _configuration["CosmosAccountKey"]);
            _playerContainer = _cosmosClient.Databases["Rochambot"].Containers["Players"];
        }

        public async Task<bool> PlayerExists(string playerId)
        {
            var playerInGameResponse = await _playerContainer.Items.ReadItemAsync<Player>(true, playerId);
            var idlePlayerResponse = await _playerContainer.Items.ReadItemAsync<Player>(false, playerId);
            return playerInGameResponse.StatusCode == HttpStatusCode.Found ||
                   idlePlayerResponse.StatusCode == HttpStatusCode.Found;
        }

        public async Task<Player> CreatePlayer(string playerId)
        {
            await _playerContainer.Items.CreateItemAsync<Player>(false, new Player
            {
                Id = playerId,
                IsInGame = false,
                IsReadyForGame = true,
                LastSeen = DateTime.Now
            });

            return await GetPlayer(playerId);
        }

        public async Task<Player> GetPlayer(string playerId)
        {
            Player result = null;
            var playerInGameResponse = await _playerContainer.Items.ReadItemAsync<Player>(true, playerId);
            if(playerInGameResponse.StatusCode == HttpStatusCode.Found) result = playerInGameResponse.Resource;

            var idlePlayerResponse = await _playerContainer.Items.ReadItemAsync<Player>(false, playerId);
            if(idlePlayerResponse.StatusCode == HttpStatusCode.Found) result = idlePlayerResponse.Resource;

            return result;
        }

        public async Task<bool> IsPlayerReady(string playerId)
        {
            bool result = false;
            var idlePlayerResponse = await _playerContainer.Items.ReadItemAsync<Player>(false, playerId);
            if(idlePlayerResponse.StatusCode == HttpStatusCode.Found)
            {
                result = idlePlayerResponse.Resource.IsReadyForGame;
            }
            return result;
        }

        public async Task<bool> ReadyUpForGame(string playerId)
        {
            var idlePlayer = (await _playerContainer.Items.ReadItemAsync<Player>(false, playerId)).Resource;
            idlePlayer.IsReadyForGame = true;
            
            await _playerContainer.Items.ReplaceItemAsync<Player>(false, playerId, idlePlayer);

            return true;
        }

        public async Task<string> SelectOpponent(string challengerPlayerId)
        {
            var options = new CosmosQueryRequestOptions { MaxItemCount = -1 };
            // todo: find a match
        }
    }
}