using Microsoft.EntityFrameworkCore;
using Proyecto1.Infrastructure.Data;
using Proyecto1.Infrastructure.Repositories.Interfaces;
using Proyecto1.Models;

namespace Proyecto1.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> CreateAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<List<User>> GetTopUsersAsync(int limit)
        {
            return await _context.Users
                .OrderByDescending(u => u.GamesWon)
                .ThenByDescending(u => u.GamesPlayed > 0 ? (double)u.GamesWon / u.GamesPlayed : 0)
                .Take(limit)
                .ToListAsync();
        }
    }
} 