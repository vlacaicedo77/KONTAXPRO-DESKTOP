using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Infrastructure.Inventory;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Infrastructure.Tesoreria;

public sealed partial class OperacionSinSustentoService
{
    private static async Task ReverseInventoryAsync(KontaxDbContext context,
        Domain.Entities.Tesoreria.OperacionSinSustento operation, long userId,
        string reason, DateTime now, CancellationToken cancellationToken)
    {
        var original = await context.MovimientosInventario
            .Include(x => x.Detalles).ThenInclude(x => x.Lotes)
            .Include(x => x.Detalles).ThenInclude(x => x.Series)
            .SingleAsync(x => x.Id == operation.MovimientoInventarioId &&
                x.EmpresaId == operation.EmpresaId, cancellationToken);
        if (original.Estado != "CONFIRMADO" || original.MovimientoReversoId.HasValue)
            throw new InvalidOperationException("El movimiento de inventario ya fue revertido.");
        var products = original.Detalles.Select(x => x.ProductoId).Distinct().ToList();
        if (await context.MovimientosInventarioDetalles.AsNoTracking().AnyAsync(x =>
                products.Contains(x.ProductoId) && x.MovimientoInventarioId != original.Id &&
                x.MovimientoInventario!.Estado == "CONFIRMADO" &&
                (x.MovimientoInventario.FechaMovimiento > original.FechaMovimiento ||
                 (x.MovimientoInventario.FechaMovimiento == original.FechaMovimiento &&
                  x.MovimientoInventarioId > original.Id)), cancellationToken))
            throw new InvalidOperationException(
                "Hay movimientos posteriores de uno de los productos. Revierte primero esas operaciones.");
        var type = await context.TiposMovimientoInventario.SingleAsync(x =>
            x.Codigo == "AJUSTE_SALIDA" && x.Estado == 1, cancellationToken);
        var reverse = new MovimientoInventario
        {
            EmpresaId = operation.EmpresaId,
            NumeroMovimiento = await InventoryService.ObtenerSiguienteNumeroAsync(
                context, operation.EmpresaId, operation.EstablecimientoId,
                "MOVIMIENTO_INVENTARIO", cancellationToken),
            TipoMovimientoId = type.Id, FechaMovimiento = now,
            BodegaId = original.BodegaId, OrigenTipoId = original.OrigenTipoId,
            OrigenId = operation.Id, NumeroDocumento = operation.NumeroOperacion,
            Referencia = $"REVERSO {operation.NumeroOperacion}", Observacion = reason,
            UsuarioId = userId, Estado = "CONFIRMADO", CreatedAt = now, UpdatedAt = now
        };
        context.MovimientosInventario.Add(reverse);
        var available = await context.EstadosSerie.SingleAsync(x =>
            x.Codigo == "DISPONIBLE" && x.Estado == 1, cancellationToken);
        var retired = await context.EstadosSerie.SingleAsync(x =>
            x.Codigo == "BAJA" && x.Estado == 1, cancellationToken);
        foreach (var group in original.Detalles.OrderBy(x => x.Id).GroupBy(x => x.ProductoId))
        {
            var ordered = group.OrderBy(x => x.Id).ToList();
            var first = ordered[0]; var last = ordered[^1];
            var stock = await context.ProductosExistencias.SingleAsync(x =>
                x.ProductoId == group.Key && x.BodegaId == original.BodegaId, cancellationToken);
            if (Math.Abs(stock.StockActual - last.StockNuevo) > ReversalTolerance || stock.StockReservado > 0)
                throw new InvalidOperationException("La existencia cambió o tiene reservas. No es seguro anular.");
            var cost = await context.ProductosCostos.SingleAsync(x => x.ProductoId == group.Key, cancellationToken);
            if (Math.Abs(cost.CostoPromedio - last.CostoPromedioNuevo) > ReversalTolerance)
                throw new InvalidOperationException("El costo promedio cambió. No es seguro anular.");
            stock.StockActual = first.StockAnterior; stock.UpdatedAt = now;
            cost.CostoPromedio = first.CostoPromedioAnterior;
            cost.UltimoCostoEfectivo = await context.MovimientosInventarioDetalles.AsNoTracking()
                .Where(x => x.ProductoId == group.Key && x.MovimientoInventarioId != original.Id &&
                    x.MovimientoInventario!.Estado == "CONFIRMADO" &&
                    (x.MovimientoInventario.FechaMovimiento < original.FechaMovimiento ||
                     (x.MovimientoInventario.FechaMovimiento == original.FechaMovimiento &&
                      x.MovimientoInventarioId < original.Id)))
                .OrderByDescending(x => x.MovimientoInventario!.FechaMovimiento)
                .ThenByDescending(x => x.MovimientoInventarioId).ThenByDescending(x => x.Id)
                .Select(x => (decimal?)x.CostoUnitarioBase).FirstOrDefaultAsync(cancellationToken) ?? 0;
            cost.UpdatedAt = now;
            foreach (var source in ordered.AsEnumerable().Reverse())
            {
                var detail = new MovimientoInventarioDetalle
                {
                    ProductoId = source.ProductoId, ProductoPresentacionId = source.ProductoPresentacionId,
                    CantidadPresentacion = source.CantidadPresentacion, FactorConversion = source.FactorConversion,
                    CantidadBase = source.CantidadBase, CostoUnitarioBase = source.CostoUnitarioBase,
                    CostoTotal = source.CostoTotal, StockAnterior = source.StockNuevo,
                    StockNuevo = source.StockAnterior, CostoPromedioAnterior = source.CostoPromedioNuevo,
                    CostoPromedioNuevo = source.CostoPromedioAnterior, Observacion = reason,
                    CreatedAt = now, UpdatedAt = now
                };
                reverse.Detalles.Add(detail);
                foreach (var sourceLot in source.Lotes)
                {
                    var lot = await context.ProductosLotesExistencias.SingleAsync(x =>
                        x.LoteId == sourceLot.ProductoLoteId && x.BodegaId == original.BodegaId, cancellationToken);
                    if (Math.Abs(lot.StockActual - sourceLot.StockLoteNuevo) > ReversalTolerance || lot.StockReservado > 0)
                        throw new InvalidOperationException("Un lote cambió o tiene reservas. No es seguro anular.");
                    lot.StockActual = sourceLot.StockLoteAnterior; lot.UpdatedAt = now;
                    detail.Lotes.Add(new MovimientoInventarioDetalleLote
                    {
                        ProductoLoteId = sourceLot.ProductoLoteId, CantidadBase = sourceLot.CantidadBase,
                        StockLoteAnterior = sourceLot.StockLoteNuevo, StockLoteNuevo = sourceLot.StockLoteAnterior,
                        CreatedAt = now
                    });
                }
                foreach (var sourceSeries in source.Series)
                {
                    var series = await context.ProductosSeries.SingleAsync(x =>
                        x.Id == sourceSeries.ProductoSerieId && x.BodegaId == original.BodegaId, cancellationToken);
                    if (series.EstadoSerieId != available.Id)
                        throw new InvalidOperationException($"La serie '{series.NumeroSerie}' ya no está disponible.");
                    series.EstadoSerieId = retired.Id; series.UpdatedAt = now;
                    detail.Series.Add(new MovimientoInventarioDetalleSerie
                    { ProductoSerieId = series.Id, CreatedAt = now });
                }
            }
        }
        await context.SaveChangesAsync(cancellationToken);
        original.Estado = "ANULADO"; original.AnuladoPorUsuarioId = userId;
        original.AnuladoAt = now; original.MotivoAnulacion = reason;
        original.MovimientoReversoId = reverse.Id; original.UpdatedAt = now;
    }
}
