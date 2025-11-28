using Proyecto1.DTOs.Games;
using Proyecto1.DTOs.Moves;
using Proyecto1.DTOs.Lobby;
using Proyecto1.Models;

namespace Proyecto1.Services.Interfaces
{
    public interface IGameService
    {
        // LOBBY
        Task<RoomSummaryDto> GetRoomSummaryAsync(int roomId);

        // GAME
        Task<Game> CreateGameAsync(int roomId);
        Task<GameStateDto> GetGameStateAsync(int gameId);
        Task<MoveResultDto> RollDiceAndMoveAsync(int gameId, int userId);
        Task SurrenderAsync(int gameId, int userId);

        // PROFESOR
        Task<ProfesorQuestionDto?> GetProfesorQuestionAsync(int gameId, int userId);
        Task<MoveResultDto> AnswerProfesorQuestionAsync(int gameId, int userId, string answer);
    }
}