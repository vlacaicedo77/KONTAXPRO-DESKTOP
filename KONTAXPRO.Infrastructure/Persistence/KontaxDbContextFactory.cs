using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KONTAXPRO.Infrastructure.Persistence;

public sealed class KontaxDbContextFactory
    : IDesignTimeDbContextFactory<KontaxDbContext>
{
    private const string ConnectionStringEnvironmentVariable =
        "KONTAXPRO_CONNECTION_STRING";

    public KontaxDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            ConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Define {ConnectionStringEnvironmentVariable} antes de usar " +
                "las herramientas de Entity Framework Core.");
        }

        var options = new DbContextOptionsBuilder<KontaxDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new KontaxDbContext(options);
    }
}
