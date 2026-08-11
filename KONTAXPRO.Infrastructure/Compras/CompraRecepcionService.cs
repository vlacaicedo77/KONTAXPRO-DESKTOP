using System.Data;
using KONTAXPRO.Application.Compras;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Compras;
using KONTAXPRO.Application.Security;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Domain.Entities.Seguridad;
using KONTAXPRO.Infrastructure.Inventory;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Infrastructure.Compras;

public sealed class CompraRecepcionService(
    IDbContextFactory<KontaxDbContext> dbContextFactory,
    CurrentSession currentSession)
    : ICompraRecepcionService
{
    private const decimal QuantityTolerance = 0.000001m;

    public async Task<IReadOnlyList<CompraRecepcionResumenDto>>
        ObtenerConfirmadasAsync(long compraId,
            CancellationToken cancellationToken = default)
    {
        if (!currentSession.IsAuthenticated ||
            !currentSession.EmpresaId.HasValue || compraId <= 0)
            return [];
        var companyId = currentSession.EmpresaId.Value;
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (!await HasPermissionAsync(context, companyId,
                ComprasPermissions.Ver, cancellationToken))
            return [];
        return await context.ComprasRecepciones.AsNoTracking()
            .Where(x => x.EmpresaId == companyId && x.CompraId == compraId &&
                        x.Estado == "CONFIRMADA")
            .OrderByDescending(x => x.FechaRecepcion)
            .ThenByDescending(x => x.Id)
            .Select(x => new CompraRecepcionResumenDto
            {
                Id = x.Id,
                NumeroRecepcion = x.NumeroRecepcion,
                FechaRecepcion = x.FechaRecepcion,
                Bodega = x.Bodega!.Nombre,
                Lineas = x.Detalles.Count,
                CantidadBase = x.Detalles.Sum(d => d.CantidadBase)
            }).ToListAsync(cancellationToken);
    }

    public async Task<CompraOperationResult> ConfirmarAsync(
        ConfirmarCompraRecepcionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentSession.IsAuthenticated ||
            !currentSession.EmpresaId.HasValue || request.CompraId <= 0 ||
            request.BodegaId <= 0 || request.OperacionUuid == Guid.Empty ||
            request.Lineas.Count == 0)
            return CompraOperationResult.Fail(
                "Compra, bodega, operación y líneas son obligatorias.");
        if (request.Lineas.Any(x => x.CompraDetalleId <= 0 ||
                                    x.CantidadPresentacion <= 0) ||
            request.Lineas.Select(x => x.CompraDetalleId).Distinct().Count() !=
            request.Lineas.Count)
            return CompraOperationResult.Fail(
                "Cada línea debe ser única y tener una cantidad mayor que cero.");

        var companyId = currentSession.EmpresaId.Value;
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable,
                cancellationToken);
        try
        {
            if (!await HasPermissionAsync(context, companyId,
                    ComprasPermissions.Recibir, cancellationToken))
                return CompraOperationResult.Fail(
                    "No tienes autorización para confirmar recepciones.");
            var prior = await context.ComprasRecepciones.AsNoTracking()
                .SingleOrDefaultAsync(x => x.OperacionUuid ==
                    request.OperacionUuid, cancellationToken);
            if (prior is not null)
                return prior.EmpresaId == companyId &&
                       prior.CompraId == request.CompraId
                    ? CompraOperationResult.Ok(prior.Id,
                        "La recepción ya había sido confirmada.")
                    : CompraOperationResult.Fail(
                        "El identificador de operación ya fue utilizado.");

            var purchase = await context.Compras
                .FromSqlInterpolated(
                    $"SELECT c.*, c.xmin FROM s_compras.compras AS c WHERE c.id = {request.CompraId} AND c.empresa_id = {companyId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (purchase is null)
                return CompraOperationResult.Fail("No se encontró la compra.");
            if (purchase.Estado is "ANULADA" or "RECIBIDA")
                return CompraOperationResult.Fail(
                    "La compra ya no admite nuevas recepciones.");

            await context.Entry(purchase).Collection(x => x.Detalles)
                .LoadAsync(cancellationToken);
            var warehouse = await context.Bodegas
                .Include(x => x.Establecimiento)
                .SingleOrDefaultAsync(x => x.Id == request.BodegaId &&
                    x.Estado == 1 && x.EstablecimientoId ==
                    purchase.EstablecimientoId &&
                    x.Establecimiento!.EmpresaId == companyId,
                    cancellationToken);
            if (warehouse is null)
                return CompraOperationResult.Fail(
                    "La bodega no pertenece al establecimiento de la compra.");
            if (!CompraBodegaRules.EsCompatible(
                    purchase.TipoCompra, warehouse.PermiteVentaFacturada))
                return CompraOperationResult.Fail(
                    purchase.TipoCompra == "SIN_FACTURA"
                        ? "Una compra sin factura debe recibirse en una bodega no facturable."
                        : "Una compra facturada debe recibirse en una bodega facturable.");

            var now = DateTime.UtcNow;
            var result = await CompraReceiptProcessor.ConfirmAsync(context,
                purchase, warehouse, companyId, currentSession.UsuarioId,
                request, now, cancellationToken);
            if (!result.Success) return result;
            if (currentSession.EmpresaId != companyId)
                throw new InvalidOperationException(
                    "La empresa activa cambió durante la recepción.");
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return CompraOperationResult.Conflict(ComprasConcurrency.UserMessage);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return CompraOperationResult.Conflict(
                "La recepción compite con otra operación o contiene un lote/serie duplicado. Recarga la compra.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return CompraOperationResult.Fail(ex.Message);
        }
    }

    public async Task<CompraOperationResult> AnularAsync(
        AnularCompraRecepcionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentSession.IsAuthenticated ||
            !currentSession.EmpresaId.HasValue || request.RecepcionId <= 0 ||
            string.IsNullOrWhiteSpace(request.Motivo))
            return CompraOperationResult.Fail(
                "Recepción y motivo de anulación son obligatorios.");

        var companyId = currentSession.EmpresaId.Value;
        var reason = request.Motivo.Trim();
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable,
                cancellationToken);
        try
        {
            if (!await HasPermissionAsync(context, companyId,
                    ComprasPermissions.Anular, cancellationToken))
                return CompraOperationResult.Fail(
                    "No tienes autorización para anular recepciones.");

            var receipt = await context.ComprasRecepciones
                .FromSqlInterpolated(
                    $"SELECT r.*, r.xmin FROM s_compras.compras_recepciones AS r WHERE r.id = {request.RecepcionId} AND r.empresa_id = {companyId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (receipt is null)
                return CompraOperationResult.Fail(
                    "No se encontró la recepción.");
            if (receipt.Estado == "ANULADA")
                return CompraOperationResult.Ok(receipt.Id,
                    "La recepción ya estaba anulada.");
            if (!receipt.MovimientoInventarioId.HasValue)
                return CompraOperationResult.Fail(
                    "La recepción no tiene un movimiento de inventario asociado.");

            var purchase = await context.Compras
                .FromSqlInterpolated(
                    $"SELECT c.*, c.xmin FROM s_compras.compras AS c WHERE c.id = {receipt.CompraId} AND c.empresa_id = {companyId} FOR UPDATE")
                .SingleAsync(cancellationToken);
            if (purchase.Estado == "ANULADA")
                return CompraOperationResult.Fail(
                    "No se puede modificar una compra anulada.");

            var original = await context.MovimientosInventario
                .Include(x => x.Detalles).ThenInclude(x => x.Lotes)
                .Include(x => x.Detalles).ThenInclude(x => x.Series)
                .SingleAsync(x => x.Id == receipt.MovimientoInventarioId &&
                                  x.EmpresaId == companyId,
                    cancellationToken);
            if (original.Estado != "CONFIRMADO" ||
                original.MovimientoReversoId.HasValue)
                return CompraOperationResult.Fail(
                    "El Kardex de la recepción ya fue anulado o revertido.");

            await context.Entry(receipt).Collection(x => x.Detalles)
                .LoadAsync(cancellationToken);
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
                return CompraOperationResult.Fail(
                    "No se puede revertir esta recepción porque uno de sus productos tiene movimientos posteriores. Revierte primero esas operaciones para conservar el Kardex y el costo promedio.");

            var now = DateTime.UtcNow;
            var reverseType = await context.TiposMovimientoInventario
                .SingleAsync(x => x.Codigo == "AJUSTE_SALIDA" && x.Estado == 1,
                    cancellationToken);
            var reverseNumber = await InventoryService.ObtenerSiguienteNumeroAsync(
                context, companyId, receipt.EstablecimientoId,
                "MOVIMIENTO_INVENTARIO", cancellationToken);
            var reverse = new MovimientoInventario
            {
                EmpresaId = companyId,
                NumeroMovimiento = reverseNumber,
                TipoMovimientoId = reverseType.Id,
                FechaMovimiento = now,
                BodegaId = receipt.BodegaId,
                OrigenTipoId = original.OrigenTipoId,
                OrigenId = purchase.Id,
                NumeroDocumento = purchase.NumeroDocumento,
                Referencia = $"REVERSO {receipt.NumeroRecepcion}",
                Observacion = reason,
                UsuarioId = currentSession.UsuarioId,
                Estado = "CONFIRMADO",
                CreatedAt = now,
                UpdatedAt = now
            };
            context.MovimientosInventario.Add(reverse);

            var availableState = await context.EstadosSerie
                .SingleAsync(x => x.Codigo == "DISPONIBLE" && x.Estado == 1,
                    cancellationToken);
            var retiredState = await context.EstadosSerie
                .SingleAsync(x => x.Codigo == "BAJA" && x.Estado == 1,
                    cancellationToken);

            foreach (var productGroup in original.Detalles
                         .OrderBy(x => x.Id).GroupBy(x => x.ProductoId))
            {
                var ordered = productGroup.OrderBy(x => x.Id).ToList();
                var first = ordered[0];
                var last = ordered[^1];
                var existence = await context.ProductosExistencias
                    .SingleAsync(x => x.ProductoId == productGroup.Key &&
                                      x.BodegaId == receipt.BodegaId,
                        cancellationToken);
                if (Math.Abs(existence.StockActual - last.StockNuevo) >
                        QuantityTolerance || existence.StockReservado > 0)
                    return CompraOperationResult.Fail(
                        "La existencia actual ya no coincide con el cierre de la recepción o tiene reservas. No es seguro revertirla.");

                existence.StockActual = first.StockAnterior;
                existence.UpdatedAt = now;
                var cost = await context.ProductosCostos.SingleAsync(x =>
                    x.ProductoId == productGroup.Key, cancellationToken);
                if (Math.Abs(cost.CostoPromedio - last.CostoPromedioNuevo) >
                    QuantityTolerance)
                    return CompraOperationResult.Fail(
                        "El costo promedio cambió después de la recepción. No es seguro revertirla.");
                cost.CostoPromedio = first.CostoPromedioAnterior;
                var firstReceiptDetail = receipt.Detalles
                    .Where(x => x.ProductoId == productGroup.Key)
                    .OrderBy(x => x.Id).FirstOrDefault();
                var priorReceiptId = await context.ComprasRecepcionesDetalles
                    .AsNoTracking()
                    .Where(x => x.ProductoId == productGroup.Key &&
                                x.CompraRecepcionId != receipt.Id &&
                                x.CompraRecepcion!.Estado == "CONFIRMADA" &&
                                (x.CompraRecepcion.FechaRecepcion <
                                     receipt.FechaRecepcion ||
                                 (x.CompraRecepcion.FechaRecepcion ==
                                      receipt.FechaRecepcion &&
                                  x.CompraRecepcionId < receipt.Id)))
                    .OrderByDescending(x => x.CompraRecepcion!.FechaRecepcion)
                    .ThenByDescending(x => x.CompraRecepcionId)
                    .Select(x => (long?)x.CompraRecepcionId)
                    .FirstOrDefaultAsync(cancellationToken);
                var priorEffectiveCost = priorReceiptId.HasValue
                    ? await context.ComprasRecepcionesDetalles.AsNoTracking()
                        .Where(x => x.CompraRecepcionId == priorReceiptId &&
                                    x.ProductoId == productGroup.Key)
                        .GroupBy(_ => 1)
                        .Select(x => x.Sum(y => y.CostoTotal) /
                                     x.Sum(y => y.CantidadBase))
                        .SingleAsync(cancellationToken)
                    : 0m;
                var priorPurchasePrice = await context
                    .ComprasRecepcionesDetalles.AsNoTracking()
                    .Where(x => x.ProductoId == productGroup.Key &&
                                !x.EsBonificacion &&
                                x.CompraRecepcionId != receipt.Id &&
                                x.CompraRecepcion!.Estado == "CONFIRMADA" &&
                                (x.CompraRecepcion.FechaRecepcion <
                                     receipt.FechaRecepcion ||
                                 (x.CompraRecepcion.FechaRecepcion ==
                                      receipt.FechaRecepcion &&
                                  x.CompraRecepcionId < receipt.Id)))
                    .OrderByDescending(x => x.CompraRecepcion!.FechaRecepcion)
                    .ThenByDescending(x => x.CompraRecepcionId)
                    .ThenByDescending(x => x.Id)
                    .Select(x => (decimal?)(x.CompraDetalle!.PrecioUnitarioCompra /
                                             x.FactorConversion))
                    .FirstOrDefaultAsync(cancellationToken) ?? 0m;
                cost.UltimoPrecioCompra = firstReceiptDetail?
                    .UltimoPrecioCompraAnterior ?? priorPurchasePrice;
                cost.UltimoCostoEfectivo = firstReceiptDetail?
                    .UltimoCostoEfectivoAnterior ?? priorEffectiveCost;
                cost.UpdatedAt = now;

                // Se deshace en orden LIFO para que cada snapshot anterior/nuevo
                // conserve una cadena de Kardex coherente cuando el mismo
                // producto o lote aparece en más de una línea de la recepción.
                foreach (var source in ordered.AsEnumerable().Reverse())
                {
                    var reversedDetail = new MovimientoInventarioDetalle
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
                        Observacion = reason,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    reverse.Detalles.Add(reversedDetail);
                    foreach (var sourceLot in source.Lotes)
                    {
                        var lotExistence = await context.ProductosLotesExistencias
                            .SingleAsync(x => x.LoteId == sourceLot.ProductoLoteId &&
                                              x.BodegaId == receipt.BodegaId,
                                cancellationToken);
                        if (Math.Abs(lotExistence.StockActual -
                                     sourceLot.StockLoteNuevo) >
                                QuantityTolerance ||
                            lotExistence.StockReservado > 0)
                            return CompraOperationResult.Fail(
                                "Un lote de la recepción ya cambió o tiene reservas. No es seguro revertirlo.");
                        lotExistence.StockActual = sourceLot.StockLoteAnterior;
                        lotExistence.UpdatedAt = now;
                        reversedDetail.Lotes.Add(
                            new MovimientoInventarioDetalleLote
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
                        var series = await context.ProductosSeries.SingleAsync(
                            x => x.Id == sourceSeries.ProductoSerieId &&
                                 x.BodegaId == receipt.BodegaId,
                            cancellationToken);
                        if (series.EstadoSerieId != availableState.Id)
                            return CompraOperationResult.Fail(
                                $"La serie '{series.NumeroSerie}' ya no está disponible en la bodega. Revierte primero su operación posterior.");
                        series.EstadoSerieId = retiredState.Id;
                        series.UpdatedAt = now;
                        reversedDetail.Series.Add(
                            new MovimientoInventarioDetalleSerie
                            {
                                ProductoSerieId = series.Id,
                                CreatedAt = now
                            });
                    }
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            original.Estado = "ANULADO";
            original.AnuladoPorUsuarioId = currentSession.UsuarioId;
            original.AnuladoAt = now;
            original.MotivoAnulacion = reason;
            original.MovimientoReversoId = reverse.Id;
            original.UpdatedAt = now;
            receipt.Estado = "ANULADA";
            receipt.AnuladaPorUsuarioId = currentSession.UsuarioId;
            receipt.AnuladaAt = now;
            receipt.MotivoAnulacion = reason;
            receipt.UpdatedAt = now;
            await context.SaveChangesAsync(cancellationToken);

            await context.Entry(purchase).Collection(x => x.Detalles)
                .LoadAsync(cancellationToken);
            var inventoryDetailIds = purchase.Detalles
                .Where(x => x.EsInventariable).Select(x => x.Id).ToList();
            var receivedByDetail = await context.ComprasRecepcionesDetalles
                .Where(x => inventoryDetailIds.Contains(x.CompraDetalleId) &&
                            x.CompraRecepcion!.Estado == "CONFIRMADA")
                .GroupBy(x => x.CompraDetalleId)
                .Select(x => new { Id = x.Key,
                    Quantity = x.Sum(y => y.CantidadPresentacion) })
                .ToDictionaryAsync(x => x.Id, x => x.Quantity,
                    cancellationToken);
            var receivedAny = receivedByDetail.Values.Any(x =>
                x > QuantityTolerance);
            var allReceived = purchase.Detalles.Where(x => x.EsInventariable)
                .All(x => receivedByDetail.GetValueOrDefault(x.Id) +
                          QuantityTolerance >= x.CantidadPresentacion);
            purchase.Estado = allReceived ? "RECIBIDA" : receivedAny
                ? "PARCIALMENTE_RECIBIDA" : "PENDIENTE_RECEPCION";
            purchase.UpdatedAt = now;
            context.Auditorias.Add(new Auditoria
            {
                UsuarioId = currentSession.UsuarioId,
                EmpresaId = companyId,
                EstablecimientoId = receipt.EstablecimientoId,
                Accion = ComprasAuditActions.RecepcionAnulada,
                Entidad = "compras_recepciones",
                EntidadId = receipt.Id,
                Descripcion =
                    $"Recepción {receipt.NumeroRecepcion} anulada. Reverso {reverse.NumeroMovimiento}. Motivo: {reason}",
                CreatedAt = now
            });
            if (currentSession.EmpresaId != companyId)
                throw new InvalidOperationException(
                    "La empresa activa cambió durante la anulación.");
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CompraOperationResult.Ok(receipt.Id,
                $"Recepción anulada. Kardex reversado con {reverse.NumeroMovimiento}.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return CompraOperationResult.Conflict(ComprasConcurrency.UserMessage);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return CompraOperationResult.Conflict(
                "La recepción cambió simultáneamente. Recarga la compra.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return CompraOperationResult.Fail(ex.Message);
        }
    }

    private async Task<bool> HasPermissionAsync(
        KontaxDbContext context, long companyId, string permission,
        CancellationToken cancellationToken) =>
        await context.UsuariosEmpresasRoles.AsNoTracking().AnyAsync(x =>
            x.UsuarioEmpresa!.UsuarioId == currentSession.UsuarioId &&
            x.UsuarioEmpresa.EmpresaId == companyId &&
            x.UsuarioEmpresa.Estado == 1 && x.Rol!.Estado == 1 &&
            (x.Rol.Codigo == "ADMINISTRADOR" ||
             x.Rol.RolesPermisos.Any(rp => rp.Permiso!.Codigo == permission &&
                                           rp.Permiso.Estado == 1)),
            cancellationToken);

}
