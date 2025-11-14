using Microsoft.EntityFrameworkCore;
using Proyecto1.Models;

namespace Proyecto1.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Board> Boards { get; set; }
        public DbSet<Snake> Snakes { get; set; }
        public DbSet<Ladder> Ladders { get; set; }
        public DbSet<Move> Moves { get; set; }
        public DbSet<DiceRoll> DiceRolls { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User Configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Room Configuration
            modelBuilder.Entity<Room>(entity =>
            {
                entity.HasOne(r => r.Game)
                    .WithOne(g => g.Room)
                    .HasForeignKey<Game>(g => g.RoomId)
                    .OnDelete(DeleteBehavior.Restrict); // ✅ CAMBIO: Restrict en vez de Cascade
            });

            // Game Configuration
            modelBuilder.Entity<Game>(entity =>
            {
                entity.HasOne(g => g.Board)
                    .WithOne(b => b.Game)
                    .HasForeignKey<Board>(b => b.GameId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(g => g.RowVersion)
                    .IsRowVersion();
            });

            // Player Configuration - AQUÍ ESTÁ EL PROBLEMA PRINCIPAL
            modelBuilder.Entity<Player>(entity =>
            {
                entity.HasOne(p => p.User)
                    .WithMany(u => u.Players)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Game)
                    .WithMany(g => g.Players)
                    .HasForeignKey(p => p.GameId)
                    .IsRequired(false) // ✅ Permitir NULL
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Room)
                    .WithMany(r => r.Players)
                    .HasForeignKey(p => p.RoomId)
                    .OnDelete(DeleteBehavior.SetNull);

                // ✅ Índice solo cuando GameId no es null
                entity.HasIndex(p => new { p.GameId, p.TurnOrder })
                    .IsUnique()
                    .HasFilter("[GameId] IS NOT NULL");
            });
            
            // Board Configuration
            modelBuilder.Entity<Board>(entity =>
            {
                entity.HasMany(b => b.Snakes)
                    .WithOne(s => s.Board)
                    .HasForeignKey(s => s.BoardId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(b => b.Ladders)
                    .WithOne(l => l.Board)
                    .HasForeignKey(l => l.BoardId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Move Configuration
            modelBuilder.Entity<Move>(entity =>
            {
                entity.HasOne(m => m.Game)
                    .WithMany(g => g.Moves)
                    .HasForeignKey(m => m.GameId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Player)
                    .WithMany(p => p.Moves)
                    .HasForeignKey(m => m.PlayerId)
                    .OnDelete(DeleteBehavior.Restrict); // ✅ NO ACTION para evitar ciclos
            });

            // DiceRoll Configuration (opcional - para auditoría)
            modelBuilder.Entity<DiceRoll>(entity =>
            {
                entity.HasIndex(e => new { e.GameId, e.PlayerId, e.RolledAt });
            });
        }
    }
}