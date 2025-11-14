using System.ComponentModel.DataAnnotations;

namespace Proyecto1.DTOs.Games
{
    public class CreateGameRequest
    {
        [Required]
        public int RoomId { get; set; }
    }
}