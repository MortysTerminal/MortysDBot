using Microsoft.EntityFrameworkCore;
using MortysDBot.Core.Sync;

namespace MortysDBot.Infrastructure.Data;

public sealed class MortysDbotDbContext : DbContext
{
    public MortysDbotDbContext(DbContextOptions<MortysDbotDbContext> options) : base(options) { }

    public DbSet<ScheduledEventLink> ScheduledEventLinks => Set<ScheduledEventLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScheduledEventLink>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.Source).HasMaxLength(32).IsRequired();
            e.Property(x => x.SourceId).HasMaxLength(256).IsRequired();

            // verhindert Duplikate pro Channel/Guild
            e.HasIndex(x => new { x.Source, x.SourceId, x.GuildId }).IsUnique();

            e.Property(x => x.CreatedAtUtc).IsRequired();
            e.Property(x => x.UpdatedAtUtc).IsRequired();
        });
    }
}
