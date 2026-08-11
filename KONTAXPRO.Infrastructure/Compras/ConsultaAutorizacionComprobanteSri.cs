using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Compras;

namespace KONTAXPRO.Infrastructure.Compras;

public sealed class ConsultaAutorizacionComprobanteSri(
    HttpClient httpClient,
    ConsultaAutorizacionSriOptions options)
    : IConsultaAutorizacionComprobanteSri
{
    private static readonly XNamespace Soap =
        "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace Consultas =
        "http://ec.gob.sri.ws.consultas";
    private static readonly XNamespace Autorizacion =
        "http://ec.gob.sri.ws.autorizacion";

    public async Task<ConsultaAutorizacionSriDto> ConsultarAsync(
        string claveAcceso,
        string ambiente,
        CancellationToken cancellationToken = default)
    {
        if (claveAcceso.Length != 49 || claveAcceso.Any(x => !char.IsDigit(x)))
            return new ConsultaAutorizacionSriDto
            {
                Estado = EstadoConsultaAutorizacionSri.NoEncontrado,
                Mensaje = "La clave de acceso no es válida para consultar al SRI."
            };

        var endpoint = ambiente == "1"
            ? options.UrlPruebas
            : options.UrlProduccion;
        var envelope = new XDocument(
            new XElement(Soap + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soapenv", Soap),
                new XAttribute(XNamespace.Xmlns + "ec", Consultas),
                new XElement(Soap + "Header"),
                new XElement(Soap + "Body",
                    new XElement(Consultas +
                                 "consultarEstadoAutorizacionComprobante",
                        new XElement("claveAcceso", claveAcceso)))));

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(
                    envelope.ToString(SaveOptions.DisableFormatting),
                    Encoding.UTF8, "text/xml")
            };
            request.Headers.TryAddWithoutValidation("SOAPAction", "");
            using var response = await httpClient.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode is HttpStatusCode.RequestTimeout or
                HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or
                HttpStatusCode.GatewayTimeout || !response.IsSuccessStatusCode)
                return NoDisponible(
                    $"El servicio de validación del SRI respondió HTTP {(int)response.StatusCode}.");

            await using var stream = await response.Content
                .ReadAsStreamAsync(cancellationToken);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                Async = true,
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = options.MaximoCaracteresRespuesta
            });
            var document = await XDocument.LoadAsync(reader,
                LoadOptions.None, cancellationToken);
            var status = Interpretar(document, claveAcceso);
            if (status.Estado != EstadoConsultaAutorizacionSri.Autorizado)
                return status;
            return await ConsultarXmlAutorizadoAsync(
                status, claveAcceso, ambiente, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or
                                   TaskCanceledException or XmlException)
        {
            return NoDisponible(
                "El servicio de validación del SRI no está disponible temporalmente.");
        }
    }

    private async Task<ConsultaAutorizacionSriDto> ConsultarXmlAutorizadoAsync(
        ConsultaAutorizacionSriDto status,
        string claveAcceso,
        string ambiente,
        CancellationToken cancellationToken)
    {
        var endpoint = ambiente == "1"
            ? options.UrlAutorizacionPruebas
            : options.UrlAutorizacionProduccion;
        var envelope = new XDocument(
            new XElement(Soap + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soapenv", Soap),
                new XAttribute(XNamespace.Xmlns + "ec", Autorizacion),
                new XElement(Soap + "Header"),
                new XElement(Soap + "Body",
                    new XElement(Autorizacion + "autorizacionComprobante",
                        new XElement("claveAccesoComprobante", claveAcceso)))));
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(
                envelope.ToString(SaveOptions.DisableFormatting),
                Encoding.UTF8, "text/xml")
        };
        request.Headers.TryAddWithoutValidation("SOAPAction", "");
        using var response = await httpClient.SendAsync(request,
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return NoDisponible(
                "El SRI confirmó el estado, pero no fue posible recuperar el XML autorizado.");

        await using var stream = await response.Content
            .ReadAsStreamAsync(cancellationToken);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            Async = true,
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = options.MaximoCaracteresRespuesta
        });
        var document = await XDocument.LoadAsync(reader,
            LoadOptions.None, cancellationToken);
        var authorization = document.Descendants().FirstOrDefault(x =>
            x.Name.LocalName == "autorizacion" &&
            string.Equals(Value(x, "estado"), "AUTORIZADO",
                StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Value(x, "numeroAutorizacion"), claveAcceso,
                StringComparison.Ordinal));
        var xmlText = authorization is null
            ? null
            : NullIfEmpty(Value(authorization, "comprobante"));
        if (xmlText is null)
            return NoDisponible(
                "El SRI confirmó el estado, pero no devolvió el XML autorizado para compararlo.");

        var officialDocument = ParseSecure(xmlText, options.MaximoCaracteresRespuesta);
        var officialRuc = officialDocument.Descendants().FirstOrDefault(x =>
            x.Name.LocalName == "ruc")?.Value.Trim();
        return new ConsultaAutorizacionSriDto
        {
            Estado = EstadoConsultaAutorizacionSri.Autorizado,
            EstadoSri = "AUTORIZADO",
            NumeroAutorizacion = claveAcceso,
            RucEmisor = NullIfEmpty(officialRuc ?? string.Empty) ?? status.RucEmisor,
            FechaAutorizacion = TryDateTime(
                Value(authorization!, "fechaAutorizacion")) ??
                                 status.FechaAutorizacion,
            ComprobanteSha256 = HashDocument(officialDocument),
            Mensaje = "Comprobante y contenido autorizados por el SRI."
        };
    }

    private static ConsultaAutorizacionSriDto Interpretar(
        XDocument document,
        string claveEsperada)
    {
        var result = document.Descendants().FirstOrDefault(x =>
            x.Name.LocalName == "EstadoAutorizacionComprobante");
        if (result is null)
            return NoDisponible(
                "El SRI devolvió una respuesta de validación no reconocida.");

        var key = Value(result, "claveAcceso");
        if (key.Length > 0 && !string.Equals(key, claveEsperada,
                StringComparison.Ordinal))
            return NoDisponible(
                "La respuesta del SRI no corresponde a la clave consultada.");

        var status = Value(result, "estadoAutorizacion")
            .Trim().ToUpperInvariant();
        var queryStatus = Value(result, "estadoConsulta")
            .Trim().ToUpperInvariant();
        var message = string.Join(" ", result.Descendants()
            .Where(x => x.Name.LocalName is "mensaje" or "informacionAdicional")
            .Select(x => x.Value.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase));

        if (queryStatus == "RECHAZADA" &&
            message.Contains("NO EXISTEN DATOS", StringComparison.OrdinalIgnoreCase))
            return new ConsultaAutorizacionSriDto
            {
                Estado = EstadoConsultaAutorizacionSri.NoEncontrado,
                EstadoSri = queryStatus,
                Mensaje = "El comprobante no consta en los registros consultables del SRI."
            };
        if (queryStatus == "RECHAZADA")
            return NoDisponible(message.Length > 0
                ? message
                : "El SRI no pudo completar la consulta de validez.");

        var mapped = status switch
        {
            "AUTORIZADO" => EstadoConsultaAutorizacionSri.Autorizado,
            "NO AUTORIZADO" => EstadoConsultaAutorizacionSri.NoAutorizado,
            "PENDIENTE DE ANULAR" =>
                EstadoConsultaAutorizacionSri.PendienteAnulacion,
            "ANULADO" => EstadoConsultaAutorizacionSri.Anulado,
            _ => EstadoConsultaAutorizacionSri.NoEncontrado
        };
        return new ConsultaAutorizacionSriDto
        {
            Estado = mapped,
            EstadoSri = status.Length > 0 ? status : queryStatus,
            NumeroAutorizacion = Value(result, "numeroAutorizacion") is var number &&
                                 number.Length > 0 ? number : claveEsperada,
            RucEmisor = NullIfEmpty(Value(result, "rucEmisor")),
            FechaAutorizacion = TryDateTime(Value(result, "fechaAutorizacion")),
            Mensaje = mapped == EstadoConsultaAutorizacionSri.Autorizado
                ? "Comprobante autorizado por el SRI."
                : message.Length > 0
                    ? message
                    : $"El SRI reporta el comprobante como {status}."
        };
    }

    private static ConsultaAutorizacionSriDto NoDisponible(string message) =>
        new()
        {
            Estado = EstadoConsultaAutorizacionSri.NoDisponible,
            Mensaje = message
        };

    private static string Value(XElement parent, string localName) =>
        parent.Descendants().FirstOrDefault(x =>
            x.Name.LocalName == localName)?.Value.Trim() ?? string.Empty;

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime? TryDateTime(string value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces, out var parsed)
            ? parsed.UtcDateTime
            : null;

    private static XDocument ParseSecure(string xml, long maxCharacters)
    {
        using var text = new StringReader(xml);
        using var reader = XmlReader.Create(text, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = maxCharacters
        });
        return XDocument.Load(reader, LoadOptions.PreserveWhitespace);
    }

    private static string HashDocument(XDocument document) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            document.ToString(SaveOptions.DisableFormatting))))
            .ToLowerInvariant();
}

public sealed class ConsultaAutorizacionSriOptions
{
    public string UrlPruebas { get; init; } =
        "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/ConsultaComprobante";
    public string UrlProduccion { get; init; } =
        "https://cel.sri.gob.ec/comprobantes-electronicos-ws/ConsultaComprobante";
    public string UrlAutorizacionPruebas { get; init; } =
        "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline";
    public string UrlAutorizacionProduccion { get; init; } =
        "https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline";
    public int TimeoutSegundos { get; init; } = 12;
    public long MaximoCaracteresRespuesta { get; init; } = 512 * 1024;
}
