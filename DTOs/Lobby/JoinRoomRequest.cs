using System.ComponentModel.DataAnnotations;

namespace Proyecto1.DTOs.Lobby
{
    public class JoinRoomRequest
    {
        [Required]
        public int RoomId { get; set; }
    }
}