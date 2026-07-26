using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Productos;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KONTAXPRO.Infrastructure.Products;

public class ProductService : IProductService
{
    private readonly IDbContextFactory<KontaxDbContext> _dbContextFactory;

    public ProductService(
        IDbContextFactory<KontaxDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<ProductoListadoDto>> ObtenerProductosAsync(
        long empresaId,
        string? busqueda = null,
        long? categoriaId = null,
        short? estado = 1,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var query = context.Productos
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId);

        if (estado.HasValue)
        {
            query = query.Where(x => x.Estado == estado.Value);
        }

        if (categoriaId.HasValue)
        {
            query = query.Where(
                x => x.CategoriaProductoId == categoriaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim().ToLower();

            query = query.Where(x =>
                x.Codigo.ToLower().Contains(texto) ||
                x.Nombre.ToLower().Contains(texto) ||
                x.Presentaciones.Any(p =>
                    p.CodigoBarras != null &&
                    p.CodigoBarras.ToLower().Contains(texto)));
        }

        var productos = await query
            .OrderBy(x => x.Nombre)
            .Select(x => new ProductoListadoDto
            {
                Id = x.Id,

                Codigo = x.Codigo,

                Nombre = x.Nombre,

                Categoria = x.CategoriaProducto != null
                    ? x.CategoriaProducto.Nombre
                    : null,

                Marca = x.Marca != null
                    ? x.Marca.Nombre
                    : null,

                UnidadBase = x.UnidadMedidaBase != null
                    ? x.UnidadMedidaBase.Nombre
                    : string.Empty,

                TarifaImpuesto = x.TarifaImpuesto != null
                    ? x.TarifaImpuesto.Nombre
                    : string.Empty,

                StockActual =
                    x.Existencias.Sum(e => e.StockActual),

                StockMinimo =
                    x.StockMinimo,

                CostoPromedio = x.Costo != null
                    ? x.Costo.CostoPromedio
                    : 0,

                PrecioPrincipal = x.Presentaciones
                    .Where(p => p.EsPresentacionBase)
                    .SelectMany(p => p.Precios)
                    .Where(p =>
                        p.Estado == 1 &&
                        p.ListaPrecio != null &&
                        p.ListaPrecio.EsPredeterminada)
                    .Select(p => p.Precio)
                    .FirstOrDefault(),

                DiasAlertaCaducidad =
                    x.DiasAlertaCaducidad,

                PorCaducar = false,

                ProximaCaducidad = null,

                Estado = x.Estado
            })
            .ToListAsync(cancellationToken);

        /*
         * =========================================================
         * PRÓXIMAS CADUCIDADES
         * =========================================================
         *
         * Solo consultamos lotes correspondientes a los productos
         * que ya fueron recuperados en el listado.
         *
         * Además:
         * - el lote debe estar activo;
         * - debe tener fecha de caducidad;
         * - debe mantener stock en al menos una bodega.
         */

        if (productos.Count > 0)
        {
            var productosIds = productos
                .Select(x => x.Id)
                .ToList();

            var lotes = await context.ProductosLotes
                .AsNoTracking()
                .Where(x =>
                    productosIds.Contains(x.ProductoId) &&
                    x.Estado == 1 &&
                    x.FechaCaducidad != null &&
                    x.Existencias.Any(e => e.StockActual > 0))
                .Select(x => new
                {
                    x.ProductoId,

                    FechaCaducidad =
                        x.FechaCaducidad!.Value
                })
                .ToListAsync(cancellationToken);

            var hoy =
                DateOnly.FromDateTime(DateTime.Today);

            foreach (var producto in productos)
            {
                /*
                 * Buscamos únicamente lotes que aún no hayan
                 * caducado.
                 *
                 * Los lotes ya vencidos los podremos manejar
                 * posteriormente como otro indicador independiente.
                 */

                var fechasProducto = lotes
                    .Where(x =>
                        x.ProductoId == producto.Id &&
                        x.FechaCaducidad >= hoy)
                    .Select(x => x.FechaCaducidad)
                    .OrderBy(x => x)
                    .ToList();

                if (fechasProducto.Count == 0)
                {
                    continue;
                }

                /*
                 * La primera fecha ordenada es la próxima fecha
                 * de caducidad disponible de ese producto.
                 */

                producto.ProximaCaducidad =
                    fechasProducto[0];

                /*
                 * Ejemplo:
                 *
                 * Hoy: 2026-07-22
                 * Días alerta: 30
                 *
                 * Fecha límite:
                 * 2026-08-21
                 *
                 * Si el lote caduca antes o ese mismo día,
                 * se considera "Por caducar".
                 */

                var fechaLimite =
                    hoy.AddDays(
                        producto.DiasAlertaCaducidad);

                producto.PorCaducar =
                    producto.ProximaCaducidad.Value
                    <= fechaLimite;
            }
        }

        return productos;
    }

    public async Task<ProductoDetalleDto?> ObtenerProductoAsync(
    long productoId,
    long empresaId,
    CancellationToken cancellationToken = default)
    {
        await using var context =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await context.Productos
            .AsNoTracking()
            .Where(x =>
                x.Id == productoId &&
                x.EmpresaId == empresaId)
            .Select(x => new ProductoDetalleDto
            {
                Id = x.Id,

                EmpresaId = x.EmpresaId,

                CategoriaProductoId =
                    x.CategoriaProductoId,

                MarcaId =
                    x.MarcaId,

                UnidadMedidaBaseId =
                    x.UnidadMedidaBaseId,

                TarifaImpuestoId =
                    x.TarifaImpuestoId,

                Codigo =
                    x.Codigo,

                Nombre =
                    x.Nombre,

                Modelo =
                    x.Modelo,

                Descripcion =
                    x.Descripcion,

                /*
                 * Presentación base del producto.
                 */
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
                    .Select(p => p.CodigoBarrasInterno)
                    .FirstOrDefault(),

                TipoProducto =
                    x.TipoProducto,

                TipoControlInventario =
                    x.TipoControlInventario,

                ManejaInventario =
                    x.ManejaInventario,

                PermiteVentaSinStock =
                    x.PermiteVentaSinStock,

                ManejaLotes =
                    x.ManejaLotes,

                ManejaSeries =
                    x.ManejaSeries,

                ManejaFechaCaducidad =
                    x.ManejaFechaCaducidad,

                AlertaStockMinimo =
                    x.AlertaStockMinimo,

                AlertaCaducidad =
                    x.AlertaCaducidad,

                StockMinimo =
                    x.StockMinimo,

                DiasAlertaCaducidad =
                    x.DiasAlertaCaducidad,

                Observacion =
                    x.Observacion,

                Estado =
                    x.Estado
            })
            .FirstOrDefaultAsync(
                cancellationToken);
    }

    public async Task<ProductOperationResult> GuardarProductoAsync(
    ProductoGuardarRequest request,
    CancellationToken cancellationToken = default)
    {
        var validationResult =
            ValidateRequest(request);

        if (validationResult is not null)
        {
            return ProductOperationResult.Fail(
                validationResult);
        }

        await using var context =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var nombre =
            request.Nombre.Trim();


        /*
         * ============================================================
         * VALIDAR CÓDIGO DE BARRAS
         * ============================================================
         *
         * Si el usuario indicó un código comercial,
         * verificamos que ninguna otra presentación lo utilice.
         *
         * Al editar excluimos las presentaciones del mismo producto.
         */

        if (!request.SinCodigoBarras)
        {
            var codigoBarras =
                request.CodigoBarras!.Trim();


            var codigoBarrasExiste =
                await context.ProductosPresentaciones
                    .AsNoTracking()
                    .AnyAsync(
                        x =>
                            x.CodigoBarras == codigoBarras &&
                            (
                                !request.Id.HasValue ||
                                x.ProductoId != request.Id.Value
                            ),
                        cancellationToken);


            if (codigoBarrasExiste)
            {
                return ProductOperationResult.Fail(
                    $"El código de barras '{codigoBarras}' ya está registrado en otra presentación.");
            }
        }


        /*
         * ============================================================
         * VALIDAR CATÁLOGOS
         * ============================================================
         */

        var referenciasValidas =
            await ValidarCatalogosAsync(
                context,
                request,
                cancellationToken);

        if (referenciasValidas is not null)
        {
            return ProductOperationResult.Fail(
                referenciasValidas);
        }


        await using var transaction =
            await context.Database
                .BeginTransactionAsync(
                    cancellationToken);


        try
        {
            Producto producto;

            ProductoPresentacion? presentacionBase;

            var esNuevo =
                !request.Id.HasValue;


            /*
             * ========================================================
             * PRODUCTO NUEVO
             * ========================================================
             */

            if (esNuevo)
            {
                producto =
                    new Producto
                    {
                        EmpresaId =
                            request.EmpresaId,

                        Codigo =
                            await GenerarCodigoProductoAsync(
                                context,
                                cancellationToken),

                        CreatedAt =
                            DateTime.Now,

                        Estado =
                            1
                    };


                context.Productos.Add(
                    producto);


                presentacionBase = null;
            }

            /*
             * ========================================================
             * EDICIÓN
             * ========================================================
             */

            else
            {
                var productoId =
                    request.Id
                    ?? throw new InvalidOperationException(
                        "El identificador del producto no es válido.");


                producto =
                    await context.Productos
                        .Include(x =>
                            x.Presentaciones)
                        .FirstOrDefaultAsync(
                            x =>
                                x.Id == productoId &&
                                x.EmpresaId ==
                                request.EmpresaId,
                            cancellationToken)
                    ?? throw new InvalidOperationException(
                        "El producto no existe.");


                presentacionBase =
                    producto.Presentaciones
                        .FirstOrDefault(
                            x => x.EsPresentacionBase);


                if (presentacionBase is null)
                {
                    throw new InvalidOperationException(
                        "El producto no tiene configurada una presentación base.");
                }


                producto.UpdatedAt =
                    DateTime.Now;
            }


            /*
             * ========================================================
             * DATOS GENERALES
             * ========================================================
             */

            producto.CategoriaProductoId =
                request.CategoriaProductoId;

            producto.MarcaId =
                request.MarcaId;

            producto.UnidadMedidaBaseId =
                request.UnidadMedidaBaseId;

            producto.TarifaImpuestoId =
                request.TarifaImpuestoId;

            producto.Nombre =
                nombre;

            producto.Modelo =
                request.Modelo?.Trim();

            producto.Descripcion =
                request.Descripcion?.Trim();

            producto.TipoProducto =
                request.TipoProducto;

            producto.TipoControlInventario =
                request.TipoControlInventario;

            producto.ManejaInventario =
                request.ManejaInventario;

            producto.PermiteVentaSinStock =
                request.PermiteVentaSinStock;

            producto.ManejaLotes =
                request.ManejaLotes;

            producto.ManejaSeries =
                request.ManejaSeries;

            producto.ManejaFechaCaducidad =
                request.ManejaFechaCaducidad;

            producto.AlertaStockMinimo =
                request.AlertaStockMinimo;

            producto.AlertaCaducidad =
                request.AlertaCaducidad;

            producto.StockMinimo =
                request.StockMinimo;

            producto.DiasAlertaCaducidad =
                request.DiasAlertaCaducidad;

            producto.Observacion =
                request.Observacion?.Trim();


            /*
             * ========================================================
             * DETERMINAR CÓDIGO DE BARRAS
             * ========================================================
             */

            string codigoBarras;
            bool codigoBarrasInterno;


            if (request.SinCodigoBarras)
            {
                /*
                 * Si estamos editando un producto cuyo código ya fue
                 * generado por KONTAXPRO, conservamos el mismo.
                 *
                 * No debemos generar un código nuevo en cada edición.
                 */

                if (!esNuevo &&
                    presentacionBase != null &&
                    presentacionBase.CodigoBarrasInterno &&
                    !string.IsNullOrWhiteSpace(
                        presentacionBase.CodigoBarras))
                {
                    codigoBarras =
                        presentacionBase.CodigoBarras;

                    codigoBarrasInterno =
                        true;
                }
                else
                {
                    codigoBarras =
                        await GenerarCodigoBarrasInternoAsync(
                            context,
                            cancellationToken);

                    codigoBarrasInterno =
                        true;
                }
            }
            else
            {
                codigoBarras =
                    request.CodigoBarras!.Trim();

                codigoBarrasInterno =
                    false;
            }


            /*
             * ========================================================
             * PRODUCTO NUEVO
             * ========================================================
             */

            if (esNuevo)
            {
                var nuevaPresentacionBase =
                    new ProductoPresentacion
                    {
                        Producto =
                            producto,

                        UnidadMedidaId =
                            request.UnidadMedidaBaseId,

                        Codigo =
                            "BASE",

                        CodigoBarras =
                            codigoBarras,

                        CodigoBarrasInterno =
                            codigoBarrasInterno,

                        Nombre =
                            request.PresentacionNombre.Trim(),

                        FactorConversion =
                            1,

                        EsPresentacionBase =
                            true,

                        PermiteCompra =
                            true,

                        PermiteVenta =
                            true,

                        Estado =
                            1,

                        CreatedAt =
                            DateTime.Now
                    };


                producto.Presentaciones.Add(
                    nuevaPresentacionBase);


                /*
                 * Registro inicial de costos.
                 */

                producto.Costo =
                    new ProductoCosto
                    {
                        UltimoPrecioCompra =
                            0,

                        UltimoCostoEfectivo =
                            0,

                        CostoPromedio =
                            0,

                        CostoMaximoExistencia =
                            0,

                        CreatedAt =
                            DateTime.Now
                    };
            }

            /*
             * ========================================================
             * ACTUALIZAR PRESENTACIÓN BASE
             * ========================================================
             */

            else if (presentacionBase != null)
            {
                presentacionBase.UnidadMedidaId =
                    request.UnidadMedidaBaseId;

                presentacionBase.Nombre =
                    request.PresentacionNombre.Trim();

                presentacionBase.CodigoBarras =
                    codigoBarras;

                presentacionBase.CodigoBarrasInterno =
                    codigoBarrasInterno;

                presentacionBase.UpdatedAt =
                    DateTime.Now;
            }


            /*
             * ========================================================
             * GUARDAR
             * ========================================================
             */

            await context.SaveChangesAsync(
                cancellationToken);


            await transaction.CommitAsync(
                cancellationToken);


            return ProductOperationResult.Ok(
                producto.Id,
                esNuevo
                    ? "Producto creado correctamente."
                    : "Producto actualizado correctamente.");
        }

        /*
         * ============================================================
         * VIOLACIÓN DE UNICIDAD
         * ============================================================
         */

        catch (DbUpdateException ex)
            when (
                ex.InnerException
                    is PostgresException postgresException
                &&
                postgresException.SqlState ==
                    PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(
                cancellationToken);


            return ProductOperationResult.Fail(
                "No fue posible guardar porque el código interno o el código de barras ya está siendo utilizado.");
        }


        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync(
                cancellationToken);


            return ProductOperationResult.Fail(
                ex.Message);
        }


        catch (Exception ex)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            var detalle =
                ex.InnerException?.Message
                ?? ex.Message;

            return ProductOperationResult.Fail(
                $"Error al guardar producto: {detalle}");
        }
    }

    public async Task<ProductOperationResult> CambiarEstadoAsync(
        long productoId,
        long empresaId,
        short nuevoEstado,
        CancellationToken cancellationToken = default)
    {
        if (nuevoEstado is not 0 and not 1)
        {
            return ProductOperationResult.Fail(
                "El estado solicitado no es válido.");
        }

        await using var context =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var producto =
            await context.Productos
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == productoId &&
                        x.EmpresaId == empresaId,
                    cancellationToken);

        if (producto is null)
        {
            return ProductOperationResult.Fail(
                "No se encontró el producto.");
        }

        producto.Estado =
            nuevoEstado;

        producto.UpdatedAt =
            DateTime.Now;

        await context.SaveChangesAsync(
            cancellationToken);

        return ProductOperationResult.Ok(
            producto.Id,
            nuevoEstado == 1
                ? "Producto activado correctamente."
                : "Producto inactivado correctamente.");
    }

    private static string? ValidateRequest(
        ProductoGuardarRequest request)
    {
        if (request.EmpresaId <= 0)
        {
            return "La empresa no es válida.";
        }

        if (string.IsNullOrWhiteSpace(
            request.Nombre))
        {
            return "Debe ingresar el nombre del producto.";
        }

        if (string.IsNullOrWhiteSpace(
                request.PresentacionNombre))
        {
            return "Debe ingresar el nombre de la presentación base.";
        }


        if (!request.SinCodigoBarras &&
            string.IsNullOrWhiteSpace(
                request.CodigoBarras))
        {
            return "Debe ingresar el código de barras o indicar que la presentación no posee uno.";
        }

        if (request.UnidadMedidaBaseId <= 0)
        {
            return "Debe seleccionar la unidad de medida.";
        }

        if (request.TarifaImpuestoId <= 0)
        {
            return "Debe seleccionar la tarifa de impuesto.";
        }

        if (request.StockMinimo < 0)
        {
            return "El stock mínimo no puede ser negativo.";
        }

        if (request.DiasAlertaCaducidad < 0)
        {
            return "Los días de alerta de caducidad no pueden ser negativos.";
        }

        if (request.TipoProducto is not
            ("PRODUCTO" or "SERVICIO"))
        {
            return "El tipo de producto no es válido.";
        }

        if (request.TipoControlInventario is not
            ("NORMAL" or
             "LOTE" or
             "SERIE" or
             "LOTE_Y_SERIE"))
        {
            return "El tipo de control de inventario no es válido.";
        }

        if (request.TipoProducto == "SERVICIO" &&
            request.ManejaInventario)
        {
            return "Un servicio no puede manejar inventario.";
        }

        if (!request.ManejaInventario &&
            (request.ManejaLotes ||
             request.ManejaSeries ||
             request.ManejaFechaCaducidad))
        {
            return "Un producto que no maneja inventario no puede manejar lotes, series o caducidad.";
        }

        if (request.ManejaFechaCaducidad &&
            !request.ManejaLotes)
        {
            return "La fecha de caducidad debe estar asociada al control por lotes.";
        }

        return null;
    }

    private static async Task<string> GenerarCodigoBarrasInternoAsync(
    KontaxDbContext context,
    CancellationToken cancellationToken)
    {
        var siguiente =
            await context.Database
                .SqlQueryRaw<long>(
                    """
                SELECT nextval(
                    's_inventario.codigo_barras_interno_seq'
                ) AS "Value"
                """)
                .SingleAsync(
                    cancellationToken);

        return siguiente.ToString();
    }

    public async Task<ProductoCodigoBarrasDto?> BuscarPorCodigoBarrasAsync(
    string codigoBarras,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codigoBarras))
        {
            return null;
        }

        var codigo =
            codigoBarras.Trim();

        await using var context =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await context.ProductosPresentaciones
            .AsNoTracking()
            .Where(x =>
                x.CodigoBarras != null &&
                x.CodigoBarras == codigo)
            .Select(x => new ProductoCodigoBarrasDto
            {
                ProductoId =
                    x.ProductoId,

                PresentacionId =
                    x.Id,

                CodigoInterno =
                    x.Producto != null
                        ? x.Producto.Codigo
                        : string.Empty,

                Nombre =
                    x.Producto != null
                        ? x.Producto.Nombre
                        : string.Empty,

                Modelo =
                    x.Producto != null
                        ? x.Producto.Modelo
                        : null,

                Presentacion =
                    x.Nombre,

                CodigoBarras =
                    x.CodigoBarras!
            })
            .FirstOrDefaultAsync(
                cancellationToken);
    }

    private static async Task<string> GenerarCodigoProductoAsync(
    KontaxDbContext context,
    CancellationToken cancellationToken)
    {
        var siguiente =
            await context.Database
                .SqlQueryRaw<long>(
                    """
                SELECT nextval(
                    's_inventario.codigo_producto_seq'
                ) AS "Value"
                """)
                .SingleAsync(
                    cancellationToken);

        return $"PRD-{siguiente:000000}";
    }

    public async Task<List<ProductoSugerenciaDto>> BuscarSimilaresAsync(
    long empresaId,
    string? nombre,
    string? modelo,
    int limite = 5,
    CancellationToken cancellationToken = default)
    {
        if (empresaId <= 0)
        {
            return new List<ProductoSugerenciaDto>();
        }

        var nombreTexto =
            nombre?.Trim() ?? string.Empty;

        var modeloTexto =
            modelo?.Trim() ?? string.Empty;

        if (nombreTexto.Length < 3 &&
            modeloTexto.Length < 3)
        {
            return new List<ProductoSugerenciaDto>();
        }

        /*
         * Construimos un texto de búsqueda combinando
         * nombre y modelo.
         */
        var textoBusqueda =
            $"{nombreTexto} {modeloTexto}"
                .Trim();

        await using var context =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        /*
         * Recuperamos una cantidad pequeña de productos
         * candidatos.
         *
         * La similitud considera:
         * - nombre;
         * - modelo;
         * - nombre de presentación.
         */
        var candidatos =
            await context.Database
                .SqlQueryRaw<long>(
                    """
                SELECT p.id AS "Value"
                FROM s_inventario.productos p
                WHERE p.empresa_id = {0}
                  AND p.estado = 1
                  AND (
                        similarity(
                            lower(
                                concat_ws(
                                    ' ',
                                    p.nombre,
                                    coalesce(p.modelo, '')
                                )
                            ),
                            lower({1})
                        ) >= 0.20

                        OR EXISTS (
                            SELECT 1
                            FROM s_inventario.productos_presentaciones pp
                            WHERE pp.producto_id = p.id
                              AND pp.estado = 1
                              AND similarity(
                                    lower(pp.nombre),
                                    lower({1})
                                  ) >= 0.20
                        )

                        OR p.nombre ILIKE '%' || {1} || '%'
                        OR coalesce(p.modelo, '') ILIKE '%' || {1} || '%'
                  )
                ORDER BY
                    GREATEST(
                        similarity(
                            lower(
                                concat_ws(
                                    ' ',
                                    p.nombre,
                                    coalesce(p.modelo, '')
                                )
                            ),
                            lower({1})
                        ),
                        COALESCE(
                            (
                                SELECT MAX(
                                    similarity(
                                        lower(pp.nombre),
                                        lower({1})
                                    )
                                )
                                FROM s_inventario.productos_presentaciones pp
                                WHERE pp.producto_id = p.id
                                  AND pp.estado = 1
                            ),
                            0
                        )
                    ) DESC
                LIMIT {2}
                """,
                    empresaId,
                    textoBusqueda,
                    limite)
                .ToListAsync(
                    cancellationToken);

        if (candidatos.Count == 0)
        {
            return new List<ProductoSugerenciaDto>();
        }

        var resultados =
    await context.Productos
        .AsNoTracking()
        .Where(x =>
            candidatos.Contains(x.Id))
        .Select(x => new ProductoSugerenciaDto
        {
            ProductoId = x.Id,

            Nombre = x.Nombre,

            Modelo = x.Modelo,

            Marca = x.Marca != null
                ? x.Marca.Nombre
                : null,

            Presentaciones =
                x.Presentaciones
                    .Where(p => p.Estado == 1)
                    .OrderBy(p => p.Nombre)
                    .Select(p => p.Nombre)
                    .Take(4)
                    .ToList()
        })
        .ToListAsync(
            cancellationToken);


        var orden =
            candidatos
                .Select((id, index) => new
                {
                    id,
                    index
                })
                .ToDictionary(
                    x => x.id,
                    x => x.index);


        return resultados
            .OrderBy(x =>
                orden[x.ProductoId])
            .ToList();
    }

    private static async Task<string?> ValidarCatalogosAsync(
        KontaxDbContext context,
        ProductoGuardarRequest request,
        CancellationToken cancellationToken)
    {
        var unidadExiste =
            await context.UnidadesMedida
                .AnyAsync(
                    x =>
                        x.Id ==
                        request.UnidadMedidaBaseId &&
                        x.Estado == 1,
                    cancellationToken);

        if (!unidadExiste)
        {
            return "La unidad de medida seleccionada no es válida.";
        }

        var tarifaExiste =
            await context.TarifasImpuesto
                .AnyAsync(
                    x =>
                        x.Id ==
                        request.TarifaImpuestoId &&
                        x.Estado == 1,
                    cancellationToken);

        if (!tarifaExiste)
        {
            return "La tarifa de impuesto seleccionada no es válida.";
        }

        if (request.CategoriaProductoId.HasValue)
        {
            var categoriaExiste =
                await context.CategoriasProducto
                    .AnyAsync(
                        x =>
                            x.Id ==
                            request.CategoriaProductoId.Value &&
                            x.EmpresaId ==
                            request.EmpresaId &&
                            x.Estado == 1,
                        cancellationToken);

            if (!categoriaExiste)
            {
                return "La categoría seleccionada no pertenece a la empresa activa.";
            }
        }

        if (request.MarcaId.HasValue)
        {
            var marcaExiste =
                await context.Marcas
                    .AnyAsync(
                        x =>
                            x.Id ==
                            request.MarcaId.Value &&
                            x.Estado == 1,
                        cancellationToken);

            if (!marcaExiste)
            {
                return "La marca seleccionada no es válida.";
            }
        }

        return null;
    }
}
