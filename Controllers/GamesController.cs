using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto1.DTOs.Games;
using Proyecto1.Services.Interfaces;

namespace Proyecto1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GamesController : ControllerBase
    {
        private readonly IGameService _gameService;

        public GamesController(IGameService gameService)
        {
            _gameService = gameService;
        }

        [HttpPost]
        public async Task<ActionResult<GameStateDto>> CreateGame([FromBody] CreateGameRequest request)
        {
            try
            {
                var game = await _gameService.CreateGameAsync(request.RoomId);
                var gameState = await _gameService.GetGameStateAsync(game.Id);

                return Ok(gameState);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{gameId}")]
        public async Task<ActionResult<GameStateDto>> GetGameState(int gameId)
        {
            try
            {
                var gameState = await _gameService.GetGameStateAsync(gameId);
                return Ok(gameState);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("{gameId}/status")]
        public async Task<ActionResult> GetGameStatus(int gameId)
        {
            try
            {
                var gameState = await _gameService.GetGameStateAsync(gameId);
                return Ok(new
                {
                    gameId = gameState.GameId,
                    status = gameState.Status,
                    currentPlayer = gameState.CurrentPlayerName,
                    currentPhase = gameState.CurrentTurnPhase
                });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}