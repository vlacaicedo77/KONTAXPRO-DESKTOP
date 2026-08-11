using System.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using KONTAXPRO.Application.Models.Compras;
using KONTAXPRO.Infrastructure.Compras;

namespace KONTAXPRO.Tests.Compras;

public sealed class ComprobanteCompraXmlReaderTests
{
    [Fact]
    public async Task ValidInvoiceReadsHeaderDetailsTaxesPaymentsAndAdditionalData()
    {
        var result = await ReadAsync(InvoiceXml(
            detailTaxes: """
                <impuesto><codigo>2</codigo><codigoPorcentaje>4</codigoPorcentaje><tarifa>15.00</tarifa><baseImponible>10.00</baseImponible><valor>1.50</valor></impuesto>
                <impuesto><codigo>3</codigo><codigoPorcentaje>3072</codigoPorcentaje><tarifa>0.00</tarifa><baseImponible>10.00</baseImponible><valor>0.25</valor></impuesto>
                """,
            payments: """
                <pago><formaPago>20</formaPago><total>5.75</total><plazo>0</plazo><unidadTiempo>dias</unidadTiempo></pago>
                <pago><formaPago>01</formaPago><total>6.00</total><plazo>30</plazo><unidadTiempo>dias</unidadTiempo></pago>
                """));

        Assert.True(result.Exito, result.Mensaje);
        var invoice = Assert.IsType<FacturaCompraXmlDto>(result.Factura);
        Assert.Equal("1799999999001", invoice.RucEmisor);
        Assert.Equal("001-002-000000123", invoice.NumeroDocumento);
        Assert.Equal(new DateOnly(2026, 8, 6), invoice.FechaEmision);
        Assert.Equal(10.00m, invoice.TotalSinImpuestos);
        Assert.Equal(11.75m, invoice.ImporteTotal);
        Assert.Equal(2, invoice.Detalles[0].Impuestos.Count);
        Assert.Equal(2, invoice.Pagos.Count);
        Assert.Equal(30, invoice.Pagos[1].Plazo);
        Assert.Equal("compras@proveedor.test",
            invoice.InformacionAdicional["Email"]);
        Assert.Equal(64, invoice.Sha256.Length);
        Assert.True(invoice.TamanoArchivo > 0);
    }

    [Fact]
    public async Task EmptyXmlIsRejected()
    {
        var result = await ReadAsync(string.Empty);
        Assert.Equal(ErrorLecturaComprobanteCompra.ArchivoVacio, result.Error);
    }

    [Fact]
    public async Task WhitespaceXmlIsRejected()
    {
        var result = await ReadAsync("  \r\n\t ");
        Assert.Equal(ErrorLecturaComprobanteCompra.ArchivoVacio, result.Error);
    }

    [Fact]
    public async Task MalformedXmlIsRejected()
    {
        var result = await ReadAsync("<factura><infoTributaria></factura>");
        Assert.Equal(ErrorLecturaComprobanteCompra.XmlMalFormado, result.Error);
    }

    [Fact]
    public async Task UnsupportedDocumentCodeIsRejected()
    {
        var result = await ReadAsync(InvoiceXml().Replace(
            "<codDoc>01</codDoc>", "<codDoc>04</codDoc>"));
        Assert.Equal(ErrorLecturaComprobanteCompra.TipoNoSoportado, result.Error);
    }

    [Fact]
    public async Task UnsupportedRootIsRejected()
    {
        var result = await ReadAsync("<notaCredito version=\"1.1.0\" />");
        Assert.Equal(ErrorLecturaComprobanteCompra.TipoNoSoportado, result.Error);
    }

    [Fact]
    public async Task InvalidAccessKeyIsRejected()
    {
        var result = await ReadAsync(InvoiceXml().Replace(
            AccessKey, "123"));
        Assert.Equal(ErrorLecturaComprobanteCompra.ClaveAccesoInvalida,
            result.Error);
    }

    [Fact]
    public async Task DetailMayOmitBothSupplierCodes()
    {
        var result = await ReadAsync(InvoiceXml(
            codes: string.Empty));
        Assert.True(result.Exito, result.Mensaje);
        Assert.Null(result.Factura!.Detalles[0].CodigoPrincipal);
        Assert.Null(result.Factura.Detalles[0].CodigoAuxiliar);
    }

    [Fact]
    public async Task DetailMayOmitAuxiliaryCode()
    {
        var result = await ReadAsync(InvoiceXml(
            codes: "<codigoPrincipal>ABC-01</codigoPrincipal>"));
        Assert.True(result.Exito, result.Mensaje);
        Assert.Equal("ABC-01", result.Factura!.Detalles[0].CodigoPrincipal);
        Assert.Null(result.Factura.Detalles[0].CodigoAuxiliar);
    }

    [Fact]
    public async Task InvalidSignatureIsRejected()
    {
        var result = await ReadAsync(InvoiceXml(signature:
            "<ds:Signature xmlns:ds=\"http://www.w3.org/2000/09/xmldsig#\"><ds:SignedInfo /></ds:Signature>"));
        Assert.False(result.Exito);
        Assert.Equal(ErrorLecturaComprobanteCompra.FirmaElectronicaInvalida,
            result.Error);
    }

    [Fact]
    public async Task SignatureAbsenceIsRejected()
    {
        var result = await ReadAsync(InvoiceXml(signature: string.Empty));
        Assert.False(result.Exito);
        Assert.Equal(ErrorLecturaComprobanteCompra.FirmaElectronicaInvalida,
            result.Error);
    }

    [Fact]
    public async Task AuthorizationEnvelopeReadsEmbeddedInvoiceAndUtcDate()
    {
        var invoice = InvoiceXml();
        var envelope = $"""
            <autorizacion>
              <estado>AUTORIZADO</estado>
              <numeroAutorizacion>{AccessKey}</numeroAutorizacion>
              <fechaAutorizacion>2026-08-06T14:30:00-05:00</fechaAutorizacion>
              <comprobante><![CDATA[{invoice}]]></comprobante>
            </autorizacion>
            """;
        var result = await ReadAsync(envelope);
        Assert.True(result.Exito, result.Mensaje);
        Assert.Equal(new DateTime(2026, 8, 6, 19, 30, 0, DateTimeKind.Utc),
            result.Factura!.FechaAutorizacion);
        Assert.True(result.Factura.IncluyeAutorizacionSri);
        Assert.Equal("AUTORIZADO", result.Factura.EstadoAutorizacionSri);
    }

    [Fact]
    public async Task RejectedAuthorizationEnvelopeIsRejected()
    {
        var envelope = $"""
            <autorizacion>
              <estado>NO AUTORIZADO</estado>
              <numeroAutorizacion>{AccessKey}</numeroAutorizacion>
              <comprobante><![CDATA[{InvoiceXml()}]]></comprobante>
            </autorizacion>
            """;
        var result = await ReadAsync(envelope);
        Assert.Equal(ErrorLecturaComprobanteCompra.ComprobanteNoAutorizado,
            result.Error);
    }

    [Fact]
    public async Task AccessKeyMustMatchInvoiceFieldsAndModulo11()
    {
        var result = await ReadAsync(InvoiceXml().Replace(
            "<ptoEmi>002</ptoEmi>", "<ptoEmi>003</ptoEmi>"));
        Assert.Equal(ErrorLecturaComprobanteCompra.ClaveAccesoInvalida,
            result.Error);
    }

    [Fact]
    public async Task DtdIsBlocked()
    {
        var xml = """
            <!DOCTYPE factura [<!ELEMENT factura ANY>]>
            <factura />
            """;
        var result = await ReadAsync(xml);
        Assert.Equal(ErrorLecturaComprobanteCompra.XmlInseguro, result.Error);
    }

    [Fact]
    public async Task ExternalEntityIsBlocked()
    {
        var xml = """
            <!DOCTYPE factura [<!ENTITY xxe SYSTEM "file:///etc/passwd">]>
            <factura>&xxe;</factura>
            """;
        var result = await ReadAsync(xml);
        Assert.Equal(ErrorLecturaComprobanteCompra.XmlInseguro, result.Error);
    }

    [Fact]
    public async Task OversizedStreamIsRejectedBeforeParsing()
    {
        var reader = new ComprobanteCompraXmlReader(
            new ComprobanteCompraXmlOptions
            {
                MaximoBytes = 32,
                MaximoCaracteres = 32
            });
        await using var stream = new MemoryStream(new byte[33]);
        var result = await reader.LeerFacturaAsync(stream, "factura.xml");
        Assert.Equal(ErrorLecturaComprobanteCompra.ArchivoDemasiadoGrande,
            result.Error);
    }

    [Fact]
    public async Task CancellationIsPropagated()
    {
        var reader = new ComprobanteCompraXmlReader();
        await using var stream = ToStream(InvoiceXml());
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            reader.LeerFacturaAsync(stream, "factura.xml", cts.Token));
    }

    [Fact]
    public async Task DecimalUsesInvariantCulture()
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture =
                System.Globalization.CultureInfo.GetCultureInfo("es-EC");
            var result = await ReadAsync(InvoiceXml());
            Assert.True(result.Exito, result.Mensaje);
            Assert.Equal(2.500000m, result.Factura!.Detalles[0].Cantidad);
            Assert.Equal(4.000000m, result.Factura.Detalles[0].PrecioUnitario);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Fact]
    public async Task PaymentWithZeroValueIsRejected()
    {
        var result = await ReadAsync(InvoiceXml(payments:
            "<pago><formaPago>20</formaPago><total>0</total></pago>"));
        Assert.Equal(ErrorLecturaComprobanteCompra.DatosObligatoriosFaltantes,
            result.Error);
    }

    [Fact]
    public async Task DiscountGreaterThanGrossLineIsRejected()
    {
        var result = await ReadAsync(InvoiceXml().Replace(
            "<descuento>0.00</descuento>",
            "<descuento>11.00</descuento>"));
        Assert.Equal(ErrorLecturaComprobanteCompra.DatosObligatoriosFaltantes,
            result.Error);
    }

    [Fact]
    public async Task OriginalFileNameIsSanitized()
    {
        var reader = new ComprobanteCompraXmlReader();
        await using var stream = ToStream(InvoiceXml());
        var result = await reader.LeerFacturaAsync(
            stream, "C:\\entrada\\factura.xml");
        Assert.True(result.Exito, result.Mensaje);
        Assert.Equal("factura.xml", result.Factura!.NombreArchivo);
    }

    private static async Task<LecturaComprobanteCompraResultado> ReadAsync(
        string xml)
    {
        var reader = new ComprobanteCompraXmlReader();
        await using var stream = ToStream(xml);
        return await reader.LeerFacturaAsync(stream, "factura.xml");
    }

    private static MemoryStream ToStream(string xml) =>
        new(Encoding.UTF8.GetBytes(xml));

    private const string AccessKey =
        "0608202601179999999900110010020000001231234567814";

    private static string InvoiceXml(
        string codes = "<codigoPrincipal>ABC-01</codigoPrincipal><codigoAuxiliar>AUX-9</codigoAuxiliar>",
        string detailTaxes = "<impuesto><codigo>2</codigo><codigoPorcentaje>4</codigoPorcentaje><tarifa>15.00</tarifa><baseImponible>10.00</baseImponible><valor>1.50</valor></impuesto>",
        string payments = "<pago><formaPago>20</formaPago><total>11.75</total></pago>",
        string? signature = null)
    {
        var xml = $$"""
        <?xml version="1.0" encoding="UTF-8"?>
        <factura id="comprobante" version="1.1.0">
          <infoTributaria>
            <ambiente>1</ambiente><tipoEmision>1</tipoEmision>
            <razonSocial>PROVEEDOR FICTICIO S.A.</razonSocial>
            <nombreComercial>PROVEEDOR DEMO</nombreComercial>
            <ruc>1799999999001</ruc><claveAcceso>{{AccessKey}}</claveAcceso>
            <codDoc>01</codDoc><estab>001</estab><ptoEmi>002</ptoEmi>
            <secuencial>000000123</secuencial><dirMatriz>QUITO</dirMatriz>
          </infoTributaria>
          <infoFactura>
            <fechaEmision>06/08/2026</fechaEmision>
            <dirEstablecimiento>QUITO</dirEstablecimiento>
            <obligadoContabilidad>SI</obligadoContabilidad>
            <tipoIdentificacionComprador>04</tipoIdentificacionComprador>
            <razonSocialComprador>EMPRESA RECEPTORA DEMO</razonSocialComprador>
            <identificacionComprador>1799999999002</identificacionComprador>
            <totalSinImpuestos>10.00</totalSinImpuestos>
            <totalDescuento>0.00</totalDescuento>
            <totalConImpuestos>
              <totalImpuesto><codigo>2</codigo><codigoPorcentaje>4</codigoPorcentaje><baseImponible>10.00</baseImponible><valor>1.50</valor></totalImpuesto>
              <totalImpuesto><codigo>3</codigo><codigoPorcentaje>3072</codigoPorcentaje><baseImponible>10.00</baseImponible><valor>0.25</valor></totalImpuesto>
            </totalConImpuestos>
            <propina>0.00</propina><importeTotal>11.75</importeTotal><moneda>DOLAR</moneda>
            <pagos>{{payments}}</pagos>
          </infoFactura>
          <detalles>
            <detalle>
              {{codes}}
              <descripcion>PRODUCTO ANÓNIMO DE PRUEBA</descripcion>
              <cantidad>2.500000</cantidad><precioUnitario>4.000000</precioUnitario>
              <descuento>0.00</descuento><precioTotalSinImpuesto>10.00</precioTotalSinImpuesto>
              <impuestos>{{detailTaxes}}</impuestos>
            </detalle>
          </detalles>
          <infoAdicional><campoAdicional nombre="Email">compras@proveedor.test</campoAdicional></infoAdicional>
          {{signature ?? string.Empty}}
        </factura>
        """;
        return signature is null ? Sign(xml) : xml;
    }

    private static string Sign(string xml)
    {
        var document = new XmlDocument { PreserveWhitespace = true };
        document.LoadXml(xml);
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=PROVEEDOR FICTICIO S.A.", rsa,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2035, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var signedXml = new XadesSignedXml(document)
        {
            SigningKey = rsa
        };
        const string signatureId = "Signature-Test";
        const string signedPropertiesId = "SignedProperties-Test";
        signedXml.Signature.Id = signatureId;
        signedXml.SignedInfo!.CanonicalizationMethod =
            SignedXml.XmlDsigCanonicalizationUrl;
        signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;
        var reference = new Reference("#comprobante")
        {
            DigestMethod = SignedXml.XmlDsigSHA256Url
        };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());
        signedXml.AddReference(reference);
        var xadesDocument = new XmlDocument { PreserveWhitespace = true };
        xadesDocument.LoadXml($$"""
            <xades:QualifyingProperties xmlns:xades="http://uri.etsi.org/01903/v1.3.2#" Target="#{{signatureId}}">
              <xades:SignedProperties Id="{{signedPropertiesId}}">
                <xades:SignedSignatureProperties>
                  <xades:SigningTime>2026-08-06T12:00:00-05:00</xades:SigningTime>
                </xades:SignedSignatureProperties>
              </xades:SignedProperties>
            </xades:QualifyingProperties>
            """);
        var dataObject = new DataObject
        {
            Data = xadesDocument.ChildNodes
        };
        signedXml.AddObject(dataObject);
        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(certificate));
        signedXml.KeyInfo = keyInfo;
        signedXml.ComputeSignature();
        document.DocumentElement!.AppendChild(document.ImportNode(
            signedXml.GetXml(), true));
        return document.OuterXml;
    }

    private sealed class XadesSignedXml(XmlDocument document)
        : SignedXml(document)
    {
        public override XmlElement? GetIdElement(
            XmlDocument? document,
            string idValue)
        {
            var found = base.GetIdElement(document, idValue);
            if (found is not null) return found;
            foreach (var dataObject in Signature.ObjectList.OfType<DataObject>())
                foreach (XmlNode node in dataObject.Data)
                    if (FindById(node, idValue) is { } element)
                        return element;
            return null;
        }

        private static XmlElement? FindById(XmlNode node, string idValue)
        {
            if (node is XmlElement element &&
                (element.GetAttribute("Id") == idValue ||
                 element.GetAttribute("ID") == idValue ||
                 element.GetAttribute("id") == idValue))
                return element;
            foreach (XmlNode child in node.ChildNodes)
                if (FindById(child, idValue) is { } found)
                    return found;
            return null;
        }
    }
}
