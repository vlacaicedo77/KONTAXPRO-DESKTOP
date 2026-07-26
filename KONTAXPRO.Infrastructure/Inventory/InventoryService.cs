using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Inventario;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Infrastructure.Inventory;

public sealed class InventoryService(
    IDbContextFactory<KontaxDbContext> dbContextFactory) : IInventoryService
{
    public async Task<InventoryOperationResult> RegistrarIngresoInicialAsync(
        IngresoInventarioRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EmpresaId <= 0 || request.BodegaId <= 0)
            return InventoryOperationResult.Fail(
                "La empresa y la bodega son obligatorias.");
        if (!request.UsuarioId.HasValue || request.UsuarioId <= 0)
            return InventoryOperationResult.Fail(
                "El usuario que registra el movimiento es obligatorio.");
        if (request.Detalles.Count == 0)
            return InventoryOperationResult.Fail(
                "Debe agregar al menos un producto.");

        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var bodega = await context.Bodegas
                .Include(x => x.Establecimiento)
                .SingleOrDefaultAsync(
                    x => x.Id == request.BodegaId &&
                         x.Estado == 1 &&
                         x.Establecimiento!.EmpresaId == request.EmpresaId,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "La bodega no pertenece a la empresa activa.");

            var tipo = await context.TiposMovimientoInventario
                .SingleAsync(x => x.Codigo == "INVENTARIO_INICIAL" &&
                                  x.Estado == 1, cancellationToken);
            var origen = await context.TiposOrigenMovimientoInventario
                .SingleAsync(x => x.Codigo == "INVENTARIO_INICIAL" &&
                                  x.Estado == 1, cancellationToken);

            var numero = await ObtenerSiguienteNumeroAsync(
                context,
                request.EmpresaId,
                bodega.EstablecimientoId,
                "MOVIMIENTO_INVENTARIO",
                cancellationToken);

            var now = DateTime.UtcNow;
            var movimiento = new MovimientoInventario
            {
                EmpresaId = request.EmpresaId,
                NumeroMovimiento = numero,
                TipoMovimientoId = tipo.Id,
                FechaMovimiento = request.FechaMovimiento.ToUniversalTime(),
                BodegaId = request.BodegaId,
                OrigenTipoId = origen.Id,
                OrigenId = 0,
                NumeroDocumento = Normalize(request.NumeroDocumento),
                Referencia = Normalize(request.Referencia),
                Observacion = Normalize(request.Observacion),
                UsuarioId = request.UsuarioId.Value,
                Estado = "CONFIRMADO",
                CreatedAt = now,
                UpdatedAt = now
            };
            context.MovimientosInventario.Add(movimiento);
            await context.SaveChangesAsync(cancellationToken);

            // El inventario inicial es su propio documento de origen.
            movimiento.OrigenId = movimiento.Id;

            foreach (var detail in request.Detalles)
                await AddInitialDetailAsync(
                    context, movimiento, detail, now, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return InventoryOperationResult.Ok(
                movimiento.Id,
                $"Inventario inicial registrado con número {numero}.");
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
                "No fue posible registrar el movimiento por un conflicto de integridad.");
        }
    }

    private static async Task AddInitialDetailAsync(
        KontaxDbContext context,
        MovimientoInventario movement,
        IngresoInventarioDetalleRequest request,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (request.Cantidad <= 0)
            throw new InvalidOperationException(
                "La cantidad debe ser mayor que cero.");
        if (request.CostoTotal < 0)
            throw new InvalidOperationException(
                "El costo total no puede ser negativo.");

        var presentation = await context.ProductosPresentaciones
            .Include(x => x.Producto)
            .SingleOrDefaultAsync(
                x => x.Id == request.ProductoPresentacionId &&
                     x.ProductoId == request.ProductoId &&
                     x.EmpresaId == movement.EmpresaId &&
                     x.Estado == 1,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "La presentación no pertenece al producto o a la empresa.");
        var product = presentation.Producto!;
        if (!product.ManejaInventario)
            throw new InvalidOperationException(
                $"'{product.Nombre}' no maneja inventario.");
        if (!presentation.PermiteCompra)
            throw new InvalidOperationException(
                $"La presentación '{presentation.Nombre}' no permite ingresos.");

        var baseQuantity = request.Cantidad * presentation.FactorConversion;
        var unitCost = request.CostoTotal / baseQuantity;

        var existence = await context.ProductosExistencias
            .SingleOrDefaultAsync(
                x => x.ProductoId == product.Id &&
                     x.BodegaId == movement.BodegaId,
                cancellationToken);
        if (existence is null)
        {
            existence = new ProductoExistencia
            {
                ProductoId = product.Id,
                BodegaId = movement.BodegaId,
                StockActual = 0,
                StockReservado = 0,
                StockMinimo = 0,
                CreatedAt = now,
                UpdatedAt = now
            };
            context.ProductosExistencias.Add(existence);
        }

        var stockBefore = existence.StockActual;
        var totalStockBefore = await context.ProductosExistencias
            .Where(x => x.ProductoId == product.Id)
            .SumAsync(x => x.StockActual, cancellationToken);
        var cost = await context.ProductosCostos
            .SingleOrDefaultAsync(x => x.ProductoId == product.Id,
                cancellationToken);
        if (cost is null)
        {
            cost = new ProductoCosto
            {
                ProductoId = product.Id,
                CreatedAt = now,
                UpdatedAt = now
            };
            context.ProductosCostos.Add(cost);
        }

        var averageBefore = cost.CostoPromedio;
        var totalStockAfter = totalStockBefore + baseQuantity;
        var averageAfter = totalStockAfter == 0
            ? 0
            : ((totalStockBefore * averageBefore) + request.CostoTotal) /
              totalStockAfter;

        existence.StockActual += baseQuantity;
        existence.UpdatedAt = now;
        cost.UltimoPrecioCompra = unitCost;
        cost.UltimoCostoEfectivo = unitCost;
        cost.CostoPromedio = averageAfter;
        cost.UpdatedAt = now;

        var detail = new MovimientoInventarioDetalle
        {
            MovimientoInventario = movement,
            ProductoId = product.Id,
            ProductoPresentacionId = presentation.Id,
            CantidadPresentacion = request.Cantidad,
            FactorConversion = presentation.FactorConversion,
            CantidadBase = baseQuantity,
            CostoUnitarioBase = unitCost,
            CostoTotal = request.CostoTotal,
            StockAnterior = stockBefore,
            StockNuevo = existence.StockActual,
            CostoPromedioAnterior = averageBefore,
            CostoPromedioNuevo = averageAfter,
            EsBonificacion = false,
            Observacion = Normalize(request.Observacion),
            CreatedAt = now,
            UpdatedAt = now
        };
        context.MovimientosInventarioDetalles.Add(detail);

        ProductoLote? lot = null;
        if (product.ManejaLotes)
        {
            if (string.IsNullOrWhiteSpace(request.NumeroLote))
                throw new InvalidOperationException(
                    $"Debe indicar el lote de '{product.Nombre}'.");

            var lotNumber = request.NumeroLote.Trim();
            lot = await context.ProductosLotes.SingleOrDefaultAsync(
                x => x.ProductoId == product.Id &&
                     x.NumeroLote == lotNumber,
                cancellationToken);
            if (lot is null)
            {
                lot = new ProductoLote
                {
                    ProductoId = product.Id,
                    NumeroLote = lotNumber,
                    FechaElaboracion = request.FechaElaboracion,
                    FechaCaducidad = request.FechaCaducidad,
                    Estado = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                context.ProductosLotes.Add(lot);
                await context.SaveChangesAsync(cancellationToken);
            }

            var lotStock = await context.ProductosLotesExistencias
                .SingleOrDefaultAsync(
                    x => x.LoteId == lot.Id &&
                         x.BodegaId == movement.BodegaId,
                    cancellationToken);
            if (lotStock is null)
            {
                lotStock = new ProductoLoteExistencia
                {
                    LoteId = lot.Id,
                    BodegaId = movement.BodegaId,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                context.ProductosLotesExistencias.Add(lotStock);
            }

            var lotBefore = lotStock.StockActual;
            lotStock.StockActual += baseQuantity;
            lotStock.UpdatedAt = now;
            detail.Lotes.Add(new MovimientoInventarioDetalleLote
            {
                ProductoLote = lot,
                CantidadBase = baseQuantity,
                StockLoteAnterior = lotBefore,
                StockLoteNuevo = lotStock.StockActual,
                CreatedAt = now
            });
        }
        else if (!string.IsNullOrWhiteSpace(request.NumeroLote))
        {
            throw new InvalidOperationException(
                $"'{product.Nombre}' no maneja lotes.");
        }

        if (product.ManejaSeries)
        {
            if (baseQuantity != decimal.Truncate(baseQuantity) ||
                request.NumerosSerie.Count != (int)baseQuantity)
                throw new InvalidOperationException(
                    $"Debe indicar una serie única por unidad de '{product.Nombre}'.");

            var normalized = request.NumerosSerie
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToList();
            if (normalized.Count != normalized.Distinct().Count())
                throw new InvalidOperationException(
                    "Los números de serie no pueden repetirse.");

            var availableState = await context.EstadosSerie
                .SingleAsync(x => x.Codigo == "DISPONIBLE" && x.Estado == 1,
                    cancellationToken);
            foreach (var serialNumber in normalized)
            {
                if (await context.ProductosSeries.AnyAsync(
                        x => x.ProductoId == product.Id &&
                             x.NumeroSerie == serialNumber,
                        cancellationToken))
                    throw new InvalidOperationException(
                        $"La serie '{serialNumber}' ya existe.");

                var serial = new ProductoSerie
                {
                    ProductoId = product.Id,
                    ProductoLote = lot,
                    BodegaId = movement.BodegaId,
                    NumeroSerie = serialNumber,
                    EstadoSerieId = availableState.Id,
                    Observacion = Normalize(request.Observacion),
                    CreatedAt = now,
                    UpdatedAt = now
                };
                context.ProductosSeries.Add(serial);
                detail.Series.Add(new MovimientoInventarioDetalleSerie
                {
                    ProductoSerie = serial,
                    CreatedAt = now
                });
            }
        }
        else if (request.NumerosSerie.Count > 0)
        {
            throw new InvalidOperationException(
                $"'{product.Nombre}' no maneja series.");
        }
    }

    private static async Task<string> ObtenerSiguienteNumeroAsync(
        KontaxDbContext context,
        long empresaId,
        long establecimientoId,
        string tipoCodigo,
        CancellationToken cancellationToken)
    {
        var type = await context.TiposDocumentoInterno.SingleAsync(
            x => x.Codigo == tipoCodigo && x.Estado == 1,
            cancellationToken);

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO s_configuracion.secuenciales_internos
                 (empresa_id, establecimiento_id, tipo_documento_interno_id,
                  ultimo_secuencial, created_at, updated_at)
             VALUES ({empresaId}, {establecimientoId}, {type.Id},
                     0, {DateTime.UtcNow}, {DateTime.UtcNow})
             ON CONFLICT (empresa_id, establecimiento_id,
                          tipo_documento_interno_id) DO NOTHING
             """, cancellationToken);

        var sequence = await context.SecuencialesInternos
            .FromSqlInterpolated(
                $"""
                 SELECT * FROM s_configuracion.secuenciales_internos
                 WHERE empresa_id = {empresaId}
                   AND establecimiento_id = {establecimientoId}
                   AND tipo_documento_interno_id = {type.Id}
                 FOR UPDATE
                 """)
            .SingleAsync(cancellationToken);
        sequence.UltimoSecuencial++;
        sequence.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return $"{type.PrefijoDefault}-{sequence.UltimoSecuencial:000000}";
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
