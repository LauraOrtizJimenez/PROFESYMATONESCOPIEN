using System.ComponentModel.DataAnnotations;

namespace Proyecto1.DTOs.Moves
{
    public class MoveRequest
    {
        [Required]
        public int GameId { get; set; }
    }
}