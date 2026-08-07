using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KONTAXPRO.Tests.PostgreSql;

internal static class PostgreSqlTestDatabase
{
    private const string ConnectionVariable =
        "KONTAXPRO_TEST_CONNECTION_STRING";
    private static readonly SemaphoreSlim InitializationLock = new(1, 1);
    private static bool _initialized;

    public static bool IsConnectionConfigured =>
        !string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable(ConnectionVariable));

    public static IDbContextFactory<KontaxDbContext> Factory =>
        new TestDbContextFactory(CreateOptions());

    public static DbContextOptions<KontaxDbContext> CreateOptions(
        params Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor[] interceptors)
    {
        var connectionString = GetValidatedConnectionString();
        var builder = new DbContextOptionsBuilder<KontaxDbContext>()
            .UseNpgsql(connectionString)
            .EnableSensitiveDataLogging(false)
            .ConfigureWarnings(warnings => warnings.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId
                    .PendingModelChangesWarning));
        if (interceptors.Length > 0)
            builder.AddInterceptors(interceptors);
        return builder.Options;
    }

    public static async Task EnsureReadyAsync()
    {
        if (_initialized)
            return;

        await InitializationLock.WaitAsync();
        try
        {
            if (_initialized)
                return;

            await using var context = new KontaxDbContext(CreateOptions());
            if (!await context.Database.CanConnectAsync())
            {
                throw new InvalidOperationException(
                    "No se pudo conectar a la base PostgreSQL de pruebas.");
            }

            await context.Database.MigrateAsync();
            await new StructuralSeeder(Factory).SeedAsync();
            _initialized = true;
        }
        finally
        {
            InitializationLock.Release();
        }
    }

    public static string CreateUniqueValue(string prefix, int maxLength = 20)
    {
        var value = prefix + Guid.NewGuid().ToString("N");
        return value[..Math.Min(value.Length, maxLength)];
    }

    private static string GetValidatedConnectionString()
    {
        var raw = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                $"Define {ConnectionVariable} para ejecutar estas pruebas.");
        }

        var environment =
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (!string.Equals(
                environment,
                "Development",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Las pruebas PostgreSQL solo pueden ejecutarse con " +
                "DOTNET_ENVIRONMENT=Development.");
        }

        var builder = new NpgsqlConnectionStringBuilder(raw);
        if (string.IsNullOrWhiteSpace(builder.Database) ||
            !builder.Database.EndsWith("_test", StringComparison.OrdinalIgnoreCase) ||
            builder.Database.Contains("prod", StringComparison.OrdinalIgnoreCase) ||
            builder.Database.Equals("kontax_desktop", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "La base para pruebas debe ser exclusiva y su nombre debe " +
                "terminar en '_test'.");
        }

        builder.Pooling = false;
        return builder.ConnectionString;
    }

    private sealed class TestDbContextFactory(
        DbContextOptions<KontaxDbContext> options)
        : IDbContextFactory<KontaxDbContext>
    {
        public KontaxDbContext CreateDbContext() => new(options);

        public Task<KontaxDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new KontaxDbContext(options));
    }
}
