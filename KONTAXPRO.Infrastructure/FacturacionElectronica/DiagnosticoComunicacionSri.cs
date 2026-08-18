using System.Diagnostics;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Xml;

namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

public sealed class DiagnosticoComunicacionSri(
    HttpClient httpClient,
    IOptions<SriEndpointsOptions> options,
    ILogger<DiagnosticoComunicacionSri> logger)
    : IDiagnosticoComunicacionSri
{
    public async Task<ResultadoDiagnosticoComunicacionSri> ProbarAsync(
        int ambiente,
        CancellationToken cancellationToken = default)
    {
        var endpoints = new[]
        {
            ("COMUNICACION_RECEPCION", "Servicio de recepción",
                options.Value.Recepcion(ambiente)),
            ("COMUNICACION_AUTORIZACION", "Servicio de autorización",
                options.Value.Autorizacion(ambiente))
        };
        var items = new List<ItemDiagnosticoFacturacionElectronica>();
        foreach (var (code, description, endpoint) in endpoints)
        {
            var correlationId = Guid.NewGuid();
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var builder = new UriBuilder(endpoint) { Query = "wsdl" };
                using var response = await httpClient.GetAsync(
                    builder.Uri, HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                var ok = response.IsSuccessStatusCode &&
                    await EsWsdlValidoAsync(response, cancellationToken);
                logger.LogInformation(
                    "Diagnóstico SRI {Servicio} respondió HTTP {CodigoHttp} en {DuracionMs} ms. " +
                    "CorrelationId {CorrelationId}.",
                    description, (int)response.StatusCode,
                    stopwatch.ElapsedMilliseconds, correlationId);
                items.Add(new(code, description, ok,
                    ok ? $"Disponible · WSDL válido · HTTP {(int)response.StatusCode}" :
                    $"Respuesta no válida · HTTP {(int)response.StatusCode}"));
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException or
                TaskCanceledException or XmlException or InvalidDataException)
            {
                logger.LogWarning(ex,
                    "Diagnóstico SRI {Servicio} falló tras {DuracionMs} ms. " +
                    "CorrelationId {CorrelationId}.",
                    description, stopwatch.ElapsedMilliseconds, correlationId);
                items.Add(new(code, description, false,
                    ex is TaskCanceledException && !cancellationToken.IsCancellationRequested
                        ? "Tiempo de espera agotado"
                        : "No fue posible establecer comunicación"));
            }
        }
        return new(items.All(x => x.Correcto), items);
    }

    private static async Task<bool> EsWsdlValidoAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode) return false;
        await using var stream = await response.Content.ReadAsStreamAsync(
            cancellationToken);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            Async = true,
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 1_000_000,
            MaxCharactersFromEntities = 0
        });
        while (await reader.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (reader.NodeType != XmlNodeType.Element) continue;
            return reader.LocalName == "definitions" &&
                   reader.NamespaceURI == "http://schemas.xmlsoap.org/wsdl/";
        }
        throw new InvalidDataException("El servicio no devolvió un documento WSDL.");
    }
}
