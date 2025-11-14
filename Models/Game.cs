using System.ComponentModel.DataAnnotations;
using Proyecto1.Models.Enums;

namespace Proyecto1.Models
{
    public class Game
    {
        [Key]
        public int Id { get; set; }
        
        public int RoomId { get; set; }
        public Room Room { get; set; } = null!;
        
        public GameStatus Status { get; set; } = GameStatus.WaitingForPlayers;
        
        public int CurrentTurnPlayerIndex { get; set; } = 0;
        public TurnPhase CurrentTurnPhase { get; set; } = TurnPhase.WaitingForDice;
        
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? FinishedAt { get; set; }
        
        public int? WinnerPlayerId { get; set; }
        
        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        
        // Navigation
        public Board Board { get; set; } = null!;
        public ICollection<Player> Players { get; set; } = new List<Player>();
        public ICollection<Move> Moves { get; set; } = new List<Move>();
    }
}