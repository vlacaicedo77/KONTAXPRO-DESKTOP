using System.Data;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Inventario;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Domain.Entities.Seguridad;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KONTAXPRO.Infrastructure.Inventory;

public sealed class InventoryCancellationService(
    IDbContextFactory<KontaxDbContext> dbContextFactory)
    : IInventoryCancellationService
{
    private const decimal Tolerance = 0.000001m;

    public Task<InventoryOperationResult> AnularAjusteAsync(
        InventoryCancellationRequest request,
        CancellationToken cancellationToken = default) =>
        CancelSingleMovementAsync(request, "AJUSTE", cancellationToken);

    public Task<InventoryOperationResult> AnularInventarioInicialAsync(
        InventoryCancellationRequest request,
        CancellationToken cancellationToken = default) =>
        CancelSingleMovementAsync(request, "INVENTARIO_INICIAL",
            cancellationToken);

    private async Task<InventoryOperationResult> CancelSingleMovementAsync(
        InventoryCancellationRequest request, string originCode,
        CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null) return InventoryOperationResult.Fail(validation);
        await using var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable,
                cancellationToken);
        try
        {
            await InventorySecurity.RequirePermissionAsync(context,
                request.UsuarioId, request.EmpresaId,
                "INVENTARIO_ANULAR_OPERACION", cancellationToken);

            AjusteInventario? adjustment = null;
            MovimientoInventario original;
            long establishmentId;
            if (originCode == "AJUSTE")
            {
                adjustment = await context.AjustesInventario
                    .SingleOrDefaultAsync(x => x.Id == request.OperacionId &&
                        x.EmpresaId == request.EmpresaId, cancellationToken)
                    ?? throw new InvalidOperationException(
                        "El ajuste no existe en la empresa activa.");
                if (adjustment.Estado != "CONFIRMADO")
                    throw new InvalidOperationException(
                        "El ajuste ya fue anulado.");
                establishmentId = adjustment.EstablecimientoId;
                original = await LoadMovementAsync(context, request.EmpresaId,
                    originCode, adjustment.Id, cancellationToken);
            }
            else
            {
                original = await LoadMovementByIdAsync(context,
                    request.EmpresaId, request.OperacionId, originCode,
                    cancellationToken);
                establishmentId = await context.Bodegas.AsNoTracking()
                    .Where(x => x.Id == original.BodegaId)
                    .Select(x => x.EstablecimientoId)
                    .SingleAsync(cancellationToken);
            }
            await RequireEstablishmentAccessAsync(context, request.UsuarioId,
                request.EmpresaId, establishmentId, cancellationToken);

            var now = DateTime.UtcNow;
            var reverse = await InventoryReversalProcessor.ReverseAsync(
                context, original, request.EmpresaId, establishmentId,
                request.UsuarioId, original.OrigenId, original.NumeroDocumento,
                originCode == "AJUSTE" ? "ANULACION_AJUSTE" :
                    "ANULACION_INVENTARIO_INICIAL",
                request.Motivo.Trim(), now, null, cancellationToken);
            if (adjustment is not null)
            {
                adjustment.Estado = "ANULADO";
                adjustment.AnuladoPorUsuarioId = request.UsuarioId;
                adjustment.AnuladaAt = now;
                adjustment.MotivoAnulacion = request.Motivo.Trim();
                adjustment.UpdatedAt = now;
            }
            context.Auditorias.Add(new Auditoria
            {
                UsuarioId = request.UsuarioId,
                EmpresaId = request.EmpresaId,
                EstablecimientoId = establishmentId,
                Accion = originCode == "AJUSTE"
                    ? "ANULAR_AJUSTE_INVENTARIO"
                    : "ANULAR_INVENTARIO_INICIAL",
                Entidad = originCode == "AJUSTE"
                    ? "ajustes_inventario" : "movimientos_inventario",
                EntidadId = request.OperacionId,
                Descripcion = $"Operación anulada mediante el movimiento reverso {reverse.NumeroMovimiento}.",
                CreatedAt = now
            });
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return InventoryOperationResult.Ok(reverse.Id,
                $"Operación anulada con el movimiento {reverse.NumeroMovimiento}.");
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return InventoryOperationResult.Fail(ex.Message);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return InventoryOperationResult.Fail(
                "No se pudo anular porque el inventario cambió simultáneamente.");
        }
        catch (PostgresException ex) when (ex.SqlState ==
            PostgresErrorCodes.SerializationFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return InventoryOperationResult.Fail(
                "El inventario cambió simultáneamente. Actualiza e intenta nuevamente.");
        }
    }

    public async Task<InventoryOperationResult> AnularTransferenciaAsync(
        InventoryCancellationRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(request);
        if (validation is not null) return InventoryOperationResult.Fail(validation);
        await using var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable,
                cancellationToken);
        try
        {
            await InventorySecurity.RequirePermissionAsync(context,
                request.UsuarioId, request.EmpresaId,
                "INVENTARIO_ANULAR_OPERACION", cancellationToken);
            var transfer = await context.TransferenciasInventario
                .SingleOrDefaultAsync(x => x.Id == request.OperacionId &&
                    x.EmpresaId == request.EmpresaId, cancellationToken)
                ?? throw new InvalidOperationException(
                    "La transferencia no existe en la empresa activa.");
            if (transfer.Estado != "CONFIRMADA")
                throw new InvalidOperationException(
                    "La transferencia ya fue anulada.");
            await RequireEstablishmentAccessAsync(context, request.UsuarioId,
                request.EmpresaId, transfer.EstablecimientoOrigenId,
                cancellationToken);
            await RequireEstablishmentAccessAsync(context, request.UsuarioId,
                request.EmpresaId, transfer.EstablecimientoDestinoId,
                cancellationToken);

            var movements = await context.MovimientosInventario
                .Include(x => x.TipoMovimiento)
                .Include(x => x.Detalles).ThenInclude(x => x.Lotes)
                .Include(x => x.Detalles).ThenInclude(x => x.Series)
                .Where(x => x.EmpresaId == request.EmpresaId &&
                    x.OrigenTipo!.Codigo == "TRANSFERENCIA" &&
                    x.OrigenId == transfer.Id && x.Estado == "CONFIRMADO" &&
                    (x.TipoMovimiento!.Codigo == "TRANSFERENCIA_SALIDA" ||
                     x.TipoMovimiento.Codigo == "TRANSFERENCIA_ENTRADA"))
                .ToListAsync(cancellationToken);
            var output = movements.SingleOrDefault(x =>
                x.TipoMovimiento!.Codigo == "TRANSFERENCIA_SALIDA" &&
                x.BodegaId == transfer.BodegaOrigenId)
                ?? throw new InvalidOperationException(
                    "No se encontró la salida original de la transferencia.");
            var input = movements.SingleOrDefault(x =>
                x.TipoMovimiento!.Codigo == "TRANSFERENCIA_ENTRADA" &&
                x.BodegaId == transfer.BodegaDestinoId)
                ?? throw new InvalidOperationException(
                    "No se encontró la entrada original de la transferencia.");
            var productIds = input.Detalles.Select(x => x.ProductoId)
                .Distinct().ToList();
            var lastId = Math.Max(output.Id, input.Id);
            var lastDate = output.FechaMovimiento > input.FechaMovimiento
                ? output.FechaMovimiento : input.FechaMovimiento;
            if (await context.MovimientosInventarioDetalles.AsNoTracking()
                    .AnyAsync(x => productIds.Contains(x.ProductoId) &&
                        x.MovimientoInventario!.Estado == "CONFIRMADO" &&
                        x.MovimientoInventarioId != output.Id &&
                        x.MovimientoInventarioId != input.Id &&
                        (x.MovimientoInventario.FechaMovimiento > lastDate ||
                         (x.MovimientoInventario.FechaMovimiento == lastDate &&
                          x.MovimientoInventarioId > lastId)), cancellationToken))
                throw new InvalidOperationException(
                    "Hay movimientos posteriores de uno de los productos. Revierte primero esas operaciones.");

            await ValidateTransferClosingAsync(context, output, input,
                cancellationToken);
            var now = DateTime.UtcNow;
            var outputType = await context.TiposMovimientoInventario
                .SingleAsync(x => x.Codigo == "TRANSFERENCIA_SALIDA" &&
                    x.Estado == 1, cancellationToken);
            var inputType = await context.TiposMovimientoInventario
                .SingleAsync(x => x.Codigo == "TRANSFERENCIA_ENTRADA" &&
                    x.Estado == 1, cancellationToken);
            var reverseOutput = CreateReverseMovement(transfer.BodegaDestinoId,
                outputType.Id, transfer, request, now);
            var reverseInput = CreateReverseMovement(transfer.BodegaOrigenId,
                inputType.Id, transfer, request, now);
            reverseOutput.NumeroMovimiento = await InventoryService
                .ObtenerSiguienteNumeroAsync(context, request.EmpresaId,
                    transfer.EstablecimientoDestinoId,
                    "MOVIMIENTO_INVENTARIO", cancellationToken);
            reverseInput.NumeroMovimiento = await InventoryService
                .ObtenerSiguienteNumeroAsync(context, request.EmpresaId,
                    transfer.EstablecimientoOrigenId,
                    "MOVIMIENTO_INVENTARIO", cancellationToken);
            context.MovimientosInventario.AddRange(reverseOutput, reverseInput);
            await ApplyTransferReversalAsync(context, input, reverseOutput,
                reverseInput, transfer.BodegaDestinoId,
                transfer.BodegaOrigenId, request.Motivo.Trim(), now,
                cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            output.Estado = input.Estado = "ANULADO";
            output.AnuladoPorUsuarioId = input.AnuladoPorUsuarioId =
                request.UsuarioId;
            output.AnuladoAt = input.AnuladoAt = now;
            output.MotivoAnulacion = input.MotivoAnulacion =
                request.Motivo.Trim();
            output.MovimientoReversoId = reverseInput.Id;
            input.MovimientoReversoId = reverseOutput.Id;
            output.UpdatedAt = input.UpdatedAt = now;
            transfer.Estado = "ANULADA";
            transfer.AnuladoPorUsuarioId = request.UsuarioId;
            transfer.AnuladaAt = now;
            transfer.MotivoAnulacion = request.Motivo.Trim();
            transfer.UpdatedAt = now;
            context.Auditorias.Add(new Auditoria
            {
                UsuarioId = request.UsuarioId,
                EmpresaId = request.EmpresaId,
                EstablecimientoId = transfer.EstablecimientoOrigenId,
                Accion = "ANULAR_TRANSFERENCIA_INVENTARIO",
                Entidad = "transferencias_inventario",
                EntidadId = transfer.Id,
                Descripcion = $"Transferencia {transfer.NumeroTransferencia} anulada mediante movimientos inversos.",
                CreatedAt = now
            });
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return InventoryOperationResult.Ok(reverseOutput.Id,
                $"Transferencia {transfer.NumeroTransferencia} anulada correctamente.");
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return InventoryOperationResult.Fail(ex.Message);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return InventoryOperationResult.Fail(
                "No se pudo anular la transferencia porque el inventario cambió simultáneamente.");
        }
        catch (PostgresException ex) when (ex.SqlState ==
            PostgresErrorCodes.SerializationFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return InventoryOperationResult.Fail(
                "El inventario cambió simultáneamente. Actualiza e intenta nuevamente.");
        }
    }

    private static async Task ValidateTransferClosingAsync(
        KontaxDbContext context, MovimientoInventario output,
        MovimientoInventario input, CancellationToken cancellationToken)
    {
        foreach (var group in output.Detalles.GroupBy(x => x.ProductoId))
        {
            var last = group.OrderBy(x => x.Id).Last();
            var stock = await context.ProductosExistencias.SingleAsync(x =>
                x.ProductoId == group.Key && x.BodegaId == output.BodegaId,
                cancellationToken);
            if (Math.Abs(stock.StockActual - last.StockNuevo) > Tolerance ||
                stock.StockReservado > 0)
                throw new InvalidOperationException(
                    "La existencia de la bodega origen cambió o tiene reservas.");
        }
        foreach (var group in input.Detalles.GroupBy(x => x.ProductoId))
        {
            var last = group.OrderBy(x => x.Id).Last();
            var stock = await context.ProductosExistencias.SingleAsync(x =>
                x.ProductoId == group.Key && x.BodegaId == input.BodegaId,
                cancellationToken);
            if (Math.Abs(stock.StockActual - last.StockNuevo) > Tolerance ||
                stock.StockReservado > 0)
                throw new InvalidOperationException(
                    "La existencia de la bodega destino cambió o tiene reservas.");
        }
    }

    private static MovimientoInventario CreateReverseMovement(long warehouseId,
        long movementTypeId, TransferenciaInventario transfer,
        InventoryCancellationRequest request, DateTime now) => new()
    {
        EmpresaId = request.EmpresaId,
        TipoMovimientoId = movementTypeId,
        FechaMovimiento = now,
        BodegaId = warehouseId,
        OrigenTipoId = 0,
        OrigenId = transfer.Id,
        NumeroDocumento = transfer.NumeroTransferencia,
        Referencia = "ANULACION_TRANSFERENCIA",
        Observacion = request.Motivo.Trim(),
        UsuarioId = request.UsuarioId,
        Estado = "CONFIRMADO",
        CreatedAt = now,
        UpdatedAt = now
    };

    private static async Task ApplyTransferReversalAsync(
        KontaxDbContext context, MovimientoInventario originalInput,
        MovimientoInventario reverseOutput, MovimientoInventario reverseInput,
        long targetWarehouseId, long sourceWarehouseId, string reason,
        DateTime now, CancellationToken cancellationToken)
    {
        reverseOutput.OrigenTipoId = reverseInput.OrigenTipoId =
            originalInput.OrigenTipoId;
        var availableStateId = await context.EstadosSerie.AsNoTracking()
            .Where(x => x.Codigo == "DISPONIBLE" && x.Estado == 1)
            .Select(x => x.Id).SingleAsync(cancellationToken);
        var productIds = originalInput.Detalles.Select(d => d.ProductoId)
            .Distinct().ToList();
        var costs = await context.ProductosCostos
            .Where(x => productIds.Contains(x.ProductoId))
            .ToDictionaryAsync(x => x.ProductoId, cancellationToken);

        foreach (var source in originalInput.Detalles.OrderBy(x => x.Id))
        {
            var targetStock = await context.ProductosExistencias.SingleAsync(x =>
                x.ProductoId == source.ProductoId &&
                x.BodegaId == targetWarehouseId, cancellationToken);
            var sourceStock = await context.ProductosExistencias.SingleAsync(x =>
                x.ProductoId == source.ProductoId &&
                x.BodegaId == sourceWarehouseId, cancellationToken);
            var average = costs[source.ProductoId].CostoPromedio;
            var targetBefore = targetStock.StockActual;
            var sourceBefore = sourceStock.StockActual;
            targetStock.StockActual -= source.CantidadBase;
            sourceStock.StockActual += source.CantidadBase;
            targetStock.UpdatedAt = sourceStock.UpdatedAt = now;
            var outDetail = CopyReverseDetail(source, reverseOutput,
                targetBefore, targetStock.StockActual, average, reason, now);
            var inDetail = CopyReverseDetail(source, reverseInput,
                sourceBefore, sourceStock.StockActual, average, reason, now);

            foreach (var sourceLot in source.Lotes)
            {
                var targetLot = await context.ProductosLotesExistencias
                    .SingleAsync(x => x.LoteId == sourceLot.ProductoLoteId &&
                        x.BodegaId == targetWarehouseId, cancellationToken);
                var originLot = await context.ProductosLotesExistencias
                    .SingleOrDefaultAsync(x =>
                        x.LoteId == sourceLot.ProductoLoteId &&
                        x.BodegaId == sourceWarehouseId, cancellationToken);
                if (originLot is null)
                {
                    originLot = new ProductoLoteExistencia
                    {
                        LoteId = sourceLot.ProductoLoteId,
                        BodegaId = sourceWarehouseId,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    context.ProductosLotesExistencias.Add(originLot);
                }
                if (targetLot.StockReservado > 0 ||
                    targetLot.StockActual < sourceLot.CantidadBase)
                    throw new InvalidOperationException(
                        "Un lote cambió o tiene reservas en la bodega destino.");
                var targetLotBefore = targetLot.StockActual;
                var originLotBefore = originLot.StockActual;
                targetLot.StockActual -= sourceLot.CantidadBase;
                originLot.StockActual += sourceLot.CantidadBase;
                targetLot.UpdatedAt = originLot.UpdatedAt = now;
                outDetail.Lotes.Add(CopyLot(sourceLot, targetLotBefore,
                    targetLot.StockActual, now));
                inDetail.Lotes.Add(CopyLot(sourceLot, originLotBefore,
                    originLot.StockActual, now));
            }
            foreach (var sourceSeries in source.Series)
            {
                var series = await context.ProductosSeries.SingleAsync(x =>
                    x.Id == sourceSeries.ProductoSerieId,
                    cancellationToken);
                if (series.BodegaId != targetWarehouseId ||
                    series.EstadoSerieId != availableStateId)
                    throw new InvalidOperationException(
                        $"La serie '{series.NumeroSerie}' cambió de bodega o estado.");
                series.BodegaId = sourceWarehouseId;
                series.UpdatedAt = now;
                outDetail.Series.Add(new MovimientoInventarioDetalleSerie
                { ProductoSerieId = series.Id, CreatedAt = now });
                inDetail.Series.Add(new MovimientoInventarioDetalleSerie
                { ProductoSerieId = series.Id, CreatedAt = now });
            }
        }
    }

    private static MovimientoInventarioDetalle CopyReverseDetail(
        MovimientoInventarioDetalle source, MovimientoInventario movement,
        decimal stockBefore, decimal stockAfter, decimal average,
        string reason, DateTime now)
    {
        var detail = new MovimientoInventarioDetalle
        {
            MovimientoInventario = movement,
            ProductoId = source.ProductoId,
            ProductoPresentacionId = source.ProductoPresentacionId,
            CantidadPresentacion = source.CantidadPresentacion,
            FactorConversion = source.FactorConversion,
            CantidadBase = source.CantidadBase,
            CostoUnitarioBase = source.CostoUnitarioBase,
            CostoTotal = source.CostoTotal,
            StockAnterior = stockBefore,
            StockNuevo = stockAfter,
            CostoPromedioAnterior = average,
            CostoPromedioNuevo = average,
            EsBonificacion = source.EsBonificacion,
            Observacion = reason,
            CreatedAt = now,
            UpdatedAt = now
        };
        movement.Detalles.Add(detail);
        return detail;
    }

    private static MovimientoInventarioDetalleLote CopyLot(
        MovimientoInventarioDetalleLote source, decimal before,
        decimal after, DateTime now) => new()
    {
        ProductoLoteId = source.ProductoLoteId,
        CantidadBase = source.CantidadBase,
        StockLoteAnterior = before,
        StockLoteNuevo = after,
        CreatedAt = now
    };

    private static Task<MovimientoInventario> LoadMovementAsync(
        KontaxDbContext context, long companyId, string originCode,
        long originId, CancellationToken cancellationToken) =>
        BaseMovementQuery(context).SingleAsync(x => x.EmpresaId == companyId &&
            x.OrigenTipo!.Codigo == originCode && x.OrigenId == originId &&
            x.Estado == "CONFIRMADO", cancellationToken);

    private static async Task<MovimientoInventario> LoadMovementByIdAsync(
        KontaxDbContext context, long companyId, long movementId,
        string originCode, CancellationToken cancellationToken) =>
        await BaseMovementQuery(context).SingleOrDefaultAsync(x =>
                x.Id == movementId && x.EmpresaId == companyId &&
                x.OrigenTipo!.Codigo == originCode, cancellationToken)
            ?? throw new InvalidOperationException(
                "El movimiento no existe o no corresponde al tipo solicitado.");

    private static IQueryable<MovimientoInventario> BaseMovementQuery(
        KontaxDbContext context) => context.MovimientosInventario
        .Include(x => x.TipoMovimiento)
        .Include(x => x.Detalles).ThenInclude(x => x.Lotes)
        .Include(x => x.Detalles).ThenInclude(x => x.Series);

    private static string? Validate(InventoryCancellationRequest request)
    {
        if (request.EmpresaId <= 0 || request.UsuarioId <= 0 ||
            request.OperacionId <= 0)
            return "Empresa, usuario y operación son obligatorios.";
        return string.IsNullOrWhiteSpace(request.Motivo) ||
               request.Motivo.Trim().Length < 5
            ? "Indica un motivo de anulación de al menos 5 caracteres."
            : null;
    }

    private static async Task RequireEstablishmentAccessAsync(
        KontaxDbContext context, long userId, long companyId,
        long establishmentId, CancellationToken cancellationToken)
    {
        var authorized = await context.UsuariosEmpresasEstablecimientos
            .AsNoTracking().AnyAsync(x =>
                x.UsuarioEmpresa!.UsuarioId == userId &&
                x.UsuarioEmpresa.EmpresaId == companyId &&
                x.UsuarioEmpresa.Estado == 1 &&
                x.EstablecimientoId == establishmentId, cancellationToken);
        if (!authorized)
            throw new InvalidOperationException(
                "El usuario no tiene acceso al establecimiento de la operación.");
    }
}
