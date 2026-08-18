using System.Globalization;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

public sealed class ClienteRecepcionSri(
    HttpClient httpClient,
    IOptions<SriEndpointsOptions> options,
    ILogger<ClienteRecepcionSri> logger) : IClienteRecepcionSri
{
    private readonly SriEndpointsOptions endpoints = options.Value;

    public async Task<ResultadoSri> EnviarAsync(
        int ambiente,
        string claveAcceso,
        ReadOnlyMemory<byte> xmlFirmado,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(claveAcceso) || claveAcceso.Length != 49)
            throw new ArgumentException("La clave de acceso no es válida.", nameof(claveAcceso));
        if (xmlFirmado.IsEmpty)
            throw new ArgumentException("El XML firmado está vacío.", nameof(xmlFirmado));

        var envelope = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(SoapParserSri.Soap + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soapenv", SoapParserSri.Soap),
                new XAttribute(XNamespace.Xmlns + "ec", SoapParserSri.RecepcionNs),
                new XElement(SoapParserSri.Soap + "Body",
                    new XElement(SoapParserSri.RecepcionNs + "validarComprobante",
                        new XElement("xml", Convert.ToBase64String(xmlFirmado.Span))))));
        using var response = await EnviarSoapAsync(
            httpClient, endpoints.Recepcion(ambiente), envelope,
            "validarComprobante", logger, cancellationToken);
        var document = await SoapParserSri.LeerAsync(response, cancellationToken);
        SoapParserSri.LanzarSiFault(document);
        var estado = SoapParserSri.Valor(document, "estado") ?? "SIN_RESPUESTA";
        var mensajes = SoapParserSri.Mensajes(document);
        return new(string.Equals(estado, "RECIBIDA", StringComparison.OrdinalIgnoreCase),
            estado.ToUpperInvariant(), null, null, mensajes);
    }

    internal static async Task<HttpResponseMessage> EnviarSoapAsync(
        HttpClient client,
        Uri endpoint,
        XDocument envelope,
        string action,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(envelope.ToString(SaveOptions.DisableFormatting),
                new UTF8Encoding(false), "text/xml")
        };
        request.Headers.TryAddWithoutValidation("SOAPAction", action);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex,
                "Comunicación SOAP SRI {Accion} falló tras {DuracionMs} ms. CorrelationId {CorrelationId}.",
                action, stopwatch.ElapsedMilliseconds, correlationId);
            throw;
        }
        logger.LogInformation(
            "Comunicación SOAP SRI {Accion} respondió HTTP {CodigoHttp} en {DuracionMs} ms. " +
            "CorrelationId {CorrelationId}.",
            action, (int)response.StatusCode, stopwatch.ElapsedMilliseconds, correlationId);
        if (response.StatusCode is HttpStatusCode.RequestTimeout or
            HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500)
            throw new HttpRequestException(
                $"El servicio SRI respondió HTTP {(int)response.StatusCode}.",
                null, response.StatusCode);
        response.EnsureSuccessStatusCode();
        return response;
    }
}

public sealed class ClienteAutorizacionSri(
    HttpClient httpClient,
    IOptions<SriEndpointsOptions> options,
    ILogger<ClienteAutorizacionSri> logger) : IClienteAutorizacionSri
{
    private readonly SriEndpointsOptions endpoints = options.Value;

    public async Task<ResultadoSri> ConsultarAsync(
        int ambiente,
        string claveAcceso,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(claveAcceso) || claveAcceso.Length != 49)
            throw new ArgumentException("La clave de acceso no es válida.", nameof(claveAcceso));
        var envelope = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(SoapParserSri.Soap + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soapenv", SoapParserSri.Soap),
                new XAttribute(XNamespace.Xmlns + "ec", SoapParserSri.AutorizacionNs),
                new XElement(SoapParserSri.Soap + "Body",
                    new XElement(SoapParserSri.AutorizacionNs + "autorizacionComprobante",
                        new XElement("claveAccesoComprobante", claveAcceso)))));
        using var response = await ClienteRecepcionSri.EnviarSoapAsync(
            httpClient, endpoints.Autorizacion(ambiente), envelope,
            "autorizacionComprobante", logger, cancellationToken);
        var document = await SoapParserSri.LeerAsync(response, cancellationToken);
        SoapParserSri.LanzarSiFault(document);
        var autorizacion = document.Descendants()
            .FirstOrDefault(x => x.Name.LocalName == "autorizacion");
        if (autorizacion is null)
            return new(false, "PENDIENTE", null, null,
                SoapParserSri.Mensajes(document));
        var estado = autorizacion.Elements()
            .FirstOrDefault(x => x.Name.LocalName == "estado")?.Value
            ?.Trim().ToUpperInvariant() ?? "PENDIENTE";
        var numero = autorizacion.Elements()
            .FirstOrDefault(x => x.Name.LocalName == "numeroAutorizacion")?.Value?.Trim();
        var fechaText = autorizacion.Elements()
            .FirstOrDefault(x => x.Name.LocalName == "fechaAutorizacion")?.Value?.Trim();
        DateTime? fecha = null;
        if (DateTimeOffset.TryParse(fechaText, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces, out var parsed))
            fecha = parsed.UtcDateTime;
        var mensajes = SoapParserSri.Mensajes(autorizacion);
        var exitoso = estado == "AUTORIZADO";
        return new(exitoso, NormalizarEstado(estado), numero, fecha, mensajes,
            exitoso ? autorizacion.ToString(SaveOptions.DisableFormatting) : null);
    }

    private static string NormalizarEstado(string estado) => estado switch
    {
        "AUTORIZADO" => "AUTORIZADO",
        "NO AUTORIZADO" or "NO_AUTORIZADO" => "NO_AUTORIZADO",
        "EN PROCESO" or "PPR" or "PENDIENTE" => "PENDIENTE",
        _ => estado
    };
}

internal static class SoapParserSri
{
    internal static readonly XNamespace Soap =
        "http://schemas.xmlsoap.org/soap/envelope/";
    internal static readonly XNamespace RecepcionNs =
        "http://ec.gob.sri.ws.recepcion";
    internal static readonly XNamespace AutorizacionNs =
        "http://ec.gob.sri.ws.autorizacion";

    internal static async Task<XDocument> LeerAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        if (stream.CanSeek && stream.Length == 0)
            throw new XmlException("El SRI devolvió una respuesta vacía.");
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            Async = true,
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 10_000_000,
            MaxCharactersFromEntities = 0
        });
        try
        {
            return await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);
        }
        catch (XmlException ex)
        {
            throw new XmlException("La respuesta SOAP del SRI no es válida.", ex);
        }
    }

    internal static void LanzarSiFault(XDocument document)
    {
        var fault = document.Descendants()
            .FirstOrDefault(x => x.Name.LocalName == "Fault");
        if (fault is null) return;
        var message = fault.Descendants()
            .FirstOrDefault(x => x.Name.LocalName is "faultstring" or "Text")?.Value;
        throw new InvalidOperationException(
            $"El SRI devolvió un SOAP Fault: {message ?? "sin detalle"}.");
    }

    internal static string? Valor(XContainer container, string localName) =>
        container.Descendants().FirstOrDefault(x => x.Name.LocalName == localName)
            ?.Value.Trim();

    internal static IReadOnlyList<MensajeSri> Mensajes(XContainer container) =>
        container.Descendants()
            .Where(x => x.Name.LocalName == "mensaje" && x.HasElements)
            .Select(x => new MensajeSri(
                Elemento(x, "identificador"),
                Elemento(x, "mensaje"),
                Elemento(x, "informacionAdicional"),
                Elemento(x, "tipo")))
            .ToArray();

    private static string? Elemento(XElement parent, string name) =>
        parent.Elements().FirstOrDefault(x => x.Name.LocalName == name)?.Value.Trim();
}
