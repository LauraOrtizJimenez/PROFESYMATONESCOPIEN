namespace Proyecto1.DTOs.Games
{
    public class PlayerGameDto
    {
        public int PlayerId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;

        public int Position { get; set; }
        public int TurnOrder { get; set; }

        public string Status { get; set; } = string.Empty;

        public bool IsCurrentTurn { get; set; }

        // 🎨 SKIN SELECCIONADA (viaja al front)
        public string? TokenColorKey { get; set; }
        public string? TokenIconKey { get; set; }
    }
}