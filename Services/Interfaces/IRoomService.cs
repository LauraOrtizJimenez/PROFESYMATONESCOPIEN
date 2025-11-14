using Proyecto1.Models;

namespace Proyecto1.Services.Interfaces
{
    public interface IRoomService
    {
        Task<Room> CreateRoomAsync(string name, int maxPlayers, int creatorUserId);
        Task<Player> JoinRoomAsync(int roomId, int userId);
        Task<List<Room>> GetAvailableRoomsAsync();
        Task<Room?> GetRoomWithDetailsAsync(int roomId);
    }
} 