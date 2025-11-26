using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Proyecto1.Services.Interfaces;
using System.Security.Claims;

namespace Proyecto1.Hubs
{
    [Authorize]
    public class GameHub : Hub
    {
        private readonly IGameService _gameService;
        private readonly ILogger<GameHub> _logger;

        public GameHub(IGameService gameService, ILogger<GameHub> logger)
        {
            _gameService = gameService;
            _logger = logger;
        }

        private string GetUserId()
        {
            return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new HubException("User not authenticated");
        }

        public override async Task OnConnectedAsync()
        {
            var uid = GetUserId();
            _logger.LogInformation($"[SignalR] User {uid} connected");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var uid = GetUserId();
            _logger.LogInformation($"[SignalR] User {uid} disconnected");
            await base.OnDisconnectedAsync(exception);
        }

        // ================================================
        // JOIN GROUP
        // ================================================
        public async Task JoinGameGroup(int gameId)
        {
            var uid = GetUserId();
            var group = $"Game_{gameId}";

            await Groups.AddToGroupAsync(Context.ConnectionId, group);
            await Clients.OthersInGroup(group).SendAsync("PlayerJoined", uid);

            try
            {
                var state = await _gameService.GetGameStateAsync(gameId);
                await Clients.Caller.SendAsync("GameStateUpdate", state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending game state");
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        // ================================================
        // LEAVE GROUP
        // ================================================
        public async Task LeaveGameGroup(int gameId)
        {
            var uid = GetUserId();
            var group = $"Game_{gameId}";

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
            await Clients.OthersInGroup(group).SendAsync("PlayerLeft", uid);
        }

        // ================================================
        // ROLL DICE
        // ================================================
        public async Task SendMove(int gameId)
        {
            var uid = GetUserId();
            var group = $"Game_{gameId}";

            try
            {
                var move = await _gameService.RollDiceAndMoveAsync(gameId, int.Parse(uid));

                // Pregunta de profesor SOLO al jugador
                if (move.RequiresProfesorAnswer && move.ProfesorQuestion != null)
                {
                    await Clients.Caller.SendAsync("ReceiveProfesorQuestion", move.ProfesorQuestion);
                }

                // Resultado del movimiento
                await Clients.Group(group).SendAsync("MoveCompleted", new
                {
                    UserId = uid,
                    MoveResult = move
                });

                // Estado actualizado
                var state = await _gameService.GetGameStateAsync(gameId);
                await Clients.Group(group).SendAsync("GameStateUpdate", state);

                // Ganador
                if (move.IsWinner)
                {
                    await Clients.Group(group).SendAsync("GameFinished", new
                    {
                        WinnerId = uid,
                        WinnerName = state.WinnerName,
                        Message = move.Message
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Roll error");
                await Clients.Caller.SendAsync("MoveError", ex.Message);
            }
        }

        // ================================================
        // SURRENDER
        // ================================================
        public async Task SendSurrender(int gameId)
        {
            var uid = GetUserId();
            var group = $"Game_{gameId}";

            try
            {
                await _gameService.SurrenderAsync(gameId, int.Parse(uid));

                await Clients.Group(group).SendAsync("PlayerSurrendered", uid);

                var state = await _gameService.GetGameStateAsync(gameId);
                await Clients.Group(group).SendAsync("GameStateUpdate", state);

                if (state.Status == "Finished")
                {
                    await Clients.Group(group).SendAsync("GameFinished", new
                    {
                        WinnerId = state.WinnerPlayerId,
                        WinnerName = state.WinnerName,
                        Reason = "Other players surrendered"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Surrender error");
                await Clients.Caller.SendAsync("SurrenderError", ex.Message);
            }
        }

        // ================================================
        // REQUEST GAME STATE
        // ================================================
        public async Task RequestGameState(int gameId)
        {
            try
            {
                var state = await _gameService.GetGameStateAsync(gameId);
                await Clients.Caller.SendAsync("GameStateUpdate", state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading game state");
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }
    }
}
 