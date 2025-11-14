using Proyecto1.Models;

namespace Proyecto1.Infrastructure.Repositories.Interfaces
{
    public interface IRoomRepository
    {
        Task<Room?> GetByIdAsync(int id);
        Task<Room?> GetByIdWithPlayersAsync(int id);
        Task<List<Room>> GetAvailableRoomsAsync();
        Task<Room> CreateAsync(Room room);
        Task<Room> UpdateAsync(Room room);
    }
}