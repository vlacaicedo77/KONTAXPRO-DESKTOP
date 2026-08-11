using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Tests.PostgreSql;

public sealed class OperacionesSinSustentoPostgreSqlTests
{
    [PostgreSqlFact]
    [Trait("Category", "PostgreSQL")]
    public async Task SchemaIsCurrentAndKeepsFinancialGuards()
    {
        await PostgreSqlTestDatabase.EnsureReadyAsync();
        await using var context = new KontaxDbContext(
            PostgreSqlTestDatabase.CreateOptions());

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        var entity = context.Model.FindEntityType(
            typeof(KONTAXPRO.Domain.Entities.Tesoreria.OperacionSinSustento))!;
        Assert.Equal("evidencia_nombre",
            entity.FindProperty("EvidenciaNombre")!.GetColumnName());

        var constraints = await context.Database.SqlQueryRaw<string>(
            """
            SELECT constraint_name AS "Value"
            FROM information_schema.table_constraints
            WHERE table_schema = 's_tesoreria'
              AND table_name = 'operaciones_sin_sustento'
              AND constraint_name IN (
                'ck_operaciones_sin_sustento_no_deducible',
                'ck_operaciones_sin_sustento_fondo',
                'ck_operaciones_sin_sustento_inventario')
            """).ToListAsync();

        Assert.Equal(3, constraints.Count);
    }
}
