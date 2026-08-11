using System.Data;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Inventario;
using KONTAXPRO.Application.Models.Tesoreria;
using KONTAXPRO.Application.Tesoreria;
using KONTAXPRO.Domain.Entities.Bancos;
using KONTAXPRO.Domain.Entities.Contabilidad;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Domain.Entities.Seguridad;
using KONTAXPRO.Domain.Entities.Tesoreria;
using KONTAXPRO.Infrastructure.Compras;
using KONTAXPRO.Infrastructure.Inventory;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Infrastructure.Tesoreria;

public sealed partial class OperacionSinSustentoService(
    IDbContextFactory<KontaxDbContext> dbContextFactory,
    ISoporteSinSustentoStorage soporteStorage)
    : IOperacionSinSustentoService
{
    public async Task<OperacionSinSustentoCatalogosDto> ObtenerCatalogosAsync(
        long empresaId,
        long establecimientoId,
        long usuarioId,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await RequirePermissionAsync(context, usuarioId, empresaId,
            establecimientoId, "TESORERIA_REGISTRAR_SIN_SUSTENTO",
            cancellationToken);
        var expenseRootCode = await context.ConfiguracionCuentas.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Estado == 1 &&
                x.TipoConfiguracionContable!.Codigo == "GASTOS_NO_DEDUCIBLES" &&
                x.TipoConfiguracionContable.Estado == 1 &&
                x.CuentaContable!.Estado == 1)
            .Select(x => x.CuentaContable!.Codigo)
            .SingleOrDefaultAsync(cancellationToken);
        return new OperacionSinSustentoCatalogosDto
        {
            CajasAbiertas = await context.CajasSesiones.AsNoTracking()
                .Where(x => x.Estado == "ABIERTA" && x.Caja!.Estado == 1 &&
                    x.Caja.EmpresaId == empresaId &&
                    x.Caja.EstablecimientoId == establecimientoId)
                .OrderBy(x => x.Caja!.Nombre)
                .Select(x => new FondoSalidaDto(x.Id,
                    $"{x.Caja!.Codigo} · {x.Caja.Nombre}"))
                .ToListAsync(cancellationToken),
            CuentasBancarias = await context.CuentasBancarias.AsNoTracking()
                .Where(x => x.EmpresaId == empresaId && x.Estado == 1)
                .OrderBy(x => x.Nombre)
                .Select(x => new FondoSalidaDto(x.Id,
                    $"{x.Banco} · {x.Nombre} · {x.NumeroCuenta}"))
                .ToListAsync(cancellationToken),
            CuentasGasto = await context.PlanCuentas.AsNoTracking()
                .Where(x => x.EmpresaId == empresaId && x.Estado == 1 &&
                    x.AceptaMovimientos && x.Naturaleza == "DEUDORA" &&
                    expenseRootCode != null && x.Codigo.StartsWith(expenseRootCode))
                .OrderBy(x => x.Codigo)
                .Select(x => new CuentaGastoDto(x.Id, x.Codigo, x.Nombre))
                .ToListAsync(cancellationToken),
            BodegasNoFacturables = await context.Bodegas.AsNoTracking()
                .Where(x => x.Estado == 1 && !x.PermiteVentaFacturada &&
                    x.EstablecimientoId == establecimientoId &&
                    x.Establecimiento!.EmpresaId == empresaId)
                .OrderBy(x => x.Codigo)
                .Select(x => new FondoSalidaDto(x.Id,
                    $"{x.Codigo} · {x.Nombre}"))
                .ToListAsync(cancellationToken)
        };
    }

    public async Task<OperacionSinSustentoResult> RegistrarAsync(
        OperacionSinSustentoRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = OperacionSinSustentoRules.Validate(request);
        if (validation is not null)
            return OperacionSinSustentoResult.Fail(validation);

        SoporteSinSustentoGuardadoDto? stored = null;
        try
        {
            if (request.EvidenciaContenido is { Length: > 0 })
                stored = await soporteStorage.GuardarAsync(request.EmpresaId,
                    request.EvidenciaNombre ?? string.Empty,
                    request.EvidenciaContenido, cancellationToken);

            await using var context =
                await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction =
                await context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, cancellationToken);
            try
            {
                var result = await RegisterCoreAsync(context, request, stored,
                    "TESORERIA_REGISTRAR_SIN_SUSTENTO", null,
                    cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (InvalidOperationException ex)
        {
            await DeleteStoredAsync(stored);
            return OperacionSinSustentoResult.Fail(ex.Message);
        }
        catch (DbUpdateException)
        {
            await DeleteStoredAsync(stored);
            return OperacionSinSustentoResult.Fail(
                "La operación no pudo confirmarse por un conflicto de integridad o concurrencia.");
        }
        catch (OperationCanceledException)
        {
            await DeleteStoredAsync(stored);
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await DeleteStoredAsync(stored);
            return OperacionSinSustentoResult.Fail(
                $"No fue posible registrar la operación. {ex.Message}");
        }
    }

    private static async Task<OperacionSinSustentoResult> RegisterCoreAsync(
        KontaxDbContext context,
        OperacionSinSustentoRequest request,
        SoporteSinSustentoGuardadoDto? stored,
        string permissionCode,
        IReadOnlyDictionary<string, long>? reusableSeries,
        CancellationToken cancellationToken)
    {
        var type = request.TipoOperacion.Trim().ToUpperInvariant();
        var medium = request.MedioSalida.Trim().ToUpperInvariant();
        await RequirePermissionAsync(context, request.UsuarioId,
            request.EmpresaId, request.EstablecimientoId, permissionCode,
            cancellationToken);
        if (!await context.Establecimientos.AsNoTracking().AnyAsync(x =>
                x.Id == request.EstablecimientoId &&
                x.EmpresaId == request.EmpresaId && x.Estado == 1,
                cancellationToken))
            throw new InvalidOperationException(
                "El establecimiento no pertenece a la empresa activa.");

        var now = DateTime.UtcNow;
        var total = request.Detalles.Sum(x => decimal.Round(x.CostoTotal, 2,
            MidpointRounding.AwayFromZero));
        var number = await InventoryService.ObtenerSiguienteNumeroAsync(context,
            request.EmpresaId, request.EstablecimientoId,
            "OPERACION_SIN_SUSTENTO", cancellationToken);
        var sourceAccountId = await ResolveSourceAccountAsync(context, request,
            medium, cancellationToken);
        var debitAccounts = new Dictionary<long, decimal>();
        MovimientoInventario? inventoryMovement = null;
        var inventoryDetails = new List<MovimientoInventarioDetalle>();

        if (type == "INVENTARIO")
        {
            await PrepareContextualProductsAsync(context, request, now,
                cancellationToken);
            var warehouseId = request.BodegaId!.Value;
            if (!await context.Bodegas.AsNoTracking().AnyAsync(x =>
                    x.Id == warehouseId && x.Estado == 1 &&
                    x.EstablecimientoId == request.EstablecimientoId &&
                    x.Establecimiento!.EmpresaId == request.EmpresaId &&
                    !x.PermiteVentaFacturada, cancellationToken))
                throw new InvalidOperationException(
                    "La bodega debe pertenecer al establecimiento y estar reservada para inventario no facturable.");
            var movementType = await context.TiposMovimientoInventario
                .SingleAsync(x => x.Codigo == "ADQUISICION_SIN_SUSTENTO" &&
                    x.Estado == 1, cancellationToken);
            var originType = await context.TiposOrigenMovimientoInventario
                .SingleAsync(x => x.Codigo == "OPERACION_SIN_SUSTENTO" &&
                    x.Estado == 1, cancellationToken);
            inventoryMovement = new MovimientoInventario
            {
                EmpresaId = request.EmpresaId,
                NumeroMovimiento = number,
                TipoMovimientoId = movementType.Id,
                FechaMovimiento = ToUtc(request.Fecha),
                BodegaId = warehouseId,
                OrigenTipoId = originType.Id,
                OrigenId = 0,
                NumeroDocumento = number,
                Referencia = Normalize(request.Referencia),
                Observacion = request.Motivo.Trim(),
                UsuarioId = request.UsuarioId,
                Estado = "CONFIRMADO",
                CreatedAt = now,
                UpdatedAt = now
            };
            context.MovimientosInventario.Add(inventoryMovement);
            await context.SaveChangesAsync(cancellationToken);
            foreach (var detail in request.Detalles)
            {
                var inventoryDetail = await InventoryService.AddInitialDetailAsync(
                    context, inventoryMovement,
                    new IngresoInventarioDetalleRequest
                    {
                        ProductoId = detail.ProductoId!.Value,
                        ProductoPresentacionId = detail.ProductoPresentacionId!.Value,
                        Cantidad = detail.Cantidad,
                        CostoTotal = decimal.Round(detail.CostoTotal, 2,
                            MidpointRounding.AwayFromZero),
                        Ubicacion = detail.Ubicacion,
                        StockMinimo = detail.StockMinimo,
                        Lotes = detail.Lotes,
                        Series = detail.Series,
                        Observacion = detail.Descripcion
                    }, now, cancellationToken,
                    seriesReutilizables: reusableSeries);
                inventoryDetails.Add(inventoryDetail);
                // El siguiente detalle del mismo producto debe leer el stock y
                // costo ya acumulados por esta línea dentro de la transacción.
                await context.SaveChangesAsync(cancellationToken);
            }
            await ApplyPendingPricesAsync(context, request, now,
                cancellationToken);
            var inventoryAccount = await CompraAccountingProcessor
                .RequireInventoryAccountAsync(context, request.EmpresaId,
                    cancellationToken);
            debitAccounts[inventoryAccount] = total;
        }
        else
        {
            var expenseRootCode = await context.ConfiguracionCuentas.AsNoTracking()
                .Where(x => x.EmpresaId == request.EmpresaId && x.Estado == 1 &&
                    x.TipoConfiguracionContable!.Codigo == "GASTOS_NO_DEDUCIBLES" &&
                    x.TipoConfiguracionContable.Estado == 1 &&
                    x.CuentaContable!.Estado == 1)
                .Select(x => x.CuentaContable!.Codigo)
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException(
                    "Configura la cuenta contable GASTOS_NO_DEDUCIBLES antes de registrar egresos sin comprobante.");
            var requestedAccounts = request.Detalles.Select(x =>
                x.CuentaContableId!.Value).Distinct().ToArray();
            var validAccounts = await context.PlanCuentas.AsNoTracking()
                .Where(x => requestedAccounts.Contains(x.Id) &&
                    x.EmpresaId == request.EmpresaId && x.Estado == 1 &&
                    x.AceptaMovimientos && x.Naturaleza == "DEUDORA" &&
                    x.Codigo.StartsWith(expenseRootCode))
                .Select(x => x.Id).ToListAsync(cancellationToken);
            if (validAccounts.Count != requestedAccounts.Length)
                throw new InvalidOperationException(
                    "Una cuenta de gasto no pertenece a la empresa, está inactiva o no acepta movimientos.");
            foreach (var detail in request.Detalles)
                debitAccounts[detail.CuentaContableId!.Value] =
                    debitAccounts.GetValueOrDefault(detail.CuentaContableId.Value) +
                    decimal.Round(detail.CostoTotal, 2,
                        MidpointRounding.AwayFromZero);
        }
        if (debitAccounts.ContainsKey(sourceAccountId))
            throw new InvalidOperationException(
                "La cuenta de salida no puede ser también la cuenta de destino del egreso.");

        var period = await CompraAccountingProcessor.RequireOpenPeriodAsync(
            context, request.EmpresaId, request.Fecha, cancellationToken);
        var accountingOrigin = await context.TiposOrigenAsiento.SingleAsync(x =>
            x.Codigo == "OPERACION_SIN_SUSTENTO" && x.Estado == 1,
            cancellationToken);
        var entry = new Asiento
        {
            EmpresaId = request.EmpresaId,
            PeriodoId = period.Id,
            UsuarioId = request.UsuarioId,
            NumeroAsiento = await CompraAccountingProcessor.NextEntryNumberAsync(
                context, request.EmpresaId, request.Fecha.Year, now,
                cancellationToken),
            Fecha = request.Fecha,
            TipoAsiento = "AUTOMATICO",
            TipoOrigenAsientoId = accountingOrigin.Id,
            OrigenId = 0,
            Concepto = $"{(type == "GASTO" ? "Egreso" : "Adquisición de inventario")} sin sustento tributario {number}. {request.Motivo.Trim()}",
            Estado = "CONTABILIZADO",
            CreatedAt = now
        };
        var order = 0;
        foreach (var debit in debitAccounts.OrderBy(x => x.Key))
            entry.Detalles.Add(new AsientoDetalle
            {
                EmpresaId = request.EmpresaId,
                CuentaContableId = debit.Key,
                Orden = ++order,
                Descripcion = "Valor no deducible sin comprobante tributario",
                Debe = debit.Value,
                Haber = 0,
                CreatedAt = now
            });
        entry.Detalles.Add(new AsientoDetalle
        {
            EmpresaId = request.EmpresaId,
            CuentaContableId = sourceAccountId,
            Orden = ++order,
            Descripcion = medium == "CAJA" ? "Salida de caja" : "Salida bancaria",
            Debe = 0,
            Haber = total,
            CreatedAt = now
        });
        if (!entry.EstaBalanceado())
            throw new InvalidOperationException(
                "El asiento automático de la operación no está balanceado.");
        context.Asientos.Add(entry);
        await context.SaveChangesAsync(cancellationToken);

        var cashMovement = medium == "CAJA"
            ? await CreateCashMovementAsync(context, request, total, now,
                cancellationToken) : null;
        var bankMovement = medium == "BANCO"
            ? await CreateBankMovementAsync(context, request, total, now,
                cancellationToken) : null;
        await context.SaveChangesAsync(cancellationToken);

        var operation = new OperacionSinSustento
        {
            EmpresaId = request.EmpresaId,
            EstablecimientoId = request.EstablecimientoId,
            UsuarioId = request.UsuarioId,
            NumeroOperacion = number,
            TipoOperacion = type,
            MedioSalida = medium,
            CajaSesionId = request.CajaSesionId,
            CuentaBancariaId = request.CuentaBancariaId,
            BodegaId = request.BodegaId,
            Fecha = request.Fecha,
            Beneficiario = request.Beneficiario.Trim(),
            Motivo = request.Motivo.Trim(),
            Referencia = Normalize(request.Referencia),
            EvidenciaRutaRelativa = stored?.RutaRelativa,
            EvidenciaNombre = stored is null ? null : Path.GetFileName(request.EvidenciaNombre),
            EvidenciaSha256 = stored?.Sha256,
            EvidenciaTamano = stored?.Tamano,
            Total = total,
            EsDeducible = false,
            Estado = "CONFIRMADO",
            MovimientoCajaId = cashMovement?.Id,
            MovimientoBancarioId = bankMovement?.Id,
            MovimientoInventarioId = inventoryMovement?.Id,
            AsientoId = entry.Id,
            OperacionSustituidaId = request.OperacionSustituidaId,
            CreatedAt = now,
            UpdatedAt = now
        };
        if (type == "GASTO")
            foreach (var detail in request.Detalles)
                operation.Detalles.Add(new OperacionSinSustentoDetalle
                {
                    EmpresaId = request.EmpresaId,
                    CuentaContableId = detail.CuentaContableId,
                    Descripcion = detail.Descripcion.Trim(),
                    CantidadPresentacion = 1,
                    FactorConversion = 1,
                    CantidadBase = 1,
                    CostoUnitarioBase = decimal.Round(detail.CostoTotal, 2,
                        MidpointRounding.AwayFromZero),
                    CostoTotal = decimal.Round(detail.CostoTotal, 2,
                        MidpointRounding.AwayFromZero),
                    CreatedAt = now
                });
        else
            for (var i = 0; i < request.Detalles.Count; i++)
            {
                var source = request.Detalles[i];
                var movementDetail = inventoryDetails[i];
                operation.Detalles.Add(new OperacionSinSustentoDetalle
                {
                    EmpresaId = request.EmpresaId,
                    ProductoId = source.ProductoId,
                    ProductoPresentacionId = source.ProductoPresentacionId,
                    Descripcion = source.Descripcion.Trim(),
                    CantidadPresentacion = movementDetail.CantidadPresentacion,
                    FactorConversion = movementDetail.FactorConversion,
                    CantidadBase = movementDetail.CantidadBase,
                    CostoUnitarioBase = movementDetail.CostoUnitarioBase,
                    CostoTotal = movementDetail.CostoTotal,
                    CreatedAt = now
                });
            }
        context.OperacionesSinSustento.Add(operation);
        await context.SaveChangesAsync(cancellationToken);
        entry.OrigenId = operation.Id;
        if (cashMovement is not null) cashMovement.OrigenId = operation.Id;
        if (bankMovement is not null) bankMovement.OrigenId = operation.Id;
        if (inventoryMovement is not null) inventoryMovement.OrigenId = operation.Id;
        context.Auditorias.Add(new Auditoria
        {
            UsuarioId = request.UsuarioId,
            EmpresaId = request.EmpresaId,
            EstablecimientoId = request.EstablecimientoId,
            Accion = "REGISTRAR_OPERACION_SIN_SUSTENTO",
            Entidad = "operaciones_sin_sustento",
            EntidadId = operation.Id,
            Descripcion = $"TIPO={type}; MEDIO={medium}; TOTAL={total:F2}; NO_DEDUCIBLE=SI; MOTIVO={operation.Motivo}",
            CreatedAt = now
        });
        await context.SaveChangesAsync(cancellationToken);
        return OperacionSinSustentoResult.Ok(operation.Id,
            $"Operación {number} registrada como no deducible y sin crédito tributario.");
    }

    private static async Task<long> ResolveSourceAccountAsync(
        KontaxDbContext context, OperacionSinSustentoRequest request,
        string medium, CancellationToken cancellationToken)
    {
        if (medium == "CAJA")
        {
            var session = await context.CajasSesiones.AsNoTracking()
                .Include(x => x.Caja).SingleOrDefaultAsync(x =>
                    x.Id == request.CajaSesionId && x.Estado == "ABIERTA" &&
                    x.Caja!.Estado == 1 && x.Caja.EmpresaId == request.EmpresaId &&
                    x.Caja.EstablecimientoId == request.EstablecimientoId,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "Selecciona una caja abierta del establecimiento activo.");
            return session.Caja!.CuentaContableId;
        }
        var bank = await context.CuentasBancarias.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.CuentaBancariaId &&
                x.EmpresaId == request.EmpresaId && x.Estado == 1,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Selecciona una cuenta bancaria activa de la empresa.");
        return bank.CuentaContableId;
    }

    private static async Task ApplyPendingPricesAsync(
        KontaxDbContext context,
        OperacionSinSustentoRequest request,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var requestedPrices = request.Detalles
            .Where(x => x.Precios.Count > 0)
            .SelectMany(detail => detail.Precios.Select(price => new
            {
                ProductId = detail.ProductoId!.Value,
                Price = price
            }))
            .ToList();
        if (requestedPrices.Count == 0) return;

        if (request.Detalles.Any(x => x.Precios.Count > 0 &&
                !x.ProductoCreadoContextualmente))
            throw new InvalidOperationException(
                "Los precios solo pueden prepararse para productos creados dentro de esta operación.");
        await RequirePermissionAsync(context, request.UsuarioId,
            request.EmpresaId, request.EstablecimientoId,
            "PRODUCTOS_CONFIGURAR_PRECIOS", cancellationToken);

        var presentationIds = requestedPrices
            .Select(x => x.Price.ProductoPresentacionId)
            .Distinct().ToArray();
        var presentations = await context.ProductosPresentaciones
            .Where(x => presentationIds.Contains(x.Id) &&
                        x.EmpresaId == request.EmpresaId && x.Estado == 1)
            .Select(x => new { x.Id, x.ProductoId })
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (presentations.Count != presentationIds.Length ||
            requestedPrices.Any(x =>
                !presentations.TryGetValue(
                    x.Price.ProductoPresentacionId, out var presentation) ||
                presentation.ProductoId != x.ProductId))
            throw new InvalidOperationException(
                "Una presentación de los precios pendientes no pertenece al producto creado.");

        var listIds = requestedPrices.Select(x => x.Price.ListaPrecioId)
            .Distinct().ToArray();
        var priceLists = await context.ListasPrecio.AsNoTracking()
            .Where(x => listIds.Contains(x.Id) &&
                        x.EmpresaId == request.EmpresaId && x.Estado == 1)
            .Select(x => new { x.Id, x.EsListaBase })
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (priceLists.Count != listIds.Length)
            throw new InvalidOperationException(
                "Una lista de los precios pendientes no pertenece a la empresa activa.");

        var changedPrices = new List<(ProductoPresentacionPrecio Price,
            string Before)>();
        foreach (var item in requestedPrices)
        {
            var input = item.Price;
            var list = priceLists[input.ListaPrecioId];
            var validMethod = list.EsListaBase
                ? input.MetodoCalculo is "PORCENTAJE_COSTO" or "PRECIO_FIJO"
                : input.MetodoCalculo is "DESCUENTO_PORCENTAJE" or "PRECIO_FIJO";
            if (!validMethod)
                throw new InvalidOperationException(
                    "El método de cálculo no corresponde al tipo de lista de precios.");
            if (input.MetodoCalculo == "PRECIO_FIJO" &&
                (!input.Precio.HasValue || input.Precio.Value <= 0) ||
                input.MetodoCalculo != "PRECIO_FIJO" &&
                (!input.Porcentaje.HasValue || input.Porcentaje.Value < 0))
                throw new InvalidOperationException(
                    "Un precio pendiente contiene un valor o porcentaje inválido.");

            var price = await context.ProductosPresentacionesPrecios
                .SingleOrDefaultAsync(x =>
                    x.ProductoPresentacionId == input.ProductoPresentacionId &&
                    x.ListaPrecioId == input.ListaPrecioId,
                    cancellationToken);
            var before = price is null
                ? "NUEVO"
                : $"METODO={price.MetodoCalculo};PORCENTAJE={price.Porcentaje};PRECIO={price.Precio};ESTADO={price.Estado}";
            price ??= new ProductoPresentacionPrecio
            {
                ProductoPresentacionId = input.ProductoPresentacionId,
                ListaPrecioId = input.ListaPrecioId,
                CreatedAt = now
            };
            if (price.Id == 0)
                context.ProductosPresentacionesPrecios.Add(price);
            price.MetodoCalculo = input.MetodoCalculo;
            price.Porcentaje = input.MetodoCalculo == "PRECIO_FIJO"
                ? null : input.Porcentaje;
            price.Precio = input.MetodoCalculo == "PRECIO_FIJO"
                ? decimal.Round(input.Precio!.Value, 2,
                    MidpointRounding.AwayFromZero)
                : null;
            price.Estado = 1;
            price.UpdatedAt = now;
            changedPrices.Add((price, before));
        }
        await context.SaveChangesAsync(cancellationToken);
        foreach (var changed in changedPrices)
            context.Auditorias.Add(new Auditoria
            {
                UsuarioId = request.UsuarioId,
                EmpresaId = request.EmpresaId,
                EstablecimientoId = request.EstablecimientoId,
                Accion = "CONFIGURAR_PRECIO_DESDE_OPERACION_SIN_SUSTENTO",
                Entidad = "productos_presentaciones_precios",
                EntidadId = changed.Price.Id,
                Descripcion = $"ANTES={changed.Before};DESPUES=METODO={changed.Price.MetodoCalculo};PORCENTAJE={changed.Price.Porcentaje};PRECIO={changed.Price.Precio};ESTADO=1",
                CreatedAt = now
            });
    }

    private static async Task PrepareContextualProductsAsync(
        KontaxDbContext context, OperacionSinSustentoRequest request,
        DateTime now, CancellationToken cancellationToken)
    {
        var draftIds = request.Detalles
            .Where(x => x.ProductoCreadoContextualmente)
            .Select(x => x.ProductoId!.Value).Distinct().ToArray();
        if (draftIds.Length == 0) return;

        var drafts = await context.Productos.Where(x =>
                draftIds.Contains(x.Id) && x.EmpresaId == request.EmpresaId)
            .ToListAsync(cancellationToken);
        if (drafts.Count != draftIds.Length || drafts.Any(x => x.Estado != 0) ||
            await context.MovimientosInventarioDetalles.AsNoTracking().AnyAsync(x =>
                draftIds.Contains(x.ProductoId) &&
                x.MovimientoInventario!.Estado == "CONFIRMADO", cancellationToken))
            throw new InvalidOperationException(
                "Un producto contextual ya no es un borrador limpio. Vuelve a seleccionarlo o créalo nuevamente.");

        foreach (var product in drafts)
        {
            product.Estado = 1;
            product.UpdatedAt = now;
            context.Auditorias.Add(new Auditoria
            {
                UsuarioId = request.UsuarioId,
                EmpresaId = request.EmpresaId,
                EstablecimientoId = request.EstablecimientoId,
                Accion = "ACTIVAR_PRODUCTO_DESDE_OPERACION_SIN_SUSTENTO",
                Entidad = "productos",
                EntidadId = product.Id,
                Descripcion = "Borrador contextual activado al confirmar inventario y precios.",
                CreatedAt = now
            });
        }
    }

    private static async Task<MovimientoCaja> CreateCashMovementAsync(
        KontaxDbContext context, OperacionSinSustentoRequest request,
        decimal total, DateTime now, CancellationToken cancellationToken)
    {
        var movementType = await context.TiposMovimientoCaja.SingleAsync(x =>
            x.Codigo == "EGRESO" && x.Estado == 1, cancellationToken);
        var movement = new MovimientoCaja
        {
            CajaSesionId = request.CajaSesionId!.Value,
            UsuarioId = request.UsuarioId,
            TipoMovimientoCajaId = movementType.Id,
            OrigenTipo = "OPERACION_SIN_SUSTENTO",
            OrigenId = 0,
            FechaMovimiento = ToUtc(request.Fecha),
            Valor = total,
            Concepto = request.Motivo.Trim(),
            Estado = "CONFIRMADO",
            CreatedAt = now
        };
        context.MovimientosCaja.Add(movement);
        return movement;
    }

    private static async Task<MovimientoBancario> CreateBankMovementAsync(
        KontaxDbContext context, OperacionSinSustentoRequest request,
        decimal total, DateTime now, CancellationToken cancellationToken)
    {
        var movementType = await context.TiposMovimientoBancario.SingleAsync(x =>
            x.Codigo == "RETIRO" && x.Estado == 1, cancellationToken);
        var movement = new MovimientoBancario
        {
            CuentaBancariaId = request.CuentaBancariaId!.Value,
            UsuarioId = request.UsuarioId,
            TipoMovimientoBancarioId = movementType.Id,
            OrigenTipo = "OPERACION_SIN_SUSTENTO",
            OrigenId = 0,
            FechaMovimiento = ToUtc(request.Fecha),
            Valor = total,
            Referencia = Normalize(request.Referencia),
            Concepto = request.Motivo.Trim(),
            Estado = "CONFIRMADO",
            CreatedAt = now
        };
        context.MovimientosBancarios.Add(movement);
        return movement;
    }

    private static async Task RequirePermissionAsync(KontaxDbContext context,
        long userId, long companyId, long establishmentId,
        string permissionCode, CancellationToken cancellationToken)
    {
        var companyUserId = await context.UsuariosEmpresas.AsNoTracking()
            .Where(x => x.UsuarioId == userId && x.EmpresaId == companyId &&
                x.Estado == 1)
            .Select(x => (long?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (!companyUserId.HasValue ||
            !await context.UsuariosEmpresasRoles.AsNoTracking().AnyAsync(x =>
                x.UsuarioEmpresa!.UsuarioId == userId &&
                x.UsuarioEmpresa.EmpresaId == companyId &&
                x.UsuarioEmpresa.Estado == 1 && x.Rol!.Estado == 1 &&
                x.Rol.RolesPermisos.Any(rp => rp.Permiso!.Estado == 1 &&
                    rp.Permiso.Codigo == permissionCode), cancellationToken) ||
            !await context.UsuariosEmpresasEstablecimientos.AsNoTracking()
                .AnyAsync(x => x.UsuarioEmpresaId == companyUserId.Value &&
                    x.EstablecimientoId == establishmentId, cancellationToken))
            throw new InvalidOperationException(
                "No tienes permiso para esta operación en el establecimiento activo.");
    }

    private async Task DeleteStoredAsync(SoporteSinSustentoGuardadoDto? stored)
    {
        if (stored is null) return;
        try { await soporteStorage.EliminarAsync(stored.RutaRelativa); }
        catch { /* La operación principal ya falló; la limpieza es compensatoria. */ }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime ToUtc(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(12, 0)), DateTimeKind.Utc);
}
