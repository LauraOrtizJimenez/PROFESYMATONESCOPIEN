namespace Proyecto1.DTOs.Games
{
    public class GameStateDto
    {
        public int GameId { get; set; }
        public string Status { get; set; } = string.Empty;

        public int CurrentTurnPlayerIndex { get; set; }
        public string CurrentTurnPhase { get; set; } = string.Empty;

        public int? CurrentPlayerId { get; set; }
        public string? CurrentPlayerName { get; set; }

        public List<PlayerGameDto> Players { get; set; } = new();
        public BoardStateDto Board { get; set; } = new();

        public int? WinnerPlayerId { get; set; }
        public string? WinnerName { get; set; }
    }
}