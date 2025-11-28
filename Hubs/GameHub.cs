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

        // ================================================
        // UTILS
        // ================================================
        private string GetUserId()
        {
            return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new HubException("User not authenticated");
        }

        private string GetUserName()
        {
            return Context.User?.Identity?.Name ?? "Unknown";
        }

        // ================================================
        // CONNECTION EVENTS
        // ================================================
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

        // ============================================================
        // 🔥 LOBBY EVENTS 
        // ============================================================
        public async Task JoinLobbyGroup(int roomId)
        {
            var uid = GetUserId();
            var username = GetUserName();
            var group = $"Lobby_{roomId}";

            await Groups.AddToGroupAsync(Context.ConnectionId, group);

            _logger.LogInformation($"[SignalR] {username} joined lobby {roomId}");

            await Clients.OthersInGroup(group).SendAsync("LobbyPlayerJoined", new
            {
                UserId = uid,
                Username = username
            });

            var room = await _gameService.GetRoomSummaryAsync(roomId);
            await Clients.Caller.SendAsync("LobbyUpdated", room);
        }

        public async Task LeaveLobbyGroup(int roomId)
        {
            var uid = GetUserId();
            var username = GetUserName();
            var group = $"Lobby_{roomId}";

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);

            _logger.LogInformation($"[SignalR] {username} left lobby {roomId}");

            await Clients.OthersInGroup(group).SendAsync("LobbyPlayerLeft", new
            {
                UserId = uid,
                Username = username
            });
        }

        public async Task NotifyLobbyUpdated(int roomId)
        {
            var group = $"Lobby_{roomId}";
            var room = await _gameService.GetRoomSummaryAsync(roomId);
            await Clients.Group(group).SendAsync("LobbyUpdated", room);
        }

        // ============================================================
        // 🔥 GAME EVENTS
        // ============================================================

        public async Task JoinGameGroup(int gameId)
        {
            var uid = GetUserId();
            var username = GetUserName();
            var group = $"Game_{gameId}";

            await Groups.AddToGroupAsync(Context.ConnectionId, group);

            _logger.LogInformation($"[SignalR] {username} joined game {gameId}");

            await Clients.OthersInGroup(group).SendAsync("PlayerJoined", username);

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

        public async Task LeaveGameGroup(int gameId)
        {
            var uid = GetUserId();
            var username = GetUserName();
            var group = $"Game_{gameId}";

            _logger.LogInformation($"[SignalR] {username} left game {gameId}");

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
            await Clients.OthersInGroup(group).SendAsync("PlayerLeft", username);
        }

        public async Task SendMove(int gameId)
        {
            var uid = GetUserId();
            var username = GetUserName();
            var group = $"Game_{gameId}";

            try
            {
                var move = await _gameService.RollDiceAndMoveAsync(gameId, int.Parse(uid));

                // Si hay pregunta de profesor, enviarla solo al jugador
                if (move.RequiresProfesorAnswer && move.ProfesorQuestion != null)
                {
                    await Clients.Caller.SendAsync("ReceiveProfesorQuestion", move.ProfesorQuestion);
                }

                // Notificar movimiento
                await Clients.Group(group).SendAsync("MoveCompleted", new
                {
                    UserId = uid,
                    Username = username,
                    MoveResult = move
                });

                // Enviar estado actualizado
                var state = await _gameService.GetGameStateAsync(gameId);
                await Clients.Group(group).SendAsync("GameStateUpdate", state);

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

        // ============================================================
        // 🔥 NEW: ANSWER PROFESOR QUESTION
        // ============================================================
        public async Task AnswerProfesorQuestion(int gameId, string answer)
        {
            var uid = GetUserId();
            var username = GetUserName();
            var group = $"Game_{gameId}";

            try
            {
                var result = await _gameService.AnswerProfesorQuestionAsync(
                    gameId,
                    int.Parse(uid),
                    answer
                );

                await Clients.Group(group).SendAsync("MoveCompleted", new
                {
                    UserId = uid,
                    Username = username,
                    MoveResult = result
                });

                var state = await _gameService.GetGameStateAsync(gameId);
                await Clients.Group(group).SendAsync("GameStateUpdate", state);

                if (result.IsWinner)
                {
                    await Clients.Group(group).SendAsync("GameFinished", new
                    {
                        WinnerId = uid,
                        WinnerName = state.WinnerName,
                        Message = result.Message
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profesor answer error");
                await Clients.Caller.SendAsync("MoveError", ex.Message);
            }
        }

        // ============================================================
        // SURRENDER
        // ============================================================
        public async Task SendSurrender(int gameId)
        {
            var uid = GetUserId();
            var username = GetUserName();
            var group = $"Game_{gameId}";

            try
            {
                await _gameService.SurrenderAsync(gameId, int.Parse(uid));

                await Clients.Group(group).SendAsync("PlayerSurrendered", username);

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