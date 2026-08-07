using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using KONTAXPRO.Application.Clientes;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Interoperabilidad;

namespace KONTAXPRO.Infrastructure.Interoperabilidad;

public sealed class GuiaIdentificacionProvider(
    IHttpClientFactory httpClientFactory,
    GuiaTokenClient tokenClient,
    InteroperabilidadOptions options) : IProveedorConsultaIdentificacion
{
    public string Fuente => "GUIA";

    public async Task<ProveedorIdentificacionResult> ConsultarAsync(
        ConsultaIdentificacionRequest request,
        string numeroNormalizado,
        CancellationToken cancellationToken = default)
    {
        if (!options.Guia.HasCredentials ||
            !Uri.TryCreate(options.Guia.ServicioUrl, UriKind.Absolute, out var uri))
            return Unavailable("GUIA no está configurado.");

        var classification = request.TipoIdentificacionCodigo
            .Equals("CEDULA", StringComparison.OrdinalIgnoreCase)
                ? GuiaClasificaciones.Cedula
                : GuiaClasificaciones.RucNatural;

        var token = await tokenClient.GetAccessTokenAsync(
            cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
            return Unavailable("No fue posible obtener autorización de GUIA.");

        var response = await SendAsync(
            uri,
            token,
            classification,
            numeroNormalizado,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            tokenClient.Invalidate(token);
            token = await tokenClient.GetAccessTokenAsync(
                forceRefresh: true,
                cancellationToken);
            if (string.IsNullOrWhiteSpace(token))
                return Unavailable("GUIA rechazó la autorización y no pudo renovarse.");

            response = await SendAsync(
                uri,
                token,
                classification,
                numeroNormalizado,
                cancellationToken);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                return Unavailable($"GUIA respondió HTTP {(int)response.StatusCode}.");

            try
            {
                using var document =
                    await JsonResponseReader.TryReadDocumentAsync(
                        response.Content,
                        cancellationToken);
                if (document is null)
                    return Unavailable("GUIA devolvió un contenido no JSON.");
                return Parse(document.RootElement, request.TipoIdentificacionCodigo);
            }
            catch (JsonException)
            {
                return Unavailable("GUIA devolvió JSON inválido.");
            }
        }
    }

    private async Task<HttpResponseMessage> SendAsync(
        Uri uri,
        string token,
        string classification,
        string number,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["clasificacion"] = classification,
            ["numero"] = number
        });
        var client = httpClientFactory.CreateClient(
            InteroperabilidadHttpClients.GuiaServicio);
        try
        {
            return await client.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new HttpResponseMessage(HttpStatusCode.RequestTimeout);
        }
        catch (HttpRequestException)
        {
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }
    }

    private ProveedorIdentificacionResult Parse(JsonElement root, string type)
    {
        var values = JsonResponseReader.TryGetProperty(root, "valores", out var element)
            ? JsonResponseReader.UnwrapObjectOrFirstArrayItem(element)
            : root;
        var functionalCode = JsonResponseReader.GetString(
            values,
            "CodigoError",
            "codigoError");
        var functionalError = JsonResponseReader.GetString(values, "Error", "error");
        var serviceMessage = JsonResponseReader.GetString(root, "mensaje", "message");

        if (IsFunctionalErrorCode(functionalCode) ||
            (string.IsNullOrWhiteSpace(functionalCode) &&
             IsFunctionalErrorText(functionalError)))
        {
            var state = ClassifyFunctionalError(
                functionalCode,
                functionalError,
                serviceMessage);
            return new ProveedorIdentificacionResult
            {
                Estado = state,
                Fuente = Fuente,
                DetalleTecnicoSeguro = "GUIA reportó un error funcional."
            };
        }

        var status = JsonResponseReader.GetString(root, "estado", "status");
        var successful = IsSuccessfulStatus(status);
        if (!successful)
        {
            return new ProveedorIdentificacionResult
            {
                Estado = EstadoConsultaIdentificacion.FuentesNoDisponibles,
                Fuente = Fuente,
                DetalleTecnicoSeguro =
                    "GUIA no confirmó el éxito funcional de la consulta."
            };
        }

        var isCedula = type.Equals("CEDULA", StringComparison.OrdinalIgnoreCase);
        var name = isCedula
            ? JsonResponseReader.GetString(values, "Nombre", "nombre")
            : JsonResponseReader.GetString(
                values,
                "razonSocial",
                "razon_social",
                "RazonSocial");
        if (string.IsNullOrWhiteSpace(name))
        {
            return new ProveedorIdentificacionResult
            {
                Estado = EstadoConsultaIdentificacion.DatosIncompletos,
                Fuente = Fuente,
                DetalleTecnicoSeguro = "GUIA no entregó el nombre requerido."
            };
        }

        return new ProveedorIdentificacionResult
        {
            Estado = EstadoConsultaIdentificacion.Encontrado,
            Fuente = Fuente,
            RazonSocial = name.Trim(),
            Correo = isCedula
                ? null
                : ContactoClienteNormalizer.FirstEmailOrNull(
                    JsonResponseReader.GetString(values, "email", "correo"))
        };
    }

    private ProveedorIdentificacionResult Unavailable(string detail) => new()
    {
        Estado = EstadoConsultaIdentificacion.FuentesNoDisponibles,
        Fuente = Fuente,
        DetalleTecnicoSeguro = detail
    };

    private static bool IsSuccessfulStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return false;

        return status.Trim().ToUpperInvariant() is
            "OK" or "EXITO" or "ÉXITO" or "EXITOSO" or
            "SUCCESS" or "SUCCESSFUL" or "TRUE" or "1" or "200";
    }

    private static bool IsFunctionalErrorCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        return !long.TryParse(code.Trim(), out var numericCode) ||
               numericCode != 0;
    }

    private static bool IsFunctionalErrorText(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
            return false;

        return error.Trim().ToUpperInvariant() is not
            ("0" or "FALSE" or "NO" or "NULL" or "NINGUNO");
    }

    private static EstadoConsultaIdentificacion ClassifyFunctionalError(
        string? code,
        string? error,
        string? message)
    {
        var text = $"{error} {message}";
        if (string.Equals(code?.Trim(), "009", StringComparison.Ordinal) ||
            text.Contains("incorrecto", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("inválido", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("invalido", StringComparison.OrdinalIgnoreCase))
            return EstadoConsultaIdentificacion.IdentificacionInvalida;

        if (string.Equals(code?.Trim(), "001", StringComparison.Ordinal) ||
            text.Contains("no existe", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("no encontrado", StringComparison.OrdinalIgnoreCase))
            return EstadoConsultaIdentificacion.NoEncontrado;

        return EstadoConsultaIdentificacion.FuentesNoDisponibles;
    }
}
