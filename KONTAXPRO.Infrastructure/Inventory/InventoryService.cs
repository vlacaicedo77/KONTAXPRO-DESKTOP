using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Inventario;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Infrastructure.Inventory;

public class InventoryService : IInventoryService
{
    private readonly IDbContextFactory<KontaxDbContext> _dbContextFactory;

    public InventoryService(
        IDbContextFactory<KontaxDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<InventoryOperationResult> RegistrarIngresoInicialAsync(
        IngresoInventarioRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EmpresaId <= 0)
        {
            return InventoryOperationResult.Fail(
                "La empresa no es válida.");
        }

        if (request.BodegaId <= 0)
        {
            return InventoryOperationResult.Fail(
                "Debe seleccionar una bodega.");
        }

        if (request.Detalles.Count == 0)
        {
            return InventoryOperationResult.Fail(
                "Debe agregar al menos un producto.");
        }

        await using var context =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var bodega = await context.Bodegas
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == request.BodegaId
                     && x.EmpresaId == request.EmpresaId
                     && x.Estado == 1,
                cancellationToken);

        if (bodega is null)
        {
            return InventoryOperationResult.Fail(
                "La bodega seleccionada no pertenece a la empresa activa.");
        }

        await using var transaction =
            await context.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var movimiento = new MovimientoInventario
            {
                EmpresaId = request.EmpresaId,
                BodegaDestinoId = request.BodegaId,
                TipoMovimiento = "INGRESO_INICIAL",
                FechaMovimiento = request.FechaMovimiento,
                NumeroDocumento = request.NumeroDocumento,
                Referencia = request.Referencia,
                Observacion = request.Observacion,
                UsuarioId = request.UsuarioId,
                Estado = "PROCESADO",
                CreatedAt = DateTime.Now
            };

            context.MovimientosInventario.Add(movimiento);

            foreach (var detalleRequest in request.Detalles)
            {
                await ProcesarDetalleIngresoInicialAsync(
                    context,
                    movimiento,
                    request,
                    detalleRequest,
                    cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return InventoryOperationResult.Ok(
                movimiento.Id,
                "El ingreso inicial se registró correctamente.");
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            return InventoryOperationResult.Fail(ex.Message);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);

            return InventoryOperationResult.Fail(
                "Ocurrió un error al registrar el ingreso inicial.");
        }
    }

    private async Task ProcesarDetalleIngresoInicialAsync(
        KontaxDbContext context,
        MovimientoInventario movimiento,
        IngresoInventarioRequest request,
        IngresoInventarioDetalleRequest detalleRequest,
        CancellationToken cancellationToken)
    {
        if (detalleRequest.Cantidad <= 0)
        {
            throw new InvalidOperationException(
                "La cantidad del producto debe ser mayor que cero.");
        }

        if (detalleRequest.CostoTotal < 0)
        {
            throw new InvalidOperationException(
                "El costo total no puede ser negativo.");
        }

        var producto = await context.Productos
            .FirstOrDefaultAsync(
                x => x.Id == detalleRequest.ProductoId
                     && x.EmpresaId == request.EmpresaId
                     && x.Estado == 1,
                cancellationToken);

        if (producto is null)
        {
            throw new InvalidOperationException(
                $"No se encontró el producto con ID {detalleRequest.ProductoId}.");
        }

        if (!producto.ManejaInventario)
        {
            throw new InvalidOperationException(
                $"El producto '{producto.Nombre}' no maneja inventario.");
        }

        var presentacion = await context.ProductosPresentaciones
            .FirstOrDefaultAsync(
                x => x.Id == detalleRequest.ProductoPresentacionId
                     && x.ProductoId == producto.Id
                     && x.Estado == 1,
                cancellationToken);

        if (presentacion is null)
        {
            throw new InvalidOperationException(
                $"La presentación seleccionada no pertenece al producto '{producto.Nombre}'.");
        }

        if (!presentacion.PermiteCompra)
        {
            throw new InvalidOperationException(
                $"La presentación '{presentacion.Nombre}' no permite ingresos por compra/inventario.");
        }

        var cantidadBase =
            detalleRequest.Cantidad * presentacion.FactorConversion;

        if (cantidadBase <= 0)
        {
            throw new InvalidOperationException(
                $"La cantidad base de '{producto.Nombre}' no es válida.");
        }

        var costoUnitarioBase =
            detalleRequest.CostoTotal / cantidadBase;

        var existencia =
            await ObtenerOCrearExistenciaAsync(
                context,
                producto.Id,
                request.BodegaId,
                cancellationToken);

        var stockAnterior =
            await context.ProductosExistencias
                .Where(x => x.ProductoId == producto.Id)
                .SumAsync(
                    x => x.StockActual,
                    cancellationToken);

        var productoCosto =
            await ObtenerOCrearProductoCostoAsync(
                context,
                producto.Id,
                cancellationToken);

        var costoPromedioAnterior =
            productoCosto.CostoPromedio;

        var valorInventarioAnterior =
            stockAnterior * costoPromedioAnterior;

        existencia.StockActual += cantidadBase;
        existencia.UpdatedAt = DateTime.Now;

        var nuevoValorInventario =
            valorInventarioAnterior + detalleRequest.CostoTotal;

        var nuevoStockTotal =
            stockAnterior + cantidadBase;

        var nuevoCostoPromedio =
            nuevoStockTotal > 0
            ? nuevoValorInventario / nuevoStockTotal
            : 0;

        productoCosto.UltimoPrecioCompra =
            costoUnitarioBase;

        productoCosto.UltimoCostoEfectivo =
            costoUnitarioBase;

        productoCosto.CostoPromedio =
            nuevoCostoPromedio;

        productoCosto.CostoMaximoExistencia =
            Math.Max(
                productoCosto.CostoMaximoExistencia,
                costoUnitarioBase);

        productoCosto.UpdatedAt = DateTime.Now;

        ProductoLote? lote = null;

        if (producto.ManejaLotes)
        {
            lote = await ObtenerOCrearLoteAsync(
                context,
                producto,
                detalleRequest,
                costoUnitarioBase,
                cancellationToken);

            var loteExistencia =
                await ObtenerOCrearLoteExistenciaAsync(
                    context,
                    lote.Id,
                    request.BodegaId,
                    cancellationToken);

            loteExistencia.StockActual += cantidadBase;
            loteExistencia.UpdatedAt = DateTime.Now;
        }
        else if (!string.IsNullOrWhiteSpace(detalleRequest.NumeroLote))
        {
            throw new InvalidOperationException(
                $"El producto '{producto.Nombre}' no está configurado para manejar lotes.");
        }

        if (producto.ManejaSeries)
        {
            await RegistrarSeriesAsync(
                context,
                producto,
                lote,
                request.BodegaId,
                detalleRequest,
                cantidadBase,
                costoUnitarioBase,
                cancellationToken);
        }
        else if (detalleRequest.NumerosSerie.Count > 0)
        {
            throw new InvalidOperationException(
                $"El producto '{producto.Nombre}' no está configurado para manejar series.");
        }

        var detalleMovimiento =
            new MovimientoInventarioDetalle
            {
                ProductoId = producto.Id,
                ProductoPresentacionId = presentacion.Id,
                ProductoLote = lote,
                CantidadPresentacion = detalleRequest.Cantidad,
                FactorConversion = presentacion.FactorConversion,
                CantidadBase = cantidadBase,
                CostoUnitarioBase = costoUnitarioBase,
                CostoTotal = detalleRequest.CostoTotal,
                EsBonificacion = false,
                Observacion = detalleRequest.Observacion,
                CreatedAt = DateTime.Now
            };

        movimiento.Detalles.Add(detalleMovimiento);
    }

    private async Task<ProductoExistencia> ObtenerOCrearExistenciaAsync(
        KontaxDbContext context,
        long productoId,
        long bodegaId,
        CancellationToken cancellationToken)
    {
        var existencia =
            await context.ProductosExistencias
                .FirstOrDefaultAsync(
                    x => x.ProductoId == productoId
                         && x.BodegaId == bodegaId,
                    cancellationToken);

        if (existencia is not null)
        {
            return existencia;
        }

        existencia = new ProductoExistencia
        {
            ProductoId = productoId,
            BodegaId = bodegaId,
            StockActual = 0,
            StockReservado = 0,
            CreatedAt = DateTime.Now
        };

        context.ProductosExistencias.Add(existencia);

        return existencia;
    }

    private async Task<ProductoCosto> ObtenerOCrearProductoCostoAsync(
        KontaxDbContext context,
        long productoId,
        CancellationToken cancellationToken)
    {
        var costo =
            await context.ProductosCostos
                .FirstOrDefaultAsync(
                    x => x.ProductoId == productoId,
                    cancellationToken);

        if (costo is not null)
        {
            return costo;
        }

        costo = new ProductoCosto
        {
            ProductoId = productoId,
            UltimoPrecioCompra = 0,
            UltimoCostoEfectivo = 0,
            CostoPromedio = 0,
            CostoMaximoExistencia = 0,
            CreatedAt = DateTime.Now
        };

        context.ProductosCostos.Add(costo);

        return costo;
    }

    private async Task<ProductoLote> ObtenerOCrearLoteAsync(
        KontaxDbContext context,
        Producto producto,
        IngresoInventarioDetalleRequest detalleRequest,
        decimal costoUnitarioBase,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(detalleRequest.NumeroLote))
        {
            throw new InvalidOperationException(
                $"Debe ingresar el lote del producto '{producto.Nombre}'.");
        }

        var numeroLote =
            detalleRequest.NumeroLote.Trim();

        var lote =
            await context.ProductosLotes
                .FirstOrDefaultAsync(
                    x => x.ProductoId == producto.Id
                         && x.NumeroLote == numeroLote,
                    cancellationToken);

        if (lote is not null)
        {
            return lote;
        }

        if (producto.ManejaFechaCaducidad &&
            detalleRequest.FechaCaducidad is null)
        {
            throw new InvalidOperationException(
                $"Debe ingresar la fecha de caducidad del producto '{producto.Nombre}'.");
        }

        if (detalleRequest.FechaFabricacion.HasValue &&
            detalleRequest.FechaCaducidad.HasValue &&
            detalleRequest.FechaCaducidad.Value <
            detalleRequest.FechaFabricacion.Value)
        {
            throw new InvalidOperationException(
                $"La fecha de caducidad de '{producto.Nombre}' no puede ser anterior a la fecha de fabricación.");
        }

        lote = new ProductoLote
        {
            ProductoId = producto.Id,
            NumeroLote = numeroLote,
            FechaFabricacion = detalleRequest.FechaFabricacion,
            FechaCaducidad = detalleRequest.FechaCaducidad,
            CostoUnitarioBase = costoUnitarioBase,
            Observacion = detalleRequest.Observacion,
            Estado = 1,
            CreatedAt = DateTime.Now
        };

        context.ProductosLotes.Add(lote);

        /*
         * Necesitamos el Id del lote antes de crear
         * productos_lotes_existencias.
         */
        await context.SaveChangesAsync(cancellationToken);

        return lote;
    }

    private async Task<ProductoLoteExistencia>
        ObtenerOCrearLoteExistenciaAsync(
            KontaxDbContext context,
            long productoLoteId,
            long bodegaId,
            CancellationToken cancellationToken)
    {
        var existencia =
            await context.ProductosLotesExistencias
                .FirstOrDefaultAsync(
                    x => x.ProductoLoteId == productoLoteId
                         && x.BodegaId == bodegaId,
                    cancellationToken);

        if (existencia is not null)
        {
            return existencia;
        }

        existencia = new ProductoLoteExistencia
        {
            ProductoLoteId = productoLoteId,
            BodegaId = bodegaId,
            StockActual = 0,
            StockReservado = 0,
            CreatedAt = DateTime.Now
        };

        context.ProductosLotesExistencias.Add(existencia);

        return existencia;
    }

    private async Task RegistrarSeriesAsync(
        KontaxDbContext context,
        Producto producto,
        ProductoLote? lote,
        long bodegaId,
        IngresoInventarioDetalleRequest detalleRequest,
        decimal cantidadBase,
        decimal costoUnitarioBase,
        CancellationToken cancellationToken)
    {
        if (cantidadBase != decimal.Truncate(cantidadBase))
        {
            throw new InvalidOperationException(
                $"El producto '{producto.Nombre}' maneja series y no puede ingresar cantidades fraccionadas.");
        }

        var cantidadSeriesEsperadas =
            Convert.ToInt32(cantidadBase);

        var series =
            detalleRequest.NumerosSerie
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        if (series.Count != cantidadSeriesEsperadas)
        {
            throw new InvalidOperationException(
                $"Debe ingresar exactamente {cantidadSeriesEsperadas} número(s) de serie para '{producto.Nombre}'.");
        }

        foreach (var numeroSerie in series)
        {
            var existe =
                await context.ProductosSeries.AnyAsync(
                    x => x.ProductoId == producto.Id
                         && x.NumeroSerie == numeroSerie,
                    cancellationToken);

            if (existe)
            {
                throw new InvalidOperationException(
                    $"La serie '{numeroSerie}' ya existe para el producto '{producto.Nombre}'.");
            }

            context.ProductosSeries.Add(
                new ProductoSerie
                {
                    ProductoId = producto.Id,
                    ProductoLoteId = lote?.Id,
                    BodegaId = bodegaId,
                    NumeroSerie = numeroSerie,
                    CostoUnitarioBase = costoUnitarioBase,
                    EstadoSerie = "DISPONIBLE",
                    CreatedAt = DateTime.Now
                });
        }
    }
}
