using Proyecto1.Models;

namespace Proyecto1.Services.Interfaces
{
    public interface ITurnService
    {
        bool IsPlayerTurn(Game game, int playerId);
        void AdvanceTurn(Game game);
        Player GetCurrentPlayer(Game game);
    }
}