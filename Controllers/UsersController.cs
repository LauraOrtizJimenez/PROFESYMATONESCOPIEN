using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto1.Infrastructure.Repositories.Interfaces;
using System.Security.Claims;
using Proyecto1.Infrastructure.Repositories;

namespace Proyecto1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IGameRepository _gameRepository;

        public UsersController(IUserRepository userRepository, IGameRepository gameRepository)
        {
            _userRepository = userRepository;
            _gameRepository = gameRepository;
        }

        [HttpGet("me")]
        public async Task<ActionResult> GetCurrentUser()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _userRepository.GetByIdAsync(userId);
            
            if (user == null)
                return NotFound();

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.GamesPlayed,
                user.GamesWon,
                WinRate = user.GamesPlayed > 0 ? (double)user.GamesWon / user.GamesPlayed * 100 : 0,
                user.CreatedAt
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetUser(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            
            if (user == null)
                return NotFound();

            return Ok(new
            {
                user.Id,
                user.Username,
                user.GamesPlayed,
                user.GamesWon,
                WinRate = user.GamesPlayed > 0 ? (double)user.GamesWon / user.GamesPlayed * 100 : 0
            });
        }

        [HttpGet("me/games")]
        public async Task<ActionResult> GetMyGames()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var games = await _gameRepository.GetGamesByUserIdAsync(userId);

            var gamesDto = games.Select(g => new
            {
                g.Id,
                Status = g.Status.ToString(),
                g.StartedAt,
                g.FinishedAt,
                Players = g.Players.Select(p => new
                {
                    p.Id,
                    p.User.Username,
                    Status = p.Status.ToString(),
                    p.Position
                }).ToList(),
                WinnerId = g.WinnerPlayerId,
                WinnerName = g.Players.FirstOrDefault(p => p.Id == g.WinnerPlayerId)?.User.Username
            }).ToList();

            return Ok(gamesDto);
        }

        [HttpGet("leaderboard")]
        [AllowAnonymous]
        public async Task<ActionResult> GetLeaderboard([FromQuery] int limit = 10)
        {
            var users = await _userRepository.GetTopUsersAsync(limit);
            
            var leaderboard = users.Select(u => new
            {
                u.Id,
                u.Username,
                u.GamesPlayed,
                u.GamesWon,
                WinRate = u.GamesPlayed > 0 ? (double)u.GamesWon / u.GamesPlayed * 100 : 0
            }).ToList();

            return Ok(leaderboard);
        }
    }
}
