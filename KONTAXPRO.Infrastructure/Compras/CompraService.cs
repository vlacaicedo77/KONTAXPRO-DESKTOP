using System.Data;
using KONTAXPRO.Application.Clientes;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Compras;
using KONTAXPRO.Application.Security;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Domain.Entities.Comercial;
using KONTAXPRO.Domain.Entities.Compras;
using KONTAXPRO.Domain.Entities.Seguridad;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Infrastructure.Compras;

public sealed class CompraService(
    IDbContextFactory<KontaxDbContext> dbContextFactory,
    CurrentSession currentSession,
    ImportacionCompraStore importStore,
    IArchivoCompraStorage archivoStorage)
    : ICompraService
{
    public async Task<CompraCatalogoDto> ObtenerCatalogoAsync(
        string? busqueda = null,
        string? estado = null,
        int pagina = 1,
        int tamanoPagina = 25,
        CompraCatalogoOrden orden = CompraCatalogoOrden.Fecha,
        bool ordenDescendente = true,
        CancellationToken cancellationToken = default)
    {
        if (!currentSession.IsAuthenticated ||
            !currentSession.EmpresaId.HasValue)
            return new CompraCatalogoDto();
        var companyId = currentSession.EmpresaId.Value;
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (!await HasPermissionAsync(context, companyId,
                ComprasPermissions.Ver, cancellationToken))
            return new CompraCatalogoDto();
        var query = context.Compras.AsNoTracking()
            .Where(x => x.EmpresaId == companyId);
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var pattern = $"%{busqueda.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.NumeroDocumento ?? string.Empty, pattern) ||
                EF.Functions.ILike(x.ProveedorRazonSocial, pattern) ||
                EF.Functions.ILike(x.ProveedorIdentificacion, pattern));
        }
        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(x => x.Estado == estado);
        else
            query = query.Where(x => x.Estado != "ANULADA");
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 1, 100);
        var totalFiltrado = await query.CountAsync(cancellationToken);
        var orderedQuery = (orden, ordenDescendente) switch
        {
            (CompraCatalogoOrden.Documento, false) => query
                .OrderBy(x => x.NumeroDocumento),
            (CompraCatalogoOrden.Documento, true) => query
                .OrderByDescending(x => x.NumeroDocumento),
            (CompraCatalogoOrden.Proveedor, false) => query
                .OrderBy(x => x.ProveedorRazonSocial),
            (CompraCatalogoOrden.Proveedor, true) => query
                .OrderByDescending(x => x.ProveedorRazonSocial),
            (CompraCatalogoOrden.Total, false) => query.OrderBy(x => x.Total),
            (CompraCatalogoOrden.Total, true) => query
                .OrderByDescending(x => x.Total),
            (CompraCatalogoOrden.Pendientes, false) => query.OrderBy(x =>
                x.Detalles.Count(d => d.EsInventariable &&
                    d.RecepcionesDetalles.Where(r =>
                        r.CompraRecepcion!.Estado == "CONFIRMADA")
                    .Sum(r => (decimal?)r.CantidadPresentacion) <
                    d.CantidadPresentacion)),
            (CompraCatalogoOrden.Pendientes, true) => query.OrderByDescending(x =>
                x.Detalles.Count(d => d.EsInventariable &&
                    d.RecepcionesDetalles.Where(r =>
                        r.CompraRecepcion!.Estado == "CONFIRMADA")
                    .Sum(r => (decimal?)r.CantidadPresentacion) <
                    d.CantidadPresentacion)),
            (CompraCatalogoOrden.Estado, false) => query.OrderBy(x => x.Estado),
            (CompraCatalogoOrden.Estado, true) => query
                .OrderByDescending(x => x.Estado),
            (CompraCatalogoOrden.Fecha, false) => query
                .OrderBy(x => x.FechaEmision),
            _ => query.OrderByDescending(x => x.FechaEmision)
        };
        var items = await orderedQuery.ThenByDescending(x => x.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(x => new CompraCatalogoItemDto
            {
                Id = x.Id,
                FechaEmision = x.FechaEmision,
                NumeroDocumento = x.NumeroDocumento ?? "SIN DOCUMENTO",
                Proveedor = x.ProveedorRazonSocial,
                IdentificacionProveedor = x.ProveedorIdentificacion,
                Total = x.Total,
                Estado = x.Estado,
                TieneXml = x.DocumentoRecibidoSriId != null,
                Origen = x.DocumentoRecibidoSriId != null ? "XML" : "MANUAL",
                LineasPendientes = x.Detalles.Count(d => d.EsInventariable &&
                    d.RecepcionesDetalles.Where(r =>
                        r.CompraRecepcion!.Estado == "CONFIRMADA")
                    .Sum(r => (decimal?)r.CantidadPresentacion) <
                    d.CantidadPresentacion),
                RecepcionesConfirmadas = x.Recepciones.Count(r =>
                    r.Estado == "CONFIRMADA"),
                PuedeEditar = x.DocumentoRecibidoSriId == null &&
                    x.Estado != "ANULADA" &&
                    !x.Recepciones.Any(r => r.Estado == "CONFIRMADA") &&
                    context.CuentasPorPagar.Any(debt =>
                        debt.EmpresaId == x.EmpresaId &&
                        debt.OrigenTipo == "COMPRA" &&
                        debt.OrigenId == x.Id &&
                        debt.Estado == "PENDIENTE" &&
                        debt.SaldoActual == debt.ValorOriginal &&
                        !debt.Movimientos.Any(m => m.Secuencia > 1))
            }).ToListAsync(cancellationToken);
        var all = context.Compras.AsNoTracking()
            .Where(x => x.EmpresaId == companyId && x.Estado != "ANULADA");
        return new CompraCatalogoDto
        {
            Items = items,
            Total = await all.CountAsync(cancellationToken),
            TotalFiltrado = totalFiltrado,
            PendientesRecepcion = await all.CountAsync(x =>
                x.Estado == "PENDIENTE_RECEPCION", cancellationToken),
            Parciales = await all.CountAsync(x =>
                x.Estado == "PARCIALMENTE_RECIBIDA", cancellationToken),
            Recibidas = await all.CountAsync(x =>
                x.Estado == "RECIBIDA", cancellationToken),
            TotalCompras = await all.SumAsync(x => (decimal?)x.Total,
                cancellationToken) ?? 0m
        };
    }

    public async Task<CompraDetalleDto?> ObtenerDetalleAsync(
        long compraId,
        CancellationToken cancellationToken = default)
    {
        if (!currentSession.EmpresaId.HasValue) return null;
        var companyId = currentSession.EmpresaId.Value;
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (!await HasPermissionAsync(context, companyId,
                ComprasPermissions.Ver, cancellationToken)) return null;
        return await context.Compras.AsNoTracking()
            .Where(x => x.Id == compraId && x.EmpresaId == companyId)
            .Select(x => new CompraDetalleDto
             {
                 Id = x.Id,
                 TipoCompra = x.TipoCompra,
                 NumeroDocumento = x.NumeroDocumento ?? "SIN DOCUMENTO",
                Proveedor = x.ProveedorRazonSocial,
                Estado = x.Estado,
                Total = x.Total,
                Lineas = x.Detalles.OrderBy(d => d.Orden).Select(d =>
                     new CompraDetalleLineaDto
                     {
                         Id = d.Id,
                         Orden = d.Orden,
                         ProductoId = d.ProductoId ?? 0,
                         Descripcion = d.Descripcion,
                         Producto = d.Producto == null ? null :
                             d.Producto.Nombre,
                         Presentacion = d.ProductoPresentacion == null ? null :
                             d.ProductoPresentacion.Nombre,
                        CantidadFacturada = d.CantidadPresentacion,
                        FactorConversion = d.FactorConversion,
                        CantidadRecibida = d.RecepcionesDetalles.Where(r =>
                                r.CompraRecepcion!.Estado == "CONFIRMADA")
                            .Sum(r => (decimal?)r.CantidadPresentacion) ?? 0m,
                        EsInventariable = d.EsInventariable,
                        ManejaLotes = d.Producto != null &&
                                      d.Producto.ManejaLotes,
                        ManejaSeries = d.Producto != null &&
                                       d.Producto.ManejaSeries,
                        ManejaFechaCaducidad = d.Producto != null &&
                                               d.Producto.ManejaFechaCaducidad
                    }).ToList()
            }).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CompraManualEdicionDto?> ObtenerManualParaEdicionAsync(
        long compraId, CancellationToken cancellationToken = default)
    {
        if (!currentSession.IsAuthenticated ||
            !currentSession.EmpresaId.HasValue || compraId <= 0)
            return null;
        var companyId = currentSession.EmpresaId.Value;
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (!await HasPermissionAsync(context, companyId,
                ComprasPermissions.Editar, cancellationToken))
            return null;
        return await context.Compras.AsNoTracking()
            .Where(x => x.Id == compraId && x.EmpresaId == companyId &&
                        x.DocumentoRecibidoSriId == null &&
                        x.Estado != "ANULADA" &&
                        !x.Recepciones.Any(r => r.Estado == "CONFIRMADA") &&
                        context.CuentasPorPagar.Any(debt =>
                            debt.EmpresaId == x.EmpresaId &&
                            debt.OrigenTipo == "COMPRA" &&
                            debt.OrigenId == x.Id &&
                            debt.Estado == "PENDIENTE" &&
                            debt.SaldoActual == debt.ValorOriginal &&
                            !debt.Movimientos.Any(m => m.Secuencia > 1)))
            .Select(x => new CompraManualEdicionDto
            {
                Id = x.Id,
                TerceroProveedorId = x.EmpresaTercero!.TerceroId,
                EstablecimientoId = x.EstablecimientoId,
                TipoComprobanteId = x.TipoComprobanteId,
                TipoCompra = x.TipoCompra,
                NumeroDocumento = x.NumeroDocumento,
                FechaEmision = x.FechaEmision,
                EsCredito = x.EsCredito,
                FechaVencimiento = x.FechaVencimiento,
                Observacion = x.Observacion,
                Lineas = x.Detalles.OrderBy(d => d.Orden).Select(d =>
                    new CompraManualEdicionLineaDto
                    {
                        Descripcion = d.Descripcion,
                        EsInventariable = d.EsInventariable,
                        ProductoPresentacionId = d.ProductoPresentacionId,
                        ClasificacionContable = d.ClasificacionContable,
                        CuentaContableId = d.EsInventariable
                            ? null : d.CuentaContableId,
                        CantidadPresentacion = d.CantidadPresentacion,
                        PrecioUnitario = d.PrecioUnitarioCompra,
                        DescuentoValor = d.DescuentoValor,
                        TarifaImpuestoId = d.Impuestos
                            .OrderBy(i => i.Id)
                            .Select(i => i.TarifaImpuestoId)
                            .FirstOrDefault(),
                        EsBonificacion = d.EsBonificacion
                    }).ToList()
            }).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CompraFormularioCatalogosDto>
        ObtenerCatalogosFormularioAsync(
            CancellationToken cancellationToken = default)
    {
        if (!currentSession.EmpresaId.HasValue)
            return new CompraFormularioCatalogosDto();
        var companyId = currentSession.EmpresaId.Value;
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (!await HasPermissionAsync(context, companyId,
                ComprasPermissions.Ver, cancellationToken))
            return new CompraFormularioCatalogosDto();
        return new CompraFormularioCatalogosDto
        {
            Proveedores = await context.Terceros.AsNoTracking()
                .Where(x => x.EsProveedor && x.EstadoProveedor == 1 &&
                            x.Estado == 1)
                .OrderBy(x => x.RazonSocial)
                .Select(x => new CompraProveedorItemDto
                {
                    Id = x.Id,
                    Identificacion = x.NumeroIdentificacion,
                    Nombre = x.RazonSocial
                }).Take(1000).ToListAsync(cancellationToken),
            Establecimientos = await context.Establecimientos.AsNoTracking()
                .Where(x => x.EmpresaId == companyId && x.Estado == 1)
                .OrderBy(x => x.Codigo)
                .Select(x => new CompraCatalogoItemBasicoDto
                {
                    Id = x.Id,
                    Codigo = x.Codigo,
                    Nombre = x.Nombre
                }).ToListAsync(cancellationToken),
            Bodegas = await context.Bodegas.AsNoTracking()
                .Where(x => x.Establecimiento!.EmpresaId == companyId &&
                            x.Estado == 1)
                .OrderBy(x => x.Codigo)
                .Select(x => new CompraCatalogoItemBasicoDto
                {
                    Id = x.Id,
                    Codigo = x.Codigo,
                    Nombre = x.Nombre,
                    PermiteVentaFacturada = x.PermiteVentaFacturada
                }).ToListAsync(cancellationToken),
            TiposComprobante = await context.TiposComprobante.AsNoTracking()
                .Where(x => x.Estado == 1)
                .OrderBy(x => x.CodigoSri)
                .Select(x => new CompraCatalogoItemBasicoDto
                {
                    Id = x.Id,
                    Codigo = x.CodigoSri,
                    Nombre = x.Nombre
                }).ToListAsync(cancellationToken),
            Presentaciones = await context.ProductosPresentaciones.AsNoTracking()
                .Where(x => x.EmpresaId == companyId && x.Estado == 1 &&
                    x.PermiteCompra && x.Producto!.Estado == 1 &&
                    x.Producto.ManejaInventario)
                .OrderBy(x => x.Producto!.Nombre).ThenBy(x => x.Nombre)
                .Select(x => new CompraPresentacionItemDto
                {
                    Id = x.Id,
                    ProductoId = x.ProductoId,
                     Codigo = x.Codigo,
                     Producto = x.Producto!.Nombre,
                     Presentacion = x.Nombre,
                     FactorConversion = x.FactorConversion,
                     EsPresentacionBase = x.EsPresentacionBase,
                     TarifaImpuestoId = x.Producto.Impuestos
                         .Where(i => i.Estado == 1 &&
                                     i.TarifaImpuesto!.Estado == 1 &&
                                     i.TarifaImpuesto.TipoCalculo == "PORCENTAJE")
                         .OrderBy(i => i.TarifaImpuestoId)
                         .Select(i => (long?)i.TarifaImpuestoId)
                         .FirstOrDefault(),
                     CodigoPorcentajeSri = x.Producto.Impuestos
                         .Where(i => i.Estado == 1 &&
                                     i.TarifaImpuesto!.Estado == 1 &&
                                     i.TarifaImpuesto.TipoCalculo == "PORCENTAJE")
                         .OrderBy(i => i.TarifaImpuestoId)
                         .Select(i => i.TarifaImpuesto!.CodigoSri)
                         .FirstOrDefault(),
                     PorcentajeImpuesto = x.Producto.Impuestos
                         .Where(i => i.Estado == 1 &&
                                     i.TarifaImpuesto!.Estado == 1 &&
                                     i.TarifaImpuesto.TipoCalculo == "PORCENTAJE")
                         .OrderBy(i => i.TarifaImpuestoId)
                         .Select(i => i.TarifaImpuesto!.Porcentaje)
                         .FirstOrDefault(),
                     NombreImpuesto = x.Producto.Impuestos
                         .Where(i => i.Estado == 1 &&
                                     i.TarifaImpuesto!.Estado == 1 &&
                                     i.TarifaImpuesto.TipoCalculo == "PORCENTAJE")
                         .OrderBy(i => i.TarifaImpuestoId)
                         .Select(i => i.TarifaImpuesto!.Nombre)
                         .FirstOrDefault()
                 }).Take(2000).ToListAsync(cancellationToken),
            TarifasImpuesto = await context.TarifasImpuesto.AsNoTracking()
                .Where(x => x.Estado == 1 && x.TipoCalculo == "PORCENTAJE" &&
                            x.Porcentaje.HasValue &&
                            x.Impuesto!.Estado == 1 && x.Impuesto.Codigo == "IVA")
                .OrderBy(x => x.Porcentaje)
                .Select(x => new CompraTarifaImpuestoItemDto
                {
                    Id = x.Id,
                    CodigoSri = x.CodigoSri,
                    Nombre = x.Nombre,
                    Porcentaje = x.Porcentaje!.Value
                }).ToListAsync(cancellationToken),
            CuentasContables = await context.PlanCuentas.AsNoTracking()
                .Where(x => x.EmpresaId == companyId && x.Estado == 1 &&
                    x.AceptaMovimientos && x.Naturaleza == "DEUDORA")
                .OrderBy(x => x.Codigo)
                .Select(x => new CompraCuentaContableItemDto
                {
                    Id = x.Id,
                    Codigo = x.Codigo,
                    Nombre = x.Nombre
                }).Take(2000).ToListAsync(cancellationToken)
        };
    }

    public async Task<CompraOperationResult> GuardarImportadaAsync(
        GuardarCompraImportadaRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentSession.IsAuthenticated ||
            !currentSession.EmpresaId.HasValue)
            return CompraOperationResult.Fail(
                "No existe una sesión empresarial activa.");
        var companyId = currentSession.EmpresaId.Value;
        if (!importStore.TryGet(request.ImportacionId,
                currentSession.UsuarioId, companyId, out var imported) ||
            imported is null)
            return CompraOperationResult.Fail(
                "La importación expiró o pertenece a otra sesión. Vuelve a cargar el XML.");

        var invoice = imported.Factura;
        var lineError = ValidateLines(invoice, request.Lineas);
        if (lineError is not null) return CompraOperationResult.Fail(lineError);
        var receiptError = ValidateImmediateReceipt(invoice, request.Lineas,
            request.RecepcionInmediata);
        if (receiptError is not null)
            return CompraOperationResult.Fail(receiptError);
        if (request.EsCredito && !request.FechaVencimiento.HasValue)
            return CompraOperationResult.Fail(
                "La fecha de vencimiento es obligatoria para una compra a crédito.");
        if (request.EsCredito &&
            request.FechaVencimiento < invoice.FechaEmision)
            return CompraOperationResult.Fail(
                "La fecha de vencimiento no puede ser anterior a la emisión.");

        ArchivoCompraGuardadoDto? stored = null;
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction =
            await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken);
        try
        {
            if (!await HasPermissionAsync(context, companyId,
                    ComprasPermissions.ImportarXml, cancellationToken) ||
                !await HasPermissionAsync(context, companyId,
                    ComprasPermissions.Crear, cancellationToken) ||
                (request.RecepcionInmediata is not null &&
                 !await HasPermissionAsync(context, companyId,
                     ComprasPermissions.Recibir, cancellationToken)))
                return CompraOperationResult.Fail(
                    "No tienes autorización para importar, crear o recibir esta compra.");
            if (currentSession.EmpresaId != companyId)
                return CompraOperationResult.Fail(
                    "La empresa activa cambió durante la importación.");
            if (await context.DocumentosRecibidosSri.AnyAsync(x =>
                    x.ClaveAcceso == invoice.ClaveAcceso ||
                    x.ArchivoSha256 == invoice.Sha256, cancellationToken))
                return CompraOperationResult.Fail(
                    "El XML ya fue registrado por otro usuario.");

            var establishment = await context.Establecimientos.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == request.EstablecimientoId &&
                    x.EmpresaId == companyId && x.Estado == 1,
                    cancellationToken);
            if (establishment is null)
                return CompraOperationResult.Fail(
                    "El establecimiento no está activo o no pertenece a la empresa.");
            var receiptWarehouse = request.RecepcionInmediata is null
                ? null
                : await context.Bodegas.Include(x => x.Establecimiento)
                    .SingleOrDefaultAsync(x =>
                        x.Id == request.RecepcionInmediata.BodegaId &&
                        x.Estado == 1 &&
                        x.EstablecimientoId == establishment.Id &&
                        x.Establecimiento!.EmpresaId == companyId,
                        cancellationToken);
            if (request.RecepcionInmediata is not null &&
                (receiptWarehouse is null ||
                 !receiptWarehouse.PermiteVentaFacturada))
                return CompraOperationResult.Fail(
                    "Selecciona una bodega facturable del establecimiento para recibir la mercadería.");
            var supplierIdentityKey = ClaveIdentidadTercero.Crear(
                "RUC", invoice.RucEmisor);
            var supplier = await context.Terceros.SingleOrDefaultAsync(x =>
                    x.Id == request.TerceroProveedorId && x.EsProveedor &&
                    x.EstadoProveedor == 1 && x.Estado == 1 &&
                    (x.ClaveIdentidad == supplierIdentityKey ||
                     x.Identificaciones.Any(i => i.Estado == 1 &&
                         i.NumeroNormalizado == invoice.RucEmisor &&
                         i.TipoIdentificacion!.Codigo == "RUC")),
                cancellationToken);
            if (supplier is null)
                return CompraOperationResult.Fail(
                    "Selecciona el proveedor activo que corresponde al RUC emisor del XML.");
            var companySupplier = await context.EmpresasTerceros
                .SingleOrDefaultAsync(x => x.EmpresaId == companyId &&
                    x.TerceroId == supplier.Id, cancellationToken);
            var now = DateTime.UtcNow;
            if (companySupplier is null)
            {
                companySupplier = new EmpresaTercero
                {
                    EmpresaId = companyId,
                    TerceroId = supplier.Id,
                    Estado = 1,
                    CreatedAt = now
                };
                context.EmpresasTerceros.Add(companySupplier);
            }
            else if (companySupplier.Estado != 1)
                return CompraOperationResult.Fail(
                    "La relación del proveedor con la empresa está inactiva.");

            var documentType = await context.TiposComprobante.AsNoTracking()
                .SingleOrDefaultAsync(x => x.CodigoSri == invoice.CodigoDocumento &&
                    x.Estado == 1, cancellationToken);
            if (documentType is null)
                return CompraOperationResult.Fail(
                    "El tipo de comprobante del XML no está habilitado.");

            var selectedPresentationIds = request.Lineas
                .Where(x => x.EsInventariable)
                .Select(x => x.ProductoPresentacionId!.Value)
                .Distinct().ToList();
            var presentations = await context.ProductosPresentaciones
                .Include(x => x.Producto)
                .Where(x => selectedPresentationIds.Contains(x.Id) &&
                    x.EmpresaId == companyId && x.Estado == 1 &&
                    x.PermiteCompra && x.Producto!.Estado == 1 &&
                    x.Producto.ManejaInventario)
                .ToDictionaryAsync(x => x.Id, cancellationToken);
            if (presentations.Count != selectedPresentationIds.Count)
                return CompraOperationResult.Fail(
                    "Una presentación inventariable dejó de estar disponible.");
            var inventoryAccountId = request.Lineas.Any(x => x.EsInventariable)
                ? await CompraAccountingProcessor.RequireInventoryAccountAsync(
                    context, companyId, cancellationToken)
                : 0;
            var nonInventoryAccounts = await LoadNonInventoryAccountsAsync(
                context, companyId, request.Lineas.Where(x => !x.EsInventariable)
                    .Select(x => x.CuentaContableId), cancellationToken);

            stored = await archivoStorage.GuardarXmlOriginalAsync(
                companyId, invoice.ClaveAcceso, invoice.Sha256,
                imported.Contenido, cancellationToken);
            var document = BuildDocument(invoice, companyId, supplier.Id,
                documentType.Id, stored, now);
            foreach (var payment in invoice.Pagos)
                document.PagosDeclarados.Add(new DocumentoRecibidoSriPago
                {
                    CodigoFormaPagoSri = payment.FormaPago,
                    Valor = payment.Total,
                    Plazo = payment.Plazo,
                    UnidadTiempo = Normalize(payment.UnidadTiempo),
                    CreatedAt = now
                });
            context.DocumentosRecibidosSri.Add(document);

            var purchase = new Compra
            {
                EmpresaId = companyId,
                EstablecimientoId = establishment.Id,
                EmpresaTercero = companySupplier,
                UsuarioId = currentSession.UsuarioId,
                DocumentoRecibidoSri = document,
                TipoCompra = "FACTURADA",
                TipoComprobanteId = documentType.Id,
                NumeroDocumento = invoice.NumeroDocumento,
                ProveedorIdentificacion = invoice.RucEmisor.Trim(),
                ProveedorRazonSocial = invoice.RazonSocialEmisor.Trim(),
                FechaEmision = invoice.FechaEmision,
                FechaIngreso = now,
                FechaVencimiento = request.EsCredito
                    ? request.FechaVencimiento : null,
                SubtotalSinImpuestos = invoice.TotalSinImpuestos,
                DescuentoTotal = invoice.TotalDescuento,
                Subtotal = invoice.TotalSinImpuestos,
                ImpuestoTotal = invoice.Impuestos.Sum(x => x.Valor),
                Total = invoice.ImporteTotal,
                EsCredito = request.EsCredito,
                Estado = request.Lineas.Any(x => x.EsInventariable)
                    ? "PENDIENTE_RECEPCION" : "RECIBIDA",
                Observacion = Normalize(request.Observacion),
                CreatedAt = now
            };
            BuildLines(purchase, invoice, request.Lineas, presentations,
                inventoryAccountId, nonInventoryAccounts, now);
            context.Compras.Add(purchase);
            await context.SaveChangesAsync(cancellationToken);
            await CompraAccountingProcessor.CreateAsync(context, purchase, now,
                cancellationToken);
            context.Auditorias.Add(new Auditoria
            {
                UsuarioId = currentSession.UsuarioId,
                EmpresaId = companyId,
                EstablecimientoId = establishment.Id,
                Accion = ComprasAuditActions.CompraCreada,
                Entidad = "compras",
                EntidadId = purchase.Id,
                Descripcion = $"Compra importada desde XML {invoice.NumeroDocumento}.",
                CreatedAt = now
            });
            await context.SaveChangesAsync(cancellationToken);
            CompraOperationResult? receiptResult = null;
            if (request.RecepcionInmediata is not null &&
                receiptWarehouse is not null)
            {
                var receiptRequest = new ConfirmarCompraRecepcionRequest
                {
                    OperacionUuid = request.RecepcionInmediata.OperacionUuid,
                    CompraId = purchase.Id,
                    BodegaId = receiptWarehouse.Id,
                    FechaRecepcion = request.RecepcionInmediata.FechaRecepcion,
                    Observacion = request.RecepcionInmediata.Observacion,
                    Lineas = request.RecepcionInmediata.Lineas.Select(input =>
                    {
                        var detail = purchase.Detalles.Single(x =>
                            x.Orden == input.Orden);
                        return new ConfirmarCompraRecepcionLineaRequest
                        {
                            CompraDetalleId = detail.Id,
                            CantidadPresentacion = input.CantidadPresentacion,
                            Lotes = input.Lotes,
                            Series = input.Series
                        };
                    }).ToList()
                };
                receiptResult = await CompraReceiptProcessor.ConfirmAsync(
                    context, purchase, receiptWarehouse, companyId,
                    currentSession.UsuarioId, receiptRequest, now,
                    cancellationToken);
                if (!receiptResult.Success)
                {
                    await RollbackAndDeleteStoredAsync(transaction, stored);
                    return receiptResult;
                }
            }
            if (currentSession.EmpresaId != companyId)
                throw new InvalidOperationException(
                    "La empresa activa cambió durante el guardado.");
            cancellationToken.ThrowIfCancellationRequested();
            await transaction.CommitAsync(CancellationToken.None);
            importStore.Remove(request.ImportacionId);
            return CompraOperationResult.Ok(purchase.Id,
                receiptResult is not null
                    ? "Compra y recepción confirmadas. Existencias, costos, Kardex, cuenta por pagar y contabilidad quedaron actualizados."
                    : purchase.Estado == "RECIBIDA"
                        ? "Compra guardada. No contiene mercadería pendiente."
                        : "Compra guardada para recibir la mercadería después.");
        }
        catch (OperationCanceledException)
        {
            await RollbackAndDeleteStoredAsync(transaction, stored);
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            await RollbackAndDeleteStoredAsync(transaction, stored);
            return CompraOperationResult.Conflict(ComprasConcurrency.UserMessage);
        }
        catch (DbUpdateException)
        {
            await RollbackAndDeleteStoredAsync(transaction, stored);
            return CompraOperationResult.Conflict(
                "La compra o el XML fueron registrados simultáneamente. Actualiza la lista.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await RollbackAndDeleteStoredAsync(transaction, stored);
            return CompraOperationResult.Fail(ex.Message);
        }
    }

    public async Task<CompraOperationResult> GuardarManualAsync(
        GuardarCompraManualRequest request,
        CancellationToken cancellationToken = default)
    {
        var type = request.TipoCompra.Trim().ToUpperInvariant();
        if (!currentSession.IsAuthenticated ||
            !currentSession.EmpresaId.HasValue)
            return CompraOperationResult.Fail(
                "No existe una sesión empresarial activa.");
        if (request.TerceroProveedorId <= 0 ||
            request.EstablecimientoId <= 0 || request.Lineas.Count == 0 ||
            type != "FACTURADA")
            return CompraOperationResult.Fail(
                "Proveedor, establecimiento, tipo y líneas son obligatorios.");
        if (type == "FACTURADA" &&
            (!request.TipoComprobanteId.HasValue ||
             string.IsNullOrWhiteSpace(request.NumeroDocumento) ||
             !IsValidSupplierInvoiceNumber(request.NumeroDocumento)))
            return CompraOperationResult.Fail(
                "Una compra facturada requiere una factura con número en formato 001-001-000000001.");
        if (request.EsCredito && !request.FechaVencimiento.HasValue)
            return CompraOperationResult.Fail(
                "La fecha de vencimiento es obligatoria para una compra a crédito.");
        if (request.EsCredito &&
            request.FechaVencimiento < request.FechaEmision)
            return CompraOperationResult.Fail(
                "La fecha de vencimiento no puede ser anterior a la emisión.");
        if (request.Lineas.Any(x => string.IsNullOrWhiteSpace(x.Descripcion) ||
            x.CantidadPresentacion <= 0 || x.PrecioUnitario < 0 ||
            x.DescuentoValor < 0 || !x.TarifaImpuestoId.HasValue ||
            x.DescuentoValor > x.CantidadPresentacion * x.PrecioUnitario ||
            x.EsInventariable != x.ProductoPresentacionId.HasValue ||
            (!x.EsInventariable && (!x.CuentaContableId.HasValue ||
                !IsValidNonInventoryClassification(
                    x.ClasificacionContable)))))
            return CompraOperationResult.Fail(
                "Revisa descripción, asociación, cantidades, precios, descuentos e impuestos de las líneas.");

        var companyId = currentSession.EmpresaId.Value;
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (!await HasPermissionAsync(context, companyId,
                    ComprasPermissions.Crear, cancellationToken))
                return CompraOperationResult.Fail(
                    "No tienes autorización para crear compras.");
            var supplier = await context.Terceros.SingleOrDefaultAsync(x =>
                x.Id == request.TerceroProveedorId && x.EsProveedor &&
                x.EstadoProveedor == 1 && x.Estado == 1, cancellationToken);
            if (supplier is null)
                return CompraOperationResult.Fail(
                    "El proveedor no existe o está inactivo.");
            var relation = await context.EmpresasTerceros.SingleOrDefaultAsync(
                x => x.EmpresaId == companyId && x.TerceroId == supplier.Id,
                cancellationToken);
            var now = DateTime.UtcNow;
            if (relation is null)
            {
                relation = new EmpresaTercero
                {
                    EmpresaId = companyId,
                    TerceroId = supplier.Id,
                    Estado = 1,
                    CreatedAt = now
                };
                context.EmpresasTerceros.Add(relation);
            }
            else if (relation.Estado != 1)
                return CompraOperationResult.Fail(
                    "La relación del proveedor con la empresa está inactiva.");
            if (!await context.Establecimientos.AsNoTracking().AnyAsync(x =>
                    x.Id == request.EstablecimientoId &&
                    x.EmpresaId == companyId && x.Estado == 1,
                    cancellationToken))
                return CompraOperationResult.Fail(
                    "El establecimiento no pertenece a la empresa.");
            if (request.TipoComprobanteId.HasValue)
            {
                var documentType = await context.TiposComprobante.AsNoTracking()
                    .Where(x => x.Id == request.TipoComprobanteId &&
                                x.Estado == 1)
                    .Select(x => x.CodigoSri)
                    .SingleOrDefaultAsync(cancellationToken);
                if (documentType is null)
                    return CompraOperationResult.Fail(
                        "El tipo de comprobante no está activo.");
                if (type == "FACTURADA" && documentType != "01")
                    return CompraOperationResult.Fail(
                        "Las compras facturadas deben registrarse con comprobante Factura (01).");
            }

            var presentationIds = request.Lineas.Where(x => x.EsInventariable)
                .Select(x => x.ProductoPresentacionId!.Value).Distinct()
                .ToList();
            var presentations = await context.ProductosPresentaciones
                .Include(x => x.Producto)
                    .ThenInclude(x => x!.Impuestos)
                .Where(x => presentationIds.Contains(x.Id) &&
                    x.EmpresaId == companyId && x.Estado == 1 &&
                    x.PermiteCompra && x.Producto!.Estado == 1 &&
                    x.Producto.ManejaInventario)
                .ToDictionaryAsync(x => x.Id, cancellationToken);
            if (presentations.Count != presentationIds.Count)
                return CompraOperationResult.Fail(
                    "Una presentación inventariable no está disponible.");
            var taxRateIds = request.Lineas
                .Select(x => x.TarifaImpuestoId!.Value).Distinct().ToList();
            var taxRates = await context.TarifasImpuesto.AsNoTracking()
                .Include(x => x.Impuesto)
                .Where(x => taxRateIds.Contains(x.Id) && x.Estado == 1 &&
                            x.TipoCalculo == "PORCENTAJE" &&
                            x.Porcentaje.HasValue && x.Impuesto!.Estado == 1 &&
                            x.Impuesto.Codigo == "IVA")
                .ToDictionaryAsync(x => x.Id, cancellationToken);
            if (taxRates.Count != taxRateIds.Count)
                return CompraOperationResult.Fail(
                    "Una tarifa de IVA seleccionada no está disponible.");
            if (request.Lineas.Where(x => x.EsInventariable).Any(input =>
                    !presentations[input.ProductoPresentacionId!.Value]
                        .Producto!.Impuestos.Any(x => x.Estado == 1 &&
                            x.TarifaImpuestoId == input.TarifaImpuestoId)))
                return CompraOperationResult.Fail(
                    "La tarifa de IVA de un producto no coincide con su configuración.");
            var inventoryAccountId = request.Lineas.Any(x => x.EsInventariable)
                ? await CompraAccountingProcessor.RequireInventoryAccountAsync(
                    context, companyId, cancellationToken)
                : 0;
            var nonInventoryAccounts = await LoadNonInventoryAccountsAsync(
                context, companyId, request.Lineas.Where(x => !x.EsInventariable)
                    .Select(x => x.CuentaContableId), cancellationToken);
            var subtotal = request.Lineas.Sum(x =>
                x.CantidadPresentacion * x.PrecioUnitario - x.DescuentoValor);
            var discount = request.Lineas.Sum(x => x.DescuentoValor);
            decimal CalculateTax(GuardarCompraManualLineaRequest line)
            {
                var taxableBase = Math.Max(0m,
                    line.CantidadPresentacion * line.PrecioUnitario -
                    line.DescuentoValor);
                var percentage = taxRates[line.TarifaImpuestoId!.Value]
                    .Porcentaje!.Value;
                return Math.Round(taxableBase * percentage / 100m, 2,
                    MidpointRounding.AwayFromZero);
            }
            var taxes = request.Lineas.Sum(CalculateTax);
            var purchase = new Compra
            {
                EmpresaId = companyId,
                EstablecimientoId = request.EstablecimientoId,
                EmpresaTercero = relation,
                UsuarioId = currentSession.UsuarioId,
                TipoCompra = type,
                TipoComprobanteId = request.TipoComprobanteId,
                NumeroDocumento = Normalize(request.NumeroDocumento),
                ProveedorIdentificacion = supplier.NumeroIdentificacion.Trim(),
                ProveedorRazonSocial = supplier.RazonSocial.Trim(),
                FechaEmision = request.FechaEmision,
                FechaIngreso = now,
                FechaVencimiento = request.EsCredito
                    ? request.FechaVencimiento : null,
                SubtotalSinImpuestos = subtotal,
                DescuentoTotal = discount,
                Subtotal = subtotal,
                ImpuestoTotal = taxes,
                Total = subtotal + taxes,
                EsCredito = request.EsCredito,
                Estado = request.Lineas.Any(x => x.EsInventariable)
                    ? "PENDIENTE_RECEPCION" : "RECIBIDA",
                Observacion = Normalize(request.Observacion),
                CreatedAt = now
            };
            var order = 0;
            foreach (var input in request.Lineas)
            {
                order++;
                presentations.TryGetValue(input.ProductoPresentacionId ?? 0,
                    out var presentation);
                var factor = presentation?.FactorConversion ?? 1m;
                var baseQuantity = input.CantidadPresentacion * factor;
                var lineSubtotal = input.CantidadPresentacion *
                                   input.PrecioUnitario - input.DescuentoValor;
                var taxRate = taxRates[input.TarifaImpuestoId!.Value];
                var lineTax = CalculateTax(input);
                var detail = new CompraDetalle
                {
                    EmpresaId = companyId,
                    Orden = order,
                    EstadoReconocimiento = input.EsInventariable
                        ? "RECONOCIDA" : "NO_INVENTARIABLE",
                    EsInventariable = input.EsInventariable,
                    ClasificacionContable = input.EsInventariable
                        ? "INVENTARIO"
                        : NormalizeClassification(input.ClasificacionContable),
                    CuentaContableId = input.EsInventariable
                        ? inventoryAccountId
                        : nonInventoryAccounts[input.CuentaContableId!.Value],
                    ProductoId = presentation?.ProductoId,
                    ProductoPresentacionId = presentation?.Id,
                    Descripcion = input.Descripcion.Trim(),
                    CantidadPresentacion = input.CantidadPresentacion,
                    FactorConversion = factor,
                    CantidadBase = baseQuantity,
                    PrecioUnitarioCompra = input.PrecioUnitario,
                    DescuentoPorcentaje = input.CantidadPresentacion *
                        input.PrecioUnitario == 0 ? 0 : input.DescuentoValor /
                        (input.CantidadPresentacion * input.PrecioUnitario) * 100m,
                    DescuentoValor = input.DescuentoValor,
                    PrecioTotalSinImpuesto = lineSubtotal,
                    CostoTotalLinea = lineSubtotal,
                    CostoUnitarioBase = baseQuantity == 0 ? 0 :
                        lineSubtotal / baseQuantity,
                    EsBonificacion = input.EsBonificacion,
                    CreatedAt = now
                };
                detail.Impuestos.Add(new CompraDetalleImpuesto
                {
                    TarifaImpuestoId = taxRate.Id,
                    CodigoImpuestoSri = taxRate.Impuesto!.CodigoSri,
                    CodigoPorcentajeSri = taxRate.CodigoSri,
                    NombreImpuesto = taxRate.Nombre,
                    TipoCalculo = taxRate.TipoCalculo,
                    Porcentaje = taxRate.Porcentaje,
                    ValorEspecifico = taxRate.ValorEspecifico,
                    BaseImponible = lineSubtotal,
                    ValorImpuesto = lineTax,
                    CreatedAt = now
                });
                purchase.Detalles.Add(detail);
            }
            context.Compras.Add(purchase);
            await context.SaveChangesAsync(cancellationToken);
            await CompraAccountingProcessor.CreateAsync(context, purchase, now,
                cancellationToken);
            context.Auditorias.Add(new Auditoria
            {
                UsuarioId = currentSession.UsuarioId,
                EmpresaId = companyId,
                EstablecimientoId = request.EstablecimientoId,
                Accion = ComprasAuditActions.CompraCreada,
                Entidad = "compras",
                EntidadId = purchase.Id,
                Descripcion = $"Compra manual {purchase.NumeroDocumento ?? purchase.Id.ToString()}.",
                CreatedAt = now
            });
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CompraOperationResult.Ok(purchase.Id,
                "Compra manual guardada sin afectar inventario.");
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return CompraOperationResult.Conflict(
                "Ya existe una compra con ese comprobante o los datos cambiaron simultáneamente.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return CompraOperationResult.Fail(ex.Message);
        }
    }

    private static bool IsValidSupplierInvoiceNumber(string? value)
    {
        var parts = value?.Trim().Split('-') ?? [];
        return parts.Length == 3 && parts[0].Length == 3 &&
               parts[1].Length == 3 && parts[2].Length == 9 &&
               parts.All(part => part.All(char.IsDigit));
    }

    public async Task<CompraOperationResult> AnularAsync(
        long compraId,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        if (!currentSession.IsAuthenticated ||
            !currentSession.EmpresaId.HasValue || compraId <= 0 ||
            string.IsNullOrWhiteSpace(motivo))
            return CompraOperationResult.Fail(
                "Compra y motivo de anulación son obligatorios.");
        var companyId = currentSession.EmpresaId.Value;
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database
            .BeginTransactionAsync(cancellationToken);
        try
        {
            if (!await HasPermissionAsync(context, companyId,
                    ComprasPermissions.Anular, cancellationToken))
                return CompraOperationResult.Fail(
                    "No tienes autorización para anular compras.");
            var purchase = await context.Compras.FromSqlInterpolated(
                    $"SELECT c.*, c.xmin FROM s_compras.compras AS c WHERE c.id = {compraId} AND c.empresa_id = {companyId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (purchase is null)
                return CompraOperationResult.Fail("No se encontró la compra.");
            if (purchase.Estado == "ANULADA")
                return CompraOperationResult.Ok(purchase.Id,
                    "La compra ya estaba anulada.");
            if (await context.ComprasRecepciones.AnyAsync(x =>
                    x.CompraId == purchase.Id &&
                    x.Estado == "CONFIRMADA", cancellationToken))
                return CompraOperationResult.Fail(
                    "La compra tiene recepciones confirmadas. Primero debe existir un flujo formal de reverso de inventario; no se anulará silenciosamente.");
            var now = DateTime.UtcNow;
            purchase.Estado = "ANULADA";
            purchase.MotivoAnulacion = motivo.Trim();
            purchase.AnuladoPorUsuarioId = currentSession.UsuarioId;
            purchase.AnuladaAt = now;
            purchase.UpdatedAt = now;
            if (purchase.DocumentoRecibidoSriId.HasValue)
            {
                var document = await context.DocumentosRecibidosSri
                    .SingleAsync(x => x.Id == purchase.DocumentoRecibidoSriId,
                        cancellationToken);
                document.EstadoProcesamiento = "NO_APLICA";
                document.UpdatedAt = now;
            }
            await CompraAccountingProcessor.ReverseAsync(context, purchase,
                motivo.Trim(), now, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            context.Auditorias.Add(new Auditoria
            {
                UsuarioId = currentSession.UsuarioId,
                EmpresaId = companyId,
                EstablecimientoId = purchase.EstablecimientoId,
                Accion = ComprasAuditActions.CompraAnulada,
                Entidad = "compras",
                EntidadId = purchase.Id,
                Descripcion = $"Compra anulada. Motivo: {motivo.Trim()}",
                CreatedAt = now
            });
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CompraOperationResult.Ok(purchase.Id,
                "Compra anulada sin eliminar su historial.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return CompraOperationResult.Conflict(ComprasConcurrency.UserMessage);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return CompraOperationResult.Fail(ex.Message);
        }
    }

    public async Task<CompraOperationResult> SustituirManualAsync(
        long compraId,
        GuardarCompraManualRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateManualRequest(request);
        if (validation is not null)
            return CompraOperationResult.Fail(validation);
        if (!currentSession.IsAuthenticated ||
            !currentSession.EmpresaId.HasValue || compraId <= 0)
            return CompraOperationResult.Fail(
                "No existe una sesión empresarial activa.");

        var companyId = currentSession.EmpresaId.Value;
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable,
                cancellationToken);
        try
        {
            if (!await HasPermissionAsync(context, companyId,
                    ComprasPermissions.Editar, cancellationToken))
                return CompraOperationResult.Fail(
                    "No tienes autorización para corregir compras.");
            var original = await context.Compras.FromSqlInterpolated(
                    $"SELECT c.*, c.xmin FROM s_compras.compras AS c WHERE c.id = {compraId} AND c.empresa_id = {companyId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (original is null)
                return CompraOperationResult.Fail("No se encontró la compra.");
            if (original.DocumentoRecibidoSriId.HasValue)
                return CompraOperationResult.Fail(
                    "Una compra importada desde XML es un documento recibido inmutable y no puede sustituirse manualmente.");
            if (original.Estado == "ANULADA")
                return CompraOperationResult.Fail(
                    "La compra ya está anulada.");
            if (await context.ComprasRecepciones.AnyAsync(x =>
                    x.CompraId == original.Id && x.Estado == "CONFIRMADA",
                    cancellationToken))
                return CompraOperationResult.Fail(
                    "La compra tiene recepciones confirmadas. Revierte primero la recepción para corregir el documento.");
            if (original.CompraSustituta is not null ||
                await context.Compras.AnyAsync(x =>
                    x.CompraSustituidaId == original.Id, cancellationToken))
                return CompraOperationResult.Fail(
                    "La compra ya tiene una versión sustituta.");

            var now = DateTime.UtcNow;
            var reason = "Sustitución controlada por corrección de compra manual.";
            original.Estado = "ANULADA";
            original.AnuladoPorUsuarioId = currentSession.UsuarioId;
            original.AnuladaAt = now;
            original.MotivoAnulacion = reason;
            original.UpdatedAt = now;
            await CompraAccountingProcessor.ReverseAsync(context, original,
                reason, now, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var replacement = await CompraManualProcessor.CreateAsync(context,
                request, companyId, currentSession.UsuarioId, now,
                cancellationToken);
            replacement.CompraSustituidaId = original.Id;
            context.Auditorias.Add(new Auditoria
            {
                UsuarioId = currentSession.UsuarioId,
                EmpresaId = companyId,
                EstablecimientoId = replacement.EstablecimientoId,
                Accion = ComprasAuditActions.CompraSustituida,
                Entidad = "compras",
                EntidadId = replacement.Id,
                Descripcion =
                    $"Compra {original.NumeroDocumento ?? original.Id.ToString()} sustituida por la compra {replacement.NumeroDocumento ?? replacement.Id.ToString()} (Id {replacement.Id}).",
                CreatedAt = now
            });
            if (currentSession.EmpresaId != companyId)
                throw new InvalidOperationException(
                    "La empresa activa cambió durante la corrección.");
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CompraOperationResult.Ok(replacement.Id,
                "Compra corregida mediante sustitución. La versión anterior y sus reversos permanecen en el historial.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return CompraOperationResult.Conflict(ComprasConcurrency.UserMessage);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return CompraOperationResult.Conflict(
                "El comprobante ya existe o la compra cambió simultáneamente. Recarga la lista.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return CompraOperationResult.Fail(ex.Message);
        }
    }

    private static void BuildLines(
        Compra purchase,
        FacturaCompraXmlDto invoice,
        IReadOnlyList<GuardarLineaCompraImportadaRequest> choices,
        IReadOnlyDictionary<long, Domain.Entities.Inventario.ProductoPresentacion> presentations,
        long inventoryAccountId,
        IReadOnlyDictionary<long, long> nonInventoryAccounts,
        DateTime now)
    {
        foreach (var xmlLine in invoice.Detalles.OrderBy(x => x.Orden))
        {
            var choice = choices.Single(x => x.Orden == xmlLine.Orden);
            presentations.TryGetValue(choice.ProductoPresentacionId ?? 0,
                out var presentation);
            var factor = presentation?.FactorConversion ?? 1m;
            var baseQuantity = xmlLine.Cantidad * factor;
            var detail = new CompraDetalle
            {
                EmpresaId = purchase.EmpresaId,
                Orden = xmlLine.Orden,
                CodigoPrincipalProveedor = Normalize(xmlLine.CodigoPrincipal),
                CodigoAuxiliarProveedor = Normalize(xmlLine.CodigoAuxiliar),
                EstadoReconocimiento = choice.EsInventariable
                    ? "RECONOCIDA" : "NO_INVENTARIABLE",
                EsInventariable = choice.EsInventariable,
                ClasificacionContable = choice.EsInventariable
                    ? "INVENTARIO"
                    : NormalizeClassification(choice.ClasificacionContable),
                CuentaContableId = choice.EsInventariable
                    ? inventoryAccountId
                    : nonInventoryAccounts[choice.CuentaContableId!.Value],
                ProductoId = presentation?.ProductoId,
                ProductoPresentacionId = presentation?.Id,
                Descripcion = xmlLine.Descripcion.Trim(),
                CantidadPresentacion = xmlLine.Cantidad,
                FactorConversion = factor,
                CantidadBase = baseQuantity,
                PrecioUnitarioCompra = xmlLine.PrecioUnitario,
                DescuentoPorcentaje = xmlLine.Cantidad * xmlLine.PrecioUnitario == 0
                    ? 0 : xmlLine.Descuento /
                        (xmlLine.Cantidad * xmlLine.PrecioUnitario) * 100m,
                DescuentoValor = xmlLine.Descuento,
                PrecioTotalSinImpuesto = xmlLine.PrecioTotalSinImpuesto,
                CostoTotalLinea = xmlLine.PrecioTotalSinImpuesto,
                CostoUnitarioBase = baseQuantity == 0 ? 0 :
                    xmlLine.PrecioTotalSinImpuesto / baseQuantity,
                EsBonificacion = choice.EsBonificacion,
                CreatedAt = now
            };
            foreach (var tax in xmlLine.Impuestos)
                detail.Impuestos.Add(new CompraDetalleImpuesto
                {
                    CodigoImpuestoSri = tax.Codigo,
                    CodigoPorcentajeSri = tax.CodigoPorcentaje,
                    NombreImpuesto = tax.Codigo == "2" ? "IVA" : "IMPUESTO",
                    TipoCalculo = tax.Tarifa.HasValue ? "PORCENTAJE" : "NINGUNO",
                    Porcentaje = tax.Tarifa,
                    BaseImponible = tax.BaseImponible,
                    ValorImpuesto = tax.Valor,
                    CreatedAt = now
                });
            purchase.Detalles.Add(detail);
        }
    }

    private static DocumentoRecibidoSri BuildDocument(
        FacturaCompraXmlDto x, long companyId, long supplierId,
        long documentTypeId, ArchivoCompraGuardadoDto file, DateTime now) => new()
    {
        EmpresaId = companyId,
        TerceroId = supplierId,
        TipoComprobanteId = documentTypeId,
        NumeroDocumento = x.NumeroDocumento,
        ClaveAcceso = x.ClaveAcceso,
        FechaEmision = x.FechaEmision,
        FechaAutorizacion = x.FechaAutorizacion,
        Ambiente = x.Ambiente,
        TipoEmision = x.TipoEmision,
        RucEmisor = x.RucEmisor,
        RazonSocialEmisor = x.RazonSocialEmisor,
        NombreComercialEmisor = x.NombreComercialEmisor,
        DireccionMatriz = x.DireccionMatriz,
        DireccionEstablecimiento = x.DireccionEstablecimiento,
        EstablecimientoCodigo = x.Establecimiento,
        PuntoEmisionCodigo = x.PuntoEmision,
        Secuencial = x.Secuencial,
        IdentificacionReceptor = x.IdentificacionComprador,
        RazonSocialReceptor = x.RazonSocialComprador,
        ValorSinImpuestos = x.TotalSinImpuestos,
        Iva = x.Impuestos.Where(i => i.Codigo == "2").Sum(i => i.Valor),
        Propina = x.Propina,
        ImporteTotal = x.ImporteTotal,
        Moneda = x.Moneda,
        FirmaPresente = x.FirmaPresente,
        EstadoValidacion = x.EstadoValidacion,
        ArchivoRutaRelativa = file.RutaRelativa,
        ArchivoSha256 = file.Sha256,
        ArchivoTamano = file.Tamano,
        EstadoProcesamiento = "PROCESADO",
        XmlObtenidoAt = now,
        CreatedAt = now
    };

    private static string? ValidateManualRequest(
        GuardarCompraManualRequest request)
    {
        var type = request.TipoCompra.Trim().ToUpperInvariant();
        if (request.TerceroProveedorId <= 0 ||
            request.EstablecimientoId <= 0 || request.Lineas.Count == 0 ||
            type != "FACTURADA")
            return "Proveedor, establecimiento, tipo y líneas son obligatorios.";
        if (type == "FACTURADA" &&
            (!request.TipoComprobanteId.HasValue ||
             string.IsNullOrWhiteSpace(request.NumeroDocumento) ||
             !IsValidSupplierInvoiceNumber(request.NumeroDocumento)))
            return "Una compra facturada requiere una factura con número en formato 001-001-000000001.";
        if (request.EsCredito && !request.FechaVencimiento.HasValue)
            return "La fecha de vencimiento es obligatoria para una compra a crédito.";
        if (request.EsCredito && request.FechaVencimiento < request.FechaEmision)
            return "La fecha de vencimiento no puede ser anterior a la emisión.";
        if (request.Lineas.Any(x => string.IsNullOrWhiteSpace(x.Descripcion) ||
            x.CantidadPresentacion <= 0 || x.PrecioUnitario < 0 ||
            x.DescuentoValor < 0 || !x.TarifaImpuestoId.HasValue ||
            x.DescuentoValor > x.CantidadPresentacion * x.PrecioUnitario ||
            x.EsInventariable != x.ProductoPresentacionId.HasValue ||
            (!x.EsInventariable && (!x.CuentaContableId.HasValue ||
                !IsValidNonInventoryClassification(
                    x.ClasificacionContable)))))
            return "Revisa descripción, asociación, cantidades, precios, descuentos e impuestos de las líneas.";
        return null;
    }

    private static string? ValidateLines(
        FacturaCompraXmlDto invoice,
        IReadOnlyList<GuardarLineaCompraImportadaRequest> lines)
    {
        if (lines.Count != invoice.Detalles.Count ||
            lines.Select(x => x.Orden).Distinct().Count() != lines.Count ||
            invoice.Detalles.Any(x => lines.All(y => y.Orden != x.Orden)))
            return "Todas las líneas del XML deben clasificarse exactamente una vez.";
        if (lines.Any(x => x.EsInventariable !=
                           x.ProductoPresentacionId.HasValue))
            return "Cada línea inventariable debe tener una presentación confirmada; las demás no deben asociarla.";
        if (lines.Any(x => !x.EsInventariable &&
            (!x.CuentaContableId.HasValue ||
             !IsValidNonInventoryClassification(x.ClasificacionContable))))
            return "Cada línea no inventariable requiere clasificación y cuenta contable.";
        return null;
    }

    private static string? ValidateImmediateReceipt(
        FacturaCompraXmlDto invoice,
        IReadOnlyList<GuardarLineaCompraImportadaRequest> lines,
        GuardarCompraRecepcionInmediataRequest? receipt)
    {
        if (receipt is null) return null;
        if (receipt.OperacionUuid == Guid.Empty || receipt.BodegaId <= 0)
            return "La recepción inmediata requiere bodega e identificador de operación.";
        var inventoryOrders = lines.Where(x => x.EsInventariable)
            .Select(x => x.Orden).OrderBy(x => x).ToList();
        var receiptOrders = receipt.Lineas.Select(x => x.Orden)
            .OrderBy(x => x).ToList();
        if (inventoryOrders.Count == 0)
            return "La compra no contiene mercadería para recibir.";
        if (!inventoryOrders.SequenceEqual(receiptOrders) ||
            receipt.Lineas.Select(x => x.Orden).Distinct().Count() !=
            receipt.Lineas.Count)
            return "La recepción inmediata debe incluir una vez cada línea inventariable.";
        foreach (var input in receipt.Lineas)
        {
            var xmlLine = invoice.Detalles.Single(x => x.Orden == input.Orden);
            if (input.CantidadPresentacion <= 0 ||
                Math.Abs(input.CantidadPresentacion - xmlLine.Cantidad) >
                0.000001m)
                return "La recepción inmediata debe confirmar la cantidad completa facturada.";
        }
        return null;
    }

    private static async Task<IReadOnlyDictionary<long, long>>
        LoadNonInventoryAccountsAsync(
            KontaxDbContext context,
            long companyId,
            IEnumerable<long?> requestedIds,
            CancellationToken cancellationToken)
    {
        var ids = requestedIds.Where(x => x.HasValue).Select(x => x!.Value)
            .Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<long, long>();
        var valid = await context.PlanCuentas.AsNoTracking()
            .Where(x => ids.Contains(x.Id) && x.EmpresaId == companyId &&
                x.Estado == 1 && x.AceptaMovimientos &&
                x.Naturaleza == "DEUDORA")
            .Select(x => x.Id).ToListAsync(cancellationToken);
        if (valid.Count != ids.Count)
            throw new InvalidOperationException(
                "Una cuenta contable seleccionada no está activa, no acepta movimientos o no pertenece a la empresa.");
        return valid.ToDictionary(x => x);
    }

    private static bool IsValidNonInventoryClassification(string? value) =>
        NormalizeClassification(value) is "GASTO" or "ACTIVO" or "OTRO";

    private static string NormalizeClassification(string? value) =>
        value?.Trim().ToUpperInvariant() ?? string.Empty;

    private async Task<bool> HasPermissionAsync(
        KontaxDbContext context, long companyId, string permission,
        CancellationToken cancellationToken) =>
        await context.UsuariosEmpresasRoles.AsNoTracking().AnyAsync(x =>
            x.UsuarioEmpresa!.UsuarioId == currentSession.UsuarioId &&
            x.UsuarioEmpresa.EmpresaId == companyId &&
            x.UsuarioEmpresa.Estado == 1 && x.Rol!.Estado == 1 &&
            (x.Rol.Codigo == "ADMINISTRADOR" ||
             x.Rol.RolesPermisos.Any(rp => rp.Permiso!.Codigo == permission &&
                                           rp.Permiso.Estado == 1)),
            cancellationToken);

    private async Task DeleteStoredAsync(ArchivoCompraGuardadoDto? stored)
    {
        if (stored is null) return;
        try { await archivoStorage.EliminarAsync(stored.RutaRelativa); }
        catch { /* La operación principal ya falló; la limpieza se diagnostica aparte. */ }
    }

    private async Task RollbackAndDeleteStoredAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        ArchivoCompraGuardadoDto? stored)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        finally
        {
            await DeleteStoredAsync(stored);
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
