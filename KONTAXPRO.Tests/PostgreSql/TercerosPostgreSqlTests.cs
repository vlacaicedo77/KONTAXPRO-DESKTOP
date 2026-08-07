using KONTAXPRO.Application.Clientes;
using KONTAXPRO.Domain.Entities.Comercial;
using KONTAXPRO.Domain.Entities.Seguridad;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KONTAXPRO.Tests.PostgreSql;

public sealed class TercerosPostgreSqlTests
{
    [PostgreSqlFact]
    [Trait("Category", "PostgreSQL")]
    public async Task OfficialMigrationsAreCurrentAndVersionMapsToXmin()
    {
        await PostgreSqlTestDatabase.EnsureReadyAsync();
        await using var context = new KontaxDbContext(
            PostgreSqlTestDatabase.CreateOptions());

        Assert.Equal(
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            context.Database.ProviderName);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        var version = context.Model.FindEntityType(typeof(Tercero))!
            .FindProperty(nameof(Tercero.Version))!;
        Assert.True(version.IsConcurrencyToken);
        Assert.Equal("xmin", version.GetColumnName());
    }

    [PostgreSqlFact]
    [Trait("Category", "PostgreSQL")]
    public async Task XminRejectsAStaleThirdPartyUpdate()
    {
        await PostgreSqlTestDatabase.EnsureReadyAsync();
        var key = PostgreSqlTestDatabase.CreateUniqueValue("XMIN", 24);
        var number = PostgreSqlTestDatabase.CreateUniqueValue("N", 20);
        long thirdPartyId = 0;
        try
        {
            await using (var seed = new KontaxDbContext(
                PostgreSqlTestDatabase.CreateOptions()))
            {
                var thirdParty = await CreateThirdPartyAsync(seed, key, number);
                thirdPartyId = thirdParty.Id;
            }

            await using var first = new KontaxDbContext(
                PostgreSqlTestDatabase.CreateOptions());
            await using var stale = new KontaxDbContext(
                PostgreSqlTestDatabase.CreateOptions());
            var firstCopy = await first.Terceros.SingleAsync(x => x.Id == thirdPartyId);
            var staleCopy = await stale.Terceros.SingleAsync(x => x.Id == thirdPartyId);
            var originalVersion = staleCopy.Version;

            firstCopy.RazonSocial = "CAMBIO CONFIRMADO";
            await first.SaveChangesAsync();
            Assert.NotEqual(originalVersion, firstCopy.Version);

            staleCopy.RazonSocial = "CAMBIO OBSOLETO";
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => stale.SaveChangesAsync());
        }
        finally
        {
            await DeleteThirdPartyAsync(thirdPartyId);
        }
    }

    [PostgreSqlFact]
    [Trait("Category", "PostgreSQL")]
    public async Task CanonicalIdentityUniqueIndexRejectsDuplicates()
    {
        await PostgreSqlTestDatabase.EnsureReadyAsync();
        var key = ClaveIdentidadTercero.Crear("CEDULA", "1710034065");
        long firstId = 0;
        try
        {
            await using (var first = new KontaxDbContext(
                PostgreSqlTestDatabase.CreateOptions()))
            {
                var thirdParty = await CreateThirdPartyAsync(
                    first,
                    key,
                    "1710034065",
                    "CEDULA");
                firstId = thirdParty.Id;
            }

            await using var duplicate = new KontaxDbContext(
                PostgreSqlTestDatabase.CreateOptions());
            duplicate.Terceros.Add(await BuildThirdPartyAsync(
                duplicate,
                key,
                "1710034065001",
                "RUC"));
            var exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => duplicate.SaveChangesAsync());
            var postgres = Assert.IsType<PostgresException>(exception.InnerException);
            Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
            Assert.Equal(ClaveIdentidadTercero.UniqueConstraintName,
                postgres.ConstraintName);
        }
        finally
        {
            await DeleteThirdPartyAsync(firstId);
        }
    }

    [PostgreSqlFact]
    [Trait("Category", "PostgreSQL")]
    public async Task MutationAndAuditRollbackTogether()
    {
        await PostgreSqlTestDatabase.EnsureReadyAsync();
        var key = PostgreSqlTestDatabase.CreateUniqueValue("ROLL", 24);
        var number = PostgreSqlTestDatabase.CreateUniqueValue("R", 20);
        long temporaryId;

        await using (var context = new KontaxDbContext(
            PostgreSqlTestDatabase.CreateOptions()))
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            var thirdParty = await CreateThirdPartyAsync(context, key, number);
            temporaryId = thirdParty.Id;
            context.Auditorias.Add(new Auditoria
            {
                Accion = "PRUEBA_ROLLBACK_TERCERO",
                Entidad = "terceros",
                EntidadId = temporaryId,
                Descripcion = "Prueba técnica sin datos personales.",
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await using var verification = new KontaxDbContext(
            PostgreSqlTestDatabase.CreateOptions());
        Assert.False(await verification.Terceros.AnyAsync(x => x.Id == temporaryId));
        Assert.False(await verification.Auditorias.AnyAsync(x =>
            x.Accion == "PRUEBA_ROLLBACK_TERCERO" &&
            x.EntidadId == temporaryId));
    }

    [PostgreSqlFact]
    [Trait("Category", "PostgreSQL")]
    public async Task SupplierStateRemainsIndependentFromClientState()
    {
        await PostgreSqlTestDatabase.EnsureReadyAsync();
        var key = PostgreSqlTestDatabase.CreateUniqueValue("ROLE", 24);
        long thirdPartyId = 0;
        try
        {
            await using (var context = new KontaxDbContext(
                PostgreSqlTestDatabase.CreateOptions()))
            {
                var thirdParty = await BuildThirdPartyAsync(
                    context,
                    key,
                    PostgreSqlTestDatabase.CreateUniqueValue("S", 20));
                thirdParty.EsCliente = true;
                thirdParty.EstadoCliente = 1;
                thirdParty.EsProveedor = true;
                thirdParty.EstadoProveedor = 1;
                context.Terceros.Add(thirdParty);
                await context.SaveChangesAsync();
                thirdPartyId = thirdParty.Id;

                thirdParty.EstadoProveedor = 0;
                await context.SaveChangesAsync();
            }

            await using var verification = new KontaxDbContext(
                PostgreSqlTestDatabase.CreateOptions());
            var saved = await verification.Terceros.AsNoTracking()
                .SingleAsync(x => x.Id == thirdPartyId);
            Assert.True(saved.EsCliente);
            Assert.Equal(1, saved.EstadoCliente);
            Assert.True(saved.EsProveedor);
            Assert.Equal(0, saved.EstadoProveedor);
            Assert.Equal(1, saved.Estado);
        }
        finally
        {
            await DeleteThirdPartyAsync(thirdPartyId);
        }
    }

    private static async Task<Tercero> CreateThirdPartyAsync(
        KontaxDbContext context,
        string canonicalKey,
        string number,
        string typeCode = "RUC")
    {
        var thirdParty = await BuildThirdPartyAsync(
            context,
            canonicalKey,
            number,
            typeCode);
        context.Terceros.Add(thirdParty);
        await context.SaveChangesAsync();
        return thirdParty;
    }

    private static async Task<Tercero> BuildThirdPartyAsync(
        KontaxDbContext context,
        string canonicalKey,
        string number,
        string typeCode = "RUC")
    {
        var typeId = await context.TiposIdentificacion.AsNoTracking()
            .Where(x => x.Codigo == typeCode)
            .Select(x => x.Id)
            .SingleAsync();
        return new Tercero
        {
            TipoIdentificacionId = typeId,
            NumeroIdentificacion = number,
            ClaveIdentidad = canonicalKey,
            RazonSocial = "TERCERO RELACIONAL DE PRUEBA",
            OrigenRegistro = "OFFLINE",
            EstadoVerificacion = "PENDIENTE",
            Estado = 1,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static async Task DeleteThirdPartyAsync(long thirdPartyId)
    {
        if (thirdPartyId <= 0)
            return;
        await using var cleanup = new KontaxDbContext(
            PostgreSqlTestDatabase.CreateOptions());
        await cleanup.Auditorias
            .Where(x => x.Entidad == "terceros" && x.EntidadId == thirdPartyId)
            .ExecuteDeleteAsync();
        await cleanup.TercerosIdentificaciones
            .Where(x => x.TerceroId == thirdPartyId)
            .ExecuteDeleteAsync();
        await cleanup.EmpresasTerceros
            .Where(x => x.TerceroId == thirdPartyId)
            .ExecuteDeleteAsync();
        await cleanup.Terceros
            .Where(x => x.Id == thirdPartyId)
            .ExecuteDeleteAsync();
    }
}
