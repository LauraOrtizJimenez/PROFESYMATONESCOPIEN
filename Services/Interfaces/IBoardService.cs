using Proyecto1.Models;

namespace Proyecto1.Services.Interfaces
{
    public interface IBoardService
    {
        Board GenerateBoard(int gameId, int size = 100);
        bool ValidatePosition(int position, int boardSize);
        int? GetSnakeDestination(Board board, int position);
        int? GetLadderDestination(Board board, int position);
    }
}