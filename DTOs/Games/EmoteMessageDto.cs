namespace Proyecto1.DTOs.Game
{
    public class EmoteMessageDto
    {
        public int GameId { get; set; }
        public int FromPlayerId { get; set; }
        public string FromUsername { get; set; } = string.Empty;
        public int EmoteCode { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}