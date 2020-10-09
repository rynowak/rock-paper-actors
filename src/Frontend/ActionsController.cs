using Dapr;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Frontend
{
    [ApiController]
    [Route("[controller]")]
    public class ActionsController : ControllerBase
    {
        private readonly GameStateService stateService;
        private readonly ILogger<ActionsController> logger;

        public ActionsController(GameStateService stateService, ILogger<ActionsController> logger)
        {
            this.stateService = stateService;
            this.logger = logger;
        }

        [Topic("pubsub", "game-complete")]
        [HttpPost("/game-complete")]
        public void Game(GameResult result)
        {
            logger.LogInformation("Completing game {GameId}", result.GameId);
            stateService.Complete(result);
        }
    }
}