using System.ComponentModel.DataAnnotations;

namespace Proyecto1.Models
{
    public class Board
    {
        [Key]
        public int Id { get; set; }
        
        public int GameId { get; set; }
        public Game Game { get; set; } = null!;
        
        public int Size { get; set; } = 100;
        
        // Navigation
        public ICollection<Snake> Snakes { get; set; } = new List<Snake>();
        public ICollection<Ladder> Ladders { get; set; } = new List<Ladder>();
    }
}