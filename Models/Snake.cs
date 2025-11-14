using System.ComponentModel.DataAnnotations;

namespace Proyecto1.Models
{
    public class Snake
    {
        [Key]
        public int Id { get; set; }
        
        public int BoardId { get; set; }
        public Board Board { get; set; } = null!;
        
        public int HeadPosition { get; set; }
        public int TailPosition { get; set; }
    }
}