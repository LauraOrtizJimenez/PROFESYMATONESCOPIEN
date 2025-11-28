using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto1.DTOs.Lobby;
using Proyecto1.Services;
using System.Security.Claims;
using Proyecto1.Services.Interfaces;

namespace Proyecto1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LobbyController : ControllerBase
    {
        private readonly IRoomService _roomService;
        private readonly ILogger<LobbyController> _logger;

        public LobbyController(IRoomService roomService, ILogger<LobbyController> logger)
        {
            _roomService = roomService;
            _logger = logger;
        }

        // ==========================================================
        // POST api/Lobby/rooms   → Crear sala
        // ==========================================================
        [HttpPost("rooms")]
        public async Task<ActionResult<RoomSummaryDto>> CreateRoom([FromBody] CreateRoomRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            try
            {
                var room = await _roomService.CreateRoomAsync(request.Name, request.MaxPlayers, userId);

                // Auto-join del creador
                var player = await _roomService.JoinRoomAsync(room.Id, userId);

                // Recargar room con players actualizados
                var roomDetail = await _roomService.GetRoomWithDetailsAsync(room.Id);

                if (roomDetail == null)
                {
                    return StatusCode(500, new { message = "Room created but could not retrieve details" });
                }

                return Ok(new RoomSummaryDto
                {
                    Id = roomDetail.Id,
                    Name = roomDetail.Name,

                    // 🔴 ANTES: CurrentPlayers = roomDetail.CurrentPlayers,
                    // ✅ AHORA: sacamos el número real de la lista de jugadores
                    CurrentPlayers = roomDetail.Players.Count,

                    MaxPlayers = roomDetail.MaxPlayers,
                    Status = roomDetail.Status.ToString(),
                    CreatedAt = roomDetail.CreatedAt,
                    PlayerNames = roomDetail.Players
                        .Select(p => p.User.Username)
                        .ToList(),
                    GameId = roomDetail.Game?.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating room for user {UserId}", userId);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================================
        // POST api/Lobby/rooms/join   → Unirse a sala
        // ==========================================================
        [HttpPost("rooms/join")]
        public async Task<ActionResult> JoinRoom([FromBody] JoinRoomRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            try
            {
                await _roomService.JoinRoomAsync(request.RoomId, userId);

                var room = await _roomService.GetRoomWithDetailsAsync(request.RoomId);

                if (room == null)
                    return NotFound(new { message = "Room not found" });

                return Ok(new
                {
                    message = "Successfully joined room",
                    room = new RoomSummaryDto
                    {
                        Id = room.Id,
                        Name = room.Name,

                        // 🔁 Igual aquí: contar jugadores reales
                        CurrentPlayers = room.Players.Count,

                        MaxPlayers = room.MaxPlayers,
                        Status = room.Status.ToString(),
                        CreatedAt = room.CreatedAt,
                        PlayerNames = room.Players
                            .Select(p => p.User.Username)
                            .ToList(),
                        GameId = room.Game?.Id
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining room {RoomId} for user {UserId}", request.RoomId, userId);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==========================================================
        // GET api/Lobby/rooms   → Listar salas disponibles
        // ==========================================================
        [HttpGet("rooms")]
        public async Task<ActionResult<List<RoomSummaryDto>>> GetAvailableRooms()
        {
            try
            {
                var rooms = await _roomService.GetAvailableRoomsAsync();

                var roomDtos = rooms.Select(r => new RoomSummaryDto
                {
                    Id = r.Id,
                    Name = r.Name,

                    // 🔁 Aquí también usamos la lista de Players
                    CurrentPlayers = r.Players.Count,

                    MaxPlayers = r.MaxPlayers,
                    Status = r.Status.ToString(),
                    CreatedAt = r.CreatedAt,
                    PlayerNames = r.Players
                        .Select(p => p.User.Username)
                        .ToList(),
                    GameId = r.Game?.Id
                }).ToList();

                return Ok(roomDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available rooms");
                return StatusCode(500, new { message = "Error retrieving rooms" });
            }
        }

        // ==========================================================
        // GET api/Lobby/rooms/{roomId}   → Detalle de una sala
        // ==========================================================
        [HttpGet("rooms/{roomId}")]
        public async Task<ActionResult<RoomSummaryDto>> GetRoom(int roomId)
        {
            try
            {
                var room = await _roomService.GetRoomWithDetailsAsync(roomId);

                if (room == null)
                    return NotFound(new { message = "Room not found" });

                return Ok(new RoomSummaryDto
                {
                    Id = room.Id,
                    Name = room.Name,

                    // 🔁 Y aquí igual:
                    CurrentPlayers = room.Players.Count,

                    MaxPlayers = room.MaxPlayers,
                    Status = room.Status.ToString(),
                    CreatedAt = room.CreatedAt,
                    PlayerNames = room.Players
                        .Select(p => p.User.Username)
                        .ToList(),
                    GameId = room.Game?.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting room {RoomId}", roomId);
                return StatusCode(500, new { message = "Error retrieving room" });
            }
        }
    }
}