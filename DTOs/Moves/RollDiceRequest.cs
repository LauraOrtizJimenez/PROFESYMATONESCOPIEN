using System.ComponentModel.DataAnnotations;

namespace Proyecto1.DTOs.Moves
{
    public class RollDiceRequest
    {
        [Required]
        public int GameId { get; set; }
    }
}