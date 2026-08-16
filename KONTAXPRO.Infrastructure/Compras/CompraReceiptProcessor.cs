using KONTAXPRO.Application.Models.Compras;
using KONTAXPRO.Application.Models.Inventario;
using KONTAXPRO.Application.Security;
using KONTAXPRO.Domain.Entities.Compras;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Domain.Entities.Seguridad;
using KONTAXPRO.Infrastructure.Inventory;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Infrastructure.Compras;

internal static class CompraReceiptProcessor
{
    private const decimal QuantityTolerance = 0.000001m;

    internal static async Task<CompraOperationResult> ConfirmAsync(
        KontaxDbContext context,
        Compra purchase,
        Bodega warehouse,
        long companyId,
        long userId,
        ConfirmarCompraRecepcionRequest request,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var detailIds = purchase.Detalles.Select(x => x.Id).ToList();
        var received = await context.ComprasRecepcionesDetalles
            .Where(x => detailIds.Contains(x.CompraDetalleId) &&
                x.CompraRecepcion!.Estado == "CONFIRMADA")
            .GroupBy(x => x.CompraDetalleId)
            .Select(x => new
            {
                DetailId = x.Key,
                Quantity = x.Sum(y => y.CantidadPresentacion)
            }).ToDictionaryAsync(x => x.DetailId, x => x.Quantity,
                cancellationToken);
        var requestedById = request.Lineas.ToDictionary(x => x.CompraDetalleId);
        foreach (var line in request.Lineas)
        {
            var detail = purchase.Detalles.SingleOrDefault(x =>
                x.Id == line.CompraDetalleId);
            if (detail is null || !detail.EsInventariable ||
                detail.ProductoId is null || detail.ProductoPresentacionId is null)
                return CompraOperationResult.Fail(
                    "Una línea no pertenece a la compra o no es inventariable.");
            var pending = detail.CantidadPresentacion -
                received.GetValueOrDefault(detail.Id);
            if (line.CantidadPresentacion - pending > QuantityTolerance)
                return CompraOperationResult.Fail(
                    $"La cantidad de '{detail.Descripcion}' supera el pendiente de {pending:N6}.");
        }

        var movementType = await context.TiposMovimientoInventario
            .SingleAsync(x => x.Codigo == "COMPRA" && x.Estado == 1,
                cancellationToken);
        var movementOrigin = await context.TiposOrigenMovimientoInventario
            .SingleAsync(x => x.Codigo == "RECEPCION_COMPRA" && x.Estado == 1,
                cancellationToken);
        var movementNumber = await InventoryService.ObtenerSiguienteNumeroAsync(
            context, companyId, purchase.EstablecimientoId,
            "MOVIMIENTO_INVENTARIO", cancellationToken);
        var receiptDate = EnsureUtc(request.FechaRecepcion);
        var receipt = new CompraRecepcion
        {
            OperacionUuid = request.OperacionUuid,
            CompraId = purchase.Id,
            EmpresaId = companyId,
            EstablecimientoId = purchase.EstablecimientoId,
            BodegaId = warehouse.Id,
            UsuarioId = userId,
            NumeroRecepcion = $"REC-{movementNumber}",
            FechaRecepcion = receiptDate,
            Estado = "CONFIRMADA",
            Observacion = Normalize(request.Observacion),
            CreatedAt = now,
            UpdatedAt = now
        };
        context.ComprasRecepciones.Add(receipt);
        await context.SaveChangesAsync(cancellationToken);

        var movement = new MovimientoInventario
        {
            EmpresaId = companyId,
            NumeroMovimiento = movementNumber,
            TipoMovimientoId = movementType.Id,
            FechaMovimiento = receiptDate,
            BodegaId = warehouse.Id,
            OrigenTipoId = movementOrigin.Id,
            // La recepción es la operación física idempotente. Una compra
            // puede recibirse parcialmente varias veces en la misma bodega.
            OrigenId = receipt.Id,
            NumeroDocumento = purchase.NumeroDocumento,
            Referencia = receipt.NumeroRecepcion,
            Observacion = Normalize(request.Observacion),
            UsuarioId = userId,
            Estado = "CONFIRMADO",
            CreatedAt = now,
            UpdatedAt = now
        };
        context.MovimientosInventario.Add(movement);

        var effectiveByProduct = new Dictionary<long, (decimal Cost, decimal Quantity)>();
        foreach (var input in request.Lineas)
        {
            var purchaseLine = purchase.Detalles.Single(x =>
                x.Id == input.CompraDetalleId);
            var receivedCost = purchaseLine.CostoTotalLinea *
                input.CantidadPresentacion / purchaseLine.CantidadPresentacion;
            var inventoryInput = new IngresoInventarioDetalleRequest
            {
                ProductoId = purchaseLine.ProductoId!.Value,
                ProductoPresentacionId = purchaseLine.ProductoPresentacionId!.Value,
                Cantidad = input.CantidadPresentacion,
                CostoTotal = receivedCost,
                Lotes = input.Lotes.Select(x => new IngresoInventarioLoteRequest
                {
                    NumeroLote = x.NumeroLote,
                    CantidadBase = x.CantidadBase,
                    FechaElaboracion = x.FechaElaboracion,
                    FechaCaducidad = x.FechaCaducidad,
                    PermitirCrearLoteSimilar = x.PermitirCrearLoteSimilar
                }).ToList(),
                Series = input.Series.Select(x => new IngresoInventarioSerieRequest
                {
                    NumeroSerie = x.NumeroSerie,
                    NumeroLote = x.NumeroLote
                }).ToList(),
                Observacion = request.Observacion
            };
            var lastPurchasePrice = purchaseLine.EsBonificacion
                ? (decimal?)null
                : purchaseLine.PrecioUnitarioCompra /
                  purchaseLine.FactorConversion;
            var costBefore = await context.ProductosCostos.AsNoTracking()
                .SingleOrDefaultAsync(x =>
                    x.ProductoId == purchaseLine.ProductoId.Value,
                    cancellationToken);
            var movementDetail = await InventoryService.AddInitialDetailAsync(
                context, movement, inventoryInput, now, cancellationToken,
                purchaseLine.EsBonificacion, lastPurchasePrice,
                purchaseLine.FactorConversion,
                actualizarConfiguracionExistencia: false);
            var receiptDetail = new CompraRecepcionDetalle
            {
                CompraRecepcion = receipt,
                CompraId = purchase.Id,
                EmpresaId = companyId,
                CompraDetalleId = purchaseLine.Id,
                ProductoId = purchaseLine.ProductoId.Value,
                ProductoPresentacionId = purchaseLine.ProductoPresentacionId.Value,
                BodegaId = warehouse.Id,
                MovimientoInventarioDetalle = movementDetail,
                CantidadPresentacion = input.CantidadPresentacion,
                FactorConversion = purchaseLine.FactorConversion,
                CantidadBase = movementDetail.CantidadBase,
                CostoUnitarioBase = movementDetail.CostoUnitarioBase,
                CostoTotal = movementDetail.CostoTotal,
                EsBonificacion = purchaseLine.EsBonificacion,
                UltimoPrecioCompraAnterior = costBefore?.UltimoPrecioCompra ?? 0,
                UltimoCostoEfectivoAnterior = costBefore?.UltimoCostoEfectivo ?? 0,
                CreatedAt = now
            };
            foreach (var lot in movementDetail.Lotes)
                receiptDetail.Lotes.Add(new CompraRecepcionDetalleLote
                {
                    EmpresaId = companyId,
                    ProductoId = purchaseLine.ProductoId.Value,
                    ProductoLote = lot.ProductoLote,
                    NumeroLote = lot.ProductoLote!.NumeroLote,
                    CantidadBase = lot.CantidadBase,
                    FechaElaboracion = lot.ProductoLote.FechaElaboracion,
                    FechaCaducidad = lot.ProductoLote.FechaCaducidad,
                    CreatedAt = now
                });
            foreach (var serial in movementDetail.Series)
                receiptDetail.Series.Add(new CompraRecepcionDetalleSerie
                {
                    EmpresaId = companyId,
                    ProductoId = purchaseLine.ProductoId.Value,
                    BodegaId = warehouse.Id,
                    ProductoSerie = serial.ProductoSerie,
                    NumeroSerie = serial.ProductoSerie!.NumeroSerie,
                    CreatedAt = now
                });
            receipt.Detalles.Add(receiptDetail);
            var current = effectiveByProduct.GetValueOrDefault(
                purchaseLine.ProductoId.Value);
            effectiveByProduct[purchaseLine.ProductoId.Value] =
                (current.Cost + receivedCost,
                 current.Quantity + movementDetail.CantidadBase);
            await context.SaveChangesAsync(cancellationToken);
        }

        foreach (var pair in effectiveByProduct)
        {
            var cost = await context.ProductosCostos.SingleAsync(x =>
                x.ProductoId == pair.Key, cancellationToken);
            cost.UltimoCostoEfectivo = pair.Value.Quantity == 0
                ? 0 : pair.Value.Cost / pair.Value.Quantity;
            cost.UpdatedAt = now;
        }
        receipt.MovimientoInventario = movement;
        var allComplete = purchase.Detalles.Where(x => x.EsInventariable)
            .All(x => x.CantidadPresentacion -
                (received.GetValueOrDefault(x.Id) +
                 (requestedById.TryGetValue(x.Id, out var added)
                     ? added.CantidadPresentacion : 0m)) <= QuantityTolerance);
        purchase.Estado = allComplete ? "RECIBIDA" : "PARCIALMENTE_RECIBIDA";
        purchase.UpdatedAt = now;
        await context.SaveChangesAsync(cancellationToken);
        context.Auditorias.Add(new Auditoria
        {
            UsuarioId = userId,
            EmpresaId = companyId,
            EstablecimientoId = purchase.EstablecimientoId,
            Accion = ComprasAuditActions.RecepcionConfirmada,
            Entidad = "compras_recepciones",
            EntidadId = receipt.Id,
            Descripcion = $"Recepción {receipt.NumeroRecepcion} confirmada con movimiento {movement.NumeroMovimiento}.",
            CreatedAt = now
        });
        await context.SaveChangesAsync(cancellationToken);
        return CompraOperationResult.Ok(receipt.Id,
            $"Recepción confirmada. Kardex {movement.NumeroMovimiento} generado.");
    }

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime()
    };

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
