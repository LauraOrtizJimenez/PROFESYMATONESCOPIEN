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

        [HttpPost("rooms")]
        public async Task<ActionResult<RoomSummaryDto>> CreateRoom([FromBody] CreateRoomRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            try
            {
                var room = await _roomService.CreateRoomAsync(request.Name, request.MaxPlayers, userId);
                
                // Auto-join creator to room
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
                    CurrentPlayers = roomDetail.CurrentPlayers,
                    MaxPlayers = roomDetail.MaxPlayers,
                    Status = roomDetail.Status.ToString(),
                    CreatedAt = roomDetail.CreatedAt,
                    PlayerNames = roomDetail.Players.Select(p => p.User.Username).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating room for user {UserId}", userId);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("rooms/join")]
        public async Task<ActionResult> JoinRoom([FromBody] JoinRoomRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            try
            {
                await _roomService.JoinRoomAsync(request.RoomId, userId);
                
                var room = await _roomService.GetRoomWithDetailsAsync(request.RoomId);
                
                return Ok(new 
                { 
                    message = "Successfully joined room",
                    room = new RoomSummaryDto
                    {
                        Id = room!.Id,
                        Name = room.Name,
                        CurrentPlayers = room.CurrentPlayers,
                        MaxPlayers = room.MaxPlayers,
                        Status = room.Status.ToString(),
                        CreatedAt = room.CreatedAt,
                        PlayerNames = room.Players.Select(p => p.User.Username).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining room {RoomId} for user {UserId}", request.RoomId, userId);
                return BadRequest(new { message = ex.Message });
            }
        }

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
                    CurrentPlayers = r.CurrentPlayers,
                    MaxPlayers = r.MaxPlayers,
                    Status = r.Status.ToString(),
                    CreatedAt = r.CreatedAt,
                    PlayerNames = r.Players.Select(p => p.User.Username).ToList()
                }).ToList();

                return Ok(roomDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available rooms");
                return StatusCode(500, new { message = "Error retrieving rooms" });
            }
        }

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
                    CurrentPlayers = room.CurrentPlayers,
                    MaxPlayers = room.MaxPlayers,
                    Status = room.Status.ToString(),
                    CreatedAt = room.CreatedAt,
                    PlayerNames = room.Players.Select(p => p.User.Username).ToList()
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