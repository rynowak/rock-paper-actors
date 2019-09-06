using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameMaster
{
    [ApiController]
    public class GameController : ControllerBase
    {
        private readonly StateClient _stateClient;
        private readonly ILogger<GameController> _logger;

        public GameController(StateClient stateClient, ILogger<GameController> logger)
        {
            _stateClient = stateClient;
            _logger = logger;
        }

        [HttpGet("{gameId}")]
        public async Task<ActionResult<GameState>> GetGameAsync(string gameId)
        {
            var game = await _stateClient.GetStateAsync<GameState>(gameId);
            if (game == null)
            {
                _logger.LogInformation("Game {GameId} was not found.", gameId);
                return NotFound();
            }

            return game;
        }

        [HttpPut("/create")]
        public async Task<ActionResult<string>> CreateGameAsync([FromBody] UserInfo[] players)
        {
            var gameId = Guid.NewGuid().ToString();
            var gameState = new GameState()
            {
                GameId = gameId,
                Players = players,
                Moves = new List<PlayerMove>(),
            };

            await _stateClient.SetStateAsync(gameId, gameState);
            return "\"" + gameId + "\"";
        }

        [HttpPost("{gameId}")]
        public async Task<ActionResult<GameState>> PlayAsync(string gameId, PlayerMove move)
        {
            var game = await _stateClient.GetStateAsync<GameState>(gameId);
            if (game == null)
            {
                _logger.LogInformation("Game {GameId} was not found.", gameId);
                return NotFound();
            }

            _logger.LogInformation("Found game {GameId}.", gameId);

            if (!game.Players.Any(p => p.Username == move.Player.Username))
            {
                _logger.LogInformation("Player {UserId} is not part of game {GameId}.", move.Player.Username, gameId);
                return BadRequest("Player is not part of this game.");
            }

            if (game.Moves.Any(p => p.Player.Username == move.Player.Username))
            {
                _logger.LogInformation("Player {UserId} has already made a move in {GameId}.", move.Player.Username, gameId);
                return BadRequest("Player has already made a move.");
            }

            _logger.LogInformation("Player {UserId} has make move {Move} in {GameId}.", move.Player.Username, move.Move, gameId);
            game.Moves.Add(move);
            if (game.IsComplete)
            {
                var (shape0, shape1) = (game.Moves[0].Move, game.Moves[1].Move);
                if (shape0 == shape1)
                {
                    // Draw
                    _logger.LogInformation("Game {GameId} is a draw.", gameId);
                } 
                else if ((((int)shape0 - (int)shape1) % 3) == 2)
                {
                    // Player0 wins!
                    game.Winner = game.Players[0];
                    _logger.LogInformation("Player {UserId} wins {GameId}.", game.Players[0].Username, gameId);

                }
                else
                {
                    // Player1 wins!
                    game.Winner = game.Players[1];
                    _logger.LogInformation("Player {UserId} wins {GameId}.", game.Players[1].Username, gameId);
                }
            }

            await _stateClient.SetStateAsync(gameId, game);
            return game;
        }
    }
}