using KONTAXPRO.Domain.Entities.Comercial;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KONTAXPRO.Tests.Persistence;

public sealed class StructuralSeederTests
{
    [Fact]
    public async Task EmptyDatabaseSeedsIdentificationTypesAndConsumerFinalIdempotently()
    {
        var options = new DbContextOptionsBuilder<KontaxDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(
                InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var factory = new TestDbContextFactory(options);
        var seeder = new StructuralSeeder(factory);

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        await using var context = await factory.CreateDbContextAsync();
        Assert.Equal(5, await context.TiposIdentificacion.CountAsync());
        var consumer = await context.Terceros
            .Include(x => x.TipoIdentificacion)
            .SingleAsync(x => x.NumeroIdentificacion ==
                TerceroEstructural.ConsumidorFinalIdentificacion);
        Assert.NotNull(consumer.TipoIdentificacion);
        Assert.Equal("CONSUMIDOR_FINAL", consumer.TipoIdentificacion!.Codigo);
        Assert.Equal(
            TerceroEstructural.ConsumidorFinalRazonSocial,
            consumer.RazonSocial);
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
