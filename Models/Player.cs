using System.ComponentModel.DataAnnotations;
using Proyecto1.Models.Enums;

namespace Proyecto1.Models
{
    public class Player
    {
        [Key]
        public int Id { get; set; }
        
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        
        // ✅ NULLABLE porque se crea en lobby antes del game
        public int? GameId { get; set; }
        public Game? Game { get; set; }
        
        public int? RoomId { get; set; }
        public Room? Room { get; set; }
        
        public int Position { get; set; } = 0;
        public int TurnOrder { get; set; }
        
        public PlayerStatus Status { get; set; } = PlayerStatus.Waiting;
        
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation
        public ICollection<Move> Moves { get; set; } = new List<Move>();
    }
}