using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using KONTAXPRO.Application.Compras;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Compras;

namespace KONTAXPRO.Infrastructure.Compras;

public sealed class ComprobanteCompraXmlReader(
    ComprobanteCompraXmlOptions? options = null)
    : IComprobanteCompraXmlReader
{
    private readonly ComprobanteCompraXmlOptions _options =
        options ?? new ComprobanteCompraXmlOptions();

    public async Task<LecturaComprobanteCompraResultado> LeerFacturaAsync(
        Stream contenido,
        string nombreArchivo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contenido);
        cancellationToken.ThrowIfCancellationRequested();

        byte[] bytes;
        try
        {
            bytes = await LeerLimitadoAsync(contenido, cancellationToken);
        }
        catch (ArchivoXmlDemasiadoGrandeException)
        {
            return LecturaComprobanteCompraResultado.Fallo(
                ErrorLecturaComprobanteCompra.ArchivoDemasiadoGrande,
                $"El XML supera el tamaño máximo permitido de " +
                $"{_options.MaximoBytes / (1024 * 1024)} MB.");
        }

        if (bytes.Length == 0 ||
            bytes.All(value => char.IsWhiteSpace((char)value)))
            return LecturaComprobanteCompraResultado.Fallo(
                ErrorLecturaComprobanteCompra.ArchivoVacio,
                "El archivo XML está vacío.");

        try
        {
            var documentoExterior = LeerDocumento(bytes);
            var extraida = ExtraerFactura(documentoExterior);
            var facturaDocument = extraida.Document;
            var fechaAutorizacion = extraida.FechaAutorizacion;
            if (extraida.IncluyeAutorizacion &&
                !string.Equals(extraida.EstadoAutorizacion, "AUTORIZADO",
                    StringComparison.OrdinalIgnoreCase))
                return LecturaComprobanteCompraResultado.Fallo(
                    ErrorLecturaComprobanteCompra.ComprobanteNoAutorizado,
                    $"El SRI reporta el comprobante como " +
                    $"'{extraida.EstadoAutorizacion ?? "SIN ESTADO"}'.");
            var root = facturaDocument.Root;
            if (root is null || root.Name.LocalName != "factura")
                return LecturaComprobanteCompraResultado.Fallo(
                    ErrorLecturaComprobanteCompra.TipoNoSoportado,
                    "Compras V1 solo admite facturas electrónicas XML.");

            var infoTributaria = Child(root, "infoTributaria");
            var infoFactura = Child(root, "infoFactura");
            if (infoTributaria is null || infoFactura is null)
                return DatosFaltantes(
                    "El XML no contiene infoTributaria e infoFactura completos.");

            var codigoDocumento = Value(infoTributaria, "codDoc");
            if (!string.Equals(codigoDocumento, "01", StringComparison.Ordinal))
                return LecturaComprobanteCompraResultado.Fallo(
                    ErrorLecturaComprobanteCompra.TipoNoSoportado,
                    "El comprobante XML no es una factura electrónica (código 01).");

            var clave = Digits(Value(infoTributaria, "claveAcceso"));
            if (clave.Length != 49)
                return LecturaComprobanteCompraResultado.Fallo(
                    ErrorLecturaComprobanteCompra.ClaveAccesoInvalida,
                    "La clave de acceso debe contener exactamente 49 dígitos.");

            if (!TryDate(Value(infoFactura, "fechaEmision"), out var fechaEmision))
                return DatosFaltantes("La fecha de emisión no es válida.");

            var detallesElement = Child(root, "detalles");
            var detalleElements = detallesElement?.Elements()
                .Where(x => x.Name.LocalName == "detalle").ToList() ?? [];
            if (detalleElements.Count == 0)
                return DatosFaltantes("La factura no contiene detalles.");

            var detalles = new List<DetalleFacturaCompraXmlDto>();
            for (var index = 0; index < detalleElements.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var element = detalleElements[index];
                var descripcion = Value(element, "descripcion");
                if (string.IsNullOrWhiteSpace(descripcion) ||
                    !TryDecimal(Value(element, "cantidad"), out var cantidad) ||
                    cantidad <= 0 ||
                    !TryDecimal(Value(element, "precioUnitario"),
                        out var precioUnitario) || precioUnitario < 0 ||
                    !TryDecimal(Value(element, "descuento"), out var descuento) ||
                    descuento < 0 ||
                    !TryDecimal(Value(element, "precioTotalSinImpuesto"),
                        out var precioTotal) || precioTotal < 0)
                    return DatosFaltantes(
                        $"El detalle {index + 1} contiene valores obligatorios inválidos.");

                detalles.Add(new DetalleFacturaCompraXmlDto
                {
                    Orden = index + 1,
                    CodigoPrincipal = NullIfEmpty(Value(element, "codigoPrincipal")),
                    CodigoAuxiliar = NullIfEmpty(Value(element, "codigoAuxiliar")),
                    Descripcion = descripcion.Trim(),
                    Cantidad = cantidad,
                    PrecioUnitario = precioUnitario,
                    Descuento = descuento,
                    PrecioTotalSinImpuesto = precioTotal,
                    Impuestos = LeerImpuestos(Child(element, "impuestos"))
                });
            }

            if (!TryDecimal(Value(infoFactura, "totalSinImpuestos"),
                    out var totalSinImpuestos) ||
                !TryDecimal(Value(infoFactura, "totalDescuento"),
                    out var totalDescuento) ||
                !TryDecimal(Value(infoFactura, "importeTotal"),
                    out var importeTotal))
                return DatosFaltantes("Los totales de la factura no son válidos.");

            var propina = DecimalOrZero(Value(infoFactura, "propina"));
            var pagos = LeerPagos(Child(infoFactura, "pagos"));
            if (totalSinImpuestos < 0 || totalDescuento < 0 ||
                importeTotal < 0 || propina < 0 ||
                detalles.Any(x => x.Descuento >
                    x.Cantidad * x.PrecioUnitario) ||
                pagos.Any(x => string.IsNullOrWhiteSpace(x.FormaPago) ||
                    x.Total <= 0 || x.Plazo < 0))
                return DatosFaltantes(
                    "El XML contiene totales, descuentos o pagos inválidos.");
            var factura = new FacturaCompraXmlDto
            {
                NombreArchivo = Path.GetFileName(nombreArchivo),
                TamanoArchivo = bytes.LongLength,
                Sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
                ComprobanteSha256 = HashDocument(facturaDocument),
                Ambiente = Required(Value(infoTributaria, "ambiente")),
                TipoEmision = Required(Value(infoTributaria, "tipoEmision")),
                RazonSocialEmisor = Required(Value(infoTributaria, "razonSocial")),
                NombreComercialEmisor = NullIfEmpty(
                    Value(infoTributaria, "nombreComercial")),
                RucEmisor = Digits(Value(infoTributaria, "ruc")),
                ClaveAcceso = clave,
                CodigoDocumento = codigoDocumento,
                Establecimiento = Required(Value(infoTributaria, "estab")),
                PuntoEmision = Required(Value(infoTributaria, "ptoEmi")),
                Secuencial = Required(Value(infoTributaria, "secuencial")),
                DireccionMatriz = NullIfEmpty(Value(infoTributaria, "dirMatriz")),
                FechaEmision = fechaEmision,
                FechaAutorizacion = fechaAutorizacion,
                DireccionEstablecimiento = NullIfEmpty(
                    Value(infoFactura, "dirEstablecimiento")),
                ContribuyenteEspecial = NullIfEmpty(
                    Value(infoFactura, "contribuyenteEspecial")),
                ObligadoContabilidad = NullIfEmpty(
                    Value(infoFactura, "obligadoContabilidad")),
                TipoIdentificacionComprador = Required(
                    Value(infoFactura, "tipoIdentificacionComprador")),
                RazonSocialComprador = Required(
                    Value(infoFactura, "razonSocialComprador")),
                IdentificacionComprador = Required(
                    Value(infoFactura, "identificacionComprador")),
                TotalSinImpuestos = totalSinImpuestos,
                TotalDescuento = totalDescuento,
                Propina = propina,
                ImporteTotal = importeTotal,
                Moneda = NullIfEmpty(Value(infoFactura, "moneda")),
                IncluyeAutorizacionSri = extraida.IncluyeAutorizacion,
                NumeroAutorizacion = extraida.NumeroAutorizacion,
                EstadoAutorizacionSri = extraida.EstadoAutorizacion,
                EstadoValidacion = "VALIDADO_LOCALMENTE",
                MensajeValidacion = extraida.IncluyeAutorizacion
                    ? "Firma, clave y contenedor de autorización válidos. Se consultará el estado actual en el SRI."
                    : "Firma y clave válidas. Se consultará la autorización en el SRI.",
                Impuestos = LeerImpuestos(Child(infoFactura, "totalConImpuestos"),
                    "totalImpuesto"),
                Detalles = detalles,
                Pagos = pagos,
                InformacionAdicional = LeerInformacionAdicional(root)
            };

            if (string.IsNullOrWhiteSpace(factura.Ambiente) ||
                string.IsNullOrWhiteSpace(factura.TipoEmision) ||
                string.IsNullOrWhiteSpace(factura.RazonSocialEmisor) ||
                factura.RucEmisor.Length != 13 ||
                factura.Establecimiento.Length != 3 ||
                factura.PuntoEmision.Length != 3 ||
                factura.Secuencial.Length is < 1 or > 9 ||
                string.IsNullOrWhiteSpace(factura.IdentificacionComprador) ||
                string.IsNullOrWhiteSpace(factura.RazonSocialComprador))
                return DatosFaltantes(
                    "El XML no contiene todos los datos obligatorios de la factura.");

            if (!CompraImportacionRules.ClaveAccesoCorrespondeFactura(
                    factura.ClaveAcceso, factura.FechaEmision,
                    factura.CodigoDocumento, factura.RucEmisor,
                    factura.Ambiente, factura.Establecimiento,
                    factura.PuntoEmision, factura.Secuencial,
                    factura.TipoEmision))
                return LecturaComprobanteCompraResultado.Fallo(
                    ErrorLecturaComprobanteCompra.ClaveAccesoInvalida,
                    "La clave de acceso no corresponde a los datos de la factura o su dígito verificador es inválido.");

            if (extraida.IncluyeAutorizacion &&
                !string.Equals(extraida.NumeroAutorizacion,
                    factura.ClaveAcceso, StringComparison.Ordinal))
                return LecturaComprobanteCompraResultado.Fallo(
                    ErrorLecturaComprobanteCompra.ClaveAccesoInvalida,
                    "El número de autorización no coincide con la clave de acceso de la factura.");

            factura.FirmaPresente = facturaDocument.Descendants()
                .Any(x => x.Name.LocalName == "Signature" &&
                          x.Name.NamespaceName == SignedXml.XmlDsigNamespaceUrl);
            factura.FirmaValida = factura.FirmaPresente &&
                                  ValidarFirma(facturaDocument, fechaEmision);
            if (!factura.FirmaValida)
                return LecturaComprobanteCompraResultado.Fallo(
                    ErrorLecturaComprobanteCompra.FirmaElectronicaInvalida,
                    factura.FirmaPresente
                        ? "La firma electrónica del XML no es válida o fue alterada."
                        : "La factura no contiene la firma electrónica obligatoria.");

            return LecturaComprobanteCompraResultado.Correcto(factura);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (XmlException ex) when (EsRechazoDeDtd(ex))
        {
            return LecturaComprobanteCompraResultado.Fallo(
                ErrorLecturaComprobanteCompra.XmlInseguro,
                "El XML contiene DTD o entidades externas no permitidas.");
        }
        catch (XmlException)
        {
            return LecturaComprobanteCompraResultado.Fallo(
                ErrorLecturaComprobanteCompra.XmlMalFormado,
                "El archivo no contiene un XML válido.");
        }
        catch (FormatException)
        {
            return DatosFaltantes(
                "El XML contiene datos obligatorios vacíos o con formato inválido.");
        }
    }

    private async Task<byte[]> LeerLimitadoAsync(
        Stream source,
        CancellationToken cancellationToken)
    {
        if (source.CanSeek && source.Length > _options.MaximoBytes)
            throw new ArchivoXmlDemasiadoGrandeException();

        await using var output = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0) break;
            if (output.Length + read > _options.MaximoBytes)
                throw new ArchivoXmlDemasiadoGrandeException();
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        return output.ToArray();
    }

    private XDocument LeerDocumento(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var reader = XmlReader.Create(stream, Settings());
        return XDocument.Load(reader, LoadOptions.PreserveWhitespace);
    }

    private FacturaExtraida ExtraerFactura(
        XDocument document)
    {
        if (document.Root?.Name.LocalName == "factura")
            return new FacturaExtraida(document, null, false, null, null);
        if (document.Root?.Name.LocalName != "autorizacion")
            return new FacturaExtraida(document, null, false, null, null);

        var estado = NullIfEmpty(Value(document.Root, "estado"));
        var numeroAutorizacion = NullIfEmpty(
            Value(document.Root, "numeroAutorizacion"));
        var comprobante = Child(document.Root, "comprobante")?.Value;
        if (string.IsNullOrWhiteSpace(comprobante))
            throw new XmlException("La autorización no contiene comprobante.");
        using var textReader = new StringReader(comprobante);
        using var xmlReader = XmlReader.Create(textReader, Settings());
        var inner = XDocument.Load(xmlReader, LoadOptions.PreserveWhitespace);
        DateTime? fecha = TryDateTime(
            Value(document.Root, "fechaAutorizacion"), out var parsed)
            ? parsed
            : null;
        return new FacturaExtraida(inner, fecha, true, estado,
            numeroAutorizacion);
    }

    private static bool ValidarFirma(
        XDocument document,
        DateOnly fechaEmision)
    {
        try
        {
            var xml = new XmlDocument { PreserveWhitespace = true };
            using (var reader = document.CreateReader()) xml.Load(reader);
            var signatures = xml.GetElementsByTagName(
                "Signature", SignedXml.XmlDsigNamespaceUrl);
            if (signatures.Count != 1 || signatures[0] is not XmlElement element)
                return false;

            var rootId = xml.DocumentElement?.GetAttribute("id");
            if (string.IsNullOrWhiteSpace(rootId))
                return false;
            var duplicateIds = xml.GetElementsByTagName("*")
                .Cast<XmlElement>()
                .Count(x => string.Equals(x.GetAttribute("id"), rootId,
                    StringComparison.Ordinal));
            if (duplicateIds != 1)
                return false;

            var signedXml = new XadesSignedXml(xml);
            signedXml.LoadXml(element);
            var references = signedXml.SignedInfo?.References
                .Cast<Reference>().ToList() ?? [];
            if (!references.Any(x => x.Uri == $"#{rootId}") ||
                references.Any(x => !string.IsNullOrEmpty(x.Uri) &&
                                    !x.Uri.StartsWith('#')))
                return false;

            var signatureId = element.GetAttribute("Id");
            var qualifyingProperties = element.GetElementsByTagName("*")
                .Cast<XmlElement>().FirstOrDefault(x =>
                    x.LocalName == "QualifyingProperties" &&
                    x.NamespaceURI.StartsWith(
                        "http://uri.etsi.org/01903", StringComparison.Ordinal));
            var signedProperties = qualifyingProperties?
                .GetElementsByTagName("*").Cast<XmlElement>()
                .FirstOrDefault(x => x.LocalName == "SignedProperties" &&
                                     x.HasAttribute("Id"));
            if (string.IsNullOrWhiteSpace(signatureId) ||
                qualifyingProperties is null || signedProperties is null ||
                !string.Equals(qualifyingProperties.GetAttribute("Target"),
                    $"#{signatureId}", StringComparison.Ordinal))
                return false;

            var certificates = signedXml.KeyInfo?
                .OfType<KeyInfoX509Data>()
                .SelectMany(x => x.Certificates?.Cast<object>() ?? [])
                .OfType<System.Security.Cryptography.X509Certificates.X509Certificate2>()
                .ToList() ?? [];
            if (certificates.Count == 0)
                return false;
            var emission = fechaEmision.ToDateTime(TimeOnly.MinValue);
            if (certificates.All(x => emission < x.NotBefore || emission > x.NotAfter))
                return false;
            return signedXml.CheckSignature();
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (XmlException)
        {
            return false;
        }
    }

    private static string HashDocument(XDocument document) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            document.ToString(SaveOptions.DisableFormatting))))
            .ToLowerInvariant();

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

    private XmlReaderSettings Settings() => new()
    {
        Async = false,
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        MaxCharactersInDocument = _options.MaximoCaracteres,
        MaxCharactersFromEntities = 0,
        IgnoreComments = true,
        IgnoreProcessingInstructions = true
    };

    private static IReadOnlyList<ImpuestoCompraXmlDto> LeerImpuestos(
        XElement? parent,
        string elementName = "impuesto")
    {
        if (parent is null) return [];
        return parent.Elements()
            .Where(x => x.Name.LocalName == elementName)
            .Select(x => new ImpuestoCompraXmlDto
            {
                Codigo = Value(x, "codigo"),
                CodigoPorcentaje = Value(x, "codigoPorcentaje"),
                Tarifa = TryDecimal(Value(x, "tarifa"), out var tarifa)
                    ? tarifa : null,
                BaseImponible = DecimalOrZero(Value(x, "baseImponible")),
                Valor = DecimalOrZero(Value(x, "valor"))
            }).ToList();
    }

    private static IReadOnlyList<PagoCompraXmlDto> LeerPagos(XElement? parent)
    {
        if (parent is null) return [];
        return parent.Elements().Where(x => x.Name.LocalName == "pago")
            .Select(x => new PagoCompraXmlDto
            {
                FormaPago = Value(x, "formaPago"),
                Total = DecimalOrZero(Value(x, "total")),
                Plazo = int.TryParse(Value(x, "plazo"),
                    NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out var plazo) ? plazo : null,
                UnidadTiempo = NullIfEmpty(Value(x, "unidadTiempo"))
            }).ToList();
    }

    private static IReadOnlyDictionary<string, string>
        LeerInformacionAdicional(XElement root)
    {
        var result = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);
        var parent = Child(root, "infoAdicional");
        if (parent is null) return result;
        foreach (var field in parent.Elements()
                     .Where(x => x.Name.LocalName == "campoAdicional"))
        {
            var name = field.Attributes()
                .FirstOrDefault(x => x.Name.LocalName == "nombre")?.Value.Trim();
            if (!string.IsNullOrWhiteSpace(name) && !result.ContainsKey(name))
                result[name] = field.Value.Trim();
        }
        return result;
    }

    private static XElement? Child(XElement parent, string localName) =>
        parent.Elements().FirstOrDefault(x => x.Name.LocalName == localName);

    private static string Value(XElement parent, string localName) =>
        Child(parent, localName)?.Value.Trim() ?? string.Empty;

    private static string Required(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new FormatException()
            : value.Trim();

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Digits(string value) =>
        new(value.Where(char.IsDigit).ToArray());

    private static bool TryDecimal(string value, out decimal result) =>
        decimal.TryParse(value, NumberStyles.Number,
            CultureInfo.InvariantCulture, out result);

    private static decimal DecimalOrZero(string value) =>
        TryDecimal(value, out var result) ? result : 0;

    private static bool TryDate(string value, out DateOnly result) =>
        DateOnly.TryParseExact(value,
            ["dd/MM/yyyy", "yyyy-MM-dd"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result);

    private static bool TryDateTime(string value, out DateTime result)
    {
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces, out var parsed))
        {
            result = default;
            return false;
        }
        result = parsed.UtcDateTime;
        return true;
    }

    private static bool EsRechazoDeDtd(XmlException exception) =>
        exception.Message.Contains("DTD", StringComparison.OrdinalIgnoreCase) ||
        exception.Message.Contains("entity", StringComparison.OrdinalIgnoreCase);

    private static LecturaComprobanteCompraResultado DatosFaltantes(
        string mensaje) => LecturaComprobanteCompraResultado.Fallo(
        ErrorLecturaComprobanteCompra.DatosObligatoriosFaltantes,
        mensaje);

    private sealed class ArchivoXmlDemasiadoGrandeException : Exception;

    private sealed record FacturaExtraida(
        XDocument Document,
        DateTime? FechaAutorizacion,
        bool IncluyeAutorizacion,
        string? EstadoAutorizacion,
        string? NumeroAutorizacion);
}

public sealed class ComprobanteCompraXmlOptions
{
    public long MaximoBytes { get; init; } = 5 * 1024 * 1024;
    public long MaximoCaracteres { get; init; } = 8 * 1024 * 1024;
}
