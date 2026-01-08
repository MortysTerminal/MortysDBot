using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MortysDBot.Infrastructure.Data;

public sealed class DesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<MortysDbotDbContext>
{
    public MortysDbotDbContext CreateDbContext(string[] args)
    {
        // Für EF Tools: ConnectionString aus ENV ziehen
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__MortysDBot");

        if (string.IsNullOrWhiteSpace(cs))
        {
            // Fallback für lokale Entwicklung (kannst du anpassen)
            cs = "Host=localhost;Port=5432;Database=mortysdbot;Username=morty;Password=zp-YBmhhG9pmxwQ@aQPDU7vDdqVZ36gC";
        }

        var options = new DbContextOptionsBuilder<MortysDbotDbContext>()
            .UseNpgsql(cs)
            .Options;

        return new MortysDbotDbContext(options);
    }
}
