using System.ComponentModel.DataAnnotations;

namespace Proyecto1.Models
{
    public class Move
    {
        [Key]
        public int Id { get; set; }
        
        public int GameId { get; set; }
        public Game Game { get; set; } = null!;
        
        public int PlayerId { get; set; }
        public Player Player { get; set; } = null!;
        
        public int DiceRoll { get; set; }
        public int FromPosition { get; set; }
        public int ToPosition { get; set; }
        
        public int? SnakeId { get; set; }
        public int? LadderId { get; set; }
        
        public int? FinalPosition { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}