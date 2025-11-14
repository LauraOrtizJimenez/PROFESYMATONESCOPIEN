using System.ComponentModel.DataAnnotations;

namespace Proyecto1.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public int GamesPlayed { get; set; } = 0;
        public int GamesWon { get; set; } = 0;
        
        // Navigation
        public ICollection<Player> Players { get; set; } = new List<Player>();
    }
} 