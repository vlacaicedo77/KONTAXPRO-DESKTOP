using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Tests.Inventory;

public sealed class InventoryPersistenceModelTests
{
    private static KontaxDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<KontaxDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=model_only;Username=model;Password=model")
            .Options;
        return new KontaxDbContext(options);
    }

    [Fact]
    public void Movimiento_ProtegeIdempotenciaPorOrigenBodegaYTipo()
    {
        using var context = CreateContext();
        var index = context.Model.FindEntityType(typeof(MovimientoInventario))!
            .GetIndexes().Single(x => x.GetDatabaseName() ==
                "ux_movimientos_inventario_origen_bodega_tipo");

        Assert.True(index.IsUnique);
        Assert.Equal("origen_id > 0", index.GetFilter());
        Assert.Equal(
            [nameof(MovimientoInventario.EmpresaId),
             nameof(MovimientoInventario.OrigenTipoId),
             nameof(MovimientoInventario.OrigenId),
             nameof(MovimientoInventario.BodegaId),
             nameof(MovimientoInventario.TipoMovimientoId)],
            index.Properties.Select(x => x.Name));
    }

    [Fact]
    public void Movimiento_TieneIndiceParaKardexPorBodegaYFecha()
    {
        using var context = CreateContext();
        var index = context.Model.FindEntityType(typeof(MovimientoInventario))!
            .GetIndexes().Single(x => x.GetDatabaseName() ==
                "ix_movimientos_inventario_empresa_bodega_fecha");

        Assert.False(index.IsUnique);
        Assert.Equal(
            [nameof(MovimientoInventario.EmpresaId),
             nameof(MovimientoInventario.BodegaId),
             nameof(MovimientoInventario.FechaMovimiento)],
            index.Properties.Select(x => x.Name));
    }

    [Fact]
    public void Detalle_ConservaSnapshotsDeStockYCosto()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(
            typeof(MovimientoInventarioDetalle))!;

        Assert.NotNull(entity.FindProperty(
            nameof(MovimientoInventarioDetalle.StockAnterior)));
        Assert.NotNull(entity.FindProperty(
            nameof(MovimientoInventarioDetalle.StockNuevo)));
        Assert.NotNull(entity.FindProperty(
            nameof(MovimientoInventarioDetalle.CostoPromedioAnterior)));
        Assert.NotNull(entity.FindProperty(
            nameof(MovimientoInventarioDetalle.CostoPromedioNuevo)));
    }
}
