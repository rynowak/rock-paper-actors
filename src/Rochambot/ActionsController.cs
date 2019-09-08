using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Rochambot
{
    [ApiController]
    [Route("[controller]")]
    public class ActionsController : ControllerBase
    {
        private readonly GameStateService _stateService;
        private readonly ILogger<ActionsController> _logger;

        public ActionsController(GameStateService stateService, ILogger<ActionsController> logger)
        {
            _stateService = stateService;
            _logger = logger;
        }

        [HttpGet("[action]")]
        public ActionResult<string[]> Subscribe()
        {
            return new [] { "game-complete", };
        }

        [HttpPost("/game-complete")]
        public void Game(GameState state)
        {
            _logger.LogInformation("Completing game {GameId}", state.GameId);
            _stateService.Complete(state);
        }
    }
}