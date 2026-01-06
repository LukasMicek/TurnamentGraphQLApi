using Microsoft.EntityFrameworkCore;
using TournamentGraphQLApi.Models;

namespace TournamentGraphQLApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<Bracket> Brackets => Set<Bracket>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<TournamentParticipant> TournamentParticipants => Set<TournamentParticipant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<TournamentParticipant>()
            .HasKey(tp => new { tp.TournamentId, tp.UserId });

        modelBuilder.Entity<TournamentParticipant>()
            .HasOne(tp => tp.Tournament)
            .WithMany(t => t.Participants)
            .HasForeignKey(tp => tp.TournamentId);

        modelBuilder.Entity<TournamentParticipant>()
            .HasOne(tp => tp.User)
            .WithMany(u => u.TournamentParticipants)
            .HasForeignKey(tp => tp.UserId);

        modelBuilder.Entity<Tournament>()
            .HasOne(t => t.Bracket)
            .WithOne(b => b.Tournament!)
            .HasForeignKey<Bracket>(b => b.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Bracket>()
            .HasMany(b => b.Matches)
            .WithOne(m => m.Bracket!)
            .HasForeignKey(m => m.BracketId)
            .OnDelete(DeleteBehavior.Cascade);

        base.OnModelCreating(modelBuilder);
    }
}
