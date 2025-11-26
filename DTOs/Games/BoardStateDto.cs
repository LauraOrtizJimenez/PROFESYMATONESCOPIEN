namespace Proyecto1.DTOs.Games
{
    public class BoardStateDto
    {
        public int Size { get; set; }
        public List<SnakeDto> Snakes { get; set; } = new();
        public List<LadderDto> Ladders { get; set; } = new();
    }
}