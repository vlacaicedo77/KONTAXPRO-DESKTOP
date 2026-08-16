using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Inventory;
using KONTAXPRO.Application.Models.Inventario;
using KONTAXPRO.Application.Models.Productos;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Infrastructure.Inventory;

public sealed class InventoryQueryService(
    IDbContextFactory<KontaxDbContext> dbContextFactory)
    : IInventoryQueryService
{
    public async Task<InventarioCatalogoDto> ListarAsync(
        InventarioCatalogoRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EmpresaId <= 0 || request.UsuarioId <= 0)
            return new();

        await using var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken);
        await InventorySecurity.RequirePermissionAsync(context,
            request.UsuarioId, request.EmpresaId, "INVENTARIO_VER_KARDEX",
            cancellationToken);
        var canViewCosts = await InventorySecurity.HasPermissionAsync(context,
            request.UsuarioId, request.EmpresaId, "INVENTARIO_VER_COSTO",
            cancellationToken);

        var authorizedWarehouses = AuthorizedWarehouses(context,
            request.EmpresaId, request.UsuarioId, request.EstablecimientoId,
            request.BodegaId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
        var expiringIds = await context.ProductosLotesExistencias.AsNoTracking()
            .Where(x => authorizedWarehouses.Contains(x.BodegaId) &&
                x.StockActual - x.StockReservado > 0 &&
                x.Lote!.Estado == 1 && x.Lote.FechaCaducidad != null &&
                x.Lote.FechaCaducidad >= today &&
                x.Lote.Producto!.AlertaCaducidad &&
                x.Lote.Producto.DiasAlertaCaducidad != null &&
                x.Lote.FechaCaducidad <= today.AddDays(
                    x.Lote.Producto.DiasAlertaCaducidad.Value))
            .Select(x => x.Lote!.ProductoId).Distinct()
            .ToListAsync(cancellationToken);

        var products = context.Productos.AsNoTracking()
            .Where(x => x.EmpresaId == request.EmpresaId && x.Estado == 1 &&
                x.ManejaInventario);
        if (!string.IsNullOrWhiteSpace(request.Busqueda))
        {
            var term = request.Busqueda.Trim().ToUpperInvariant();
            products = products.Where(x => x.Codigo.ToUpper().Contains(term) ||
                x.Nombre.ToUpper().Contains(term) ||
                (x.Marca != null && x.Marca.Nombre.ToUpper().Contains(term)) ||
                x.Presentaciones.Any(p => p.Estado == 1 &&
                    (p.Nombre.ToUpper().Contains(term) ||
                     p.Codigo.ToUpper().Contains(term) ||
                     (p.CodigoBarras != null &&
                      p.CodigoBarras.ToUpper().Contains(term)))));
        }

        var metrics = products.Select(p => new
        {
            Producto = p,
            Stock = p.Existencias.Where(e =>
                authorizedWarehouses.Contains(e.BodegaId))
                .Sum(e => (decimal?)e.StockActual) ?? 0,
            Reservado = p.Existencias.Where(e =>
                authorizedWarehouses.Contains(e.BodegaId))
                .Sum(e => (decimal?)e.StockReservado) ?? 0,
            Minimo = p.Existencias.Where(e =>
                authorizedWarehouses.Contains(e.BodegaId))
                .Sum(e => (decimal?)e.StockMinimo) ?? 0,
            Bodegas = p.Existencias.Count(e =>
                authorizedWarehouses.Contains(e.BodegaId) && e.StockActual != 0),
            Bajo = p.Existencias.Any(e =>
                authorizedWarehouses.Contains(e.BodegaId) &&
                e.StockActual - e.StockReservado > 0 && e.StockMinimo > 0 &&
                e.StockActual - e.StockReservado <= e.StockMinimo),
            Costo = canViewCosts && p.Costo != null
                ? p.Costo.CostoPromedio : 0,
            TieneMovimientosOperativos = p.MovimientosDetalles.Any(d =>
                d.MovimientoInventario!.EmpresaId == request.EmpresaId &&
                d.MovimientoInventario.Estado == "CONFIRMADO" &&
                d.MovimientoInventario.TipoMovimiento!.Codigo !=
                    "INVENTARIO_INICIAL" &&
                !(d.MovimientoInventario.OrigenTipo!.Codigo == "TRANSFERENCIA" &&
                  d.MovimientoInventario.Observacion ==
                    TransferenciaInventarioRules
                        .InitialWarehouseCorrectionMarker))
        });

        var result = new InventarioCatalogoDto
        {
            Productos = await metrics.CountAsync(cancellationToken),
            SinStock = await metrics.CountAsync(x =>
                x.Stock - x.Reservado <= 0, cancellationToken),
            StockBajo = await metrics.CountAsync(x => x.Bajo,
                cancellationToken),
            PorCaducar = expiringIds.Count,
            ValorInventario = canViewCosts
                ? await metrics.SumAsync(x => x.Stock * x.Costo,
                    cancellationToken)
                : 0
        };

        var indicator = request.Indicador.Trim().ToUpperInvariant();
        if (indicator == "SIN_STOCK")
            metrics = metrics.Where(x => x.Stock - x.Reservado <= 0);
        else if (indicator == "STOCK_BAJO")
            metrics = metrics.Where(x => x.Bajo);
        else if (indicator == "POR_CADUCAR")
            metrics = metrics.Where(x => expiringIds.Contains(x.Producto.Id));

        result.Total = await metrics.CountAsync(cancellationToken);
        var size = Math.Clamp(request.TamanoPagina, 10, 100);
        var page = Math.Max(1, request.Pagina);
        var effectiveOrder = canViewCosts ? request.Orden :
            request.Orden is InventarioCatalogoOrden.CostoPromedio or
                InventarioCatalogoOrden.Valor
                ? InventarioCatalogoOrden.Producto : request.Orden;
        var orderedMetrics = (effectiveOrder, request.OrdenDescendente) switch
        {
            (InventarioCatalogoOrden.Stock, false) => metrics
                .OrderBy(x => x.Stock)
                .ThenBy(x => x.Producto.Nombre),
            (InventarioCatalogoOrden.Stock, true) => metrics
                .OrderByDescending(x => x.Stock)
                .ThenBy(x => x.Producto.Nombre),
            (InventarioCatalogoOrden.Reservado, false) => metrics
                .OrderBy(x => x.Reservado)
                .ThenBy(x => x.Producto.Nombre),
            (InventarioCatalogoOrden.Reservado, true) => metrics
                .OrderByDescending(x => x.Reservado)
                .ThenBy(x => x.Producto.Nombre),
            (InventarioCatalogoOrden.Disponible, false) => metrics
                .OrderBy(x => x.Stock - x.Reservado)
                .ThenBy(x => x.Producto.Nombre),
            (InventarioCatalogoOrden.Disponible, true) => metrics
                .OrderByDescending(x => x.Stock - x.Reservado)
                .ThenBy(x => x.Producto.Nombre),
            (InventarioCatalogoOrden.CostoPromedio, false) => metrics
                .OrderBy(x => x.Costo)
                .ThenBy(x => x.Producto.Nombre),
            (InventarioCatalogoOrden.CostoPromedio, true) => metrics
                .OrderByDescending(x => x.Costo)
                .ThenBy(x => x.Producto.Nombre),
            (InventarioCatalogoOrden.Valor, false) => metrics
                .OrderBy(x => x.Stock * x.Costo)
                .ThenBy(x => x.Producto.Nombre),
            (InventarioCatalogoOrden.Valor, true) => metrics
                .OrderByDescending(x => x.Stock * x.Costo)
                .ThenBy(x => x.Producto.Nombre),
            (InventarioCatalogoOrden.Producto, true) => metrics
                .OrderByDescending(x => x.Producto.Nombre)
                .ThenByDescending(x => x.Producto.Codigo),
            _ => metrics.OrderBy(x => x.Producto.Nombre)
                .ThenBy(x => x.Producto.Codigo)
        };
        var pageIds = await orderedMetrics
            .ThenBy(x => x.Producto.Id)
            .Skip((page - 1) * size).Take(size)
            .Select(x => new InventarioItemDto
            {
                ProductoId = x.Producto.Id,
                Codigo = x.Producto.Codigo,
                Producto = x.Producto.Nombre,
                Marca = x.Producto.Marca == null ? null : x.Producto.Marca.Nombre,
                Categoria = x.Producto.CategoriaProducto == null
                    ? null : x.Producto.CategoriaProducto.Nombre,
                Modelo = x.Producto.Modelo,
                TipoControl = x.Producto.ManejaLotes
                    ? (x.Producto.ManejaSeries ? "LOTE Y SERIE" : "LOTE")
                    : (x.Producto.ManejaSeries ? "SERIE" : "NORMAL"),
                StockActual = x.Stock,
                StockReservado = x.Reservado,
                StockMinimo = x.Minimo,
                CostoPromedio = x.Costo,
                BodegasConStock = x.Bodegas,
                LotesPorCaducar = 0,
                PuedeCompletarInventarioInicial =
                    !x.TieneMovimientosOperativos
            }).ToListAsync(cancellationToken);

        if (pageIds.Count > 0)
        {
            var visibleProductIds = pageIds.Select(x => x.ProductoId).ToList();
            var presentations = await context.ProductosPresentaciones
                .AsNoTracking()
                .Where(x => visibleProductIds.Contains(x.ProductoId) &&
                            x.Estado == 1)
                .OrderByDescending(x => x.EsPresentacionBase)
                .ThenBy(x => x.FactorConversion)
                .ThenBy(x => x.Nombre)
                .Select(x => new
                {
                    x.ProductoId,
                    Item = new InventarioPresentacionResumenDto
                    {
                        Nombre = x.Nombre,
                        Factor = x.FactorConversion,
                        EsBase = x.EsPresentacionBase
                    }
                })
                .ToListAsync(cancellationToken);
            var presentationsByProduct = presentations
                .GroupBy(x => x.ProductoId)
                .ToDictionary(x => x.Key, x => x.Select(y => y.Item).ToList());
            foreach (var item in pageIds)
            {
                item.Presentaciones = presentationsByProduct
                    .GetValueOrDefault(item.ProductoId) ?? [];
                for (var index = 0; index < item.Presentaciones.Count; index++)
                    item.Presentaciones[index].MostrarSeparador =
                        index < item.Presentaciones.Count - 1;
            }
        }

        if (pageIds.Count > 0 && expiringIds.Count > 0)
        {
            var ids = pageIds.Select(x => x.ProductoId).ToList();
            var counts = await context.ProductosLotesExistencias.AsNoTracking()
                .Where(x => ids.Contains(x.Lote!.ProductoId) &&
                    authorizedWarehouses.Contains(x.BodegaId) &&
                    x.StockActual - x.StockReservado > 0 &&
                    x.Lote!.FechaCaducidad >= today &&
                    x.Lote.FechaCaducidad <= today.AddDays(
                        x.Lote.Producto!.DiasAlertaCaducidad!.Value))
                .GroupBy(x => x.Lote!.ProductoId)
                .Select(g => new { ProductId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ProductId, x => x.Count,
                    cancellationToken);
            foreach (var item in pageIds)
                item.LotesPorCaducar = counts.GetValueOrDefault(item.ProductoId);
        }
        result.Items = pageIds;
        return result;
    }

    public async Task<InventarioCatalogosDto> ObtenerCatalogosAsync(
        long empresaId, long usuarioId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken);
        await InventorySecurity.RequirePermissionAsync(context, usuarioId,
            empresaId, "INVENTARIO_VER_KARDEX", cancellationToken);
        var warehouses = AuthorizedWarehouses(context, empresaId, usuarioId,
            null, null);
        return new InventarioCatalogosDto
        {
            Bodegas = await context.Bodegas.AsNoTracking()
                .Where(x => warehouses.Contains(x.Id) && x.Estado == 1)
                .OrderBy(x => x.Codigo).Select(x => new InventarioOpcionDto
                {
                    Id = x.Id, Codigo = x.Codigo, Nombre = x.Nombre,
                    EstablecimientoId = x.EstablecimientoId
                }).ToListAsync(cancellationToken),
            TiposMovimiento = await context.TiposMovimientoInventario
                .AsNoTracking().Where(x => x.Estado == 1)
                .OrderBy(x => x.Codigo).Select(x => new InventarioOpcionDto
                { Id = x.Id, Codigo = x.Codigo, Nombre = x.Codigo })
                .ToListAsync(cancellationToken),
            TiposOrigen = await context.TiposOrigenMovimientoInventario
                .AsNoTracking().Where(x => x.Estado == 1)
                .OrderBy(x => x.Codigo).Select(x => new InventarioOpcionDto
                { Id = x.Id, Codigo = x.Codigo, Nombre = x.Codigo })
                .ToListAsync(cancellationToken)
        };
    }

    public async Task<InventarioProductoDetalleDto?> ObtenerDetalleAsync(
        long empresaId, long usuarioId, long productoId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken);
        await InventorySecurity.RequirePermissionAsync(context, usuarioId,
            empresaId, "INVENTARIO_VER_KARDEX", cancellationToken);
        var canViewCosts = await InventorySecurity.HasPermissionAsync(context,
            usuarioId, empresaId, "INVENTARIO_VER_COSTO", cancellationToken);
        var warehouses = AuthorizedWarehouses(context, empresaId, usuarioId,
            null, null);
        var product = await context.Productos.AsNoTracking()
            .Where(x => x.Id == productoId && x.EmpresaId == empresaId &&
                x.ManejaInventario)
            .Select(x => new InventarioProductoDetalleDto
            {
                ProductoId = x.Id, Codigo = x.Codigo, Producto = x.Nombre,
                Marca = x.Marca == null ? null : x.Marca.Nombre,
                Categoria = x.CategoriaProducto == null
                    ? null : x.CategoriaProducto.Nombre,
                Modelo = x.Modelo,
                TipoControl = x.ManejaLotes
                    ? (x.ManejaSeries ? "LOTE Y SERIE" : "LOTE")
                    : (x.ManejaSeries ? "SERIE" : "NORMAL"),
                StockActual = x.Existencias.Where(e =>
                    warehouses.Contains(e.BodegaId)).Sum(e =>
                    (decimal?)e.StockActual) ?? 0,
                StockReservado = x.Existencias.Where(e =>
                    warehouses.Contains(e.BodegaId)).Sum(e =>
                    (decimal?)e.StockReservado) ?? 0,
                CostoPromedio = canViewCosts && x.Costo != null
                    ? x.Costo.CostoPromedio : 0,
                UltimoCosto = canViewCosts && x.Costo != null
                    ? x.Costo.UltimoCostoEfectivo : 0,
                UltimoPrecioCompra = canViewCosts && x.Costo != null
                    ? x.Costo.UltimoPrecioCompra : 0
            }).SingleOrDefaultAsync(cancellationToken);
        if (product is null) return null;

        product.Presentaciones = await context.ProductosPresentaciones
            .AsNoTracking().Where(x => x.ProductoId == productoId &&
                x.EmpresaId == empresaId && x.Estado == 1)
            .OrderByDescending(x => x.EsPresentacionBase)
            .ThenBy(x => x.FactorConversion)
            .Select(x => new InventarioPresentacionDto
            {
                Id = x.Id, Nombre = x.Nombre,
                Factor = x.FactorConversion,
                EsBase = x.EsPresentacionBase
            }).ToListAsync(cancellationToken);

        product.Bodegas = await context.ProductosExistencias.AsNoTracking()
            .Where(x => x.ProductoId == productoId &&
                warehouses.Contains(x.BodegaId))
            .OrderBy(x => x.Bodega!.Codigo)
            .Select(x => new InventarioBodegaDetalleDto
            {
                BodegaId = x.BodegaId,
                Bodega = x.Bodega!.Codigo + " · " + x.Bodega.Nombre,
                StockActual = x.StockActual,
                StockReservado = x.StockReservado,
                StockMinimo = x.StockMinimo,
                Ubicacion = x.Ubicacion
            }).ToListAsync(cancellationToken);
        product.Lotes = await context.ProductosLotesExistencias.AsNoTracking()
            .Where(x => x.Lote!.ProductoId == productoId &&
                warehouses.Contains(x.BodegaId) && x.StockActual != 0)
            .OrderBy(x => x.Lote!.FechaCaducidad).ThenBy(x => x.Lote!.NumeroLote)
            .Select(x => new InventarioLoteDetalleDto
            {
                LoteId = x.LoteId, BodegaId = x.BodegaId,
                Numero = x.Lote!.NumeroLote,
                Bodega = x.Bodega!.Codigo + " · " + x.Bodega.Nombre,
                StockActual = x.StockActual,
                StockReservado = x.StockReservado,
                Elaboracion = x.Lote.FechaElaboracion,
                Caducidad = x.Lote.FechaCaducidad
            }).ToListAsync(cancellationToken);
        product.Series = await context.ProductosSeries.AsNoTracking()
            .Where(x => x.ProductoId == productoId &&
                warehouses.Contains(x.BodegaId))
            .OrderBy(x => x.NumeroSerie)
            .Select(x => new InventarioSerieDetalleDto
            {
                SerieId = x.Id, BodegaId = x.BodegaId,
                Numero = x.NumeroSerie,
                Bodega = x.Bodega!.Codigo + " · " + x.Bodega.Nombre,
                Lote = x.ProductoLote == null ? null : x.ProductoLote.NumeroLote,
                Estado = x.EstadoSerie!.Codigo
            }).ToListAsync(cancellationToken);
        product.MovimientosRecientes = await BuildKardex(context,
                context.MovimientosInventarioDetalles.AsNoTracking()
                    .Where(x => x.ProductoId == productoId &&
                        x.MovimientoInventario!.EmpresaId == empresaId &&
                        warehouses.Contains(x.MovimientoInventario.BodegaId))
                    .OrderByDescending(x => x.MovimientoInventario!.FechaMovimiento)
                    .ThenByDescending(x => x.Id).Take(10), canViewCosts,
                cancellationToken);
        return product;
    }

    public async Task<KardexPaginaDto> ObtenerKardexPaginadoAsync(
        KardexPaginadoRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken);
        await InventorySecurity.RequirePermissionAsync(context,
            request.UsuarioId, request.EmpresaId, "INVENTARIO_VER_KARDEX",
            cancellationToken);
        var canViewCosts = await InventorySecurity.HasPermissionAsync(context,
            request.UsuarioId, request.EmpresaId, "INVENTARIO_VER_COSTO",
            cancellationToken);
        var warehouses = AuthorizedWarehouses(context, request.EmpresaId,
            request.UsuarioId, request.EstablecimientoId, request.BodegaId);
        var query = context.MovimientosInventarioDetalles.AsNoTracking()
            .Where(x => x.MovimientoInventario!.EmpresaId == request.EmpresaId &&
                warehouses.Contains(x.MovimientoInventario.BodegaId));
        if (request.ProductoId.HasValue)
            query = query.Where(x => x.ProductoId == request.ProductoId);
        if (request.Desde.HasValue)
        {
            var from = request.Desde.Value.ToDateTime(TimeOnly.MinValue,
                DateTimeKind.Utc);
            query = query.Where(x => x.MovimientoInventario!.FechaMovimiento >= from);
        }
        if (request.Hasta.HasValue)
        {
            var until = request.Hasta.Value.AddDays(1).ToDateTime(
                TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.MovimientoInventario!.FechaMovimiento < until);
        }
        if (request.TipoMovimientoId.HasValue)
            query = query.Where(x => x.MovimientoInventario!.TipoMovimientoId ==
                request.TipoMovimientoId);
        if (request.TipoOrigenId.HasValue)
            query = query.Where(x => x.MovimientoInventario!.OrigenTipoId ==
                request.TipoOrigenId);
        if (request.Naturaleza is "ENTRADA" or "SALIDA")
            query = query.Where(x => x.MovimientoInventario!.TipoMovimiento!
                .Naturaleza == request.Naturaleza);
        if (!string.IsNullOrWhiteSpace(request.Busqueda))
        {
            var term = request.Busqueda.Trim().ToUpperInvariant();
            query = query.Where(x => x.Producto!.Nombre.ToUpper().Contains(term) ||
                x.Producto.Codigo.ToUpper().Contains(term) ||
                x.MovimientoInventario!.NumeroMovimiento.ToUpper().Contains(term) ||
                (x.MovimientoInventario.NumeroDocumento != null &&
                 x.MovimientoInventario.NumeroDocumento.ToUpper().Contains(term)));
        }
        var result = new KardexPaginaDto
        {
            Total = await query.CountAsync(cancellationToken)
        };
        var size = Math.Clamp(request.TamanoPagina, 10, 100);
        var ordered = request.FechaDescendente
            ? query.OrderByDescending(x =>
                    x.MovimientoInventario!.FechaMovimiento)
                .ThenByDescending(x => x.Id)
            : query.OrderBy(x => x.MovimientoInventario!.FechaMovimiento)
                .ThenBy(x => x.Id);
        var paged = ordered
            .Skip((Math.Max(1, request.Pagina) - 1) * size).Take(size);
        result.Items = await BuildKardex(context, paged, canViewCosts,
            cancellationToken);
        return result;
    }

    public async Task<InventarioReconciliacionDto> ReconciliarAsync(
        long empresaId, long usuarioId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken);
        await InventorySecurity.RequirePermissionAsync(context, usuarioId,
            empresaId, "INVENTARIO_RECONCILIAR", cancellationToken);
        var warehouses = AuthorizedWarehouses(context, empresaId, usuarioId,
            null, null);
        var result = new InventarioReconciliacionDto
        { VerificadoAt = DateTime.UtcNow };

        var products = await context.Productos.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.ManejaInventario)
            .Select(x => new { x.Id, x.Nombre, x.ManejaLotes, x.ManejaSeries })
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var existenceRows = await context.ProductosExistencias.AsNoTracking()
            .Where(x => x.Producto!.EmpresaId == empresaId &&
                warehouses.Contains(x.BodegaId))
            .Select(x => new
            {
                x.ProductoId, x.BodegaId, x.StockActual, x.StockReservado
            }).ToListAsync(cancellationToken);
        var movementRows = await context.MovimientosInventarioDetalles
            .AsNoTracking().Where(x =>
                x.MovimientoInventario!.EmpresaId == empresaId &&
                warehouses.Contains(x.MovimientoInventario.BodegaId))
            .GroupBy(x => new
            {
                x.ProductoId,
                BodegaId = x.MovimientoInventario!.BodegaId
            }).Select(g => new
            {
                g.Key.ProductoId, g.Key.BodegaId,
                Stock = g.Sum(x => x.MovimientoInventario!.TipoMovimiento!
                    .Naturaleza == "ENTRADA" ? x.CantidadBase : -x.CantidadBase)
            }).ToListAsync(cancellationToken);
        var lotRows = await context.ProductosLotesExistencias.AsNoTracking()
            .Where(x => x.Lote!.Producto!.EmpresaId == empresaId &&
                warehouses.Contains(x.BodegaId))
            .GroupBy(x => new { x.Lote!.ProductoId, x.BodegaId })
            .Select(g => new
            {
                g.Key.ProductoId, g.Key.BodegaId,
                Stock = g.Sum(x => x.StockActual),
                Reserved = g.Sum(x => x.StockReservado)
            }).ToListAsync(cancellationToken);
        var seriesRows = await context.ProductosSeries.AsNoTracking()
            .Where(x => x.Producto!.EmpresaId == empresaId &&
                warehouses.Contains(x.BodegaId) &&
                (x.EstadoSerie!.Codigo == "DISPONIBLE" ||
                 x.EstadoSerie.Codigo == "RESERVADA"))
            .GroupBy(x => new { x.ProductoId, x.BodegaId })
            .Select(g => new
            {
                g.Key.ProductoId, g.Key.BodegaId,
                Stock = g.Count(),
                Reserved = g.Count(x => x.EstadoSerie!.Codigo == "RESERVADA")
            }).ToListAsync(cancellationToken);

        var existenceByKey = existenceRows.ToDictionary(
            x => (x.ProductoId, x.BodegaId));
        var movementByKey = movementRows.ToDictionary(
            x => (x.ProductoId, x.BodegaId));
        var lotsByKey = lotRows.ToDictionary(x => (x.ProductoId, x.BodegaId));
        var seriesByKey = seriesRows.ToDictionary(
            x => (x.ProductoId, x.BodegaId));
        var keys = existenceByKey.Keys.Union(movementByKey.Keys)
            .Union(lotsByKey.Keys).Union(seriesByKey.Keys)
            .OrderBy(x => x.ProductoId).ThenBy(x => x.BodegaId);

        foreach (var key in keys)
        {
            existenceByKey.TryGetValue(key, out var existence);
            movementByKey.TryGetValue(key, out var movement);
            lotsByKey.TryGetValue(key, out var lots);
            seriesByKey.TryGetValue(key, out var series);
            products.TryGetValue(key.ProductoId, out var product);
            var productName = product?.Nombre ?? $"Producto {key.ProductoId}";
            var stock = existence?.StockActual ?? 0;
            var reserved = existence?.StockReservado ?? 0;
            var history = movement?.Stock ?? 0;
            if (existence is null || movement is null || stock != history)
                result.Hallazgos.Add(new InventarioReconciliacionHallazgoDto
                {
                    ProductoId = key.ProductoId, Producto = productName,
                    Tipo = "MOVIMIENTOS",
                    Descripcion = $"Bodega {key.BodegaId}: existencia {stock:0.######}, historial {history:0.######}."
                });
            if ((product?.ManejaLotes == true || lots is not null) &&
                (existence is null || lots is null ||
                 stock != (lots?.Stock ?? 0) ||
                 reserved != (lots?.Reserved ?? 0)))
                result.Hallazgos.Add(new InventarioReconciliacionHallazgoDto
                {
                    ProductoId = key.ProductoId, Producto = productName,
                    Tipo = "LOTES",
                    Descripcion = $"Bodega {key.BodegaId}: stock/reservado {stock:0.######}/{reserved:0.######}, lotes {(lots?.Stock ?? 0):0.######}/{(lots?.Reserved ?? 0):0.######}."
                });
            if ((product?.ManejaSeries == true || series is not null) &&
                (existence is null || series is null ||
                 stock != (series?.Stock ?? 0) ||
                 reserved != (series?.Reserved ?? 0)))
                result.Hallazgos.Add(new InventarioReconciliacionHallazgoDto
                {
                    ProductoId = key.ProductoId, Producto = productName,
                    Tipo = "SERIES",
                    Descripcion = $"Bodega {key.BodegaId}: stock/reservado {stock:0.######}/{reserved:0.######}, series activas/reservadas {series?.Stock ?? 0}/{series?.Reserved ?? 0}."
                });
        }
        return result;
    }

    private static IQueryable<long> AuthorizedWarehouses(
        KontaxDbContext context, long empresaId, long usuarioId,
        long? establecimientoId, long? bodegaId)
    {
        var query = context.Bodegas.AsNoTracking().Where(x => x.Estado == 1 &&
            x.Establecimiento!.EmpresaId == empresaId &&
            x.Establecimiento.Estado == 1 &&
            context.UsuariosEmpresasEstablecimientos.Any(a =>
                a.UsuarioEmpresa!.UsuarioId == usuarioId &&
                a.UsuarioEmpresa.EmpresaId == empresaId &&
                a.UsuarioEmpresa.Estado == 1 &&
                a.EstablecimientoId == x.EstablecimientoId));
        if (establecimientoId.HasValue)
            query = query.Where(x => x.EstablecimientoId == establecimientoId);
        if (bodegaId.HasValue) query = query.Where(x => x.Id == bodegaId);
        return query.Select(x => x.Id);
    }

    private static Task<List<KardexItemDto>> BuildKardex(
        KontaxDbContext context,
        IQueryable<Domain.Entities.Inventario.MovimientoInventarioDetalle> query,
        bool canViewCosts,
        CancellationToken cancellationToken) => query.Select(x =>
            new KardexItemDto
            {
                MovimientoId = x.MovimientoInventarioId,
                ProductoId = x.ProductoId,
                Producto = x.Producto!.Nombre,
                Fecha = x.MovimientoInventario!.FechaMovimiento,
                NumeroMovimiento = x.MovimientoInventario.NumeroMovimiento,
                Tipo = x.MovimientoInventario.TipoMovimiento!.Codigo,
                Origen = x.MovimientoInventario.OrigenTipo!.Codigo,
                Documento = x.MovimientoInventario.NumeroDocumento,
                Bodega = BodegaDisplayFormatter.Format(
                    x.MovimientoInventario.Bodega!.Codigo,
                    x.MovimientoInventario.Bodega.Nombre),
                Presentacion = x.ProductoPresentacion!.Nombre,
                CantidadPresentacion = x.CantidadPresentacion,
                Factor = x.FactorConversion,
                EntradaBase = x.MovimientoInventario.TipoMovimiento.Naturaleza ==
                    "ENTRADA" ? x.CantidadBase : 0,
                SalidaBase = x.MovimientoInventario.TipoMovimiento.Naturaleza ==
                    "SALIDA" ? x.CantidadBase : 0,
                StockAnterior = x.StockAnterior,
                StockNuevo = x.StockNuevo,
                CostoUnitario = canViewCosts ? x.CostoUnitarioBase : 0,
                CostoTotal = canViewCosts ? x.CostoTotal : 0,
                CostoPromedioAnterior = canViewCosts
                    ? x.CostoPromedioAnterior : 0,
                CostoPromedioNuevo = canViewCosts
                    ? x.CostoPromedioNuevo : 0,
                Usuario = x.MovimientoInventario.Usuario!.NombreCompleto,
                Observacion = x.Observacion ?? x.MovimientoInventario.Observacion
            }).ToListAsync(cancellationToken);
}

internal static class InventorySecurity
{
    internal static Task<bool> HasPermissionAsync(KontaxDbContext context,
        long userId, long companyId, string permission,
        CancellationToken cancellationToken) =>
        context.UsuariosEmpresasRoles.AsNoTracking()
            .AnyAsync(x => x.UsuarioEmpresa!.UsuarioId == userId &&
                x.UsuarioEmpresa.EmpresaId == companyId &&
                x.UsuarioEmpresa.Estado == 1 && x.Rol!.Estado == 1 &&
                x.Rol.RolesPermisos.Any(rp => rp.Permiso!.Estado == 1 &&
                    rp.Permiso.Codigo == permission), cancellationToken);

    internal static async Task RequireCompanyAccessAsync(
        KontaxDbContext context, long userId, long companyId,
        CancellationToken cancellationToken)
    {
        var authorized = await context.UsuariosEmpresas.AsNoTracking()
            .AnyAsync(x => x.UsuarioId == userId &&
                x.EmpresaId == companyId && x.Estado == 1,
                cancellationToken);
        if (!authorized)
            throw new InvalidOperationException(
                "El usuario no tiene acceso a la empresa seleccionada.");
    }

    internal static async Task RequirePermissionAsync(KontaxDbContext context,
        long userId, long companyId, string permission,
        CancellationToken cancellationToken)
    {
        var authorized = await HasPermissionAsync(context, userId, companyId,
            permission, cancellationToken);
        if (!authorized)
            throw new InvalidOperationException(
                $"El usuario no posee el permiso {permission}.");
    }
}
