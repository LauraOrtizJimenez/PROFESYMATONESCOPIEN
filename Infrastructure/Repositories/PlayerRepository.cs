using Microsoft.EntityFrameworkCore;
using Proyecto1.Infrastructure.Data;
using Proyecto1.Infrastructure.Repositories.Interfaces;
using Proyecto1.Models;

namespace Proyecto1.Infrastructure.Repositories
{
    public class PlayerRepository : IPlayerRepository
    {
        private readonly AppDbContext _context;

        public PlayerRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Player?> GetByIdAsync(int id)
        {
            return await _context.Players
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Player?> GetByGameAndUserAsync(int gameId, int userId)
        {
            return await _context.Players
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.GameId == gameId && p.UserId == userId);
        }

        public async Task<List<Player>> GetByGameIdAsync(int gameId)
        {
            return await _context.Players
                .Include(p => p.User)
                .Where(p => p.GameId == gameId)
                .OrderBy(p => p.TurnOrder)
                .ToListAsync();
        }

        public async Task<Player> CreateAsync(Player player)
        {
            _context.Players.Add(player);
            await _context.SaveChangesAsync();
            return player;
        }

        public async Task<Player> UpdateAsync(Player player)
        {
            _context.Players.Update(player);
            await _context.SaveChangesAsync();
            return player;
        }
    }
}