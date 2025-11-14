using System.ComponentModel.DataAnnotations;
using Proyecto1.Models.Enums;

namespace Proyecto1.Models
{
    public class Room
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public int MaxPlayers { get; set; } = 4;
        public int CurrentPlayers { get; set; } = 0;
        
        public RoomStatus Status { get; set; } = RoomStatus.Open;
        
        public int CreatorUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation
        public Game? Game { get; set; }
        public ICollection<Player> Players { get; set; } = new List<Player>();
    }
}