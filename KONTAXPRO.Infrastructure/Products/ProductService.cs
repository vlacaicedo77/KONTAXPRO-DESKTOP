using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Productos;
using KONTAXPRO.Application.Models.Inventario;
using KONTAXPRO.Application.Products;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Infrastructure.Inventory;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KONTAXPRO.Infrastructure.Products;

public sealed class ProductService(
    IDbContextFactory<KontaxDbContext> dbContextFactory) : IProductService
{
    public async Task<ProductoCatalogoResultadoDto>
        ObtenerCatalogoProductosAsync(
            ProductoCatalogoQuery request,
            CancellationToken cancellationToken = default)
    {
        if (request.EmpresaId <= 0)
            return new ProductoCatalogoResultadoDto();

        var tamanoPagina = request.TamanoPagina is 25 or 50 or 100
            ? request.TamanoPagina
            : 25;
        var hoy = DateOnly.FromDateTime(DateTime.Today);

        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var productos = context.Productos.AsNoTracking()
            .Where(x => x.EmpresaId == request.EmpresaId);

        productos = request.Estado switch
        {
            ProductoCatalogoEstado.Activos =>
                productos.Where(x => x.Estado == 1),
            ProductoCatalogoEstado.Inactivos =>
                productos.Where(x => x.Estado == 0),
            _ => productos
        };

        var texto = string.IsNullOrWhiteSpace(request.Busqueda)
            ? null
            : request.Busqueda.Trim();
        if (texto is not null)
        {
            var patron = $"%{texto}%";
            productos = productos.Where(x =>
                EF.Functions.ILike(x.Codigo, patron) ||
                EF.Functions.ILike(x.Nombre, patron) ||
                (x.Modelo != null && EF.Functions.ILike(x.Modelo, patron)) ||
                (x.Descripcion != null &&
                 EF.Functions.ILike(x.Descripcion, patron)) ||
                (x.CategoriaProducto != null &&
                 EF.Functions.ILike(x.CategoriaProducto.Nombre, patron)) ||
                (x.Marca != null &&
                 EF.Functions.ILike(x.Marca.Nombre, patron)) ||
                (x.UnidadMedidaBase != null &&
                 (EF.Functions.ILike(x.UnidadMedidaBase.Nombre, patron) ||
                  EF.Functions.ILike(
                      x.UnidadMedidaBase.Abreviatura, patron))) ||
                x.Presentaciones.Any(p => p.Estado == 1 &&
                    p.PermiteVenta &&
                    (EF.Functions.ILike(p.Nombre, patron) ||
                     EF.Functions.ILike(p.Codigo, patron) ||
                     (p.CodigoBarras != null &&
                      EF.Functions.ILike(p.CodigoBarras, patron)))));
        }

        var proyeccion = productos.Select(x =>
            new ProductoCatalogoProjection
            {
                Id = x.Id,
                Codigo = x.Codigo,
                Nombre = x.Nombre,
                Modelo = x.Modelo,
                Categoria = x.CategoriaProducto == null
                    ? null
                    : x.CategoriaProducto.Nombre,
                Marca = x.Marca == null ? null : x.Marca.Nombre,
                UnidadBase = x.UnidadMedidaBase!.Abreviatura,
                TarifaImpuesto = x.Impuestos
                    .Where(i => i.Estado == 1 &&
                                i.TarifaImpuesto!.Estado == 1)
                    .OrderBy(i => i.TarifaImpuestoId)
                    .Select(i => i.TarifaImpuesto!.Nombre)
                    .FirstOrDefault() ?? string.Empty,
                StockDisponible = x.Existencias
                    .Where(e => e.Bodega!.Estado == 1 &&
                                e.Bodega.Establecimiento!.Estado == 1 &&
                                e.Bodega.Establecimiento.EmpresaId ==
                                    request.EmpresaId)
                    .Sum(e => e.StockActual - e.StockReservado),
                StockMinimo = x.Existencias
                    .Where(e => e.Bodega!.Estado == 1 &&
                                e.Bodega.Establecimiento!.Estado == 1 &&
                                e.Bodega.Establecimiento.EmpresaId ==
                                    request.EmpresaId)
                    .Sum(e => e.StockMinimo),
                TieneStockBajo = x.Existencias.Any(e =>
                    e.Bodega!.Estado == 1 &&
                    e.Bodega.Establecimiento!.Estado == 1 &&
                    e.Bodega.Establecimiento.EmpresaId == request.EmpresaId &&
                    e.StockMinimo > 0 &&
                    e.StockActual - e.StockReservado > 0 &&
                    e.StockActual - e.StockReservado <= e.StockMinimo),
                SinStock = x.Existencias
                    .Where(e => e.Bodega!.Estado == 1 &&
                                e.Bodega.Establecimiento!.Estado == 1 &&
                                e.Bodega.Establecimiento.EmpresaId ==
                                    request.EmpresaId)
                    .Sum(e => e.StockActual - e.StockReservado) <= 0,
                CostoPromedio = x.Costo == null ? 0 : x.Costo.CostoPromedio,
                PrecioBase = x.Presentaciones
                    .Where(p => p.EsPresentacionBase && p.Estado == 1)
                    .SelectMany(p => p.Precios)
                    .Where(p => p.Estado == 1 &&
                                p.ListaPrecio!.Estado == 1 &&
                                p.ListaPrecio.EsListaBase)
                    .Select(p => p.MetodoCalculo == "PRECIO_FIJO"
                        ? p.Precio
                        : p.MetodoCalculo == "PORCENTAJE_COSTO"
                            ? (decimal?)(
                                (x.Costo == null ? 0 : x.Costo.CostoPromedio) *
                                p.ProductoPresentacion!.FactorConversion *
                                (1 + (p.Porcentaje ?? 0) / 100m))
                            : null)
                    .FirstOrDefault(),
                ListaPrecioBaseCodigo = x.Presentaciones
                    .Where(p => p.EsPresentacionBase && p.Estado == 1)
                    .SelectMany(p => p.Precios)
                    .Where(p => p.Estado == 1 &&
                                p.ListaPrecio!.Estado == 1 &&
                                p.ListaPrecio.EsListaBase)
                    .Select(p => p.ListaPrecio!.Codigo)
                    .FirstOrDefault() ?? string.Empty,
                CantidadPorCaducar = x.ManejaFechaCaducidad &&
                    x.AlertaCaducidad && x.DiasAlertaCaducidad > 0
                        ? x.Lotes
                            .Where(l => l.Estado == 1 &&
                                l.FechaCaducidad.HasValue &&
                                l.FechaCaducidad.Value >= hoy &&
                                l.FechaCaducidad.Value <= hoy.AddDays(
                                    x.DiasAlertaCaducidad ?? 0))
                            .SelectMany(l => l.Existencias)
                            .Where(e => e.StockActual > 0 &&
                                e.Bodega!.Estado == 1 &&
                                e.Bodega.Establecimiento!.Estado == 1 &&
                                e.Bodega.Establecimiento.EmpresaId ==
                                    request.EmpresaId)
                            .Sum(e => e.StockActual)
                        : 0,
                PorCaducar = x.ManejaFechaCaducidad &&
                    x.AlertaCaducidad && x.DiasAlertaCaducidad > 0 &&
                    x.Lotes.Any(l => l.Estado == 1 &&
                        l.FechaCaducidad.HasValue &&
                        l.FechaCaducidad.Value >= hoy &&
                        l.FechaCaducidad.Value <= hoy.AddDays(
                            x.DiasAlertaCaducidad ?? 0) &&
                        l.Existencias.Any(e => e.StockActual > 0 &&
                            e.Bodega!.Estado == 1 &&
                            e.Bodega.Establecimiento!.Estado == 1 &&
                            e.Bodega.Establecimiento.EmpresaId ==
                                request.EmpresaId)),
                ProximaCaducidad = x.Lotes
                    .Where(l => l.Estado == 1 &&
                                l.FechaCaducidad.HasValue &&
                                l.Existencias.Any(e => e.StockActual > 0))
                    .Min(l => l.FechaCaducidad),
                Estado = (short)x.Estado,
                CoincidenciaExactaCodigoBarras = texto != null &&
                    x.Presentaciones.Any(p => p.Estado == 1 &&
                        p.PermiteVenta &&
                        p.CodigoBarras == texto)
            });

        var kpis = await proyeccion.GroupBy(_ => 1)
            .Select(grupo => new ProductoCatalogoKpisDto
            {
                Productos = grupo.Count(),
                StockBajo = grupo.Count(x => x.TieneStockBajo),
                SinStock = grupo.Count(x => x.SinStock),
                PorCaducar = grupo.Count(x => x.PorCaducar)
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? new ProductoCatalogoKpisDto();

        proyeccion = request.Kpi switch
        {
            ProductoCatalogoKpi.StockBajo =>
                proyeccion.Where(x => x.TieneStockBajo),
            ProductoCatalogoKpi.SinStock =>
                proyeccion.Where(x => x.SinStock),
            ProductoCatalogoKpi.PorCaducar =>
                proyeccion.Where(x => x.PorCaducar),
            _ => proyeccion
        };

        var totalItems = await proyeccion.CountAsync(cancellationToken);
        var totalPaginas = ProductoCatalogoRules.CalcularTotalPaginas(
            totalItems, tamanoPagina);
        var pagina = ProductoCatalogoRules.NormalizarPagina(
            request.Pagina, totalPaginas);

        var ordenados = proyeccion
            .OrderByDescending(x => x.CoincidenciaExactaCodigoBarras);
        ordenados = AplicarOrden(
            ordenados,
            request.Orden,
            request.OrdenDescendente);

        var items = await ordenados
            .ThenBy(x => x.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync(cancellationToken);

        var productosPagina = items.Select(MapearListado).ToList();
        if (productosPagina.Count > 0)
        {
            var productosIds = productosPagina.Select(x => x.Id).ToList();
            var presentaciones = await context.ProductosPresentaciones
                .AsNoTracking()
                .Where(x => x.EmpresaId == request.EmpresaId &&
                            productosIds.Contains(x.ProductoId) &&
                            x.Estado == 1 && x.PermiteVenta)
                .OrderBy(x => x.ProductoId)
                .ThenByDescending(x => x.EsPresentacionBase)
                .ThenBy(x => x.Nombre)
                .Select(x => new PresentacionComercialCatalogoProjection
                {
                    ProductoId = x.ProductoId,
                    Nombre = x.Nombre
                })
                .ToListAsync(cancellationToken);
            var nombresPorProducto = presentaciones
                .GroupBy(x => x.ProductoId)
                .ToDictionary(
                    x => x.Key,
                    x => (IReadOnlyList<string>)x.Select(p => p.Nombre)
                        .ToList());

            foreach (var producto in productosPagina)
            {
                producto.PresentacionesComerciales =
                    nombresPorProducto.GetValueOrDefault(producto.Id) ?? [];
                producto.CantidadPresentaciones =
                    producto.PresentacionesComerciales.Count;
            }

            var configuracionesPrecio = await (
                from presentacion in context.ProductosPresentaciones
                    .AsNoTracking()
                where presentacion.EmpresaId == request.EmpresaId &&
                      productosIds.Contains(presentacion.ProductoId) &&
                      presentacion.Estado == 1 &&
                      presentacion.EsPresentacionBase
                from lista in context.ListasPrecio.AsNoTracking()
                    .Where(x => x.EmpresaId == request.EmpresaId &&
                                x.Estado == 1)
                join precio in context.ProductosPresentacionesPrecios
                        .AsNoTracking()
                        .Where(x => x.Estado == 1)
                    on new
                    {
                        ProductoPresentacionId = presentacion.Id,
                        ListaPrecioId = lista.Id
                    }
                    equals new
                    {
                        precio.ProductoPresentacionId,
                        precio.ListaPrecioId
                    }
                    into preciosConfigurados
                from precio in preciosConfigurados.DefaultIfEmpty()
                select new PrecioBaseListaCatalogoProjection
                {
                    ProductoId = presentacion.ProductoId,
                    ListaCodigo = lista.Codigo,
                    Orden = lista.Orden,
                    EsListaBase = lista.EsListaBase,
                    FactorConversion = presentacion.FactorConversion,
                    CostoPromedio = presentacion.Producto!.Costo == null
                        ? 0
                        : presentacion.Producto.Costo.CostoPromedio,
                    MetodoCalculo = precio == null
                        ? null
                        : precio.MetodoCalculo,
                    Porcentaje = precio == null ? null : precio.Porcentaje,
                    Precio = precio == null ? null : precio.Precio
                })
                .ToListAsync(cancellationToken);

            foreach (var grupo in configuracionesPrecio
                         .GroupBy(x => x.ProductoId))
            {
                var configuracionBase = grupo.FirstOrDefault(x =>
                    x.EsListaBase);
                var precioBase = configuracionBase is null
                    ? null
                    : CalcularPrecioBaseLista(configuracionBase, null);
                var precios = grupo
                    .OrderBy(x => x.Orden)
                    .ThenBy(x => x.ListaCodigo)
                    .Select(x => new ProductoPrecioBaseListaDto
                    {
                        ListaCodigo = x.ListaCodigo,
                        Orden = x.Orden,
                        Precio = CalcularPrecioBaseLista(x, precioBase)
                    })
                    .ToList();
                var producto = productosPagina.First(x => x.Id == grupo.Key);
                producto.PreciosBasePorLista = precios;
            }
        }

        return new ProductoCatalogoResultadoDto
        {
            Items = productosPagina,
            TotalItems = totalItems,
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            TotalPaginas = totalPaginas,
            Kpis = kpis
        };
    }

    public async Task<ProductoDetalleDto?> ObtenerProductoAsync(
        long productoId,
        long empresaId,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var producto = await context.Productos.AsNoTracking()
            .Where(x => x.Id == productoId && x.EmpresaId == empresaId)
            .Select(x => new ProductoDetalleDto
            {
                Id = x.Id,
                EmpresaId = x.EmpresaId,
                CategoriaProductoId = x.CategoriaProductoId,
                MarcaId = x.MarcaId,
                UnidadMedidaBaseId = x.UnidadMedidaBaseId,
                UnidadMedidaBaseAbreviatura =
                    x.UnidadMedidaBase!.Abreviatura,
                Impuestos = x.Impuestos
                    .OrderBy(i => i.TarifaImpuesto!.Nombre)
                    .Select(i => new ProductoImpuestoDto
                    {
                        TarifaImpuestoId = i.TarifaImpuestoId,
                        Nombre = i.TarifaImpuesto!.Nombre,
                        Estado = i.Estado
                    }).ToList(),
                Codigo = x.Codigo,
                Nombre = x.Nombre,
                Modelo = x.Modelo,
                PresentacionNombre = x.Presentaciones
                    .Where(p => p.EsPresentacionBase)
                    .Select(p => p.Nombre)
                    .FirstOrDefault() ?? string.Empty,
                CodigoBarras = x.Presentaciones
                    .Where(p => p.EsPresentacionBase)
                    .Select(p => p.CodigoBarras)
                    .FirstOrDefault(),
                CodigoBarrasInterno = x.Presentaciones
                    .Where(p => p.EsPresentacionBase)
                    .Select(p => p.CodigoBarras != null &&
                                 p.CodigoBarras.StartsWith("KPX-"))
                    .FirstOrDefault(),
                Descripcion = x.Descripcion,
                TipoProducto = x.TipoProducto,
                TipoControlInventario = x.ManejaSeries
                    ? (x.ManejaLotes ? "LOTE_Y_SERIE" : "SERIE")
                    : (x.ManejaLotes ? "LOTE" : "NORMAL"),
                ManejaInventario = x.ManejaInventario,
                ManejaLotes = x.ManejaLotes,
                ManejaSeries = x.ManejaSeries,
                ManejaFechaCaducidad = x.ManejaFechaCaducidad,
                AlertaCaducidad = x.AlertaCaducidad,
                DiasAlertaCaducidad = x.DiasAlertaCaducidad ?? 0,
                Observacion = x.Observacion,
                Estado = (short)x.Estado,
                Presentaciones = x.Presentaciones
                    .OrderByDescending(p => p.EsPresentacionBase)
                    .ThenBy(p => p.Nombre)
                    .Select(p => new ProductoPresentacionDto
                    {
                        Id = p.Id,
                        Codigo = p.Codigo,
                        CodigoBarras = p.CodigoBarras,
                        Nombre = p.Nombre,
                        FactorConversion = p.FactorConversion,
                        EsPresentacionBase = p.EsPresentacionBase,
                        PermiteCompra = p.PermiteCompra,
                        PermiteVenta = p.PermiteVenta,
                        Estado = p.Estado,
                        Precios = p.Precios.Select(pp => new ProductoPrecioDto
                        {
                            Id = pp.Id,
                            ListaPrecioId = pp.ListaPrecioId,
                            ListaPrecioNombre = pp.ListaPrecio!.Nombre,
                            MetodoCalculo = pp.MetodoCalculo,
                            Porcentaje = pp.Porcentaje,
                            Precio = pp.Precio,
                            Estado = pp.Estado
                        }).ToList()
                    }).ToList(),
                Costo = x.Costo == null
                    ? new ProductoCostoDto()
                    : new ProductoCostoDto
                    {
                        UltimoPrecioCompra = x.Costo.UltimoPrecioCompra,
                        UltimoCostoEfectivo = x.Costo.UltimoCostoEfectivo,
                        CostoPromedio = x.Costo.CostoPromedio
                    },
                Existencias = x.Existencias.Select(e => new ProductoExistenciaDto
                {
                    BodegaId = e.BodegaId,
                    BodegaCodigo = e.Bodega!.Codigo,
                    BodegaNombre = e.Bodega!.Nombre,
                    StockActual = e.StockActual,
                    StockReservado = e.StockReservado,
                    StockMinimo = e.StockMinimo,
                    Ubicacion = e.Ubicacion
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (producto is null)
            return null;

        var detallesIniciales = await context.MovimientosInventarioDetalles
            .AsNoTracking()
            .Include(x => x.MovimientoInventario)
                .ThenInclude(x => x!.Bodega)
            .Include(x => x.MovimientoInventario)
                .ThenInclude(x => x!.TipoMovimiento)
            .Include(x => x.ProductoPresentacion)
            .Include(x => x.Lotes)
                .ThenInclude(x => x.ProductoLote)
            .Include(x => x.Series)
                .ThenInclude(x => x.ProductoSerie)
                    .ThenInclude(x => x!.ProductoLote)
            .Where(x =>
                x.ProductoId == productoId &&
                x.MovimientoInventario!.EmpresaId == empresaId &&
                x.MovimientoInventario.Estado == "CONFIRMADO" &&
                x.MovimientoInventario.TipoMovimiento!.Codigo ==
                    "INVENTARIO_INICIAL")
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        producto.InventariosIniciales = detallesIniciales.Select(x =>
            new ProductoInventarioInicialRequest
            {
                MovimientoId = x.MovimientoInventarioId,
                BodegaId = x.MovimientoInventario!.BodegaId,
                BodegaCodigo = x.MovimientoInventario.Bodega!.Codigo,
                BodegaNombre = x.MovimientoInventario.Bodega!.Nombre,
                PresentacionCodigo =
                    x.ProductoPresentacion!.Codigo,
                PresentacionNombre =
                    x.ProductoPresentacion.Nombre,
                CantidadPresentaciones = x.CantidadPresentacion,
                CostoUnitarioPresentacion =
                    x.CantidadPresentacion == 0
                        ? 0
                        : x.CostoTotal / x.CantidadPresentacion,
                Ubicacion = producto.Existencias
                    .FirstOrDefault(e =>
                        e.BodegaId == x.MovimientoInventario.BodegaId)
                    ?.Ubicacion,
                StockMinimo = producto.Existencias
                    .FirstOrDefault(e =>
                        e.BodegaId == x.MovimientoInventario.BodegaId)
                    ?.StockMinimo ?? 0,
                Lotes = x.Lotes.Select(l =>
                    new ProductoInventarioInicialLoteRequest
                    {
                        NumeroLote =
                            l.ProductoLote!.NumeroLote,
                        CantidadBase = l.CantidadBase,
                        FechaElaboracion =
                            l.ProductoLote.FechaElaboracion.HasValue
                                ? l.ProductoLote.FechaElaboracion.Value
                                    .ToDateTime(TimeOnly.MinValue)
                                : null,
                        FechaCaducidad =
                            l.ProductoLote.FechaCaducidad.HasValue
                                ? l.ProductoLote.FechaCaducidad.Value
                                    .ToDateTime(TimeOnly.MinValue)
                                : null
                    }).ToList(),
                NumerosSerie = x.Series.Select(s =>
                    s.ProductoSerie!.ProductoLote == null
                        ? s.ProductoSerie.NumeroSerie
                        : $"{s.ProductoSerie.ProductoLote.NumeroLote}|" +
                          s.ProductoSerie.NumeroSerie).ToList(),
                EsHistorico = true
            }).ToList();

        var tiposMovimientoProducto =
            await context.MovimientosInventarioDetalles.AsNoTracking()
                .Where(x =>
                    x.ProductoId == productoId &&
                    x.MovimientoInventario!.EmpresaId == empresaId &&
                    x.MovimientoInventario.Estado == "CONFIRMADO")
                .Select(x => x.MovimientoInventario!.TipoMovimiento!.Codigo)
                .Distinct()
                .ToListAsync(cancellationToken);
        var tieneMovimientosPosteriores =
            !ProductoInventarioRules.PuedeCompletarInventarioInicial(
                tiposMovimientoProducto);
        producto.PuedeCompletarInventarioInicial =
            !tieneMovimientosPosteriores;
        producto.MotivoNoPuedeCompletarInventarioInicial =
            tieneMovimientosPosteriores
                ? "El producto ya tiene operación posterior. Para corregir " +
                  "existencias utilice Registrar ajuste."
                : null;

        return producto;
    }

    public async Task<ProductOperationResult> GuardarProductoAsync(
        ProductoGuardarRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(request);
        if (validation is not null)
            return ProductOperationResult.Fail(validation);

        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (!await context.Empresas.AnyAsync(
                    x => x.Id == request.EmpresaId && x.Estado == 1,
                    cancellationToken))
                return ProductOperationResult.Fail("La empresa no es válida.");
            if (request.EstablecimientoId.HasValue &&
                !await context.Establecimientos.AnyAsync(
                    x => x.Id == request.EstablecimientoId.Value &&
                         x.EmpresaId == request.EmpresaId &&
                         x.Estado == 1,
                    cancellationToken))
                return ProductOperationResult.Fail(
                    "El establecimiento no pertenece a la empresa activa.");

            if (!await context.UnidadesMedida.AnyAsync(
                    x => x.Id == request.UnidadMedidaBaseId && x.Estado == 1,
                    cancellationToken))
                return ProductOperationResult.Fail(
                    "La unidad de medida no es válida.");

            if (request.CategoriaProductoId.HasValue &&
                !await context.CategoriasProducto.AnyAsync(
                    x => x.Id == request.CategoriaProductoId &&
                         x.EmpresaId == request.EmpresaId &&
                         x.Estado == 1,
                    cancellationToken))
                return ProductOperationResult.Fail(
                    "La categoría no pertenece a la empresa activa.");

            if (request.MarcaId.HasValue &&
                !await context.Marcas.AnyAsync(
                    x => x.Id == request.MarcaId && x.Estado == 1,
                    cancellationToken))
                return ProductOperationResult.Fail("La marca no es válida.");

            var tarifasIds = request.TarifasImpuestoIds.Distinct().ToList();
            var fechaActual = DateOnly.FromDateTime(DateTime.UtcNow);
            if (tarifasIds.Count != 0 &&
                await context.TarifasImpuesto.CountAsync(
                    x => tarifasIds.Contains(x.Id) &&
                         x.Estado == 1 &&
                         x.Impuesto!.Estado == 1 &&
                         x.VigenteDesde <= fechaActual &&
                         (x.VigenteHasta == null ||
                          x.VigenteHasta >= fechaActual),
                    cancellationToken) != tarifasIds.Count)
                return ProductOperationResult.Fail(
                    "Una o más tarifas de impuesto no son válidas.");

            var listaIds = request.Presentaciones
                .SelectMany(x => x.Precios)
                .Select(x => x.ListaPrecioId)
                .Distinct()
                .ToList();
            if (listaIds.Count != 0 &&
                await context.ListasPrecio.CountAsync(
                    x => listaIds.Contains(x.Id) &&
                         x.EmpresaId == request.EmpresaId &&
                         x.Estado == 1,
                    cancellationToken) != listaIds.Count)
                return ProductOperationResult.Fail(
                    "Una o más listas de precio no pertenecen a la empresa.");

            var tiposLista = await context.ListasPrecio.AsNoTracking()
                .Where(x => listaIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.EsListaBase,
                    cancellationToken);
            foreach (var precio in request.Presentaciones.SelectMany(x => x.Precios))
            {
                var metodoValido = tiposLista[precio.ListaPrecioId]
                    ? precio.MetodoCalculo is "PORCENTAJE_COSTO" or "PRECIO_FIJO"
                    : precio.MetodoCalculo is "DESCUENTO_PORCENTAJE" or "PRECIO_FIJO";
                if (!metodoValido)
                    return ProductOperationResult.Fail(
                        "El método de cálculo no corresponde al tipo de lista.");
            }
            if (request.InventariosIniciales.Count > 0)
            {
                var listasActivas = await context.ListasPrecio.AsNoTracking()
                    .Where(x => x.EmpresaId == request.EmpresaId &&
                                x.Estado == 1)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);
                foreach (var presentacion in request.Presentaciones
                             .Where(x => x.Estado == 1))
                {
                    var configuradas = presentacion.Precios
                        .Where(x => x.Estado == 1)
                        .Select(x => x.ListaPrecioId)
                        .Distinct()
                        .ToList();
                    if (configuradas.Count != listasActivas.Count ||
                        configuradas.Except(listasActivas).Any())
                        return ProductOperationResult.Fail(
                            $"Debe configurar todas las listas activas para la presentación '{presentacion.Nombre}'.");
                }
            }

            var bodegaIds = request.Existencias
                .Select(x => x.BodegaId)
                .Concat(request.InventariosIniciales.Select(x => x.BodegaId))
                .Distinct()
                .ToList();
            if (bodegaIds.Count != 0 &&
                await context.Bodegas.CountAsync(
                    x => bodegaIds.Contains(x.Id) &&
                         x.Establecimiento!.EmpresaId == request.EmpresaId &&
                         x.Estado == 1,
                    cancellationToken) != bodegaIds.Count)
                return ProductOperationResult.Fail(
                    "Una o más bodegas no pertenecen a la empresa.");
            var usuarioIds = request.InventariosIniciales
                .Select(x => x.UsuarioId)
                .Distinct()
                .ToList();
            if (usuarioIds.Count != 0 &&
                await context.UsuariosEmpresas.CountAsync(
                    x => usuarioIds.Contains(x.UsuarioId) &&
                         x.EmpresaId == request.EmpresaId &&
                         x.Estado == 1,
                    cancellationToken) != usuarioIds.Count)
                return ProductOperationResult.Fail(
                    "El usuario no tiene acceso activo a la empresa.");

            var isNew = !request.Id.HasValue;
            Producto producto;

            if (isNew)
            {
                var uuid = Guid.NewGuid();
                producto = new Producto
                {
                    Uuid = uuid,
                    EmpresaId = request.EmpresaId,
                    Codigo = $"PRD-{uuid:N}"[..16].ToUpperInvariant(),
                    Estado = 1,
                    CreatedAt = DateTime.UtcNow
                };
                context.Productos.Add(producto);
            }
            else
            {
                producto = await context.Productos
                    .Include(x => x.Presentaciones)
                        .ThenInclude(x => x.Precios)
                    .Include(x => x.Impuestos)
                    .Include(x => x.Costo)
                    .Include(x => x.Existencias)
                    .SingleOrDefaultAsync(
                        x => x.Id == request.Id &&
                             x.EmpresaId == request.EmpresaId,
                        cancellationToken)
                    ?? throw new InvalidOperationException(
                        "No se encontró el producto.");

                if (request.InventariosIniciales.Any(x => !x.EsHistorico) &&
                    await context.MovimientosInventarioDetalles.AsNoTracking()
                        .AnyAsync(x =>
                            x.ProductoId == producto.Id &&
                            x.MovimientoInventario!.Estado == "CONFIRMADO" &&
                            x.MovimientoInventario.TipoMovimiento!.Codigo !=
                                "INVENTARIO_INICIAL",
                            cancellationToken))
                    return ProductOperationResult.Fail(
                        "El producto ya tiene operación posterior. Para " +
                        "corregir existencias utilice Registrar ajuste.");

                var tieneHistoriaInventario = await context.MovimientosInventarioDetalles
                    .AnyAsync(x => x.ProductoId == producto.Id, cancellationToken);
                var tieneExistencias = producto.Existencias.Any(x =>
                    x.StockActual != 0 || x.StockReservado != 0);
                var tieneLotes = await context.ProductosLotes
                    .AnyAsync(x => x.ProductoId == producto.Id, cancellationToken);
                var tieneSeries = await context.ProductosSeries
                    .AnyAsync(x => x.ProductoId == producto.Id, cancellationToken);

                if (producto.UnidadMedidaBaseId != request.UnidadMedidaBaseId &&
                    (tieneHistoriaInventario || tieneExistencias ||
                     tieneLotes || tieneSeries))
                    return ProductOperationResult.Fail(
                        "La unidad base no puede modificarse porque el producto posee stock o historia de inventario.");

                var controlActual = ObtenerTipoControl(
                    producto.ManejaLotes, producto.ManejaSeries);
                var controlSolicitado = ObtenerTipoControl(
                    request.ManejaLotes, request.ManejaSeries);
                if (controlActual != controlSolicitado &&
                    (tieneHistoriaInventario || tieneExistencias ||
                     tieneLotes || tieneSeries))
                    return ProductOperationResult.Fail(
                        "El tipo de control no puede cambiarse directamente porque el producto posee stock o historia. Utilice Convertir control de inventario.");
            }

            if (!string.IsNullOrWhiteSpace(request.CodigoInterno))
                producto.Codigo = NormalizeUpper(request.CodigoInterno)!;
            producto.CategoriaProductoId = request.CategoriaProductoId;
            producto.MarcaId = request.MarcaId;
            producto.UnidadMedidaBaseId = request.UnidadMedidaBaseId;
            producto.Nombre = NormalizeUpper(request.Nombre)!;
            producto.Descripcion = Normalize(request.Descripcion);
            producto.Modelo = NormalizeUpper(request.Modelo);
            producto.TipoProducto = request.TipoProducto;
            producto.ManejaInventario = request.ManejaInventario;
            producto.ManejaLotes = request.ManejaLotes;
            producto.ManejaSeries = request.ManejaSeries;
            producto.ManejaFechaCaducidad = request.ManejaFechaCaducidad;
            producto.AlertaCaducidad = request.AlertaCaducidad;
            producto.DiasAlertaCaducidad = request.AlertaCaducidad
                ? request.DiasAlertaCaducidad
                : null;
            producto.Observacion = Normalize(request.Observacion);
            producto.UpdatedAt = DateTime.UtcNow;

            var basePresentation = producto.Presentaciones
                .SingleOrDefault(x => x.EsPresentacionBase);
            if (basePresentation is null)
            {
                basePresentation = new ProductoPresentacion
                {
                    Uuid = Guid.NewGuid(),
                    EmpresaId = request.EmpresaId,
                    Producto = producto,
                    Codigo = "BASE",
                    FactorConversion = 1,
                    EsPresentacionBase = true,
                    PermiteCompra = true,
                    PermiteVenta = true,
                    Estado = 1,
                    CreatedAt = DateTime.UtcNow
                };
                context.ProductosPresentaciones.Add(basePresentation);
            }

            basePresentation.Nombre = NormalizeUpper(request.PresentacionNombre)!;
            basePresentation.CodigoBarras = request.SinCodigoBarras
                ? null
                : Normalize(request.CodigoBarras);
            basePresentation.UpdatedAt = DateTime.UtcNow;

            foreach (var tarifaId in tarifasIds)
            {
                var impuesto = producto.Impuestos
                    .SingleOrDefault(x => x.TarifaImpuestoId == tarifaId);
                if (impuesto is null)
                {
                    producto.Impuestos.Add(new ProductoImpuesto
                    {
                        TarifaImpuestoId = tarifaId,
                        Estado = 1,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    impuesto.Estado = 1;
                    impuesto.UpdatedAt = DateTime.UtcNow;
                }
            }

            foreach (var impuesto in producto.Impuestos
                         .Where(x => !tarifasIds.Contains(x.TarifaImpuestoId)))
            {
                impuesto.Estado = 0;
                impuesto.UpdatedAt = DateTime.UtcNow;
            }

            foreach (var input in request.Presentaciones
                         .Where(x => !x.EsPresentacionBase))
            {
                var presentation = input.Id.HasValue
                    ? producto.Presentaciones.SingleOrDefault(
                        x => x.Id == input.Id.Value &&
                             !x.EsPresentacionBase)
                    : null;
                if (input.Id.HasValue && presentation is null)
                    throw new InvalidOperationException(
                        "Una presentación adicional no pertenece al producto.");
                if (presentation is not null &&
                    presentation.FactorConversion != input.FactorConversion &&
                    await PresentacionTieneUsoAsync(
                        context, presentation.Id, cancellationToken))
                    return ProductOperationResult.Fail(
                        "El factor no puede modificarse porque la presentación posee movimientos o documentos.");
                if (presentation is null)
                {
                    presentation = new ProductoPresentacion
                    {
                        Uuid = Guid.NewGuid(),
                        EmpresaId = request.EmpresaId,
                        Producto = producto,
                        CreatedAt = DateTime.UtcNow
                    };
                    producto.Presentaciones.Add(presentation);
                }
                presentation.Codigo = NormalizeUpper(input.Codigo)!;
                presentation.CodigoBarras = Normalize(input.CodigoBarras);
                presentation.Nombre = NormalizeUpper(input.Nombre)!;
                presentation.FactorConversion = input.FactorConversion;
                presentation.EsPresentacionBase = false;
                presentation.PermiteCompra = input.PermiteCompra;
                presentation.PermiteVenta = input.PermiteVenta;
                presentation.Estado = input.Estado;
                presentation.UpdatedAt = DateTime.UtcNow;

                foreach (var priceInput in input.Precios)
                {
                    var price = priceInput.Id.HasValue
                        ? presentation.Precios.SingleOrDefault(
                            x => x.Id == priceInput.Id.Value)
                        : presentation.Precios.SingleOrDefault(
                            x => x.ListaPrecioId == priceInput.ListaPrecioId);
                    if (priceInput.Id.HasValue && price is null)
                        throw new InvalidOperationException(
                            "Un precio no pertenece a la presentación.");
                    price ??= new ProductoPresentacionPrecio
                    {
                        ListaPrecioId = priceInput.ListaPrecioId,
                        CreatedAt = DateTime.UtcNow
                    };
                    if (price.Id == 0 &&
                        !presentation.Precios.Contains(price))
                        presentation.Precios.Add(price);
                    price.MetodoCalculo = priceInput.MetodoCalculo;
                    price.Porcentaje = priceInput.Porcentaje;
                    price.Precio = priceInput.Precio;
                    price.Estado = priceInput.Estado;
                    price.UpdatedAt = DateTime.UtcNow;
                }
            }

            var baseInput = request.Presentaciones
                .SingleOrDefault(x => x.EsPresentacionBase);
            if (baseInput is not null)
            {
                foreach (var priceInput in baseInput.Precios)
                {
                    var price = basePresentation.Precios.SingleOrDefault(
                        x => x.ListaPrecioId == priceInput.ListaPrecioId);
                    price ??= new ProductoPresentacionPrecio
                    {
                        ListaPrecioId = priceInput.ListaPrecioId,
                        CreatedAt = DateTime.UtcNow
                    };
                    if (price.Id == 0 &&
                        !basePresentation.Precios.Contains(price))
                        basePresentation.Precios.Add(price);
                    price.MetodoCalculo = priceInput.MetodoCalculo;
                    price.Porcentaje = priceInput.Porcentaje;
                    price.Precio = priceInput.Precio;
                    price.Estado = priceInput.Estado;
                    price.UpdatedAt = DateTime.UtcNow;
                }
            }

            foreach (var input in request.Existencias)
            {
                var existence = producto.Existencias.SingleOrDefault(
                    x => x.BodegaId == input.BodegaId);
                if (existence is null)
                {
                    existence = new ProductoExistencia
                    {
                        BodegaId = input.BodegaId,
                        StockActual = 0,
                        StockReservado = 0,
                        CreatedAt = DateTime.UtcNow
                    };
                    producto.Existencias.Add(existence);
                }
                existence.StockMinimo = input.StockMinimo;
                existence.Ubicacion = NormalizeUpper(input.Ubicacion);
                existence.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync(cancellationToken);

            if (producto.Presentaciones.Any(x =>
                    x.Estado == 1 && x.CodigoBarras == null))
            {
                var prefijo = await context.Establecimientos.AsNoTracking()
                    .Where(x => x.EmpresaId == request.EmpresaId &&
                                x.Estado == 1 &&
                                (!request.EstablecimientoId.HasValue ||
                                 x.Id == request.EstablecimientoId.Value))
                    .OrderByDescending(x => x.EsMatriz)
                    .ThenBy(x => x.Codigo)
                    .Select(x => x.Prefijo)
                    .FirstOrDefaultAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(prefijo))
                    throw new InvalidOperationException(
                        "No existe un establecimiento activo para generar los códigos internos.");
                foreach (var presentation in producto.Presentaciones
                             .Where(x => x.Estado == 1 &&
                                         x.CodigoBarras == null)
                             .OrderByDescending(x => x.EsPresentacionBase)
                             .ThenBy(x => x.Id))
                {
                    presentation.CodigoBarras =
                        ProductoNuevoRules.CrearCodigoBarrasInterno(
                            prefijo,
                            presentation.Id);
                    presentation.UpdatedAt = DateTime.UtcNow;
                }
            }

            var nuevasEntradasIniciales = request.InventariosIniciales
                .Where(x => !x.EsHistorico)
                .ToList();
            if (nuevasEntradasIniciales.Count > 0)
            {
                foreach (var inicial in nuevasEntradasIniciales)
                {
                var bodega = await context.Bodegas
                    .Include(x => x.Establecimiento)
                    .SingleOrDefaultAsync(
                        x => x.Id == inicial.BodegaId &&
                             x.Estado == 1 &&
                             x.Establecimiento!.EmpresaId == request.EmpresaId,
                        cancellationToken)
                    ?? throw new InvalidOperationException(
                        "La bodega del inventario inicial no pertenece a la empresa.");
                var presentacion = producto.Presentaciones.SingleOrDefault(x =>
                    x.Estado == 1 &&
                    x.Codigo == inicial.PresentacionCodigo)
                    ?? throw new InvalidOperationException(
                        "La presentación seleccionada para el inventario inicial no es válida.");
                var tipoMovimiento = await context.TiposMovimientoInventario
                    .SingleAsync(x => x.Codigo == "INVENTARIO_INICIAL" &&
                                      x.Estado == 1, cancellationToken);
                var tipoOrigen = await context.TiposOrigenMovimientoInventario
                    .SingleAsync(x => x.Codigo == "INVENTARIO_INICIAL" &&
                                      x.Estado == 1, cancellationToken);
                var numero = await InventoryService.ObtenerSiguienteNumeroAsync(
                    context, request.EmpresaId, bodega.EstablecimientoId,
                    "MOVIMIENTO_INVENTARIO", cancellationToken);
                var now = DateTime.UtcNow;
                var movimiento = new MovimientoInventario
                {
                    EmpresaId = request.EmpresaId,
                    NumeroMovimiento = numero,
                    TipoMovimientoId = tipoMovimiento.Id,
                    FechaMovimiento = now,
                    BodegaId = bodega.Id,
                    OrigenTipoId = tipoOrigen.Id,
                    OrigenId = 0,
                    Referencia = isNew
                        ? "ALTA DE PRODUCTO"
                        : "COMPLETAR INVENTARIO INICIAL",
                    UsuarioId = inicial.UsuarioId,
                    Estado = "CONFIRMADO",
                    CreatedAt = now,
                    UpdatedAt = now
                };
                context.MovimientosInventario.Add(movimiento);
                await context.SaveChangesAsync(cancellationToken);
                movimiento.OrigenId = movimiento.Id;

                await InventoryService.AddInitialDetailAsync(
                    context,
                    movimiento,
                    new IngresoInventarioDetalleRequest
                    {
                        ProductoId = producto.Id,
                        ProductoPresentacionId = presentacion.Id,
                        Cantidad = inicial.CantidadPresentaciones,
                        CostoTotal = inicial.CantidadPresentaciones *
                                     inicial.CostoUnitarioPresentacion,
                        Ubicacion = NormalizeUpper(inicial.Ubicacion),
                        StockMinimo = inicial.StockMinimo,
                        NumeroLote = NormalizeUpper(inicial.NumeroLote),
                        FechaElaboracion = inicial.FechaElaboracion,
                        FechaCaducidad = inicial.FechaCaducidad,
                        NumerosSerie = inicial.NumerosSerie
                            .Select(x => x.Trim().ToUpperInvariant())
                            .ToList(),
                        Series = inicial.NumerosSerie.Select(x =>
                        {
                            var partes = x.Split('|', 2,
                                StringSplitOptions.TrimEntries);
                            return new IngresoInventarioSerieRequest
                            {
                                NumeroLote = partes.Length == 2
                                    ? NormalizeUpper(partes[0])
                                    : null,
                                NumeroSerie = NormalizeUpper(
                                    partes.Length == 2
                                        ? partes[1]
                                        : partes[0])!
                            };
                        }).ToList(),
                        Lotes = inicial.Lotes.Select(x =>
                            new IngresoInventarioLoteRequest
                            {
                                NumeroLote =
                                    NormalizeUpper(x.NumeroLote)!,
                                CantidadBase = x.CantidadBase,
                                FechaElaboracion = x.FechaElaboracion.HasValue
                                    ? DateOnly.FromDateTime(
                                        x.FechaElaboracion.Value)
                                    : null,
                                FechaCaducidad = x.FechaCaducidad.HasValue
                                    ? DateOnly.FromDateTime(
                                        x.FechaCaducidad.Value)
                                    : null
                            }).ToList()
                    },
                    now,
                    cancellationToken);
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ProductOperationResult.Ok(
                producto.Id,
                isNew
                    ? "Producto creado correctamente."
                    : "Producto actualizado correctamente.");
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException
                  { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaction.RollbackAsync(cancellationToken);
            return ProductOperationResult.Fail(
                "El código del producto o código de barras ya está registrado.");
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ProductOperationResult.Fail(ex.Message);
        }
    }

    public async Task<ProductOperationResult> CambiarEstadoAsync(
        long productoId,
        long empresaId,
        short nuevoEstado,
        CancellationToken cancellationToken = default)
    {
        if (nuevoEstado is not 0 and not 1)
            return ProductOperationResult.Fail("El estado no es válido.");

        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var producto = await context.Productos.SingleOrDefaultAsync(
            x => x.Id == productoId && x.EmpresaId == empresaId,
            cancellationToken);
        if (producto is null)
            return ProductOperationResult.Fail("No se encontró el producto.");

        producto.Estado = nuevoEstado;
        producto.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return ProductOperationResult.Ok(producto.Id,
            nuevoEstado == 1 ? "Producto activado." : "Producto inactivado.");
    }

    public async Task<ProductoCodigoBarrasDto?> BuscarPorCodigoBarrasAsync(
        long empresaId,
        string codigoBarras,
        CancellationToken cancellationToken = default)
    {
        if (empresaId <= 0 || string.IsNullOrWhiteSpace(codigoBarras))
            return null;

        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var code = codigoBarras.Trim();
        return await context.ProductosPresentaciones.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId &&
                        x.CodigoBarras == code &&
                        x.Estado == 1)
            .Select(x => new ProductoCodigoBarrasDto
            {
                ProductoId = x.ProductoId,
                PresentacionId = x.Id,
                CodigoInterno = x.Producto!.Codigo,
                Nombre = x.Producto!.Nombre,
                Modelo = x.Producto!.Modelo,
                Presentacion = x.Nombre,
                CodigoBarras = x.CodigoBarras!
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<ProductoSugerenciaDto>> BuscarSimilaresAsync(
        long empresaId,
        string? nombre,
        string? modelo,
        int limite = 5,
        CancellationToken cancellationToken = default)
    {
        var text = $"{nombre} {modelo}".Trim().ToLowerInvariant();
        if (empresaId <= 0 || text.Length < 3)
            return [];

        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Productos.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId &&
                        x.Estado == 1 &&
                        (x.Nombre.ToLower().Contains(text) ||
                         (x.Modelo != null &&
                          x.Modelo.ToLower().Contains(text))))
            .OrderBy(x => x.Nombre)
            .Take(Math.Clamp(limite, 1, 20))
            .Select(x => new ProductoSugerenciaDto
            {
                ProductoId = x.Id,
                Nombre = x.Nombre,
                Modelo = x.Modelo,
                Marca = x.Marca == null ? null : x.Marca.Nombre,
                Presentaciones = x.Presentaciones
                    .Where(p => p.Estado == 1)
                    .OrderBy(p => p.Nombre)
                    .Select(p => p.Nombre)
                    .Take(4)
                    .ToList()
            })
            .ToListAsync(cancellationToken);
    }

    private static IOrderedQueryable<ProductoCatalogoProjection> AplicarOrden(
        IOrderedQueryable<ProductoCatalogoProjection> query,
        ProductoCatalogoOrden orden,
        bool descendente) =>
        (orden, descendente) switch
        {
            (ProductoCatalogoOrden.Codigo, false) =>
                query.ThenBy(x => x.Codigo),
            (ProductoCatalogoOrden.Codigo, true) =>
                query.ThenByDescending(x => x.Codigo),
            (ProductoCatalogoOrden.Categoria, false) =>
                query.ThenBy(x => x.Categoria).ThenBy(x => x.Nombre),
            (ProductoCatalogoOrden.Categoria, true) =>
                query.ThenByDescending(x => x.Categoria)
                    .ThenByDescending(x => x.Nombre),
            (ProductoCatalogoOrden.Unidad, false) =>
                query.ThenBy(x => x.UnidadBase).ThenBy(x => x.Nombre),
            (ProductoCatalogoOrden.Unidad, true) =>
                query.ThenByDescending(x => x.UnidadBase)
                    .ThenByDescending(x => x.Nombre),
            (ProductoCatalogoOrden.Stock, false) =>
                query.ThenBy(x => x.StockDisponible)
                    .ThenBy(x => x.Nombre),
            (ProductoCatalogoOrden.Stock, true) =>
                query.ThenByDescending(x => x.StockDisponible)
                    .ThenByDescending(x => x.Nombre),
            (ProductoCatalogoOrden.CostoPromedio, false) =>
                query.ThenBy(x => x.CostoPromedio)
                    .ThenBy(x => x.Nombre),
            (ProductoCatalogoOrden.CostoPromedio, true) =>
                query.ThenByDescending(x => x.CostoPromedio)
                    .ThenByDescending(x => x.Nombre),
            (ProductoCatalogoOrden.PrecioBase, false) =>
                query.ThenBy(x => x.PrecioBase).ThenBy(x => x.Nombre),
            (ProductoCatalogoOrden.PrecioBase, true) =>
                query.ThenByDescending(x => x.PrecioBase)
                    .ThenByDescending(x => x.Nombre),
            (ProductoCatalogoOrden.Estado, false) =>
                query.ThenBy(x => x.Estado).ThenBy(x => x.Nombre),
            (ProductoCatalogoOrden.Estado, true) =>
                query.ThenByDescending(x => x.Estado)
                    .ThenByDescending(x => x.Nombre),
            (ProductoCatalogoOrden.Producto, true) =>
                query.ThenByDescending(x => x.Nombre),
            _ => query.ThenBy(x => x.Nombre)
        };

    private static ProductoListadoDto MapearListado(
        ProductoCatalogoProjection x) =>
        new()
        {
            Id = x.Id,
            Codigo = x.Codigo,
            Nombre = x.Nombre,
            Modelo = x.Modelo,
            Categoria = x.Categoria,
            Marca = x.Marca,
            UnidadBase = x.UnidadBase,
            TarifaImpuesto = x.TarifaImpuesto,
            StockDisponible = x.StockDisponible,
            StockMinimo = x.StockMinimo,
            TieneStockBajo = x.TieneStockBajo,
            SinStock = x.SinStock,
            CostoPromedio = x.CostoPromedio,
            PrecioBase = x.PrecioBase,
            ListaPrecioBaseCodigo = x.ListaPrecioBaseCodigo,
            CantidadPorCaducar = x.CantidadPorCaducar,
            PorCaducar = x.PorCaducar,
            ProximaCaducidad = x.ProximaCaducidad,
            Estado = x.Estado
        };

    private static decimal? CalcularPrecioBaseLista(
        PrecioBaseListaCatalogoProjection configuracion,
        decimal? precioListaBase) =>
        configuracion.MetodoCalculo switch
        {
            "PRECIO_FIJO" => configuracion.Precio,
            "PORCENTAJE_COSTO" when configuracion.Porcentaje.HasValue =>
                ProductoNuevoRules.CalcularPrecioPorcentajeCosto(
                    configuracion.CostoPromedio *
                    configuracion.FactorConversion,
                    configuracion.Porcentaje.Value),
            "DESCUENTO_PORCENTAJE" when
                configuracion.Porcentaje.HasValue &&
                precioListaBase.HasValue =>
                ProductoNuevoRules.CalcularPrecioConDescuento(
                    precioListaBase.Value,
                    configuracion.Porcentaje.Value),
            _ => null
        };

    private sealed class ProductoCatalogoProjection
    {
        public long Id { get; init; }
        public string Codigo { get; init; } = string.Empty;
        public string Nombre { get; init; } = string.Empty;
        public string? Modelo { get; init; }
        public string? Categoria { get; init; }
        public string? Marca { get; init; }
        public string UnidadBase { get; init; } = string.Empty;
        public string TarifaImpuesto { get; init; } = string.Empty;
        public decimal StockDisponible { get; init; }
        public decimal StockMinimo { get; init; }
        public bool TieneStockBajo { get; init; }
        public bool SinStock { get; init; }
        public decimal CostoPromedio { get; init; }
        public decimal? PrecioBase { get; init; }
        public string ListaPrecioBaseCodigo { get; init; } = string.Empty;
        public decimal CantidadPorCaducar { get; init; }
        public bool PorCaducar { get; init; }
        public DateOnly? ProximaCaducidad { get; init; }
        public short Estado { get; init; }
        public bool CoincidenciaExactaCodigoBarras { get; init; }
    }

    private sealed class PresentacionComercialCatalogoProjection
    {
        public long ProductoId { get; init; }
        public string Nombre { get; init; } = string.Empty;
    }

    private sealed class PrecioBaseListaCatalogoProjection
    {
        public long ProductoId { get; init; }
        public string ListaCodigo { get; init; } = string.Empty;
        public int Orden { get; init; }
        public bool EsListaBase { get; init; }
        public decimal FactorConversion { get; init; }
        public decimal CostoPromedio { get; init; }
        public string? MetodoCalculo { get; init; }
        public decimal? Porcentaje { get; init; }
        public decimal? Precio { get; init; }
    }

    private static string? Validate(ProductoGuardarRequest request)
    {
        if (request.EmpresaId <= 0) return "La empresa no es válida.";
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return "Debe ingresar el nombre.";
        if (string.IsNullOrWhiteSpace(request.PresentacionNombre))
            return "Debe ingresar la presentación base.";
        if (request.UnidadMedidaBaseId <= 0)
            return "Debe seleccionar la unidad de medida.";
        if (request.TarifasImpuestoIds.Count == 0 ||
            request.TarifasImpuestoIds.Any(x => x <= 0))
            return "Debe seleccionar el IVA / impuesto.";
        if (request.TipoProducto is not ("PRODUCTO" or "SERVICIO"))
            return "El tipo de producto no es válido.";
        if (request.TipoProducto == "SERVICIO" && request.ManejaInventario)
            return "Un servicio no puede manejar inventario.";
        var controlValido = request.TipoControlInventario switch
        {
            "NORMAL" => !request.ManejaLotes && !request.ManejaSeries,
            "LOTE" => request.ManejaLotes && !request.ManejaSeries,
            "SERIE" => !request.ManejaLotes && request.ManejaSeries,
            "LOTE_Y_SERIE" => request.ManejaLotes && request.ManejaSeries,
            _ => false
        };
        if (!controlValido)
            return "El tipo de control de inventario no es consistente.";
        if (!request.ManejaInventario &&
            (request.ManejaLotes || request.ManejaSeries ||
             request.ManejaFechaCaducidad))
            return "El control de lotes, series o caducidad requiere inventario.";
        if (request.ManejaFechaCaducidad && !request.ManejaLotes)
            return "La caducidad requiere manejo de lotes.";
        if (request.ManejaFechaCaducidad &&
            request.DiasAlertaCaducidad <= 0)
            return "Los días de anticipación deben ser mayores que cero.";
        if (request.Presentaciones.Count(x => x.EsPresentacionBase) != 1)
            return "Debe existir exactamente una presentación BASE.";
        var presentacionesActivas = request.Presentaciones
            .Where(x => x.Estado == 1)
            .ToList();
        if (presentacionesActivas.Select(x => x.Codigo.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
            presentacionesActivas.Count)
            return "Los códigos de presentación no pueden repetirse.";
        var codigosBarras = presentacionesActivas
            .Select(x => x.CodigoBarras?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToList();
        if (codigosBarras.Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
            codigosBarras.Count)
            return "Los códigos de barras no pueden repetirse.";
        foreach (var presentation in request.Presentaciones)
        {
            if (!presentation.EsPresentacionBase &&
                (string.IsNullOrWhiteSpace(presentation.Codigo) ||
                string.IsNullOrWhiteSpace(presentation.Nombre))
               )
                return "Cada presentación requiere código y nombre.";
            if (!presentation.EsPresentacionBase &&
                (presentation.Codigo.Trim().Length < 3 ||
                 char.ToUpperInvariant(presentation.Codigo.Trim()[0]) != 'P' ||
                 !int.TryParse(
                     presentation.Codigo.Trim()[1..],
                     out var numeroPresentacion) ||
                 numeroPresentacion <= 0))
                return "Las presentaciones adicionales deben usar códigos P01, P02, etc.";
            if (presentation.FactorConversion <= 0)
                return "El factor de conversión debe ser mayor que cero.";
            if (presentation.EsPresentacionBase &&
                presentation.FactorConversion != 1)
                return "La presentación BASE debe conservar factor 1.";
            if (!presentation.PermiteCompra || !presentation.PermiteVenta)
                return "Todas las presentaciones deben permitir compra y venta.";
            foreach (var price in presentation.Precios)
            {
                var porcentaje = price.MetodoCalculo
                    is "PORCENTAJE_COSTO" or "DESCUENTO_PORCENTAJE";
                var fijo = price.MetodoCalculo == "PRECIO_FIJO";
                if ((!porcentaje && !fijo) ||
                    (porcentaje && (price.Porcentaje is null or < 0 ||
                                    price.Precio is not null)) ||
                    (price.MetodoCalculo == "DESCUENTO_PORCENTAJE" &&
                     price.Porcentaje >= 100) ||
                    (fijo && (price.Precio is null or <= 0 ||
                              price.Porcentaje is not null)))
                    return "La configuración de un precio no es válida.";
                if (price.Porcentaje.HasValue &&
                    decimal.Round(price.Porcentaje.Value, 6) !=
                    price.Porcentaje.Value)
                    return "Los porcentajes admiten hasta 6 decimales.";
                if (price.Precio.HasValue &&
                    decimal.Round(price.Precio.Value, 6) !=
                    price.Precio.Value)
                    return "Los precios admiten hasta 6 decimales.";
            }
        }
        if (request.Existencias.Any(x => x.StockMinimo < 0))
            return "El stock mínimo no puede ser negativo.";
        if (request.InventariosIniciales.Count > 0)
        {
            if (!request.ManejaInventario)
                return "El inventario inicial requiere control de inventario.";
            foreach (var inicial in request.InventariosIniciales)
            {
                if (inicial.BodegaId <= 0 || inicial.UsuarioId <= 0)
                    return "Cada ingreso inicial requiere una bodega.";
                if (inicial.CantidadPresentaciones <= 0)
                    return "La cantidad inicial debe ser mayor que cero.";
                if (decimal.Round(inicial.CantidadPresentaciones, 2) !=
                    inicial.CantidadPresentaciones)
                    return "La cantidad inicial admite hasta 2 decimales.";
                if (inicial.CostoUnitarioPresentacion <= 0)
                    return "El costo inicial debe ser mayor que cero.";
                if (inicial.StockMinimo < 0)
                    return "El stock mínimo no puede ser negativo.";
                if (decimal.Round(inicial.CostoUnitarioPresentacion, 6) !=
                    inicial.CostoUnitarioPresentacion)
                    return "El costo unitario admite hasta 6 decimales.";
                var presentacion = request.Presentaciones.SingleOrDefault(
                    x => x.Codigo == inicial.PresentacionCodigo &&
                         x.Estado == 1);
                if (presentacion is null)
                    return "Una presentación del inventario inicial no es válida.";
                var cantidadBase =
                    inicial.CantidadPresentaciones *
                    presentacion.FactorConversion;
                if (request.ManejaLotes)
                {
                    if (inicial.Lotes.Count == 0 ||
                        inicial.Lotes.Any(x =>
                            string.IsNullOrWhiteSpace(x.NumeroLote) ||
                            x.CantidadBase <= 0) ||
                        inicial.Lotes.Sum(x => x.CantidadBase) != cantidadBase)
                        return "Los lotes deben distribuir exactamente la cantidad base.";
                    if (request.ManejaFechaCaducidad &&
                        inicial.Lotes.Any(x => !x.FechaCaducidad.HasValue))
                        return "La caducidad es obligatoria para todos los lotes.";
                    var hoy = DateTime.Today;
                    if (inicial.Lotes.Any(x =>
                            x.FechaElaboracion.HasValue &&
                            x.FechaElaboracion.Value.Date > hoy))
                        return "La fecha de elaboración no puede ser posterior a hoy.";
                    if (inicial.Lotes.Any(x =>
                            x.FechaCaducidad.HasValue &&
                            x.FechaCaducidad.Value.Date <= hoy))
                        return "La fecha de caducidad debe ser posterior a hoy.";
                    if (inicial.Lotes.Any(x =>
                            x.FechaElaboracion.HasValue &&
                            x.FechaCaducidad.HasValue &&
                            x.FechaElaboracion.Value.Date >
                            x.FechaCaducidad.Value.Date))
                        return "La fecha de elaboración no puede ser posterior a la caducidad.";
                }
                if (request.ManejaSeries)
                {
                    if (cantidadBase != decimal.Truncate(cantidadBase) ||
                        inicial.NumerosSerie.Count != (int)cantidadBase)
                        return "Debe existir una serie por cada unidad base.";
                    var series = inicial.NumerosSerie.Select(x =>
                    {
                        var partes = x.Split('|', 2,
                            StringSplitOptions.TrimEntries);
                        return new
                        {
                            Lote = partes.Length == 2 ? partes[0] : null,
                            Serie = partes.Length == 2 ? partes[1] : partes[0]
                        };
                    }).ToList();
                    if (series.Any(x => string.IsNullOrWhiteSpace(x.Serie)) ||
                        series.Select(x => x.Serie)
                            .Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
                        series.Count)
                        return "Las series deben estar completas y no repetirse.";
                    if (request.ManejaLotes &&
                        inicial.Lotes.Any(lote =>
                            lote.CantidadBase !=
                            series.Count(serie => string.Equals(
                                serie.Lote, lote.NumeroLote,
                                StringComparison.OrdinalIgnoreCase))))
                        return "Las series por lote deben coincidir con la cantidad de cada lote.";
                }
            }
            foreach (var grupo in request.InventariosIniciales
                         .GroupBy(x => x.BodegaId))
            {
                var configuracion = grupo.First();
                if (grupo.Any(x =>
                        x.StockMinimo != configuracion.StockMinimo ||
                        !string.Equals(
                            NormalizeUpper(x.Ubicacion),
                            NormalizeUpper(configuracion.Ubicacion),
                            StringComparison.Ordinal)))
                    return "La ubicación y el stock mínimo deben ser consistentes para cada bodega.";
            }
            if (!request.Id.HasValue && request.InventariosIniciales
                .GroupBy(
                    x => new
                    {
                        x.BodegaId,
                        Presentacion = x.PresentacionCodigo.ToUpperInvariant()
                    })
                .Any(x => x.Count() > 1))
                return "Una presentación solo puede tener una entrada inicial por bodega.";
            var lotes = request.InventariosIniciales
                .SelectMany(x => x.Lotes)
                .GroupBy(x => x.NumeroLote.Trim(),
                    StringComparer.OrdinalIgnoreCase);
            foreach (var grupo in lotes)
            {
                var referencia = grupo.First();
                if (grupo.Any(x =>
                        x.FechaElaboracion?.Date !=
                            referencia.FechaElaboracion?.Date ||
                        x.FechaCaducidad?.Date !=
                            referencia.FechaCaducidad?.Date))
                    return $"El lote '{grupo.Key}' tiene fechas contradictorias.";
            }
            var todasLasSeries = request.InventariosIniciales
                .SelectMany(x => x.NumerosSerie)
                .Select(x => x.Split('|', 2,
                    StringSplitOptions.TrimEntries)[^1])
                .ToList();
            if (todasLasSeries.Distinct(
                    StringComparer.OrdinalIgnoreCase).Count() !=
                todasLasSeries.Count)
                return "Las series no pueden repetirse entre entradas iniciales.";
        }
        return null;
    }

    private static string ObtenerTipoControl(bool manejaLotes, bool manejaSeries) =>
        (manejaLotes, manejaSeries) switch
        {
            (true, true) => "LOTE_Y_SERIE",
            (true, false) => "LOTE",
            (false, true) => "SERIE",
            _ => "NORMAL"
        };

    private static async Task<bool> PresentacionTieneUsoAsync(
        KontaxDbContext context,
        long presentacionId,
        CancellationToken cancellationToken) =>
        await context.MovimientosInventarioDetalles.AnyAsync(
            x => x.ProductoPresentacionId == presentacionId, cancellationToken) ||
        await context.TransferenciasInventarioDetalles.AnyAsync(
            x => x.ProductoPresentacionId == presentacionId, cancellationToken) ||
        await context.AjustesInventarioDetalles.AnyAsync(
            x => x.ProductoPresentacionId == presentacionId, cancellationToken) ||
        await context.VentasXfDetalles.AnyAsync(
            x => x.ProductoPresentacionId == presentacionId, cancellationToken) ||
        await context.NotasEntregaDetalles.AnyAsync(
            x => x.ProductoPresentacionId == presentacionId, cancellationToken) ||
        await context.FacturasDetalles.AnyAsync(
            x => x.ProductoPresentacionId == presentacionId, cancellationToken) ||
        await context.ProformasDetalles.AnyAsync(
            x => x.ProductoPresentacionId == presentacionId, cancellationToken) ||
        await context.ComprasDetalles.AnyAsync(
            x => x.ProductoPresentacionId == presentacionId, cancellationToken) ||
        await context.LiquidacionesCompraDetalles.AnyAsync(
            x => x.ProductoPresentacionId == presentacionId, cancellationToken) ||
        await context.DevolucionesVentasDetalles.AnyAsync(
            x => x.ProductoPresentacionId == presentacionId, cancellationToken) ||
        await context.DevolucionesComprasDetalles.AnyAsync(
            x => x.ProductoPresentacionId == presentacionId, cancellationToken);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeUpper(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToUpperInvariant();
}
