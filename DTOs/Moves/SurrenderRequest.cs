using System.ComponentModel.DataAnnotations;

namespace Proyecto1.DTOs.Moves
{
    public class SurrenderRequest
    {
        [Required]
        public int GameId { get; set; }
    }
}