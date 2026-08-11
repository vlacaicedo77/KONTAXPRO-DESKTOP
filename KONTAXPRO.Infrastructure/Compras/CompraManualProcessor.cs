using KONTAXPRO.Application.Models.Compras;
using KONTAXPRO.Domain.Entities.Comercial;
using KONTAXPRO.Domain.Entities.Compras;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Infrastructure.Compras;

internal static class CompraManualProcessor
{
    internal static async Task<Compra> CreateAsync(
        KontaxDbContext context,
        GuardarCompraManualRequest request,
        long companyId,
        long userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var type = request.TipoCompra.Trim().ToUpperInvariant();
        if (type != "FACTURADA")
            throw new InvalidOperationException(
                "El módulo Compras solo admite documentos tributarios válidos. Usa Operación sin sustento para egresos o inventario sin factura.");
        var supplier = await context.Terceros.SingleOrDefaultAsync(x =>
            x.Id == request.TerceroProveedorId && x.EsProveedor &&
            x.EstadoProveedor == 1 && x.Estado == 1, cancellationToken)
            ?? throw new InvalidOperationException(
                "El proveedor no existe o está inactivo.");
        var relation = await context.EmpresasTerceros.SingleOrDefaultAsync(x =>
            x.EmpresaId == companyId && x.TerceroId == supplier.Id,
            cancellationToken);
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
            throw new InvalidOperationException(
                "La relación del proveedor con la empresa está inactiva.");

        if (!await context.Establecimientos.AsNoTracking().AnyAsync(x =>
                x.Id == request.EstablecimientoId &&
                x.EmpresaId == companyId && x.Estado == 1,
                cancellationToken))
            throw new InvalidOperationException(
                "El establecimiento no pertenece a la empresa.");
        if (request.TipoComprobanteId.HasValue)
        {
            var documentType = await context.TiposComprobante.AsNoTracking()
                .Where(x => x.Id == request.TipoComprobanteId && x.Estado == 1)
                .Select(x => x.CodigoSri)
                .SingleOrDefaultAsync(cancellationToken);
            if (documentType is null)
                throw new InvalidOperationException(
                    "El tipo de comprobante no está activo.");
            if (type == "FACTURADA" && documentType != "01")
                throw new InvalidOperationException(
                    "Las compras facturadas deben registrarse con comprobante Factura (01).");
        }

        var presentationIds = request.Lineas.Where(x => x.EsInventariable)
            .Select(x => x.ProductoPresentacionId!.Value).Distinct().ToList();
        var presentations = await context.ProductosPresentaciones
            .Include(x => x.Producto).ThenInclude(x => x!.Impuestos)
            .Where(x => presentationIds.Contains(x.Id) &&
                x.EmpresaId == companyId && x.Estado == 1 &&
                x.PermiteCompra && x.Producto!.Estado == 1 &&
                x.Producto.ManejaInventario)
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (presentations.Count != presentationIds.Count)
            throw new InvalidOperationException(
                "Una presentación inventariable no está disponible.");

        var taxRateIds = request.Lineas.Select(x => x.TarifaImpuestoId!.Value)
            .Distinct().ToList();
        var taxRates = await context.TarifasImpuesto.AsNoTracking()
            .Include(x => x.Impuesto)
            .Where(x => taxRateIds.Contains(x.Id) && x.Estado == 1 &&
                        x.TipoCalculo == "PORCENTAJE" &&
                        x.Porcentaje.HasValue && x.Impuesto!.Estado == 1 &&
                        x.Impuesto.Codigo == "IVA")
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (taxRates.Count != taxRateIds.Count)
            throw new InvalidOperationException(
                "Una tarifa de IVA seleccionada no está disponible.");
        if (request.Lineas.Where(x => x.EsInventariable).Any(input =>
                !presentations[input.ProductoPresentacionId!.Value]
                    .Producto!.Impuestos.Any(x => x.Estado == 1 &&
                        x.TarifaImpuestoId == input.TarifaImpuestoId)))
            throw new InvalidOperationException(
                "La tarifa de IVA de un producto no coincide con su configuración.");

        var inventoryAccountId = request.Lineas.Any(x => x.EsInventariable)
            ? await CompraAccountingProcessor.RequireInventoryAccountAsync(
                context, companyId, cancellationToken)
            : 0;
        var nonInventoryAccounts = await LoadNonInventoryAccountsAsync(context,
            companyId, request.Lineas.Where(x => !x.EsInventariable)
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
            UsuarioId = userId,
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
            presentations.TryGetValue(input.ProductoPresentacionId ?? 0,
                out var presentation);
            var factor = presentation?.FactorConversion ?? 1m;
            var baseQuantity = input.CantidadPresentacion * factor;
            var lineSubtotal = input.CantidadPresentacion * input.PrecioUnitario -
                               input.DescuentoValor;
            var taxRate = taxRates[input.TarifaImpuestoId!.Value];
            var detail = new CompraDetalle
            {
                EmpresaId = companyId,
                Orden = ++order,
                EstadoReconocimiento = input.EsInventariable
                    ? "RECONOCIDA" : "NO_INVENTARIABLE",
                EsInventariable = input.EsInventariable,
                ClasificacionContable = input.EsInventariable
                    ? "INVENTARIO" : NormalizeClassification(
                        input.ClasificacionContable),
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
                ValorImpuesto = CalculateTax(input),
                CreatedAt = now
            });
            purchase.Detalles.Add(detail);
        }
        context.Compras.Add(purchase);
        await context.SaveChangesAsync(cancellationToken);
        await CompraAccountingProcessor.CreateAsync(context, purchase, now,
            cancellationToken);
        return purchase;
    }

    private static async Task<IReadOnlyDictionary<long, long>>
        LoadNonInventoryAccountsAsync(KontaxDbContext context, long companyId,
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

    private static string NormalizeClassification(string? value) =>
        value?.Trim().ToUpperInvariant() ?? string.Empty;

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
