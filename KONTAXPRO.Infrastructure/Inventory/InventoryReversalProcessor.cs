using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Infrastructure.Inventory;

/// <summary>
/// Reversa un movimiento aplicado sin alterar sus detalles históricos.
/// El contexto y la transacción pertenecen al caso de uso que originó la
/// operación, de modo que documento, Kardex y proyección se confirman juntos.
/// </summary>
internal static class InventoryReversalProcessor
{
    private const decimal Tolerance = 0.000001m;

    internal static async Task<MovimientoInventario> ReverseAsync(
        KontaxDbContext context,
        MovimientoInventario original,
        long empresaId,
        long establecimientoId,
        long usuarioId,
        long origenId,
        string? numeroDocumento,
        string referencia,
        string motivo,
        DateTime now,
        Func<long, ProductoCosto, CancellationToken, Task>?
            restoreAdditionalCosts,
        CancellationToken cancellationToken)
    {
        if (original.EmpresaId != empresaId)
            throw new InvalidOperationException(
                "El movimiento no pertenece a la empresa activa.");
        if (original.Estado != "CONFIRMADO" ||
            original.MovimientoReversoId.HasValue)
            throw new InvalidOperationException(
                "El movimiento de inventario ya fue revertido.");

        var productIds = original.Detalles.Select(x => x.ProductoId)
            .Distinct().ToList();
        var hasLaterMovements = await context.MovimientosInventarioDetalles
            .AsNoTracking().AnyAsync(x => productIds.Contains(x.ProductoId) &&
                x.MovimientoInventarioId != original.Id &&
                x.MovimientoInventario!.Estado == "CONFIRMADO" &&
                (x.MovimientoInventario.FechaMovimiento >
                     original.FechaMovimiento ||
                 (x.MovimientoInventario.FechaMovimiento ==
                      original.FechaMovimiento &&
                  x.MovimientoInventarioId > original.Id)),
                cancellationToken);
        if (hasLaterMovements)
            throw new InvalidOperationException(
                "Hay movimientos posteriores de uno de los productos. Revierte primero esas operaciones para conservar el Kardex y el costo promedio.");

        var originalNature = await context.TiposMovimientoInventario
            .Where(x => x.Id == original.TipoMovimientoId)
            .Select(x => x.Naturaleza).SingleAsync(cancellationToken);
        var reverseCode = originalNature == "ENTRADA"
            ? "AJUSTE_SALIDA" : "AJUSTE_ENTRADA";
        var reverseType = await context.TiposMovimientoInventario.SingleAsync(
            x => x.Codigo == reverseCode && x.Estado == 1,
            cancellationToken);
        var reverse = new MovimientoInventario
        {
            EmpresaId = empresaId,
            NumeroMovimiento = await InventoryService.ObtenerSiguienteNumeroAsync(
                context, empresaId, establecimientoId,
                "MOVIMIENTO_INVENTARIO", cancellationToken),
            TipoMovimientoId = reverseType.Id,
            FechaMovimiento = now,
            BodegaId = original.BodegaId,
            OrigenTipoId = original.OrigenTipoId,
            OrigenId = origenId,
            NumeroDocumento = Normalize(numeroDocumento),
            Referencia = referencia,
            Observacion = motivo,
            UsuarioId = usuarioId,
            Estado = "CONFIRMADO",
            CreatedAt = now,
            UpdatedAt = now
        };
        context.MovimientosInventario.Add(reverse);

        var availableState = await context.EstadosSerie.SingleAsync(x =>
            x.Codigo == "DISPONIBLE" && x.Estado == 1, cancellationToken);
        var retiredState = await context.EstadosSerie.SingleAsync(x =>
            x.Codigo == "BAJA" && x.Estado == 1, cancellationToken);

        foreach (var group in original.Detalles.OrderBy(x => x.Id)
                     .GroupBy(x => x.ProductoId))
        {
            var ordered = group.OrderBy(x => x.Id).ToList();
            var first = ordered[0];
            var last = ordered[^1];
            var stock = await context.ProductosExistencias.SingleAsync(x =>
                x.ProductoId == group.Key &&
                x.BodegaId == original.BodegaId, cancellationToken);
            if (Math.Abs(stock.StockActual - last.StockNuevo) > Tolerance ||
                stock.StockReservado > 0)
                throw new InvalidOperationException(
                    "La existencia actual ya no coincide con el cierre del movimiento o tiene reservas. No es seguro revertirla.");
            var cost = await context.ProductosCostos.SingleAsync(x =>
                x.ProductoId == group.Key, cancellationToken);
            if (Math.Abs(cost.CostoPromedio - last.CostoPromedioNuevo) >
                Tolerance)
                throw new InvalidOperationException(
                    "El costo promedio cambió después del movimiento. No es seguro revertirlo.");

            stock.StockActual = first.StockAnterior;
            stock.UpdatedAt = now;
            cost.CostoPromedio = first.CostoPromedioAnterior;
            if (restoreAdditionalCosts is not null)
                await restoreAdditionalCosts(group.Key, cost,
                    cancellationToken);
            cost.UpdatedAt = now;

            foreach (var source in ordered.AsEnumerable().Reverse())
            {
                var detail = new MovimientoInventarioDetalle
                {
                    MovimientoInventario = reverse,
                    ProductoId = source.ProductoId,
                    ProductoPresentacionId = source.ProductoPresentacionId,
                    CantidadPresentacion = source.CantidadPresentacion,
                    FactorConversion = source.FactorConversion,
                    CantidadBase = source.CantidadBase,
                    CostoUnitarioBase = source.CostoUnitarioBase,
                    CostoTotal = source.CostoTotal,
                    StockAnterior = source.StockNuevo,
                    StockNuevo = source.StockAnterior,
                    CostoPromedioAnterior = source.CostoPromedioNuevo,
                    CostoPromedioNuevo = source.CostoPromedioAnterior,
                    EsBonificacion = source.EsBonificacion,
                    Observacion = motivo,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                reverse.Detalles.Add(detail);
                foreach (var sourceLot in source.Lotes)
                {
                    var lot = await context.ProductosLotesExistencias
                        .SingleAsync(x =>
                            x.LoteId == sourceLot.ProductoLoteId &&
                            x.BodegaId == original.BodegaId,
                            cancellationToken);
                    if (Math.Abs(lot.StockActual - sourceLot.StockLoteNuevo) >
                            Tolerance || lot.StockReservado > 0)
                        throw new InvalidOperationException(
                            "Un lote cambió o tiene reservas. No es seguro revertirlo.");
                    lot.StockActual = sourceLot.StockLoteAnterior;
                    lot.UpdatedAt = now;
                    detail.Lotes.Add(new MovimientoInventarioDetalleLote
                    {
                        ProductoLoteId = sourceLot.ProductoLoteId,
                        CantidadBase = sourceLot.CantidadBase,
                        StockLoteAnterior = sourceLot.StockLoteNuevo,
                        StockLoteNuevo = sourceLot.StockLoteAnterior,
                        CreatedAt = now
                    });
                }
                foreach (var sourceSeries in source.Series)
                {
                    var series = await context.ProductosSeries.SingleAsync(x =>
                        x.Id == sourceSeries.ProductoSerieId &&
                        x.BodegaId == original.BodegaId, cancellationToken);
                    var expectedState = originalNature == "ENTRADA"
                        ? availableState.Id : retiredState.Id;
                    if (series.EstadoSerieId != expectedState)
                        throw new InvalidOperationException(
                            $"La serie '{series.NumeroSerie}' ya cambió de estado o bodega.");
                    series.EstadoSerieId = originalNature == "ENTRADA"
                        ? retiredState.Id : availableState.Id;
                    series.UpdatedAt = now;
                    detail.Series.Add(new MovimientoInventarioDetalleSerie
                    {
                        ProductoSerieId = series.Id,
                        CreatedAt = now
                    });
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        original.Estado = "ANULADO";
        original.AnuladoPorUsuarioId = usuarioId;
        original.AnuladoAt = now;
        original.MotivoAnulacion = motivo;
        original.MovimientoReversoId = reverse.Id;
        original.UpdatedAt = now;
        return reverse;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
