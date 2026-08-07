using System.Net;
using System.Text.Json;
using KONTAXPRO.Application.Clientes;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Interoperabilidad;

namespace KONTAXPRO.Infrastructure.Interoperabilidad;

public sealed class SifaeIdentificacionProvider(
    IHttpClientFactory httpClientFactory,
    InteroperabilidadOptions options) : IProveedorConsultaIdentificacion
{
    public string Fuente => "SIFAE";

    public async Task<ProveedorIdentificacionResult> ConsultarAsync(
        ConsultaIdentificacionRequest request,
        string numeroNormalizado,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(options.Sifae.BaseUrl, UriKind.Absolute, out _))
            return Unavailable("SIFAE no está configurado.");

        return request.TipoIdentificacionCodigo.Equals(
            "CEDULA",
            StringComparison.OrdinalIgnoreCase)
            ? await ConsultarCedulaAsync(numeroNormalizado, cancellationToken)
            : await ConsultarRucAsync(
                numeroNormalizado,
                request.Proposito,
                cancellationToken);
    }

    private async Task<ProveedorIdentificacionResult> ConsultarCedulaAsync(
        string number,
        CancellationToken cancellationToken)
    {
        var response = await GetSectionAsync(
            $"datos_demograficos/{Uri.EscapeDataString(number)}",
            cancellationToken);
        if (response.State != SectionState.Ok || response.Data is null)
            return FromSectionFailure(response, "datos demográficos");

        var name = JsonResponseReader.GetString(
            response.Data.Value,
            "nombre",
            "nombreCompleto",
            "nombres");
        return string.IsNullOrWhiteSpace(name)
            ? Incomplete("SIFAE no entregó el nombre requerido.")
            : Found(name);
    }

    private async Task<ProveedorIdentificacionResult> ConsultarRucAsync(
        string number,
        PropositoConsultaIdentificacion purpose,
        CancellationToken cancellationToken)
    {
        if (purpose is PropositoConsultaIdentificacion.Cliente or
            PropositoConsultaIdentificacion.Proveedor)
        {
            var clientLocationsTask = GetSectionAsync(
                $"ubicaciones_sri/{Uri.EscapeDataString(number)}",
                cancellationToken);
            var emailTask = GetSectionAsync(
                $"email_contribuyente/{Uri.EscapeDataString(number)}",
                cancellationToken);
            await Task.WhenAll(clientLocationsTask, emailTask);

            var clientLocations = await clientLocationsTask;
            if (clientLocations.State != SectionState.Ok ||
                clientLocations.Data is null)
                return FromSectionFailure(clientLocations, "ubicaciones del RUC");

            var clientName = ReadBusinessName(clientLocations.Data.Value);
            if (string.IsNullOrWhiteSpace(clientName))
                return Incomplete("SIFAE no entregó la razón social requerida.");

            var email = await emailTask;
            return Found(
                clientName,
                email.State == SectionState.Ok && email.Data is not null
                    ? JsonResponseReader.GetString(
                        email.Data.Value,
                        "email",
                        "correo")
                    : null);
        }

        var locationsTask = GetSectionAsync(
            $"ubicaciones_sri/{Uri.EscapeDataString(number)}",
            cancellationToken);
        var representativeTask = GetSectionAsync(
            $"ruc_representante_legal/{Uri.EscapeDataString(number)}",
            cancellationToken);
        var emailRequiredTask = GetSectionAsync(
            $"email_contribuyente/{Uri.EscapeDataString(number)}",
            cancellationToken);
        var contributorOptionalTask = GetSectionAsync(
            $"ruc_contribuyente/{Uri.EscapeDataString(number)}",
            cancellationToken);
        await Task.WhenAll(
            locationsTask,
            representativeTask,
            emailRequiredTask,
            contributorOptionalTask);

        var missing = new List<string>();
        if ((await locationsTask).State != SectionState.Ok)
            missing.Add("ubicaciones_sri");
        if ((await representativeTask).State != SectionState.Ok)
            missing.Add("ruc_representante_legal");
        if ((await emailRequiredTask).State != SectionState.Ok)
            missing.Add("email_contribuyente");

        if (missing.Count > 0)
        {
            return new ProveedorIdentificacionResult
            {
                Estado = EstadoConsultaIdentificacion.DatosIncompletos,
                Fuente = Fuente,
                SeccionesFaltantes = missing,
                DetalleTecnicoSeguro =
                    "SIFAE no completó todas las secciones obligatorias."
            };
        }

        var contributor = await contributorOptionalTask;
        var locations = await locationsTask;
        var data = contributor.Data ?? locations.Data;
        var name = data is null ? null : ReadBusinessName(data.Value);
        return string.IsNullOrWhiteSpace(name)
            ? Incomplete("SIFAE no entregó la razón social requerida.")
            : Found(
                name,
                JsonResponseReader.GetString(
                    (await emailRequiredTask).Data!.Value,
                    "email",
                    "correo"));
    }

    private async Task<SectionResult> GetSectionAsync(
        string route,
        CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = options.Sifae.BaseUrl;
            var separator = baseUrl.EndsWith('=') || baseUrl.EndsWith('/')
                ? string.Empty
                : "/";
            var client = httpClientFactory.CreateClient(
                InteroperabilidadHttpClients.Sifae);
            using var response = await client.GetAsync(
                $"{baseUrl}{separator}{route}",
                cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return new(SectionState.NotFound, null);
            if (!response.IsSuccessStatusCode)
                return new(SectionState.Unavailable, null);

            using var document = await JsonResponseReader.TryReadDocumentAsync(
                response.Content,
                cancellationToken);
            if (document is null)
                return new(SectionState.Unavailable, null);
            var status = JsonResponseReader.GetString(
                document.RootElement,
                "estado",
                "status");
            var data = JsonResponseReader.GetFirstResult(document.RootElement);
            if (!string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
                return new(SectionState.NotFound, null);
            if (data is null)
                return new(SectionState.NotFound, null);

            return new(SectionState.Ok, data.Value.Clone());
        }
        catch (JsonException)
        {
            return new(SectionState.Unavailable, null);
        }
        catch (HttpRequestException)
        {
            return new(SectionState.Unavailable, null);
        }
        catch (IOException)
        {
            return new(SectionState.Unavailable, null);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(SectionState.Unavailable, null);
        }
    }

    private static string? ReadBusinessName(JsonElement data) =>
        JsonResponseReader.GetString(
            data,
            "razonSocial",
            "razon_social",
            "nombre",
            "nombreComercial");

    private ProveedorIdentificacionResult Found(string name, string? email = null) =>
        new()
        {
            Estado = EstadoConsultaIdentificacion.Encontrado,
            Fuente = Fuente,
            RazonSocial = name.Trim(),
            Correo = ContactoClienteNormalizer.FirstEmailOrNull(email)
        };

    private ProveedorIdentificacionResult Incomplete(string detail) => new()
    {
        Estado = EstadoConsultaIdentificacion.DatosIncompletos,
        Fuente = Fuente,
        DetalleTecnicoSeguro = detail
    };

    private ProveedorIdentificacionResult Unavailable(string detail) => new()
    {
        Estado = EstadoConsultaIdentificacion.FuentesNoDisponibles,
        Fuente = Fuente,
        DetalleTecnicoSeguro = detail
    };

    private ProveedorIdentificacionResult FromSectionFailure(
        SectionResult section,
        string sectionName) =>
        section.State == SectionState.NotFound
            ? new ProveedorIdentificacionResult
            {
                Estado = EstadoConsultaIdentificacion.NoEncontrado,
                Fuente = Fuente,
                DetalleTecnicoSeguro = $"SIFAE no encontró {sectionName}."
            }
            : Unavailable($"SIFAE no pudo consultar {sectionName}.");

    private enum SectionState
    {
        Ok,
        NotFound,
        Unavailable
    }

    private sealed record SectionResult(SectionState State, JsonElement? Data);
}
