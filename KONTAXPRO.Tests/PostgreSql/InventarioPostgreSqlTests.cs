using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Tests.PostgreSql;

public sealed class InventarioPostgreSqlTests
{
    [PostgreSqlFact]
    [Trait("Category", "PostgreSQL")]
    public async Task SchemaIsCurrentAndContainsInventoryIntegrityIndexes()
    {
        await PostgreSqlTestDatabase.EnsureReadyAsync();
        await using var context = new KontaxDbContext(
            PostgreSqlTestDatabase.CreateOptions());

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        var indexes = await context.Database.SqlQueryRaw<string>(
            """
            SELECT indexname AS "Value"
            FROM pg_indexes
            WHERE schemaname = 's_inventario'
              AND tablename = 'movimientos_inventario'
              AND indexname IN (
                'ux_movimientos_inventario_origen_bodega_tipo',
                'ix_movimientos_inventario_empresa_bodega_fecha')
            """).ToListAsync();

        Assert.Equal(2, indexes.Count);
    }
}
