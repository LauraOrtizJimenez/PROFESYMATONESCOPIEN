using System.ComponentModel.DataAnnotations;

namespace Proyecto1.DTOs.Lobby
{
    public class CreateRoomRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Range(2, 6)]
        public int MaxPlayers { get; set; } = 4;
    }
}