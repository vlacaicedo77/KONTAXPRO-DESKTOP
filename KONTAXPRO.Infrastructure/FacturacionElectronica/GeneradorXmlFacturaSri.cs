using System.Globalization;
using System.Text;
using System.Xml;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;

namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

public sealed class GeneradorXmlFacturaSri : IGeneradorXmlFacturaSri
{
    private const string CodigoFactura = "01";

    public XmlDocument Generar(FacturaElectronicaSri factura)
    {
        ValidarEntrada(factura);

        var document = new XmlDocument { PreserveWhitespace = true };
        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = false,
            OmitXmlDeclaration = false,
            NewLineHandling = NewLineHandling.None
        }))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("factura");
            writer.WriteAttributeString("id", "comprobante");
            writer.WriteAttributeString("version", factura.Version);

            EscribirInfoTributaria(writer, factura);
            EscribirInfoFactura(writer, factura);
            EscribirDetalles(writer, factura.Detalles);
            EscribirInformacionAdicional(writer, factura.InformacionAdicional);

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        stream.Position = 0;
        using var reader = XmlReader.Create(stream, LecturaSegura());
        document.Load(reader);
        return document;
    }

    private static void EscribirInfoTributaria(XmlWriter w, FacturaElectronicaSri f)
    {
        w.WriteStartElement("infoTributaria");
        Elemento(w, "ambiente", f.Ambiente.ToString(CultureInfo.InvariantCulture));
        Elemento(w, "tipoEmision", f.TipoEmision.ToString(CultureInfo.InvariantCulture));
        Elemento(w, "razonSocial", f.RazonSocialEmisor);
        ElementoOpcional(w, "nombreComercial", f.NombreComercialEmisor);
        Elemento(w, "ruc", f.RucEmisor);
        Elemento(w, "claveAcceso", f.ClaveAcceso);
        Elemento(w, "codDoc", CodigoFactura);
        Elemento(w, "estab", f.Establecimiento);
        Elemento(w, "ptoEmi", f.PuntoEmision);
        Elemento(w, "secuencial", f.Secuencial.ToString("D9", CultureInfo.InvariantCulture));
        Elemento(w, "dirMatriz", f.DireccionMatriz);
        ElementoOpcional(w, "agenteRetencion", f.AgenteRetencion);
        ElementoOpcional(w, "contribuyenteRimpe", f.LeyendaRegimen);
        w.WriteEndElement();
    }

    private static void EscribirInfoFactura(XmlWriter w, FacturaElectronicaSri f)
    {
        w.WriteStartElement("infoFactura");
        Elemento(w, "fechaEmision", f.FechaEmision.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
        ElementoOpcional(w, "dirEstablecimiento", f.DireccionEstablecimiento);
        ElementoOpcional(w, "contribuyenteEspecial", f.ContribuyenteEspecial);
        if (f.ObligadoContabilidad.HasValue)
            Elemento(w, "obligadoContabilidad", f.ObligadoContabilidad.Value ? "SI" : "NO");
        Elemento(w, "tipoIdentificacionComprador", f.TipoIdentificacionComprador);
        Elemento(w, "razonSocialComprador", f.RazonSocialComprador);
        Elemento(w, "identificacionComprador", f.IdentificacionComprador);
        ElementoOpcional(w, "direccionComprador", f.DireccionComprador);
        Elemento(w, "totalSinImpuestos", Dinero(f.TotalSinImpuestos));
        Elemento(w, "totalDescuento", Dinero(f.TotalDescuento));
        w.WriteStartElement("totalConImpuestos");
        foreach (var impuesto in f.TotalesImpuestos)
        {
            w.WriteStartElement("totalImpuesto");
            Elemento(w, "codigo", impuesto.Codigo);
            Elemento(w, "codigoPorcentaje", impuesto.CodigoPorcentaje);
            Elemento(w, "baseImponible", Dinero(impuesto.BaseImponible));
            Elemento(w, "tarifa", Dinero(impuesto.Tarifa));
            Elemento(w, "valor", Dinero(impuesto.Valor));
            w.WriteEndElement();
        }
        w.WriteEndElement();
        Elemento(w, "propina", "0.00");
        Elemento(w, "importeTotal", Dinero(f.ImporteTotal));
        Elemento(w, "moneda", f.Moneda);
        if (f.Pagos.Count > 0)
        {
            w.WriteStartElement("pagos");
            foreach (var pago in f.Pagos)
            {
                w.WriteStartElement("pago");
                Elemento(w, "formaPago", pago.CodigoFormaPagoSri);
                Elemento(w, "total", Dinero(pago.Total));
                if (pago.Plazo.HasValue)
                    Elemento(w, "plazo", Numero(pago.Plazo.Value));
                ElementoOpcional(w, "unidadTiempo", pago.UnidadTiempo);
                w.WriteEndElement();
            }
            w.WriteEndElement();
        }
        w.WriteEndElement();
    }

    private static void EscribirDetalles(XmlWriter w, IReadOnlyList<DetalleFacturaSri> detalles)
    {
        w.WriteStartElement("detalles");
        foreach (var detalle in detalles)
        {
            w.WriteStartElement("detalle");
            ElementoOpcional(w, "codigoPrincipal", detalle.CodigoPrincipal);
            ElementoOpcional(w, "codigoAuxiliar", detalle.CodigoAuxiliar);
            Elemento(w, "descripcion", detalle.Descripcion);
            ElementoOpcional(w, "unidadMedida", detalle.UnidadMedida);
            Elemento(w, "cantidad", Numero(detalle.Cantidad));
            Elemento(w, "precioUnitario", Numero(detalle.PrecioUnitario));
            Elemento(w, "descuento", Dinero(detalle.Descuento));
            Elemento(w, "precioTotalSinImpuesto", Dinero(detalle.PrecioTotalSinImpuesto));
            if (detalle.DetallesAdicionales is { Count: > 0 })
            {
                w.WriteStartElement("detallesAdicionales");
                foreach (var adicional in detalle.DetallesAdicionales)
                {
                    w.WriteStartElement("detAdicional");
                    w.WriteAttributeString("nombre", adicional.Nombre);
                    w.WriteAttributeString("valor", adicional.Valor);
                    w.WriteEndElement();
                }
                w.WriteEndElement();
            }
            w.WriteStartElement("impuestos");
            foreach (var impuesto in detalle.Impuestos)
            {
                w.WriteStartElement("impuesto");
                Elemento(w, "codigo", impuesto.Codigo);
                Elemento(w, "codigoPorcentaje", impuesto.CodigoPorcentaje);
                Elemento(w, "tarifa", Dinero(impuesto.Tarifa));
                Elemento(w, "baseImponible", Dinero(impuesto.BaseImponible));
                Elemento(w, "valor", Dinero(impuesto.Valor));
                w.WriteEndElement();
            }
            w.WriteEndElement();
            w.WriteEndElement();
        }
        w.WriteEndElement();
    }

    private static void EscribirInformacionAdicional(
        XmlWriter w,
        IReadOnlyList<CampoAdicionalFacturaSri> campos)
    {
        if (campos.Count == 0)
            return;
        w.WriteStartElement("infoAdicional");
        foreach (var campo in campos)
        {
            w.WriteStartElement("campoAdicional");
            w.WriteAttributeString("nombre", campo.Nombre);
            w.WriteString(campo.Valor);
            w.WriteEndElement();
        }
        w.WriteEndElement();
    }

    private static void ValidarEntrada(FacturaElectronicaSri f)
    {
        if (f.Detalles.Count == 0) throw new ArgumentException("La factura debe contener al menos un detalle.");
        if (f.TotalesImpuestos.Count == 0) throw new ArgumentException("La factura debe contener totales de impuestos.");
        if (f.Ambiente is not (1 or 2)) throw new ArgumentException("El ambiente SRI debe ser 1 o 2.");
        if (f.TipoEmision != 1) throw new ArgumentException("El tipo de emisión Offline permitido es 1.");
        if (f.ClaveAcceso.Length != 49 || !f.ClaveAcceso.All(char.IsDigit)) throw new ArgumentException("La clave de acceso debe contener 49 dígitos.");
        if (f.InformacionAdicional.Count > 15) throw new ArgumentException("El SRI permite máximo 15 campos adicionales.");
        if (f.Detalles.Any(x => x.Impuestos.Count == 0)) throw new ArgumentException("Cada detalle debe contener al menos un impuesto.");
        if (f.Detalles.Any(x => x.DetallesAdicionales?.Count > 3)) throw new ArgumentException("Cada detalle permite máximo tres campos adicionales.");
        if (f.ImporteTotal < 0 || f.TotalSinImpuestos < 0 || f.TotalDescuento < 0) throw new ArgumentException("Los totales no pueden ser negativos.");
    }

    private static XmlReaderSettings LecturaSegura() => new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        MaxCharactersFromEntities = 0
    };

    private static string Dinero(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    private static string Numero(decimal value) => value.ToString("0.######", CultureInfo.InvariantCulture);
    private static void Elemento(XmlWriter w, string nombre, string valor) => w.WriteElementString(nombre, valor.Trim());
    private static void ElementoOpcional(XmlWriter w, string nombre, string? valor)
    {
        if (!string.IsNullOrWhiteSpace(valor)) Elemento(w, nombre, valor);
    }
}
