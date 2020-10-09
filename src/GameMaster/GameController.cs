using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameMaster
{
    [ApiController]
    public class GameController : ControllerBase
    {
        private readonly DaprClient daprClient;
        private readonly ILogger<GameController> logger;

        public GameController(DaprClient daprClient, ILogger<GameController> logger)
        {
            this.daprClient = daprClient;
            this.logger = logger;
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

            await daprClient.SaveStateAsync("statestore", gameId, gameState);
            return "\"" + gameId + "\"";
        }

        [HttpPost("{gameId}")]
        public async Task<ActionResult<GameState>> PlayAsync(string gameId, PlayerMove move, CancellationToken cancellationToken = default)
        {
            var game = await daprClient.GetStateAsync<GameState>("statestore", gameId);
            if (game == null)
            {
                logger.LogInformation("Game {GameId} was not found.", gameId);
                return NotFound();
            }

            logger.LogInformation("Found game {GameId}.", gameId);

            if (!game.Players.Any(p => p.Username == move.Player.Username))
            {
                logger.LogInformation("Player {UserId} is not part of game {GameId}.", move.Player.Username, gameId);
                return BadRequest("Player is not part of this game.");
            }

            if (game.Moves.Any(p => p.Player.Username == move.Player.Username))
            {
                logger.LogInformation("Player {UserId} has already made a move in {GameId}.", move.Player.Username, gameId);
                return BadRequest("Player has already made a move.");
            }

            logger.LogInformation("Player {UserId} has make move {Move} in {GameId}.", move.Player.Username, move.Move, gameId);
            game.Moves.Add(move);
            if (game.IsComplete)
            {
                var (shape0, shape1) = (game.Moves[0].Move, game.Moves[1].Move);
                if (shape0 == shape1)
                {
                    // Draw
                    logger.LogInformation("Game {GameId} is a draw.", gameId);
                } 
                else if ((((int)shape0 - (int)shape1) % 3) == 2)
                {
                    // Player0 wins!
                    game.Winner = game.Players[0];
                    logger.LogInformation("Player {UserId} wins {GameId}.", game.Players[0].Username, gameId);

                }
                else
                {
                    // Player1 wins!
                    game.Winner = game.Players[1];
                    logger.LogInformation("Player {UserId} wins {GameId}.", game.Players[1].Username, gameId);
                }

                await daprClient.PublishEventAsync<GameState>("pubsub", "game-complete", game, cancellationToken);
            }

            await daprClient.SaveStateAsync("statestore", gameId, game);
            return game;
        }
    }
}