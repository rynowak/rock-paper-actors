using Dapr;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Player;

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

        [Topic("pubsub", "game-ready")]
        [HttpPost("/game-ready")]
        public void GameComplete(GameInfo game)
        {
            stateService.Complete(game);
        }

        [Topic("pubsub", "game-complete")]
        [HttpPost("/game-complete")]
        public void GameComplete(GameResult result)
        {
            stateService.Complete(result);
        }
    }
}