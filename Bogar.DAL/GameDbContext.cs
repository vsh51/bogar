using Bogar.DAL;
using Microsoft.EntityFrameworkCore;

public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options)
        : base(options)
    {
    }

    public DbSet<Lobby> Lobbies { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Match> Matches { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Lobby)
            .WithMany(l => l.Users)
            .HasForeignKey(u => u.LobbyId);

        modelBuilder.Entity<Match>()
            .HasOne(m => m.WhiteBot)
            .WithMany(u => u.MatchesAsWhite)
            .HasForeignKey(m => m.WhiteBotId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Match>()
            .HasOne(m => m.BlackBot)
            .WithMany(u => u.MatchesAsBlack)
            .HasForeignKey(m => m.BlackBotId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Match>()
            .HasOne(m => m.Winner)
            .WithMany(u => u.WonMatches)
            .HasForeignKey(m => m.WinnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Match>()
            .HasOne(m => m.Lobby)
            .WithMany(l => l.Matches)
            .HasForeignKey(m => m.LobbyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Match>()
            .Property(m => m.Status)
            .HasConversion<string>();
    }
}