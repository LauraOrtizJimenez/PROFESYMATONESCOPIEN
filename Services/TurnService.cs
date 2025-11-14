using Proyecto1.Models;
using Proyecto1.Models.Enums;
using Proyecto1.Services.Interfaces;

namespace Proyecto1.Services
{
    public class TurnService : ITurnService
    {
        public bool IsPlayerTurn(Game game, int playerId)
        {
            var currentPlayer = GetCurrentPlayer(game);
            return currentPlayer?.Id == playerId;
        }

        public void AdvanceTurn(Game game)
        {
            var players = game.Players
                .Where(p => p.Status == PlayerStatus.Playing)
                .OrderBy(p => p.TurnOrder)
                .ToList();

            if (!players.Any()) return;

            do
            {
                game.CurrentTurnPlayerIndex = (game.CurrentTurnPlayerIndex + 1) % game.Players.Count;
            } while (game.Players.ElementAt(game.CurrentTurnPlayerIndex).Status != PlayerStatus.Playing);

            game.CurrentTurnPhase = TurnPhase.WaitingForDice;
        }

        public Player GetCurrentPlayer(Game game)
        {
            return game.Players
                .OrderBy(p => p.TurnOrder)
                .ElementAt(game.CurrentTurnPlayerIndex);
        }
    }
} 