using System.ComponentModel.DataAnnotations;

namespace Proyecto1.Models
{
    public class DiceRoll
    {
        [Key]
        public int Id { get; set; }
        
        public int GameId { get; set; }
        public int PlayerId { get; set; }
        
        public int Value { get; set; }
        public DateTime RolledAt { get; set; } = DateTime.UtcNow;
    }
}