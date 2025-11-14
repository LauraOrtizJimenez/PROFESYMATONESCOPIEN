using System.ComponentModel.DataAnnotations;

namespace Proyecto1.Models
{
    public class Ladder
    {
        [Key]
        public int Id { get; set; }
        
        public int BoardId { get; set; }
        public Board Board { get; set; } = null!;
        
        public int BottomPosition { get; set; }
        public int TopPosition { get; set; }
    }
}