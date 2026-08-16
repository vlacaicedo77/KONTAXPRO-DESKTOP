using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Inventory;
using KONTAXPRO.Application.Models.Inventario;
using KONTAXPRO.Application.Models.Productos;
using KONTAXPRO.Application.Products;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Seguridad;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KONTAXPRO.Infrastructure.Inventory;

public sealed partial class InventoryService(
    IDbContextFactory<KontaxDbContext> dbContextFactory) : IInventoryService
{
    public async Task<List<MotivoOperacionInventarioDto>> ObtenerMotivosOperacionAsync(
        long empresaId, long usuarioId, string tipoOperacion,
        CancellationToken cancellationToken = default)
    {
        var tipo = MotivoOperacionInventarioRules.NormalizarTipo(tipoOperacion);
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await InventorySecurity.RequireCompanyAccessAsync(context, usuarioId,
            empresaId, cancellationToken);
        return await context.MotivosOperacionInventario.AsNoTracking()
            .Where(x => x.Estado == 1 && x.TipoOperacion == tipo &&
                        (x.EmpresaId == null || x.EmpresaId == empresaId))
            .OrderBy(x => x.EmpresaId == null ? 0 : 1)
            .ThenBy(x => x.Orden).ThenBy(x => x.Nombre)
            .Select(x => new MotivoOperacionInventarioDto
            {
                Id = x.Id,
                Codigo = x.Codigo,
                Nombre = x.Nombre,
                Descripcion = x.Descripcion,
                TipoOperacion = x.TipoOperacion,
                EsSistema = x.EsSistema
            }).ToListAsync(cancellationToken);
    }

    public async Task<InventoryOperationResult> CrearMotivoOperacionAsync(
        CrearMotivoOperacionInventarioRequest request,
        CancellationToken cancellationToken = default)
    {
        var tipo = MotivoOperacionInventarioRules.NormalizarTipo(
            request.TipoOperacion);
        var nombre = MotivoOperacionInventarioRules.NormalizarNombre(
            request.Nombre);
        if (request.EmpresaId <= 0 || request.UsuarioId <= 0 ||
            string.IsNullOrWhiteSpace(nombre) ||
            !MotivoOperacionInventarioRules.EsTipoValido(tipo))
            return InventoryOperationResult.Fail(
                "Empresa, usuario, tipo y nombre del motivo son obligatorios.");
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await ExigirPermisoAsync(context, request.UsuarioId, request.EmpresaId,
            "INVENTARIO_CREAR_MOTIVO", cancellationToken);
        var existente = await context.MotivosOperacionInventario
            .FirstOrDefaultAsync(x => x.TipoOperacion == tipo &&
                (x.EmpresaId == null || x.EmpresaId == request.EmpresaId) &&
                x.Nombre == nombre, cancellationToken);
        if (existente is not null)
            return InventoryOperationResult.Ok(existente.Id,
                $"Ya existe el motivo {existente.Nombre}.");
        var now = DateTime.UtcNow;
        var motivo = new MotivoOperacionInventario
        {
            EmpresaId = request.EmpresaId,
            Codigo = $"EMP_{Guid.NewGuid():N}".ToUpperInvariant(),
            Nombre = nombre,
            Descripcion = Normalize(request.Descripcion),
            TipoOperacion = tipo,
            EsSistema = false,
            Orden = 1000,
            Estado = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
        context.MotivosOperacionInventario.Add(motivo);
        await context.SaveChangesAsync(cancellationToken);
        return InventoryOperationResult.Ok(motivo.Id, "Motivo creado correctamente.");
    }

    public async Task<EstadoControlInventarioDto?> ObtenerEstadoControlAsync(
        long empresaId,
        long usuarioId,
        long productoId,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await InventorySecurity.RequireCompanyAccessAsync(context, usuarioId,
            empresaId, cancellationToken);
        var authorizedWarehouses = context
            .UsuariosEmpresasEstablecimientos.AsNoTracking()
            .Where(x => x.UsuarioEmpresa!.UsuarioId == usuarioId &&
                x.UsuarioEmpresa.EmpresaId == empresaId &&
                x.UsuarioEmpresa.Estado == 1)
            .SelectMany(x => context.Bodegas.Where(b =>
                b.EstablecimientoId == x.EstablecimientoId && b.Estado == 1)
                .Select(b => b.Id));
        var canViewCosts = await InventorySecurity.HasPermissionAsync(context,
            usuarioId, empresaId, "INVENTARIO_VER_COSTO", cancellationToken);
        var producto = await context.Productos.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == productoId &&
                x.EmpresaId == empresaId, cancellationToken);
        if (producto is null)
            return null;
        var resultado = new EstadoControlInventarioDto
        {
            ProductoId = producto.Id,
            TipoControl = ObtenerTipoControl(
                producto.ManejaLotes, producto.ManejaSeries),
            ManejaFechaCaducidad = producto.ManejaFechaCaducidad
        };
        var bodegas = await context.ProductosExistencias.AsNoTracking()
            .Where(x => x.ProductoId == productoId &&
                authorizedWarehouses.Contains(x.BodegaId))
            .OrderBy(x => x.Bodega!.Nombre)
            .Select(x => new EstadoControlBodegaDto
            {
                BodegaId = x.BodegaId,
                BodegaCodigo = x.Bodega!.Codigo,
                BodegaNombre = x.Bodega!.Nombre,
                StockActual = x.StockActual,
                StockReservado = x.StockReservado
            }).ToListAsync(cancellationToken);
        var lotesDatos = await context.ProductosLotesExistencias.AsNoTracking()
            .Where(x => x.Lote!.ProductoId == productoId &&
                authorizedWarehouses.Contains(x.BodegaId))
            .Select(x => new
            {
                x.BodegaId,
                x.LoteId,
                NumeroLote = x.Lote!.NumeroLote,
                x.StockActual,
                x.StockReservado,
                x.Lote.FechaElaboracion,
                x.Lote.FechaCaducidad
            }).ToListAsync(cancellationToken);
        var costosLotes = await context.MovimientosInventarioDetallesLotes
            .AsNoTracking()
            .Where(x => canViewCosts &&
                x.ProductoLote!.ProductoId == productoId &&
                authorizedWarehouses.Contains(x.MovimientoInventarioDetalle!
                    .MovimientoInventario!.BodegaId) &&
                x.MovimientoInventarioDetalle!.MovimientoInventario!
                    .TipoMovimiento!.Naturaleza == "ENTRADA")
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.ProductoLoteId,
                x.MovimientoInventarioDetalle!.CostoUnitarioBase
            })
            .ToListAsync(cancellationToken);
        var ultimoCostoPorLote = costosLotes
            .GroupBy(x => x.ProductoLoteId)
            .ToDictionary(x => x.Key, x => (decimal?)x.First().CostoUnitarioBase);
        var series = await context.ProductosSeries.AsNoTracking()
            .Where(x => x.ProductoId == productoId &&
                authorizedWarehouses.Contains(x.BodegaId) &&
                x.EstadoSerie!.Codigo == "DISPONIBLE")
            .Select(x => new
            {
                x.BodegaId,
                Item = new EstadoControlSerieDto
                {
                    SerieId = x.Id,
                    NumeroSerie = x.NumeroSerie,
                    NumeroLote = x.ProductoLote != null
                        ? x.ProductoLote.NumeroLote : null
                    ,Ubicacion = x.Ubicacion
                }
            }).ToListAsync(cancellationToken);
        resultado.SeriesProducto = await context.ProductosSeries.AsNoTracking()
            .Where(x => x.ProductoId == productoId &&
                authorizedWarehouses.Contains(x.BodegaId))
            .Select(x => x.NumeroSerie)
            .ToListAsync(cancellationToken);
        foreach (var bodega in bodegas)
        {
            bodega.Lotes = lotesDatos.Where(x => x.BodegaId == bodega.BodegaId)
                .Select(x => new EstadoControlLoteDto
                {
                    LoteId = x.LoteId,
                    NumeroLote = x.NumeroLote,
                    StockActual = x.StockActual,
                    StockReservado = x.StockReservado,
                    FechaElaboracion = x.FechaElaboracion?.ToDateTime(TimeOnly.MinValue),
                    FechaCaducidad = x.FechaCaducidad?.ToDateTime(TimeOnly.MinValue),
                    UltimoCostoUnitarioBase = ultimoCostoPorLote
                        .GetValueOrDefault(x.LoteId)
                }).ToList();
            bodega.Series = series.Where(x => x.BodegaId == bodega.BodegaId)
                .Select(x => x.Item).ToList();
        }
        resultado.Bodegas = bodegas;
        return resultado;
    }

    public async Task<InventoryOperationResult> ConvertirControlAsync(
        ConversionControlInventarioRequest request,
        CancellationToken cancellationToken = default)
    {
        var anterior = request.TipoControlAnterior.Trim().ToUpperInvariant();
        var nuevo = request.TipoControlNuevo.Trim().ToUpperInvariant();
        if (request.EmpresaId <= 0 || request.ProductoId <= 0 ||
            request.UsuarioId <= 0 || request.MotivoOperacionInventarioId <= 0)
            return InventoryOperationResult.Fail(
                "Empresa, producto, usuario y motivo son obligatorios.");

        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database
            .BeginTransactionAsync(System.Data.IsolationLevel.Serializable,
                cancellationToken);
        try
        {
            await ExigirPermisoAsync(context, request.UsuarioId,
                request.EmpresaId, "INVENTARIO_CONVERTIR_TIPO_CONTROL",
                cancellationToken);
            var motivoOperacion = await ObtenerMotivoValidoAsync(context,
                request.MotivoOperacionInventarioId, request.EmpresaId,
                "CONVERSION_CONTROL", cancellationToken);
            var producto = await context.Productos.SingleOrDefaultAsync(x =>
                x.Id == request.ProductoId && x.EmpresaId == request.EmpresaId,
                cancellationToken)
                ?? throw new InvalidOperationException("No se encontró el producto.");
            if (!producto.ManejaInventario)
                throw new InvalidOperationException(
                    "Solo puede convertirse un producto que maneja inventario.");
            var controlActual = ObtenerTipoControl(
                producto.ManejaLotes, producto.ManejaSeries);
            if (controlActual != anterior)
                throw new InvalidOperationException(
                    "El tipo de control cambió desde que se abrió el asistente. Recargue la información.");

            var existencias = await context.ProductosExistencias
                .FromSqlInterpolated(
                    $"SELECT * FROM s_inventario.productos_existencias WHERE producto_id = {producto.Id} FOR UPDATE")
                .OrderBy(x => x.BodegaId)
                .ToListAsync(cancellationToken);
            var snapshots = existencias.Select(x =>
                new ExistenciaControlSnapshot(
                    x.BodegaId, x.StockActual, x.StockReservado)).ToList();
            var tieneHistoria = await context.MovimientosInventarioDetalles
                .AnyAsync(x => x.ProductoId == producto.Id, cancellationToken);
            var reduccionError = InventarioControlRules.ValidarReduccionControl(
                anterior, nuevo, tieneHistoria, snapshots);
            if (reduccionError is not null)
                throw new InvalidOperationException(reduccionError);

            foreach (var existencia in existencias)
            {
                var entrada = request.Bodegas.SingleOrDefault(x =>
                    x.BodegaId == existencia.BodegaId);
                if (entrada is null || entrada.StockEsperado != existencia.StockActual)
                    throw new InvalidOperationException(
                        "Las existencias cambiaron desde que se abrió el asistente. Recargue la distribución.");
            }
            if (request.Bodegas.Any(x =>
                    existencias.All(e => e.BodegaId != x.BodegaId)))
                throw new InvalidOperationException(
                    "La distribución contiene una bodega que no pertenece a las existencias del producto.");

            var requiereLotes = nuevo is "LOTE" or "LOTE_Y_SERIE";
            var requiereSeries = nuevo is "SERIE" or "LOTE_Y_SERIE";
            var controlaCaducidad = requiereLotes && request.ControlCaducidad;
            if (controlaCaducidad && request.DiasAnticipacionCaducidad <= 0)
                throw new InvalidOperationException(
                    "Los días de anticipación deben ser mayores que cero.");
            var distribuciones = request.Bodegas.Select(x =>
                new DistribucionControlSnapshot(
                    x.BodegaId,
                    requiereLotes ? x.Lotes.Sum(l => l.CantidadBase) : x.StockEsperado,
                    x.Series.Count)).ToList();
            var error = InventarioControlRules.ValidarConversion(
                anterior, nuevo, snapshots, distribuciones);
            if (error is not null)
                throw new InvalidOperationException(error);

            if (request.Bodegas.SelectMany(x => x.Lotes)
                .Any(x => x.EsRegularizacion))
                await ExigirPermisoAsync(context, request.UsuarioId,
                    request.EmpresaId,
                    "INVENTARIO_CREAR_LOTE_REGULARIZACION",
                    cancellationToken);

            var now = DateTime.UtcNow;
            var conversion = new ConversionControlInventario
            {
                EmpresaId = request.EmpresaId,
                ProductoId = producto.Id,
                TipoControlAnterior = anterior,
                TipoControlNuevo = nuevo,
                FechaConversion = now,
                UsuarioId = request.UsuarioId,
                MotivoOperacionInventarioId = motivoOperacion.Id,
                Motivo = MotivoOperacionInventarioRules.CrearSnapshot(
                    motivoOperacion.Nombre),
                Estado = "CONFIRMADA",
                CreatedAt = now,
                UpdatedAt = now
            };
            context.ConversionesControlInventario.Add(conversion);

            var lotesPorNumero = new Dictionary<string, ProductoLote>(
                StringComparer.OrdinalIgnoreCase);
            if (requiereLotes)
            {
                var hoy = DateTime.Today;
                foreach (var bodega in request.Bodegas)
                {
                if (bodega.Lotes.Select(x => x.NumeroLote.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
                    bodega.Lotes.Count)
                    throw new InvalidOperationException(
                        "No pueden repetirse números de lote dentro de una bodega.");
                foreach (var loteRequest in bodega.Lotes)
                {
                    if (string.IsNullOrWhiteSpace(loteRequest.NumeroLote) ||
                        loteRequest.CantidadBase <= 0)
                        throw new InvalidOperationException(
                            "Cada lote debe tener número y cantidad válida.");
                    if (loteRequest.FechaElaboracion.HasValue &&
                        loteRequest.FechaElaboracion.Value.Date > hoy)
                        throw new InvalidOperationException(
                            "La fecha de elaboración no puede ser posterior a la fecha actual.");
                    if (loteRequest.FechaCaducidad.HasValue &&
                        loteRequest.FechaCaducidad.Value.Date <= hoy)
                        throw new InvalidOperationException(
                            "La fecha de caducidad debe ser posterior a la fecha actual.");
                    if (loteRequest.FechaElaboracion.HasValue &&
                        loteRequest.FechaCaducidad.HasValue &&
                        loteRequest.FechaElaboracion.Value.Date >
                        loteRequest.FechaCaducidad.Value.Date)
                        throw new InvalidOperationException(
                            "La elaboración de un lote no puede ser posterior a su caducidad.");
                    if (controlaCaducidad &&
                        !loteRequest.FechaCaducidad.HasValue)
                        throw new InvalidOperationException(
                            "La caducidad es obligatoria para todos los lotes de este producto.");

                    var numero = loteRequest.NumeroLote.Trim().ToUpperInvariant();
                    var lote = lotesPorNumero.GetValueOrDefault(numero) ??
                        await context.ProductosLotes.SingleOrDefaultAsync(x =>
                            x.ProductoId == producto.Id &&
                            x.NumeroLote == numero, cancellationToken);
                    if (lote is null)
                    {
                        lote = new ProductoLote
                        {
                            ProductoId = producto.Id,
                            NumeroLote = numero,
                            FechaElaboracion = loteRequest.FechaElaboracion.HasValue
                                ? DateOnly.FromDateTime(loteRequest.FechaElaboracion.Value) : null,
                            FechaCaducidad = loteRequest.FechaCaducidad.HasValue
                                ? DateOnly.FromDateTime(loteRequest.FechaCaducidad.Value) : null,
                            Observacion = loteRequest.EsRegularizacion
                                ? "LOTE DE REGULARIZACIÓN PENDIENTE DE VERIFICACIÓN" : null,
                            Estado = 1,
                            CreatedAt = now,
                            UpdatedAt = now
                        };
                        context.ProductosLotes.Add(lote);
                        await context.SaveChangesAsync(cancellationToken);
                    }
                    lotesPorNumero[numero] = lote;
                    var loteExistencia = await context.ProductosLotesExistencias
                        .SingleOrDefaultAsync(x => x.LoteId == lote.Id &&
                            x.BodegaId == bodega.BodegaId, cancellationToken);
                    if (producto.ManejaLotes)
                    {
                        if (loteExistencia is null ||
                            loteExistencia.StockActual != loteRequest.CantidadBase)
                            throw new InvalidOperationException(
                                "La distribución existente por lote cambió o no coincide con el stock informado.");
                    }
                    else
                    {
                        if (loteExistencia is null)
                        {
                            loteExistencia = new ProductoLoteExistencia
                            {
                                LoteId = lote.Id,
                                BodegaId = bodega.BodegaId,
                                StockActual = loteRequest.CantidadBase,
                                StockReservado = 0,
                                CreatedAt = now,
                                UpdatedAt = now
                            };
                            context.ProductosLotesExistencias.Add(loteExistencia);
                        }
                        else if (loteExistencia.StockActual != 0)
                            throw new InvalidOperationException(
                                $"El lote '{numero}' ya tiene existencia en la bodega y no puede fusionarse automáticamente.");
                        else
                            loteExistencia.StockActual = loteRequest.CantidadBase;
                    }
                    conversion.Detalles.Add(new ConversionControlInventarioDetalle
                    {
                        BodegaId = bodega.BodegaId,
                        ProductoLoteId = lote.Id,
                        CantidadBase = loteRequest.CantidadBase,
                        CreatedAt = now
                    });
                }
                }
            }
            else
            {
                foreach (var bodega in request.Bodegas)
                    conversion.Detalles.Add(new ConversionControlInventarioDetalle
                    {
                        BodegaId = bodega.BodegaId,
                        CantidadBase = bodega.StockEsperado,
                        CreatedAt = now
                    });
            }

            if (requiereSeries)
            {
                var disponible = await context.EstadosSerie.SingleAsync(x =>
                    x.Codigo == "DISPONIBLE" && x.Estado == 1,
                    cancellationToken);
                var numerosGlobales = request.Bodegas.SelectMany(x => x.Series)
                    .Select(x => x.NumeroSerie.Trim().ToUpperInvariant()).ToList();
                if (numerosGlobales.Any(string.IsNullOrWhiteSpace) ||
                    numerosGlobales.Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
                    numerosGlobales.Count)
                    throw new InvalidOperationException(
                        "Las series deben estar completas y no repetirse.");

                foreach (var bodega in request.Bodegas)
                foreach (var serieRequest in bodega.Series)
                {
                    var numero = serieRequest.NumeroSerie.Trim().ToUpperInvariant();
                    ProductoSerie serie;
                    if (producto.ManejaSeries)
                    {
                        serie = await context.ProductosSeries
                            .Include(x => x.EstadoSerie)
                            .SingleOrDefaultAsync(x => x.ProductoId == producto.Id &&
                                x.BodegaId == bodega.BodegaId &&
                                x.NumeroSerie == numero, cancellationToken)
                            ?? throw new InvalidOperationException(
                                $"La serie '{numero}' no existe en la bodega indicada.");
                        if (serie.EstadoSerie!.Codigo != "DISPONIBLE")
                            throw new InvalidOperationException(
                                $"La serie '{numero}' no está disponible.");
                    }
                    else
                    {
                        if (await context.ProductosSeries.AnyAsync(x =>
                                x.ProductoId == producto.Id &&
                                x.NumeroSerie == numero, cancellationToken))
                            throw new InvalidOperationException(
                                $"La serie '{numero}' ya existe.");
                        serie = new ProductoSerie
                        {
                            ProductoId = producto.Id,
                            BodegaId = bodega.BodegaId,
                            NumeroSerie = numero,
                            EstadoSerieId = disponible.Id,
                            CreatedAt = now,
                            UpdatedAt = now
                        };
                        context.ProductosSeries.Add(serie);
                    }
                    if (requiereLotes)
                    {
                        if (string.IsNullOrWhiteSpace(serieRequest.NumeroLote) ||
                            !lotesPorNumero.TryGetValue(
                                serieRequest.NumeroLote.Trim().ToUpperInvariant(),
                                out var lote))
                            throw new InvalidOperationException(
                                $"Debe asociar la serie '{numero}' a un lote válido.");
                        serie.ProductoLoteId = lote.Id;
                    }
                    await context.SaveChangesAsync(cancellationToken);
                    conversion.Series.Add(new ConversionControlInventarioSerie
                    {
                        ProductoSerieId = serie.Id,
                        CreatedAt = now
                    });
                }
            }

            producto.ManejaLotes = requiereLotes;
            producto.ManejaSeries = requiereSeries;
            producto.ManejaFechaCaducidad = controlaCaducidad;
            producto.AlertaCaducidad = controlaCaducidad;
            producto.DiasAlertaCaducidad = controlaCaducidad
                ? request.DiasAnticipacionCaducidad
                : null;
            producto.UpdatedAt = now;
            context.Auditorias.Add(new Auditoria
            {
                UsuarioId = request.UsuarioId,
                EmpresaId = request.EmpresaId,
                Accion = "CONVERTIR_CONTROL_INVENTARIO",
                Entidad = "productos",
                EntidadId = producto.Id,
                Descripcion = $"CONTROL_ANTERIOR={anterior}; CONTROL_NUEVO={nuevo}; MOTIVO={conversion.Motivo}",
                CreatedAt = now
            });
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return InventoryOperationResult.Ok(conversion.Id,
                $"Control convertido de {anterior} a {nuevo} sin alterar el stock general.");
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
                "La conversión no pudo confirmarse porque el inventario cambió o existe un conflicto de integridad.");
        }
    }

    public async Task<InventoryOperationResult> RegistrarAjusteAsync(
        AjusteInventarioRequest request,
        CancellationToken cancellationToken = default)
    {
        var tipoAjuste = request.TipoAjuste.Trim().ToUpperInvariant();
        if (request.EmpresaId <= 0 || request.EstablecimientoId <= 0 ||
            request.BodegaId <= 0 || request.UsuarioId <= 0 ||
            tipoAjuste is not ("ENTRADA" or "SALIDA") ||
            request.MotivoOperacionInventarioId <= 0 || request.Detalles.Count == 0)
            return InventoryOperationResult.Fail(
                "Empresa, establecimiento, bodega, tipo, motivo, usuario y detalles son obligatorios.");

        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database
            .BeginTransactionAsync(System.Data.IsolationLevel.Serializable,
                cancellationToken);
        try
        {
            await ExigirPermisoAsync(context, request.UsuarioId,
                request.EmpresaId, "INVENTARIO_REGISTRAR_AJUSTE", cancellationToken);
            var motivoOperacion = await ObtenerMotivoValidoAsync(context,
                request.MotivoOperacionInventarioId, request.EmpresaId,
                tipoAjuste == "ENTRADA" ? "AJUSTE_ENTRADA" : "AJUSTE_SALIDA",
                cancellationToken);
            var bodegaValida = await context.Bodegas.AnyAsync(x =>
                x.Id == request.BodegaId &&
                x.EstablecimientoId == request.EstablecimientoId &&
                x.Establecimiento!.EmpresaId == request.EmpresaId && x.Estado == 1,
                cancellationToken);
            if (!bodegaValida)
                throw new InvalidOperationException(
                    "La bodega no pertenece al establecimiento y empresa activos.");
            await ExigirAccesoEstablecimientoAsync(context, request.UsuarioId,
                request.EmpresaId, request.EstablecimientoId, cancellationToken);

            var numero = await ObtenerSiguienteNumeroAsync(context,
                request.EmpresaId, request.EstablecimientoId,
                "MOVIMIENTO_INVENTARIO", cancellationToken);
            var now = DateTime.UtcNow;
            var ajuste = new AjusteInventario
            {
                EmpresaId = request.EmpresaId,
                EstablecimientoId = request.EstablecimientoId,
                BodegaId = request.BodegaId,
                UsuarioId = request.UsuarioId,
                NumeroAjuste = numero,
                TipoAjuste = tipoAjuste,
                FechaAjuste = AsegurarUtc(request.Fecha),
                MotivoOperacionInventarioId = motivoOperacion.Id,
                Motivo = MotivoOperacionInventarioRules.CrearSnapshot(
                    motivoOperacion.Nombre),
                Estado = "CONFIRMADO",
                CreatedAt = now,
                UpdatedAt = now
            };
            context.AjustesInventario.Add(ajuste);
            await context.SaveChangesAsync(cancellationToken);

            var codigoMovimiento = $"AJUSTE_{tipoAjuste}";
            var tipoMovimiento = await context.TiposMovimientoInventario
                .SingleAsync(x => x.Codigo == codigoMovimiento && x.Estado == 1,
                    cancellationToken);
            var origen = await context.TiposOrigenMovimientoInventario
                .SingleAsync(x => x.Codigo == "AJUSTE" && x.Estado == 1,
                    cancellationToken);
            var movimiento = new MovimientoInventario
            {
                EmpresaId = request.EmpresaId,
                NumeroMovimiento = numero,
                TipoMovimientoId = tipoMovimiento.Id,
                FechaMovimiento = ajuste.FechaAjuste,
                BodegaId = request.BodegaId,
                OrigenTipoId = origen.Id,
                OrigenId = ajuste.Id,
                Referencia = ajuste.Motivo,
                Observacion = Normalize(request.Observacion),
                UsuarioId = request.UsuarioId,
                Estado = "CONFIRMADO",
                CreatedAt = now,
                UpdatedAt = now
            };
            context.MovimientosInventario.Add(movimiento);
            await context.SaveChangesAsync(cancellationToken);

            foreach (var detalleRequest in request.Detalles)
            {
                ProductoPresentacion presentacion;
                if (tipoAjuste == "ENTRADA")
                {
                    await AddInitialDetailAsync(context, movimiento,
                        detalleRequest, now, cancellationToken);
                    presentacion = await context.ProductosPresentaciones
                        .SingleAsync(x => x.Id == detalleRequest.ProductoPresentacionId,
                            cancellationToken);
                }
                else
                {
                    presentacion = await AddAdjustmentOutputDetailAsync(
                        context, movimiento, detalleRequest, now,
                        cancellationToken);
                }
                ajuste.Detalles.Add(new AjusteInventarioDetalle
                {
                    ProductoId = detalleRequest.ProductoId,
                    ProductoPresentacionId = presentacion.Id,
                    CantidadPresentacion = detalleRequest.Cantidad,
                    FactorConversion = presentacion.FactorConversion,
                    CantidadBase = detalleRequest.Cantidad * presentacion.FactorConversion,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                // El siguiente detalle del mismo producto debe observar el
                // stock y costo ya aplicados por esta línea dentro de la
                // misma transacción.
                await context.SaveChangesAsync(cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return InventoryOperationResult.Ok(ajuste.Id,
                $"Ajuste {tipoAjuste.ToLowerInvariant()} registrado con número {numero}.");
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
                "No fue posible registrar el ajuste por un conflicto de integridad o concurrencia.");
        }
        catch (PostgresException ex) when (ex.SqlState ==
            PostgresErrorCodes.SerializationFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return InventoryOperationResult.Fail(
                "El inventario cambió simultáneamente. Actualiza la información e intenta nuevamente.");
        }
    }

    public async Task<InventoryOperationResult> CorregirLoteAsync(
        CorregirLoteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EmpresaId <= 0 || request.ProductoId <= 0 ||
            request.LoteId <= 0 || request.UsuarioId <= 0 ||
            string.IsNullOrWhiteSpace(request.NumeroLote) ||
            request.MotivoOperacionInventarioId <= 0)
            return InventoryOperationResult.Fail(
                "Producto, lote, usuario, número y motivo son obligatorios.");
        if (request.FechaElaboracion.HasValue && request.FechaCaducidad.HasValue &&
            request.FechaElaboracion.Value.Date > request.FechaCaducidad.Value.Date)
            return InventoryOperationResult.Fail(
                "La fecha de elaboración no puede ser posterior a la caducidad.");

        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database
            .BeginTransactionAsync(System.Data.IsolationLevel.Serializable,
                cancellationToken);
        try
        {
            await ExigirPermisoAsync(context, request.UsuarioId,
                request.EmpresaId, "INVENTARIO_CORREGIR_LOTE", cancellationToken);
            var motivoOperacion = await ObtenerMotivoValidoAsync(context,
                request.MotivoOperacionInventarioId, request.EmpresaId,
                "CORRECCION_LOTE_SERIE", cancellationToken);
            var lote = await context.ProductosLotes
                .Include(x => x.Producto)
                .SingleOrDefaultAsync(x => x.Id == request.LoteId &&
                    x.ProductoId == request.ProductoId &&
                    x.Producto!.EmpresaId == request.EmpresaId,
                    cancellationToken)
                ?? throw new InvalidOperationException("No se encontró el lote.");
            var numero = request.NumeroLote.Trim().ToUpperInvariant();
            if (await context.ProductosLotes.AnyAsync(x =>
                    x.ProductoId == request.ProductoId && x.Id != lote.Id &&
                    x.NumeroLote == numero, cancellationToken))
                throw new InvalidOperationException(
                    "Ya existe ese número de lote para el producto; los lotes no se fusionan automáticamente.");
            if (lote.Producto!.ManejaFechaCaducidad &&
                !request.FechaCaducidad.HasValue)
                throw new InvalidOperationException(
                    "La fecha de caducidad es obligatoria para este producto.");

            var anterior = $"LOTE={lote.NumeroLote}; ELABORACION={lote.FechaElaboracion}; CADUCIDAD={lote.FechaCaducidad}";
            lote.NumeroLote = numero;
            lote.FechaElaboracion = request.FechaElaboracion.HasValue
                ? DateOnly.FromDateTime(request.FechaElaboracion.Value) : null;
            lote.FechaCaducidad = request.FechaCaducidad.HasValue
                ? DateOnly.FromDateTime(request.FechaCaducidad.Value) : null;
            lote.UpdatedAt = DateTime.UtcNow;
            var nuevoValor = $"LOTE={numero}; ELABORACION={lote.FechaElaboracion}; CADUCIDAD={lote.FechaCaducidad}";
            context.CorreccionesDatosInventario.Add(new CorreccionDatoInventario
            {
                EmpresaId = request.EmpresaId,
                ProductoId = request.ProductoId,
                TipoEntidad = "LOTE",
                EntidadId = lote.Id,
                MotivoOperacionInventarioId = motivoOperacion.Id,
                Motivo = MotivoOperacionInventarioRules.CrearSnapshot(
                    motivoOperacion.Nombre),
                UsuarioId = request.UsuarioId,
                FechaCorreccion = DateTime.UtcNow,
                ValorAnterior = anterior,
                ValorNuevo = nuevoValor,
                CreatedAt = DateTime.UtcNow
            });
            context.Auditorias.Add(new Auditoria
            {
                UsuarioId = request.UsuarioId,
                EmpresaId = request.EmpresaId,
                Accion = "CORREGIR_LOTE",
                Entidad = "productos_lotes",
                EntidadId = lote.Id,
                Descripcion = $"{anterior}; {nuevoValor}; MOTIVO={motivoOperacion.Nombre}",
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return InventoryOperationResult.Ok(lote.Id, "Lote corregido y auditado.");
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
                "No fue posible corregir el lote por un conflicto de integridad.");
        }
    }

    public async Task<InventoryOperationResult> CorregirSerieAsync(
        CorregirSerieRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EmpresaId <= 0 || request.ProductoId <= 0 ||
            request.SerieId <= 0 || request.UsuarioId <= 0 ||
            string.IsNullOrWhiteSpace(request.NumeroSerie) ||
            request.MotivoOperacionInventarioId <= 0)
            return InventoryOperationResult.Fail(
                "Producto, serie, usuario, número y motivo son obligatorios.");

        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await ExigirPermisoAsync(context, request.UsuarioId,
                request.EmpresaId, "INVENTARIO_CORREGIR_SERIE", cancellationToken);
            var motivoOperacion = await ObtenerMotivoValidoAsync(context,
                request.MotivoOperacionInventarioId, request.EmpresaId,
                "CORRECCION_LOTE_SERIE", cancellationToken);
            var serie = await context.ProductosSeries
                .Include(x => x.Producto)
                .Include(x => x.EstadoSerie)
                .SingleOrDefaultAsync(x => x.Id == request.SerieId &&
                    x.ProductoId == request.ProductoId &&
                    x.Producto!.EmpresaId == request.EmpresaId,
                    cancellationToken)
                ?? throw new InvalidOperationException("No se encontró la serie.");
            if (serie.EstadoSerie!.Codigo != "DISPONIBLE")
                throw new InvalidOperationException(
                    "Solo puede corregirse una serie disponible.");
            if (await context.ProductosExistencias.AnyAsync(x =>
                    x.ProductoId == request.ProductoId &&
                    x.BodegaId == serie.BodegaId && x.StockReservado > 0,
                    cancellationToken))
                throw new InvalidOperationException(
                    "No puede corregirse la serie mientras existan reservas en su bodega.");
            var numero = request.NumeroSerie.Trim().ToUpperInvariant();
            if (await context.ProductosSeries.AnyAsync(x =>
                    x.ProductoId == request.ProductoId && x.Id != serie.Id &&
                    x.NumeroSerie == numero, cancellationToken))
                throw new InvalidOperationException(
                    "Ya existe ese número de serie para el producto.");

            var anterior = serie.NumeroSerie;
            serie.NumeroSerie = numero;
            serie.UpdatedAt = DateTime.UtcNow;
            context.CorreccionesDatosInventario.Add(new CorreccionDatoInventario
            {
                EmpresaId = request.EmpresaId,
                ProductoId = request.ProductoId,
                TipoEntidad = "SERIE",
                EntidadId = serie.Id,
                MotivoOperacionInventarioId = motivoOperacion.Id,
                Motivo = MotivoOperacionInventarioRules.CrearSnapshot(
                    motivoOperacion.Nombre),
                UsuarioId = request.UsuarioId,
                FechaCorreccion = DateTime.UtcNow,
                ValorAnterior = anterior,
                ValorNuevo = numero,
                CreatedAt = DateTime.UtcNow
            });
            context.Auditorias.Add(new Auditoria
            {
                UsuarioId = request.UsuarioId,
                EmpresaId = request.EmpresaId,
                Accion = "CORREGIR_SERIE",
                Entidad = "productos_series",
                EntidadId = serie.Id,
                Descripcion = $"SERIE_ANTERIOR={anterior}; SERIE_NUEVA={numero}; MOTIVO={motivoOperacion.Nombre}",
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return InventoryOperationResult.Ok(serie.Id, "Serie corregida y auditada.");
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
                "No fue posible corregir la serie por un conflicto de integridad.");
        }
    }

    public async Task<List<KardexItemDto>> ObtenerKardexAsync(
        KardexFiltro filtro,
        CancellationToken cancellationToken = default)
    {
        if (filtro.EmpresaId <= 0 || filtro.ProductoId <= 0)
            return [];

        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await ExigirPermisoAsync(context, filtro.UsuarioId,
            filtro.EmpresaId, "INVENTARIO_VER_KARDEX", cancellationToken);
        var canViewCosts = await InventorySecurity.HasPermissionAsync(context,
            filtro.UsuarioId, filtro.EmpresaId, "INVENTARIO_VER_COSTO",
            cancellationToken);
        var query = context.MovimientosInventarioDetalles
            .AsNoTracking()
            .Where(x => x.ProductoId == filtro.ProductoId &&
                        x.MovimientoInventario!.EmpresaId == filtro.EmpresaId);
        if (filtro.EstablecimientoId.HasValue)
            query = query.Where(x =>
                x.MovimientoInventario!.Bodega!.EstablecimientoId ==
                filtro.EstablecimientoId.Value);
        if (filtro.BodegaId.HasValue)
            query = query.Where(x =>
                x.MovimientoInventario!.BodegaId == filtro.BodegaId.Value);
        if (filtro.Desde.HasValue)
            query = query.Where(x =>
                x.MovimientoInventario!.FechaMovimiento >= filtro.Desde.Value);
        if (filtro.Hasta.HasValue)
            query = query.Where(x =>
                x.MovimientoInventario!.FechaMovimiento < filtro.Hasta.Value.Date.AddDays(1));
        if (filtro.TipoMovimientoId.HasValue)
            query = query.Where(x =>
                x.MovimientoInventario!.TipoMovimientoId == filtro.TipoMovimientoId.Value);
        if (filtro.OrigenTipoId.HasValue)
            query = query.Where(x =>
                x.MovimientoInventario!.OrigenTipoId == filtro.OrigenTipoId.Value);
        if (!string.IsNullOrWhiteSpace(filtro.NumeroDocumento))
        {
            var documento = filtro.NumeroDocumento.Trim().ToUpperInvariant();
            query = query.Where(x =>
                x.MovimientoInventario!.NumeroDocumento != null &&
                x.MovimientoInventario.NumeroDocumento.ToUpper().Contains(documento));
        }

        return await query
            .OrderByDescending(x => x.MovimientoInventario!.FechaMovimiento)
            .ThenByDescending(x => x.Id)
            .Select(x => new KardexItemDto
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
                EntradaBase = x.MovimientoInventario.TipoMovimiento.Naturaleza == "ENTRADA"
                    ? x.CantidadBase : 0,
                SalidaBase = x.MovimientoInventario.TipoMovimiento.Naturaleza == "SALIDA"
                    ? x.CantidadBase : 0,
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
            })
            .ToListAsync(cancellationToken);
    }

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
        await using var transaction = await context.Database
            .BeginTransactionAsync(System.Data.IsolationLevel.Serializable,
                cancellationToken);

        try
        {
            await ExigirPermisoAsync(context, request.UsuarioId.Value,
                request.EmpresaId, "INVENTARIO_AGREGAR_ENTRADA_INICIAL",
                cancellationToken);
            var bodega = await context.Bodegas
                .Include(x => x.Establecimiento)
                .SingleOrDefaultAsync(
                    x => x.Id == request.BodegaId &&
                         x.Estado == 1 &&
                         x.Establecimiento!.EmpresaId == request.EmpresaId,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "La bodega no pertenece a la empresa activa.");
            await ExigirAccesoEstablecimientoAsync(context,
                request.UsuarioId.Value, request.EmpresaId,
                bodega.EstablecimientoId, cancellationToken);

            var productIds = request.Detalles.Select(x => x.ProductoId)
                .Distinct().ToList();
            var hasOperationalHistory = await context
                .MovimientosInventarioDetalles.AsNoTracking()
                .AnyAsync(x => productIds.Contains(x.ProductoId) &&
                    x.MovimientoInventario!.EmpresaId == request.EmpresaId &&
                    x.MovimientoInventario.Estado == "CONFIRMADO" &&
                    x.MovimientoInventario.TipoMovimiento!.Codigo !=
                        "INVENTARIO_INICIAL", cancellationToken);
            if (hasOperationalHistory)
                throw new InvalidOperationException(
                    "El inventario inicial solo puede completarse antes de registrar movimientos operativos del producto.");

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
        catch (PostgresException ex) when (ex.SqlState ==
            PostgresErrorCodes.SerializationFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return InventoryOperationResult.Fail(
                "El inventario cambió simultáneamente. Actualiza la información e intenta nuevamente.");
        }
    }

    internal static async Task<MovimientoInventarioDetalle> AddInitialDetailAsync(
        KontaxDbContext context,
        MovimientoInventario movement,
        IngresoInventarioDetalleRequest request,
        DateTime now,
        CancellationToken cancellationToken,
        bool esBonificacion = false,
        decimal? ultimoPrecioCompraUnitarioBase = null,
        decimal? factorConversionHistorico = null,
        IReadOnlyDictionary<string, long>? seriesReutilizables = null,
        bool actualizarUltimoCostoEfectivo = true,
        bool actualizarConfiguracionExistencia = true)
    {
        if (request.Cantidad <= 0)
            throw new InvalidOperationException(
                "La cantidad debe ser mayor que cero.");
        if (request.CostoTotal < 0)
            throw new InvalidOperationException(
                "El costo total no puede ser negativo.");
        if (request.StockMinimo < 0)
            throw new InvalidOperationException(
                "El stock mínimo no puede ser negativo.");

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

        var factorAplicado = factorConversionHistorico ??
            presentation.FactorConversion;
        if (factorAplicado <= 0)
            throw new InvalidOperationException(
                "El factor histórico de conversión debe ser mayor que cero.");
        var baseQuantity = ProductoNuevoRules.CalcularCantidadBase(
            request.Cantidad, factorAplicado);
        var unitCost = ProductoNuevoRules.CalcularCostoUnitarioBase(
            request.CostoTotal,
            baseQuantity);

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
        if (actualizarConfiguracionExistencia)
        {
            existence.Ubicacion = Normalize(request.Ubicacion);
            existence.StockMinimo = request.StockMinimo;
        }
        existence.UpdatedAt = now;

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
        var averageAfter = KONTAXPRO.Application.Inventory.InventoryCostRules
            .CalculateAverageAfterEntry(totalStockBefore, averageBefore,
                baseQuantity, request.CostoTotal);

        existence.StockActual += baseQuantity;
        existence.UpdatedAt = now;
        if (actualizarUltimoCostoEfectivo)
            cost.UltimoCostoEfectivo = unitCost;
        if (ultimoPrecioCompraUnitarioBase.HasValue)
            cost.UltimoPrecioCompra = ultimoPrecioCompraUnitarioBase.Value;
        cost.CostoPromedio = averageAfter;
        cost.UpdatedAt = now;

        var detail = new MovimientoInventarioDetalle
        {
            MovimientoInventario = movement,
            ProductoId = product.Id,
            ProductoPresentacionId = presentation.Id,
            CantidadPresentacion = request.Cantidad,
            FactorConversion = factorAplicado,
            CantidadBase = baseQuantity,
            CostoUnitarioBase = unitCost,
            CostoTotal = request.CostoTotal,
            StockAnterior = stockBefore,
            StockNuevo = existence.StockActual,
            CostoPromedioAnterior = averageBefore,
            CostoPromedioNuevo = averageAfter,
            EsBonificacion = esBonificacion,
            Observacion = Normalize(request.Observacion),
            CreatedAt = now,
            UpdatedAt = now
        };
        context.MovimientosInventarioDetalles.Add(detail);

        ProductoLote? lot = null;
        var lotsByNumber = new Dictionary<string, ProductoLote>(
            StringComparer.OrdinalIgnoreCase);
        if (product.ManejaLotes)
        {
            var lotes = request.Lotes.Count > 0
                ? request.Lotes
                :
                [
                    new IngresoInventarioLoteRequest
                    {
                        NumeroLote = request.NumeroLote ?? string.Empty,
                        CantidadBase = baseQuantity,
                        FechaElaboracion = request.FechaElaboracion,
                        FechaCaducidad = request.FechaCaducidad
                    }
                ];
            if (lotes.Any(x => string.IsNullOrWhiteSpace(x.NumeroLote) ||
                               x.CantidadBase <= 0))
                throw new InvalidOperationException(
                    $"Debe indicar número y cantidad válida para cada lote de '{product.Nombre}'.");
            if (lotes.Sum(x => x.CantidadBase) != baseQuantity)
                throw new InvalidOperationException(
                    "La suma de cantidades de los lotes debe coincidir con la cantidad base.");
            var numerosLote = lotes.Select(x => x.NumeroLote.Trim()
                    .ToUpperInvariant()).ToList();
            if (numerosLote.Count != numerosLote
                    .Distinct(StringComparer.OrdinalIgnoreCase).Count())
                throw new InvalidOperationException(
                    "Un lote no puede repetirse dentro de la misma entrada.");

            foreach (var loteRequest in lotes)
            {
                if (loteRequest.FechaElaboracion.HasValue &&
                    loteRequest.FechaCaducidad.HasValue &&
                    loteRequest.FechaElaboracion.Value >
                    loteRequest.FechaCaducidad.Value)
                    throw new InvalidOperationException(
                        $"La elaboración del lote '{loteRequest.NumeroLote}' no puede ser posterior a su caducidad.");
                var lotNumber = loteRequest.NumeroLote.Trim().ToUpperInvariant();
                var currentLot = await context.ProductosLotes
                    .SingleOrDefaultAsync(
                        x => x.ProductoId == product.Id &&
                             x.NumeroLote == lotNumber,
                         cancellationToken);
                if (currentLot is null)
                {
                    var claveProbable = AjusteInventarioRules
                        .NormalizarLoteComparable(lotNumber);
                    var loteSimilar = (await context.ProductosLotes
                            .AsNoTracking()
                            .Where(x => x.ProductoId == product.Id)
                            .Select(x => x.NumeroLote)
                            .ToListAsync(cancellationToken))
                        .FirstOrDefault(x =>
                            AjusteInventarioRules.NormalizarLoteComparable(x) ==
                            claveProbable);
                    if (loteSimilar is not null &&
                        !loteRequest.PermitirCrearLoteSimilar)
                        throw new InvalidOperationException(
                            $"Existe un lote posiblemente equivalente: {loteSimilar}. Selecciónelo o confirme expresamente la creación de '{lotNumber}'.");
                    if (product.ManejaFechaCaducidad &&
                        !loteRequest.FechaCaducidad.HasValue)
                        throw new InvalidOperationException(
                            $"La fecha de caducidad del lote nuevo '{lotNumber}' es obligatoria.");
                    currentLot = new ProductoLote
                    {
                        ProductoId = product.Id,
                        NumeroLote = lotNumber,
                        FechaElaboracion = loteRequest.FechaElaboracion,
                        FechaCaducidad = loteRequest.FechaCaducidad,
                        Estado = 1,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    context.ProductosLotes.Add(currentLot);
                    await context.SaveChangesAsync(cancellationToken);
                }
                else if (currentLot.FechaElaboracion !=
                             loteRequest.FechaElaboracion ||
                         currentLot.FechaCaducidad !=
                             loteRequest.FechaCaducidad)
                {
                    throw new InvalidOperationException(
                        $"El lote '{lotNumber}' ya existe con fechas diferentes.");
                }

                var lotStock = await context.ProductosLotesExistencias
                    .SingleOrDefaultAsync(
                        x => x.LoteId == currentLot.Id &&
                             x.BodegaId == movement.BodegaId,
                        cancellationToken);
                if (lotStock is null)
                {
                    lotStock = new ProductoLoteExistencia
                    {
                        LoteId = currentLot.Id,
                        BodegaId = movement.BodegaId,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    context.ProductosLotesExistencias.Add(lotStock);
                }

                var lotBefore = lotStock.StockActual;
                lotStock.StockActual += loteRequest.CantidadBase;
                lotStock.UpdatedAt = now;
                detail.Lotes.Add(new MovimientoInventarioDetalleLote
                {
                    ProductoLote = currentLot,
                    CantidadBase = loteRequest.CantidadBase,
                    StockLoteAnterior = lotBefore,
                    StockLoteNuevo = lotStock.StockActual,
                    CreatedAt = now
                });
                lotsByNumber[lotNumber] = currentLot;
                lot ??= currentLot;
            }
        }
        else if (!string.IsNullOrWhiteSpace(request.NumeroLote) ||
                 request.Lotes.Count > 0)
        {
            throw new InvalidOperationException(
                $"'{product.Nombre}' no maneja lotes.");
        }

        if (product.ManejaSeries)
        {
            var seriesInput = request.Series.Count > 0
                ? request.Series
                : request.NumerosSerie.Select(x =>
                    new IngresoInventarioSerieRequest
                    {
                        NumeroSerie = x
                    }).ToList();
            if (baseQuantity != decimal.Truncate(baseQuantity) ||
                seriesInput.Count != (int)baseQuantity)
                throw new InvalidOperationException(
                    $"Debe indicar una serie única por unidad de '{product.Nombre}'.");

            var normalized = seriesInput
                .Select(x => new IngresoInventarioSerieRequest
                {
                    NumeroSerie = x.NumeroSerie.Trim().ToUpperInvariant(),
                    NumeroLote = string.IsNullOrWhiteSpace(x.NumeroLote)
                        ? null
                        : x.NumeroLote.Trim().ToUpperInvariant()
                })
                .Where(x => x.NumeroSerie.Length > 0)
                .ToList();
            if (normalized.Count != (int)baseQuantity)
                throw new InvalidOperationException(
                    $"Debe indicar una serie válida por cada unidad de '{product.Nombre}'.");
            if (normalized.Count != normalized
                    .Select(x => x.NumeroSerie)
                    .Distinct(StringComparer.OrdinalIgnoreCase).Count())
                throw new InvalidOperationException(
                    "Los números de serie no pueden repetirse.");

            var availableState = await context.EstadosSerie
                .SingleAsync(x => x.Codigo == "DISPONIBLE" && x.Estado == 1,
                    cancellationToken);
            foreach (var serialInput in normalized)
            {
                var serialNumber = serialInput.NumeroSerie;
                ProductoLote? serialLot = lot;
                if (product.ManejaLotes)
                {
                    if (string.IsNullOrWhiteSpace(serialInput.NumeroLote) ||
                        !lotsByNumber.TryGetValue(
                            serialInput.NumeroLote, out serialLot))
                        throw new InvalidOperationException(
                            $"Debe asociar la serie '{serialNumber}' a un lote válido.");
                }
                var existingSerial = await context.ProductosSeries
                    .SingleOrDefaultAsync(x => x.ProductoId == product.Id &&
                        x.NumeroSerie == serialNumber, cancellationToken);
                ProductoSerie serial;
                var reuseKey = $"{product.Id}|{serialNumber}";
                if (existingSerial is not null)
                {
                    if (seriesReutilizables is null ||
                        !seriesReutilizables.TryGetValue(reuseKey, out var reusableId) ||
                        reusableId != existingSerial.Id)
                        throw new InvalidOperationException(
                            $"La serie '{serialNumber}' ya existe.");
                    serial = existingSerial;
                    serial.ProductoLote = serialLot;
                    serial.BodegaId = movement.BodegaId;
                    serial.EstadoSerieId = availableState.Id;
                    serial.Observacion = Normalize(request.Observacion);
                    serial.UpdatedAt = now;
                }
                else
                {
                    serial = new ProductoSerie
                    {
                        ProductoId = product.Id,
                        ProductoLote = serialLot,
                        BodegaId = movement.BodegaId,
                        NumeroSerie = serialNumber,
                        EstadoSerieId = availableState.Id,
                        Observacion = Normalize(request.Observacion),
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    context.ProductosSeries.Add(serial);
                }
                detail.Series.Add(new MovimientoInventarioDetalleSerie
                {
                    ProductoSerie = serial,
                    CreatedAt = now
                });
            }
        }
        else if (request.NumerosSerie.Count > 0 ||
                 request.Series.Count > 0)
        {
            throw new InvalidOperationException(
                $"'{product.Nombre}' no maneja series.");
        }
        // Centraliza la aplicación incremental: cuando un documento contiene
        // varias líneas del mismo producto, la siguiente línea consulta el
        // resultado confirmado por la anterior sin abandonar la transacción.
        await context.SaveChangesAsync(cancellationToken);
        return detail;
    }

    internal static async Task<ProductoPresentacion>
        AddAdjustmentOutputDetailAsync(
            KontaxDbContext context,
            MovimientoInventario movement,
            IngresoInventarioDetalleRequest request,
            DateTime now,
            CancellationToken cancellationToken)
    {
        if (request.Cantidad <= 0)
            throw new InvalidOperationException(
                "La cantidad de salida debe ser mayor que cero.");
        var presentacion = await context.ProductosPresentaciones
            .Include(x => x.Producto)
            .SingleOrDefaultAsync(x =>
                x.Id == request.ProductoPresentacionId &&
                x.ProductoId == request.ProductoId &&
                x.EmpresaId == movement.EmpresaId && x.Estado == 1,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "La presentación no pertenece al producto o a la empresa.");
        var producto = presentacion.Producto!;
        var cantidadBase = ProductoNuevoRules.CalcularCantidadBase(
            request.Cantidad, presentacion.FactorConversion);
        var existencia = await context.ProductosExistencias
            .SingleOrDefaultAsync(x => x.ProductoId == producto.Id &&
                x.BodegaId == movement.BodegaId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"'{producto.Nombre}' no posee existencia en la bodega.");
        var disponible = existencia.StockActual - existencia.StockReservado;
        if (disponible < cantidadBase)
            throw new InvalidOperationException(
                $"Stock disponible insuficiente para '{producto.Nombre}'. Disponible: {disponible:0.######}.");

        var costo = await context.ProductosCostos.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ProductoId == producto.Id,
                cancellationToken);
        var costoPromedio = costo?.CostoPromedio ?? 0;
        var stockAnterior = existencia.StockActual;
        existencia.StockActual -= cantidadBase;
        existencia.UpdatedAt = now;
        var detalle = new MovimientoInventarioDetalle
        {
            MovimientoInventario = movement,
            ProductoId = producto.Id,
            ProductoPresentacionId = presentacion.Id,
            CantidadPresentacion = request.Cantidad,
            FactorConversion = presentacion.FactorConversion,
            CantidadBase = cantidadBase,
            CostoUnitarioBase = costoPromedio,
            CostoTotal = costoPromedio * cantidadBase,
            StockAnterior = stockAnterior,
            StockNuevo = existencia.StockActual,
            CostoPromedioAnterior = costoPromedio,
            CostoPromedioNuevo = costoPromedio,
            EsBonificacion = false,
            Observacion = Normalize(request.Observacion),
            CreatedAt = now,
            UpdatedAt = now
        };
        context.MovimientosInventarioDetalles.Add(detalle);

        if (producto.ManejaLotes)
        {
            var solicitudes = request.Lotes.ToList();
            if (solicitudes.Count == 0)
            {
                var restante = cantidadBase;
                var candidatos = await context.ProductosLotesExistencias
                    .Include(x => x.Lote)
                    .Where(x => x.BodegaId == movement.BodegaId &&
                        x.Lote!.ProductoId == producto.Id &&
                        x.StockActual - x.StockReservado > 0)
                    .OrderBy(x => x.Lote!.FechaCaducidad == null)
                    .ThenBy(x => x.Lote!.FechaCaducidad)
                    .ThenBy(x => x.LoteId)
                    .ToListAsync(cancellationToken);
                foreach (var candidato in candidatos)
                {
                    if (restante <= 0) break;
                    var retirar = Math.Min(restante,
                        candidato.StockActual - candidato.StockReservado);
                    solicitudes.Add(new IngresoInventarioLoteRequest
                    {
                        NumeroLote = candidato.Lote!.NumeroLote,
                        CantidadBase = retirar
                    });
                    restante -= retirar;
                }
                if (restante > 0)
                    throw new InvalidOperationException(
                        "Los lotes disponibles no cubren la cantidad solicitada.");
            }
            if (solicitudes.Sum(x => x.CantidadBase) != cantidadBase)
                throw new InvalidOperationException(
                    "La suma de lotes debe coincidir con la cantidad base de salida.");
            foreach (var solicitud in solicitudes)
            {
                var numero = solicitud.NumeroLote.Trim().ToUpperInvariant();
                var loteExistencia = await context.ProductosLotesExistencias
                    .Include(x => x.Lote)
                    .SingleOrDefaultAsync(x => x.BodegaId == movement.BodegaId &&
                        x.Lote!.ProductoId == producto.Id &&
                        x.Lote.NumeroLote == numero, cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"El lote '{numero}' no existe en la bodega.");
                if (loteExistencia.StockActual - loteExistencia.StockReservado <
                    solicitud.CantidadBase)
                    throw new InvalidOperationException(
                        $"El lote '{numero}' no tiene stock disponible suficiente.");
                var anterior = loteExistencia.StockActual;
                loteExistencia.StockActual -= solicitud.CantidadBase;
                loteExistencia.UpdatedAt = now;
                detalle.Lotes.Add(new MovimientoInventarioDetalleLote
                {
                    ProductoLoteId = loteExistencia.LoteId,
                    CantidadBase = solicitud.CantidadBase,
                    StockLoteAnterior = anterior,
                    StockLoteNuevo = loteExistencia.StockActual,
                    CreatedAt = now
                });
            }
        }

        if (producto.ManejaSeries)
        {
            var numeros = request.Series.Count > 0
                ? request.Series.Select(x => x.NumeroSerie).ToList()
                : request.NumerosSerie;
            if (cantidadBase != decimal.Truncate(cantidadBase) ||
                numeros.Count != (int)cantidadBase)
                throw new InvalidOperationException(
                    "Debe seleccionar una serie por cada unidad base de salida.");
            var normalizados = numeros.Select(x => x.Trim().ToUpperInvariant()).ToList();
            if (normalizados.Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
                normalizados.Count)
                throw new InvalidOperationException("Las series no pueden repetirse.");
            var estadoBaja = await context.EstadosSerie.SingleAsync(
                x => x.Codigo == "BAJA" && x.Estado == 1, cancellationToken);
            var series = await context.ProductosSeries
                .Include(x => x.EstadoSerie)
                .Where(x => x.ProductoId == producto.Id &&
                    x.BodegaId == movement.BodegaId &&
                    normalizados.Contains(x.NumeroSerie))
                .ToListAsync(cancellationToken);
            if (series.Count != normalizados.Count ||
                series.Any(x => x.EstadoSerie!.Codigo != "DISPONIBLE"))
                throw new InvalidOperationException(
                    "Una o más series no existen, no pertenecen a la bodega o no están disponibles.");
            foreach (var serie in series)
            {
                serie.EstadoSerieId = estadoBaja.Id;
                serie.UpdatedAt = now;
                detalle.Series.Add(new MovimientoInventarioDetalleSerie
                {
                    ProductoSerieId = serie.Id,
                    CreatedAt = now
                });
            }
        }
        return presentacion;
    }

    internal static async Task<string> ObtenerSiguienteNumeroAsync(
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

    private static async Task ExigirPermisoAsync(
        KontaxDbContext context,
        long usuarioId,
        long empresaId,
        string permiso,
        CancellationToken cancellationToken)
    {
        var autorizado = await context.UsuariosEmpresasRoles.AsNoTracking()
            .AnyAsync(x =>
                x.UsuarioEmpresa!.UsuarioId == usuarioId &&
                x.UsuarioEmpresa.EmpresaId == empresaId &&
                x.UsuarioEmpresa.Estado == 1 &&
                x.Rol!.Estado == 1 &&
                x.Rol.RolesPermisos.Any(rp =>
                    rp.Permiso!.Codigo == permiso && rp.Permiso.Estado == 1),
                cancellationToken);
        if (!autorizado)
            throw new InvalidOperationException(
                $"El usuario no posee el permiso {permiso}.");
    }

    private static async Task ExigirAccesoEstablecimientoAsync(
        KontaxDbContext context,
        long usuarioId,
        long empresaId,
        long establecimientoId,
        CancellationToken cancellationToken)
    {
        var autorizado = await context.UsuariosEmpresasEstablecimientos
            .AsNoTracking().AnyAsync(x =>
                x.UsuarioEmpresa!.UsuarioId == usuarioId &&
                x.UsuarioEmpresa.EmpresaId == empresaId &&
                x.UsuarioEmpresa.Estado == 1 &&
                x.EstablecimientoId == establecimientoId,
                cancellationToken);
        if (!autorizado)
            throw new InvalidOperationException(
                "El usuario no tiene acceso al establecimiento de la bodega seleccionada.");
    }

    private static async Task<MotivoOperacionInventario>
        ObtenerMotivoValidoAsync(
            KontaxDbContext context,
            long motivoId,
            long empresaId,
            string tipoOperacion,
            CancellationToken cancellationToken)
    {
        return await context.MotivosOperacionInventario.SingleOrDefaultAsync(x =>
                   x.Id == motivoId && x.Estado == 1 &&
                   x.TipoOperacion == tipoOperacion &&
                   (x.EmpresaId == null || x.EmpresaId == empresaId),
                   cancellationToken)
               ?? throw new InvalidOperationException(
                   "El motivo no está activo o no corresponde a la empresa y operación seleccionadas.");
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime AsegurarUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime()
        };

    private static string ObtenerTipoControl(bool manejaLotes, bool manejaSeries) =>
        (manejaLotes, manejaSeries) switch
        {
            (true, true) => "LOTE_Y_SERIE",
            (true, false) => "LOTE",
            (false, true) => "SERIE",
            _ => "NORMAL"
        };
}
