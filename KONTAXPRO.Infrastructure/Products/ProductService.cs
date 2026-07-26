using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Productos;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KONTAXPRO.Infrastructure.Products;

public sealed class ProductService(
    IDbContextFactory<KontaxDbContext> dbContextFactory) : IProductService
{
    public async Task<List<ProductoListadoDto>> ObtenerProductosAsync(
        long empresaId,
        string? busqueda = null,
        long? categoriaId = null,
        short? estado = 1,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Productos.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var text = busqueda.Trim().ToLower();
            query = query.Where(x =>
                x.Codigo.ToLower().Contains(text) ||
                x.Nombre.ToLower().Contains(text) ||
                (x.Modelo != null && x.Modelo.ToLower().Contains(text)) ||
                x.Presentaciones.Any(p =>
                    p.CodigoBarras != null &&
                    p.CodigoBarras.ToLower().Contains(text)));
        }

        if (categoriaId.HasValue)
            query = query.Where(x => x.CategoriaProductoId == categoriaId);

        if (estado.HasValue)
            query = query.Where(x => x.Estado == estado);

        return await query
            .OrderBy(x => x.Nombre)
            .Select(x => new ProductoListadoDto
            {
                Id = x.Id,
                Codigo = x.Codigo,
                Nombre = x.Nombre,
                Categoria = x.CategoriaProducto == null
                    ? null
                    : x.CategoriaProducto.Nombre,
                Marca = x.Marca == null ? null : x.Marca.Nombre,
                UnidadBase = x.UnidadMedidaBase!.Abreviatura,
                TarifaImpuesto = x.Impuestos
                    .Where(i => i.Estado == 1)
                    .Select(i => i.TarifaImpuesto!.Nombre)
                    .FirstOrDefault() ?? string.Empty,
                StockActual = x.Existencias.Sum(e => e.StockActual),
                StockMinimo = x.Existencias.Sum(e => e.StockMinimo),
                CostoPromedio = x.Costo == null ? 0 : x.Costo.CostoPromedio,
                PrecioPrincipal = x.Presentaciones
                    .Where(p => p.EsPresentacionBase)
                    .SelectMany(p => p.Precios)
                    .Where(p => p.ListaPrecio!.EsListaBase &&
                                p.MetodoCalculo == "PRECIO_FIJO")
                    .Select(p => p.Precio ?? 0)
                    .FirstOrDefault(),
                DiasAlertaCaducidad = x.DiasAlertaCaducidad ?? 0,
                ProximaCaducidad = x.Lotes
                    .Where(l => l.FechaCaducidad != null &&
                                l.Estado == 1)
                    .Min(l => l.FechaCaducidad),
                PorCaducar = x.AlertaCaducidad &&
                    x.Lotes.Any(l => l.FechaCaducidad != null),
                Estado = (short)x.Estado
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductoDetalleDto?> ObtenerProductoAsync(
        long productoId,
        long empresaId,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Productos.AsNoTracking()
            .Where(x => x.Id == productoId && x.EmpresaId == empresaId)
            .Select(x => new ProductoDetalleDto
            {
                Id = x.Id,
                EmpresaId = x.EmpresaId,
                CategoriaProductoId = x.CategoriaProductoId,
                MarcaId = x.MarcaId,
                UnidadMedidaBaseId = x.UnidadMedidaBaseId,
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
                    BodegaNombre = e.Bodega!.Nombre,
                    StockActual = e.StockActual,
                    StockReservado = e.StockReservado,
                    StockMinimo = e.StockMinimo,
                    Ubicacion = e.Ubicacion
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
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
            if (tarifasIds.Count != 0 &&
                await context.TarifasImpuesto.CountAsync(
                    x => tarifasIds.Contains(x.Id) && x.Estado == 1,
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

            var bodegaIds = request.Existencias
                .Select(x => x.BodegaId)
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
            }

            producto.CategoriaProductoId = request.CategoriaProductoId;
            producto.MarcaId = request.MarcaId;
            producto.UnidadMedidaBaseId = request.UnidadMedidaBaseId;
            producto.Nombre = request.Nombre.Trim();
            producto.Descripcion = Normalize(request.Descripcion);
            producto.Modelo = Normalize(request.Modelo);
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

            basePresentation.Nombre = request.PresentacionNombre.Trim();
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
                presentation.Codigo = input.Codigo.Trim();
                presentation.CodigoBarras = Normalize(input.CodigoBarras);
                presentation.Nombre = input.Nombre.Trim();
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
                existence.Ubicacion = Normalize(input.Ubicacion);
                existence.UpdatedAt = DateTime.UtcNow;
            }

            if (producto.Costo is null)
            {
                producto.Costo = new ProductoCosto
                {
                    Producto = producto,
                    UltimoPrecioCompra = 0,
                    UltimoCostoEfectivo = 0,
                    CostoPromedio = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
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
        string codigoBarras,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codigoBarras))
            return null;

        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var code = codigoBarras.Trim();
        return await context.ProductosPresentaciones.AsNoTracking()
            .Where(x => x.CodigoBarras == code && x.Estado == 1)
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

    private static string? Validate(ProductoGuardarRequest request)
    {
        if (request.EmpresaId <= 0) return "La empresa no es válida.";
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return "Debe ingresar el nombre.";
        if (string.IsNullOrWhiteSpace(request.PresentacionNombre))
            return "Debe ingresar la presentación base.";
        if (request.UnidadMedidaBaseId <= 0)
            return "Debe seleccionar la unidad de medida.";
        if (!request.SinCodigoBarras &&
            string.IsNullOrWhiteSpace(request.CodigoBarras))
            return "Debe ingresar el código de barras.";
        if (request.TipoProducto is not ("PRODUCTO" or "SERVICIO"))
            return "El tipo de producto no es válido.";
        if (request.TipoProducto == "SERVICIO" && request.ManejaInventario)
            return "Un servicio no puede manejar inventario.";
        if (!request.ManejaInventario &&
            (request.ManejaLotes || request.ManejaSeries ||
             request.ManejaFechaCaducidad))
            return "El control de lotes, series o caducidad requiere inventario.";
        if (request.ManejaFechaCaducidad && !request.ManejaLotes)
            return "La caducidad requiere manejo de lotes.";
        if (request.DiasAlertaCaducidad < 0)
            return "Los días de alerta no pueden ser negativos.";
        if (request.Presentaciones.Count(x => x.EsPresentacionBase) > 1)
            return "Solo puede existir una presentación BASE.";
        foreach (var presentation in request.Presentaciones
                     .Where(x => !x.EsPresentacionBase))
        {
            if (string.IsNullOrWhiteSpace(presentation.Codigo) ||
                string.IsNullOrWhiteSpace(presentation.Nombre))
                return "Cada presentación requiere código y nombre.";
            if (presentation.FactorConversion <= 0)
                return "El factor de conversión debe ser mayor que cero.";
            foreach (var price in presentation.Precios)
            {
                var porcentaje = price.MetodoCalculo
                    is "PORCENTAJE_COSTO" or "DESCUENTO_PORCENTAJE";
                var fijo = price.MetodoCalculo == "PRECIO_FIJO";
                if ((!porcentaje && !fijo) ||
                    (porcentaje && (price.Porcentaje is null or < 0 ||
                                    price.Precio is not null)) ||
                    (fijo && (price.Precio is null or < 0 ||
                              price.Porcentaje is not null)))
                    return "La configuración de un precio no es válida.";
            }
        }
        if (request.Existencias.Any(x => x.StockMinimo < 0))
            return "El stock mínimo no puede ser negativo.";
        return null;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
