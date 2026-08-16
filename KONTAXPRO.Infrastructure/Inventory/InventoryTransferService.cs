using System.Data;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Inventory;
using KONTAXPRO.Application.Models.Inventario;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Domain.Entities.Seguridad;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KONTAXPRO.Infrastructure.Inventory;

public sealed class InventoryTransferService(
    IDbContextFactory<KontaxDbContext> dbContextFactory)
    : IInventoryTransferService
{
    public async Task<InventoryOperationResult> RegistrarAsync(
        TransferenciaInventarioRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(request.Observacion?.Trim(),
                TransferenciaInventarioRules.InitialWarehouseCorrectionMarker,
                StringComparison.OrdinalIgnoreCase))
            return InventoryOperationResult.Fail(
                "La marca de corrección inicial está reservada para el proceso correspondiente.");
        var validation = TransferenciaInventarioRules.Validate(request);
        if (validation is not null) return InventoryOperationResult.Fail(validation);

        return await RegistrarCoreAsync(request, null, cancellationToken);
    }

    public async Task<InventoryOperationResult> CorregirBodegaInventarioInicialAsync(
        CorreccionBodegaInventarioInicialRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = TransferenciaInventarioRules.Validate(request);
        if (validation is not null) return InventoryOperationResult.Fail(validation);

        var transferRequest = new TransferenciaInventarioRequest
        {
            EmpresaId = request.EmpresaId,
            UsuarioId = request.UsuarioId,
            BodegaOrigenId = request.BodegaOrigenId,
            BodegaDestinoId = request.BodegaDestinoId,
            Fecha = request.Fecha,
            Motivo = request.Motivo.Trim(),
            Observacion = TransferenciaInventarioRules
                .InitialWarehouseCorrectionMarker
        };
        return await RegistrarCoreAsync(transferRequest, request,
            cancellationToken);
    }

    private async Task<InventoryOperationResult> RegistrarCoreAsync(
        TransferenciaInventarioRequest request,
        CorreccionBodegaInventarioInicialRequest? correction,
        CancellationToken cancellationToken)
    {
        var esCorreccionInventarioInicial = correction is not null;

        await using var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable,
                cancellationToken);
        try
        {
            await InventorySecurity.RequirePermissionAsync(context,
                request.UsuarioId, request.EmpresaId,
                esCorreccionInventarioInicial
                    ? "INVENTARIO_AGREGAR_ENTRADA_INICIAL"
                    : "INVENTARIO_TRANSFERIR", cancellationToken);
            var warehouses = await context.Bodegas
                .Include(x => x.Establecimiento)
                .Where(x => (x.Id == request.BodegaOrigenId ||
                             x.Id == request.BodegaDestinoId) && x.Estado == 1 &&
                    x.Establecimiento!.Estado == 1 &&
                    x.Establecimiento.EmpresaId == request.EmpresaId)
                .ToDictionaryAsync(x => x.Id, cancellationToken);
            if (!warehouses.TryGetValue(request.BodegaOrigenId, out var source) ||
                !warehouses.TryGetValue(request.BodegaDestinoId, out var target))
                throw new InvalidOperationException(
                    "Las bodegas no pertenecen a la empresa seleccionada.");
            if (!esCorreccionInventarioInicial &&
                (!source.PermiteTransferenciasInternas ||
                 !target.PermiteTransferenciasInternas))
                throw new InvalidOperationException(
                    "Una de las bodegas no permite transferencias internas.");
            if (esCorreccionInventarioInicial)
            {
                if (source.EstablecimientoId != target.EstablecimientoId)
                    throw new InvalidOperationException(
                        "La corrección inicial solo puede realizarse entre bodegas del mismo establecimiento.");
                request.EstablecimientoOrigenId = source.EstablecimientoId;
                request.EstablecimientoDestinoId = target.EstablecimientoId;
            }
            else if (source.EstablecimientoId != request.EstablecimientoOrigenId ||
                     target.EstablecimientoId != request.EstablecimientoDestinoId)
                throw new InvalidOperationException(
                    "Las bodegas no pertenecen a los establecimientos seleccionados.");
            var authorizedEstablishments = await context
                .UsuariosEmpresasEstablecimientos.AsNoTracking()
                .Where(x => x.UsuarioEmpresa!.UsuarioId == request.UsuarioId &&
                    x.UsuarioEmpresa.EmpresaId == request.EmpresaId &&
                    x.UsuarioEmpresa.Estado == 1)
                .Select(x => x.EstablecimientoId).ToListAsync(cancellationToken);
            if (!authorizedEstablishments.Contains(source.EstablecimientoId) ||
                !authorizedEstablishments.Contains(target.EstablecimientoId))
                throw new InvalidOperationException(
                    "No tienes acceso a uno de los establecimientos de la transferencia.");
            if (esCorreccionInventarioInicial)
            {
                request.Detalles =
                [
                    await PrepararCorreccionInventarioInicialAsync(context,
                        request, correction!, cancellationToken)
                ];
                var correctionValidation = TransferenciaInventarioRules
                    .Validate(request);
                if (correctionValidation is not null)
                    throw new InvalidOperationException(correctionValidation);
            }

            var now = DateTime.UtcNow;
            var transferNumber = await InventoryService.ObtenerSiguienteNumeroAsync(
                context, request.EmpresaId, source.EstablecimientoId,
                "TRANSFERENCIA_INVENTARIO", cancellationToken);
            var transfer = new TransferenciaInventario
            {
                EmpresaId = request.EmpresaId,
                EstablecimientoOrigenId = source.EstablecimientoId,
                EstablecimientoDestinoId = target.EstablecimientoId,
                BodegaOrigenId = source.Id,
                BodegaDestinoId = target.Id,
                UsuarioId = request.UsuarioId,
                NumeroTransferencia = transferNumber,
                FechaTransferencia = EnsureUtc(request.Fecha),
                Estado = "CONFIRMADA",
                Observacion = Normalize(request.Observacion),
                CreatedAt = now,
                UpdatedAt = now
            };
            context.TransferenciasInventario.Add(transfer);
            await context.SaveChangesAsync(cancellationToken);

            var outputType = await context.TiposMovimientoInventario.SingleAsync(
                x => x.Codigo == "TRANSFERENCIA_SALIDA" && x.Estado == 1,
                cancellationToken);
            var inputType = await context.TiposMovimientoInventario.SingleAsync(
                x => x.Codigo == "TRANSFERENCIA_ENTRADA" && x.Estado == 1,
                cancellationToken);
            var origin = await context.TiposOrigenMovimientoInventario.SingleAsync(
                x => x.Codigo == "TRANSFERENCIA" && x.Estado == 1,
                cancellationToken);
            var output = new MovimientoInventario
            {
                EmpresaId = request.EmpresaId,
                NumeroMovimiento = await InventoryService.ObtenerSiguienteNumeroAsync(
                    context, request.EmpresaId, source.EstablecimientoId,
                    "MOVIMIENTO_INVENTARIO", cancellationToken),
                TipoMovimientoId = outputType.Id,
                FechaMovimiento = transfer.FechaTransferencia,
                BodegaId = source.Id,
                OrigenTipoId = origin.Id,
                OrigenId = transfer.Id,
                NumeroDocumento = transfer.NumeroTransferencia,
                Referencia = Normalize(request.Motivo),
                Observacion = Normalize(request.Observacion),
                UsuarioId = request.UsuarioId,
                Estado = "CONFIRMADO", CreatedAt = now, UpdatedAt = now
            };
            var input = new MovimientoInventario
            {
                EmpresaId = request.EmpresaId,
                NumeroMovimiento = await InventoryService.ObtenerSiguienteNumeroAsync(
                    context, request.EmpresaId, target.EstablecimientoId,
                    "MOVIMIENTO_INVENTARIO", cancellationToken),
                TipoMovimientoId = inputType.Id,
                FechaMovimiento = transfer.FechaTransferencia,
                BodegaId = target.Id,
                OrigenTipoId = origin.Id,
                OrigenId = transfer.Id,
                NumeroDocumento = transfer.NumeroTransferencia,
                Referencia = Normalize(request.Motivo),
                Observacion = Normalize(request.Observacion),
                UsuarioId = request.UsuarioId,
                Estado = "CONFIRMADO", CreatedAt = now, UpdatedAt = now
            };
            context.MovimientosInventario.AddRange(output, input);
            await context.SaveChangesAsync(cancellationToken);

            foreach (var line in request.Detalles)
            {
                var presentation = await context.ProductosPresentaciones
                    .Include(x => x.Producto)
                    .SingleOrDefaultAsync(x => x.Id == line.ProductoPresentacionId &&
                        x.ProductoId == line.ProductoId &&
                        x.EmpresaId == request.EmpresaId && x.Estado == 1,
                        cancellationToken)
                    ?? throw new InvalidOperationException(
                        "Una presentación no pertenece al producto o a la empresa.");
                var product = presentation.Producto!;
                if (!product.ManejaInventario)
                    throw new InvalidOperationException(
                        $"'{product.Nombre}' no maneja inventario.");
                if (product.ManejaLotes && line.Lotes.Count == 0)
                    throw new InvalidOperationException(
                        $"Selecciona explícitamente los lotes de '{product.Nombre}'.");
                if (product.ManejaSeries && line.Series.Count == 0)
                    throw new InvalidOperationException(
                        $"Selecciona explícitamente las series de '{product.Nombre}'.");

                var serialNumbers = line.Series.Select(x =>
                    x.NumeroSerie.Trim().ToUpperInvariant()).ToList();
                var reusable = serialNumbers.Count == 0
                    ? new Dictionary<string, long>()
                    : await context.ProductosSeries.AsNoTracking()
                        .Where(x => x.ProductoId == product.Id &&
                            serialNumbers.Contains(x.NumeroSerie))
                        .ToDictionaryAsync(x => $"{product.Id}|{x.NumeroSerie}",
                            x => x.Id, cancellationToken);
                var averageCost = await context.ProductosCostos.AsNoTracking()
                    .Where(x => x.ProductoId == product.Id)
                    .Select(x => (decimal?)x.CostoPromedio)
                    .SingleOrDefaultAsync(cancellationToken) ?? 0;
                var outputRequest = new IngresoInventarioDetalleRequest
                {
                    ProductoId = product.Id,
                    ProductoPresentacionId = presentation.Id,
                    Cantidad = line.Cantidad,
                    Lotes = line.Lotes,
                    Series = line.Series,
                    Observacion = request.Motivo
                };
                await InventoryService.AddAdjustmentOutputDetailAsync(context,
                    output, outputRequest, now, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                var baseQuantity = line.Cantidad * presentation.FactorConversion;
                await InventoryService.AddInitialDetailAsync(context, input,
                    new IngresoInventarioDetalleRequest
                    {
                        ProductoId = product.Id,
                        ProductoPresentacionId = presentation.Id,
                        Cantidad = line.Cantidad,
                        CostoTotal = averageCost * baseQuantity,
                        Lotes = line.Lotes,
                        Series = line.Series,
                        Observacion = request.Motivo
                    }, now, cancellationToken,
                    factorConversionHistorico: presentation.FactorConversion,
                    seriesReutilizables: reusable,
                    actualizarUltimoCostoEfectivo: false,
                    actualizarConfiguracionExistencia: false);
                transfer.Detalles.Add(new TransferenciaInventarioDetalle
                {
                    ProductoId = product.Id,
                    ProductoPresentacionId = presentation.Id,
                    CantidadPresentacion = line.Cantidad,
                    FactorConversion = presentation.FactorConversion,
                    CantidadBase = baseQuantity,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            context.Auditorias.Add(new Auditoria
            {
                UsuarioId = request.UsuarioId,
                EmpresaId = request.EmpresaId,
                EstablecimientoId = source.EstablecimientoId,
                Accion = esCorreccionInventarioInicial
                    ? "CORREGIR_BODEGA_INVENTARIO_INICIAL"
                    : "REGISTRAR_TRANSFERENCIA_INVENTARIO",
                Entidad = "transferencias_inventario",
                EntidadId = transfer.Id,
                Descripcion = esCorreccionInventarioInicial
                    ? $"Corrección de bodega inicial {transfer.NumeroTransferencia} confirmada."
                    : $"Transferencia {transfer.NumeroTransferencia} confirmada entre bodegas.",
                CreatedAt = now
            });
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return InventoryOperationResult.Ok(output.Id,
                esCorreccionInventarioInicial
                    ? $"Bodega inicial corregida con el movimiento {transfer.NumeroTransferencia}."
                    : $"Transferencia {transfer.NumeroTransferencia} registrada correctamente.");
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
                "No se pudo confirmar la transferencia porque el inventario cambió simultáneamente o la operación ya fue aplicada.");
        }
        catch (PostgresException ex) when (ex.SqlState ==
            PostgresErrorCodes.SerializationFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return InventoryOperationResult.Fail(
                "El inventario cambió simultáneamente. Actualiza la información e intenta nuevamente.");
        }
    }

    private static async Task<TransferenciaInventarioLineaRequest>
        PrepararCorreccionInventarioInicialAsync(
            KontaxDbContext context,
            TransferenciaInventarioRequest request,
            CorreccionBodegaInventarioInicialRequest correction,
            CancellationToken cancellationToken)
    {
        var productoId = correction.ProductoId;
        var product = await context.Productos.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == productoId &&
                x.EmpresaId == request.EmpresaId && x.Estado == 1 &&
                x.ManejaInventario, cancellationToken)
            ?? throw new InvalidOperationException(
                "El producto no existe, está inactivo o no maneja inventario.");
        var hasOperationalMovements = await context
            .MovimientosInventarioDetalles.AsNoTracking().AnyAsync(x =>
                x.ProductoId == productoId &&
                x.MovimientoInventario!.EmpresaId == request.EmpresaId &&
                x.MovimientoInventario.Estado == "CONFIRMADO" &&
                x.MovimientoInventario.TipoMovimiento!.Codigo !=
                    "INVENTARIO_INICIAL" &&
                !(x.MovimientoInventario.OrigenTipo!.Codigo == "TRANSFERENCIA" &&
                  x.MovimientoInventario.Observacion ==
                    TransferenciaInventarioRules
                        .InitialWarehouseCorrectionMarker), cancellationToken);
        if (hasOperationalMovements)
            throw new InvalidOperationException(
                "El producto ya tiene movimientos posteriores al inventario inicial. Utiliza un ajuste trazable en lugar de esta corrección.");

        var sourceStock = await context.ProductosExistencias.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ProductoId == productoId &&
                x.BodegaId == request.BodegaOrigenId, cancellationToken)
            ?? throw new InvalidOperationException(
                "El producto no tiene existencias en la bodega indicada.");
        if (sourceStock.StockActual <= 0)
            throw new InvalidOperationException(
                "La bodega de origen no tiene stock positivo para corregir.");
        var availableStock = sourceStock.StockActual -
            sourceStock.StockReservado;
        if (correction.CantidadBase > availableStock)
            throw new InvalidOperationException(
                $"Solo existen {availableStock:0.######} unidades disponibles para corregir.");
        var hasInitialMovement = await context.MovimientosInventarioDetalles
            .AsNoTracking().AnyAsync(x => x.ProductoId == productoId &&
                x.MovimientoInventario!.EmpresaId == request.EmpresaId &&
                x.MovimientoInventario.Estado == "CONFIRMADO" &&
                x.MovimientoInventario.TipoMovimiento!.Codigo ==
                    "INVENTARIO_INICIAL", cancellationToken);
        if (!hasInitialMovement)
            throw new InvalidOperationException(
                "El producto no tiene un movimiento confirmado de inventario inicial.");

        var basePresentation = await context.ProductosPresentaciones
            .AsNoTracking().SingleOrDefaultAsync(x =>
                x.ProductoId == productoId &&
                x.EmpresaId == request.EmpresaId &&
                x.EsPresentacionBase && x.Estado == 1, cancellationToken)
            ?? throw new InvalidOperationException(
                "El producto no tiene una presentación base activa.");

        var lots = new List<IngresoInventarioLoteRequest>();
        var series = new List<IngresoInventarioSerieRequest>();
        if (product.ManejaSeries)
        {
            if (correction.Lotes.Count != 0)
                throw new InvalidOperationException(
                    "En productos serializados los lotes se determinan a partir de las series seleccionadas.");
            if (correction.CantidadBase !=
                decimal.Truncate(correction.CantidadBase))
                throw new InvalidOperationException(
                    "La cantidad de un producto serializado debe ser entera.");
            var requestedSeries = correction.Series
                .Select(x => x.NumeroSerie.Trim().ToUpperInvariant())
                .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            if (requestedSeries.Count != correction.CantidadBase ||
                requestedSeries.Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() != requestedSeries.Count)
                throw new InvalidOperationException(
                    "Selecciona exactamente las series que se cambiarán de bodega, sin repetirlas.");
            var sourceSeries = await context.ProductosSeries.AsNoTracking()
                .Where(x => x.ProductoId == productoId &&
                    x.BodegaId == request.BodegaOrigenId &&
                    x.EstadoSerie!.Codigo == "DISPONIBLE" &&
                    requestedSeries.Contains(x.NumeroSerie))
                .Select(x => new
                {
                    x.NumeroSerie,
                    Lote = x.ProductoLote == null
                        ? null : x.ProductoLote.NumeroLote,
                    Elaboracion = x.ProductoLote == null
                        ? null : x.ProductoLote.FechaElaboracion,
                    Caducidad = x.ProductoLote == null
                        ? null : x.ProductoLote.FechaCaducidad
                }).ToListAsync(cancellationToken);
            if (sourceSeries.Count != requestedSeries.Count)
                throw new InvalidOperationException(
                    "Una o más series no están disponibles en la bodega de origen.");
            if (product.ManejaLotes && sourceSeries.Any(x =>
                    string.IsNullOrWhiteSpace(x.Lote)))
                throw new InvalidOperationException(
                    "Todas las series seleccionadas deben estar asociadas a un lote.");
            series = sourceSeries.OrderBy(x => x.NumeroSerie).Select(x =>
                new IngresoInventarioSerieRequest
                {
                    NumeroSerie = x.NumeroSerie,
                    NumeroLote = x.Lote
                }).ToList();
            if (product.ManejaLotes)
                lots = sourceSeries.GroupBy(x => x.Lote!,
                        StringComparer.OrdinalIgnoreCase)
                    .Select(x => new IngresoInventarioLoteRequest
                    {
                        NumeroLote = x.Key,
                        CantidadBase = x.Count(),
                        FechaElaboracion = x.First().Elaboracion,
                        FechaCaducidad = x.First().Caducidad
                    }).ToList();
        }
        else if (product.ManejaLotes)
        {
            if (correction.Series.Count != 0)
                throw new InvalidOperationException(
                    "Este producto no maneja series.");
            var requestedLots = correction.Lotes.Where(x =>
                x.CantidadBase > 0).ToList();
            if (requestedLots.Sum(x => x.CantidadBase) !=
                correction.CantidadBase)
                throw new InvalidOperationException(
                    "La distribución por lotes debe coincidir con la cantidad a corregir.");
            if (requestedLots.Select(x => AjusteInventarioRules
                    .NormalizarLoteExacto(x.NumeroLote))
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
                requestedLots.Count)
                throw new InvalidOperationException(
                    "Un lote no puede repetirse en la distribución.");
            var sourceLots = await context.ProductosLotesExistencias
                .AsNoTracking().Where(x =>
                    x.Lote!.ProductoId == productoId &&
                    x.BodegaId == request.BodegaOrigenId &&
                    x.StockActual - x.StockReservado > 0)
                .Select(x => new
                {
                    Disponible = x.StockActual - x.StockReservado,
                    x.Lote!.NumeroLote,
                    x.Lote.FechaElaboracion,
                    x.Lote.FechaCaducidad
                }).ToListAsync(cancellationToken);
            foreach (var requested in requestedLots)
            {
                var normalized = AjusteInventarioRules.NormalizarLoteExacto(
                    requested.NumeroLote);
                var sourceLot = sourceLots.SingleOrDefault(x =>
                    AjusteInventarioRules.NormalizarLoteExacto(x.NumeroLote) ==
                    normalized) ?? throw new InvalidOperationException(
                        $"El lote '{requested.NumeroLote}' no está disponible en la bodega de origen.");
                if (requested.CantidadBase > sourceLot.Disponible)
                    throw new InvalidOperationException(
                        $"El lote '{sourceLot.NumeroLote}' solo tiene {sourceLot.Disponible:0.######} unidades disponibles.");
                lots.Add(new IngresoInventarioLoteRequest
                {
                    NumeroLote = sourceLot.NumeroLote,
                    CantidadBase = requested.CantidadBase,
                    FechaElaboracion = sourceLot.FechaElaboracion,
                    FechaCaducidad = sourceLot.FechaCaducidad
                });
            }
        }
        else if (correction.Lotes.Count != 0 || correction.Series.Count != 0)
        {
            throw new InvalidOperationException(
                "El producto con control normal no admite lotes ni series.");
        }

        return new TransferenciaInventarioLineaRequest
        {
            ProductoId = productoId,
            ProductoPresentacionId = basePresentation.Id,
            Cantidad = correction.CantidadBase,
            Lotes = lots,
            Series = series
        };
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime()
    };
}
