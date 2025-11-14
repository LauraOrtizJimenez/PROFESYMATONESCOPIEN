namespace Proyecto1.DTOs.Lobby
{
    public class RoomSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CurrentPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<string> PlayerNames { get; set; } = new();
    }
}