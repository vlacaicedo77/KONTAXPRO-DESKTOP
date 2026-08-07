using KONTAXPRO.Application.Clientes;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Interoperabilidad;
using KONTAXPRO.Application.Session;

namespace KONTAXPRO.Infrastructure.Interoperabilidad;

public sealed class ConsultaIdentificacionService(
    IEnumerable<IProveedorConsultaIdentificacion> providers,
    ConstanciaVerificacionIdentificacionStore proofStore,
    CurrentSession currentSession)
    : IConsultaIdentificacionService
{
    private readonly IProveedorConsultaIdentificacion _guia =
        providers.Single(x => x.Fuente == "GUIA");
    private readonly IProveedorConsultaIdentificacion _sifae =
        providers.Single(x => x.Fuente == "SIFAE");

    public async Task<ConsultaIdentificacionResult> ConsultarAsync(
        ConsultaIdentificacionRequest request,
        CancellationToken cancellationToken = default)
    {
        var type = request.TipoIdentificacionCodigo.Trim().ToUpperInvariant();
        var validation = IdentificacionEcuadorValidator.Validate(
            type,
            request.NumeroIdentificacion);
        if (!validation.IsValid)
        {
            return Register(request, new ConsultaIdentificacionResult
            {
                Estado = EstadoConsultaIdentificacion.IdentificacionInvalida,
                NumeroNormalizado = validation.Normalized,
                MensajeUsuario = validation.Error ??
                    "La identificación ingresada no es válida."
            });
        }

        if (type is not ("CEDULA" or "RUC"))
        {
            return Register(request, new ConsultaIdentificacionResult
            {
                Estado = EstadoConsultaIdentificacion.IdentificacionInvalida,
                NumeroNormalizado = validation.Normalized,
                MensajeUsuario =
                    "Este tipo de identificación se registra manualmente."
            });
        }

        var normalizedRequest = request with
        {
            TipoIdentificacionCodigo = type,
            NumeroIdentificacion = validation.Normalized
        };
        var guiaResult = await ConsultarProveedorSeguroAsync(
            _guia,
            normalizedRequest,
            validation.Normalized,
            cancellationToken);
        if (guiaResult.TieneDatosMinimos)
            return Register(
                normalizedRequest,
                ToPublicResult(guiaResult, validation.Normalized));

        var sifaeResult = await ConsultarProveedorSeguroAsync(
            _sifae,
            normalizedRequest,
            validation.Normalized,
            cancellationToken);
        if (sifaeResult.TieneDatosMinimos)
            return Register(
                normalizedRequest,
                ToPublicResult(sifaeResult, validation.Normalized));

        var finalState = ResolveFinalState(guiaResult.Estado, sifaeResult.Estado);
        return Register(normalizedRequest, new ConsultaIdentificacionResult
        {
            Estado = finalState,
            NumeroNormalizado = validation.Normalized,
            MensajeUsuario = UserMessage(finalState),
            DetalleTecnicoSeguro =
                $"GUIA: {guiaResult.Estado}; SIFAE: {sifaeResult.Estado}.",
            SeccionesFaltantes = sifaeResult.SeccionesFaltantes
        });
    }

    private ConsultaIdentificacionResult Register(
        ConsultaIdentificacionRequest request,
        ConsultaIdentificacionResult result) =>
        proofStore.RegisterResult(request, result, currentSession.UsuarioId);

    private static ConsultaIdentificacionResult ToPublicResult(
        ProveedorIdentificacionResult result,
        string normalized) => new()
    {
        Estado = EstadoConsultaIdentificacion.Encontrado,
        NumeroNormalizado = normalized,
        RazonSocial = result.RazonSocial,
        NombreComercial = result.NombreComercial,
        Correo = ContactoClienteNormalizer.FirstEmailOrNull(result.Correo),
        Direccion = result.Direccion,
        Fuente = result.Fuente,
        MensajeUsuario = $"Datos verificados con {result.Fuente}."
    };

    private static async Task<ProveedorIdentificacionResult>
        ConsultarProveedorSeguroAsync(
            IProveedorConsultaIdentificacion provider,
            ConsultaIdentificacionRequest request,
            string normalized,
            CancellationToken cancellationToken)
    {
        try
        {
            return await provider.ConsultarAsync(
                request,
                normalized,
                cancellationToken);
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new ProveedorIdentificacionResult
            {
                Estado = EstadoConsultaIdentificacion.FuentesNoDisponibles,
                Fuente = provider.Fuente,
                DetalleTecnicoSeguro =
                    $"{provider.Fuente} interrumpió la consulta por un fallo técnico."
            };
        }
    }

    private static EstadoConsultaIdentificacion ResolveFinalState(
        EstadoConsultaIdentificacion guiaState,
        EstadoConsultaIdentificacion sifaeState)
    {
        if (guiaState == EstadoConsultaIdentificacion.IdentificacionInvalida ||
            sifaeState == EstadoConsultaIdentificacion.IdentificacionInvalida)
            return EstadoConsultaIdentificacion.IdentificacionInvalida;

        if (guiaState == EstadoConsultaIdentificacion.DatosIncompletos ||
            sifaeState == EstadoConsultaIdentificacion.DatosIncompletos)
            return EstadoConsultaIdentificacion.DatosIncompletos;

        if (guiaState == EstadoConsultaIdentificacion.NoEncontrado ||
            sifaeState == EstadoConsultaIdentificacion.NoEncontrado)
            return EstadoConsultaIdentificacion.NoEncontrado;

        return EstadoConsultaIdentificacion.FuentesNoDisponibles;
    }

    private static string UserMessage(EstadoConsultaIdentificacion state) =>
        state switch
        {
            EstadoConsultaIdentificacion.IdentificacionInvalida =>
                "El número de identificación es incorrecto. Verifica los dígitos ingresados.",
            EstadoConsultaIdentificacion.NoEncontrado =>
                "El número de identificación no existe en las fuentes oficiales. Verifica los dígitos ingresados.",
            EstadoConsultaIdentificacion.DatosIncompletos =>
                "Las fuentes no entregaron los datos mínimos. Puedes completar el registro manualmente.",
            _ =>
                "Los servicios de consulta no están disponibles. Puedes registrar el cliente manualmente."
        };
}
