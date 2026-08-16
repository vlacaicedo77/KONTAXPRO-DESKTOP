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

        await InventoryReversalProcessor.ReverseAsync(context, original,
            operation.EmpresaId, operation.EstablecimientoId, userId,
            operation.Id, operation.NumeroOperacion,
            $"REVERSO {operation.NumeroOperacion}", reason, now,
            async (productId, cost, token) =>
            {
                cost.UltimoCostoEfectivo = await context
                    .MovimientosInventarioDetalles.AsNoTracking()
                    .Where(x => x.ProductoId == productId &&
                        x.MovimientoInventarioId != original.Id &&
                        x.MovimientoInventario!.Estado == "CONFIRMADO" &&
                        (x.MovimientoInventario.FechaMovimiento <
                             original.FechaMovimiento ||
                         (x.MovimientoInventario.FechaMovimiento ==
                              original.FechaMovimiento &&
                          x.MovimientoInventarioId < original.Id)))
                    .OrderByDescending(x =>
                        x.MovimientoInventario!.FechaMovimiento)
                    .ThenByDescending(x => x.MovimientoInventarioId)
                    .ThenByDescending(x => x.Id)
                    .Select(x => (decimal?)x.CostoUnitarioBase)
                    .FirstOrDefaultAsync(token) ?? 0;
            }, cancellationToken);
    }
}
