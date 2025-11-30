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

        // 🔥 NUEVO
        public DbSet<TokenSkin> TokenSkins { get; set; }
        public DbSet<UserTokenSkin> UserTokenSkins { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ================= USER =================
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();

                // 1 (User) -> N (UserTokenSkin)
                entity.HasMany(u => u.OwnedTokenSkins)
                    .WithOne(uts => uts.User)
                    .HasForeignKey(uts => uts.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // 1 (User) -> 0..1 (SelectedTokenSkin)
                entity.HasOne(u => u.SelectedTokenSkin)
                    .WithMany()
                    .HasForeignKey(u => u.SelectedTokenSkinId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ================= ROOM =================
            modelBuilder.Entity<Room>(entity =>
            {
                entity.HasOne(r => r.Game)
                    .WithOne(g => g.Room)
                    .HasForeignKey<Game>(g => g.RoomId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ================= GAME =================
            modelBuilder.Entity<Game>(entity =>
            {
                entity.HasOne(g => g.Board)
                    .WithOne(b => b.Game)
                    .HasForeignKey<Board>(b => b.GameId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(g => g.RowVersion)
                    .IsRowVersion();
            });

            // ================= PLAYER =================
            modelBuilder.Entity<Player>(entity =>
            {
                entity.HasOne(p => p.User)
                    .WithMany(u => u.Players)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Game)
                    .WithMany(g => g.Players)
                    .HasForeignKey(p => p.GameId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Room)
                    .WithMany(r => r.Players)
                    .HasForeignKey(p => p.RoomId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(p => new { p.GameId, p.TurnOrder })
                    .IsUnique()
                    .HasFilter("[GameId] IS NOT NULL");
            });

            // ================= BOARD =================
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

            // ================= MOVE =================
            modelBuilder.Entity<Move>(entity =>
            {
                entity.HasOne(m => m.Game)
                    .WithMany(g => g.Moves)
                    .HasForeignKey(m => m.GameId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Player)
                    .WithMany(p => p.Moves)
                    .HasForeignKey(m => m.PlayerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ================= DICEROLL =================
            modelBuilder.Entity<DiceRoll>(entity =>
            {
                entity.HasIndex(e => new { e.GameId, e.PlayerId, e.RolledAt });
            });

            // ================= TOKEN SKIN =================
            modelBuilder.Entity<TokenSkin>(entity =>
            {
                entity.HasIndex(t => t.Name).IsUnique();

                entity.Property(t => t.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(t => t.ColorKey)
                    .HasMaxLength(50);

                entity.Property(t => t.IconKey)
                    .HasMaxLength(50);

                // 👈 AQUÍ ESTABA EL ERROR: usar PriceCoins
                entity.Property(t => t.PriceCoins)
                    .HasDefaultValue(0);

                entity.Property(t => t.IsActive)
                    .HasDefaultValue(true);
            });

            // ================= USER TOKEN SKIN (M:N) =================
            modelBuilder.Entity<UserTokenSkin>(entity =>
            {
                entity.HasKey(uts => new { uts.UserId, uts.TokenSkinId });

                entity.HasOne(uts => uts.User)
                    .WithMany(u => u.OwnedTokenSkins)
                    .HasForeignKey(uts => uts.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(uts => uts.TokenSkin)
                    .WithMany(s => s.Owners)
                    .HasForeignKey(uts => uts.TokenSkinId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}