namespace Proyecto1.DTOs.Games
{
    public class PlayerStateDto
    {
        public int PlayerId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int Position { get; set; }
        public int TurnOrder { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsCurrentTurn { get; set; }
    }
}