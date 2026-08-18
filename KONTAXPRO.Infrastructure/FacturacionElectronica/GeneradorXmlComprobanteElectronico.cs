using System.Text;
using System.Xml;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

public sealed class GeneradorXmlComprobanteElectronico(
    IDbContextFactory<KontaxDbContext> dbContextFactory,
    IGeneradorXmlFacturaSri builder)
    : IGeneradorXmlComprobanteElectronico
{
    public async Task<Stream> GenerarAsync(
        long comprobanteElectronicoId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var electronic = await context.ComprobantesElectronicos.AsNoTracking()
            .Include(x => x.TipoOrigenComprobanteElectronico)
            .Include(x => x.TipoAmbiente)
            .SingleOrDefaultAsync(x => x.Id == comprobanteElectronicoId,
                cancellationToken)
            ?? throw new InvalidOperationException("No existe el comprobante electrónico.");
        if (electronic.TipoOrigenComprobanteElectronico?.Codigo != "FACTURA")
            throw new NotSupportedException("Facturación electrónica V1 genera únicamente FACTURA.");

        var invoice = await context.Facturas.AsNoTracking()
            .Include(x => x.Empresa).ThenInclude(x => x!.RegimenTributario)
            .Include(x => x.Establecimiento)
            .Include(x => x.PuntoEmision)
            .Include(x => x.EmpresaTercero).ThenInclude(x => x!.Tercero)
                .ThenInclude(x => x!.TipoIdentificacion)
            .Include(x => x.Detalles).ThenInclude(x => x.Producto)
            .Include(x => x.Detalles).ThenInclude(x => x.ProductoPresentacion)
            .Include(x => x.Detalles).ThenInclude(x => x.Impuestos)
            .Include(x => x.FormasPago)
            .SingleOrDefaultAsync(x => x.Id == electronic.OrigenId &&
                                       x.EmpresaId == electronic.EmpresaId,
                cancellationToken)
            ?? throw new InvalidOperationException("La factura origen no existe en la empresa indicada.");
        var matrixAddress = await context.Establecimientos.AsNoTracking()
            .Where(x => x.EmpresaId == invoice.EmpresaId && x.EsMatriz && x.Estado == 1)
            .Select(x => x.Direccion).SingleAsync(cancellationToken);
        var customer = invoice.EmpresaTercero?.Tercero
            ?? throw new InvalidOperationException("La factura no tiene receptor.");
        var taxes = invoice.Detalles.SelectMany(x => x.Impuestos)
            .GroupBy(x => new { x.CodigoImpuestoSri, x.CodigoPorcentajeSri, x.Porcentaje })
            .Select(x => new TotalImpuestoFacturaSri(
                x.Key.CodigoImpuestoSri, x.Key.CodigoPorcentajeSri,
                x.Sum(i => i.BaseImponible), x.Key.Porcentaje ?? 0,
                x.Sum(i => i.ValorImpuesto))).ToArray();
        var details = invoice.Detalles.Select(x => new DetalleFacturaSri(
            x.Descripcion, x.CantidadPresentacion, x.PrecioUnitario,
            x.DescuentoValor, x.Subtotal,
            x.Impuestos.Select(i => new ImpuestoDetalleFacturaSri(
                i.CodigoImpuestoSri, i.CodigoPorcentajeSri,
                i.Porcentaje ?? 0, i.BaseImponible, i.ValorImpuesto)).ToArray(),
            x.ProductoPresentacion?.Codigo ?? x.Producto?.Codigo,
            x.Producto?.Codigo, x.ProductoPresentacion?.Nombre)).ToArray();
        var additional = new List<CampoAdicionalFacturaSri>();
        if (!string.IsNullOrWhiteSpace(customer.Correo))
            additional.Add(new("Email", customer.Correo));
        if (!string.IsNullOrWhiteSpace(customer.Telefono))
            additional.Add(new("Teléfono", customer.Telefono));
        var dto = new FacturaElectronicaSri(
            electronic.ClaveAcceso, electronic.TipoAmbiente!.Codigo, 1,
            invoice.Empresa!.NumeroIdentificacion, invoice.Empresa.RazonSocial,
            invoice.Establecimiento!.NombreComercial ??
                invoice.Empresa.NombreComercial, matrixAddress,
            invoice.Establecimiento!.Codigo, invoice.PuntoEmision!.Codigo,
            invoice.Secuencial, DateOnly.FromDateTime(invoice.FechaEmision),
            invoice.Establecimiento.Direccion,
            invoice.Empresa.ContribuyenteEspecialNumero,
            invoice.Empresa.ObligadoContabilidad,
            ObtenerLeyendaRegimen(invoice.Empresa.RegimenTributario?.Codigo),
            customer.TipoIdentificacion!.CodigoSri, customer.RazonSocial,
            customer.NumeroIdentificacion, customer.Direccion,
            invoice.SubtotalSinImpuestos, invoice.DescuentoTotal, invoice.Total,
            taxes, details,
            invoice.FormasPago.Select(x => new PagoFacturaSri(
                x.CodigoFormaPagoSri, x.Valor, x.Plazo, x.UnidadTiempo)).ToArray(),
            additional);
        var document = builder.Generar(dto);
        var output = new MemoryStream();
        using (var writer = XmlWriter.Create(output, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = false,
            OmitXmlDeclaration = false,
            CloseOutput = false
        })) document.Save(writer);
        output.Position = 0;
        return output;
    }

    internal static string? ObtenerLeyendaRegimen(string? codigoRegimen) =>
        codigoRegimen switch
        {
            "RIMPE" => "CONTRIBUYENTE RÉGIMEN RIMPE",
            "RIMPE_NEGOCIO_POPULAR" =>
                "CONTRIBUYENTE NEGOCIO POPULAR - RÉGIMEN RIMPE",
            _ => null
        };
}
