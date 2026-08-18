using KONTAXPRO.Application.Models.FacturacionElectronica;

namespace KONTAXPRO.Application.Interfaces;

public interface IContextoInstalacion
{
    Guid InstalacionId { get; }
    string TipoInstalacion { get; }
    string DirectorioBase { get; }
    bool EsServidor => string.Equals(
        TipoInstalacion, "SERVIDOR", StringComparison.OrdinalIgnoreCase);
    bool EsCliente => !EsServidor;
}

public interface IFacturacionElectronicaWorkerScheduler
{
    bool EstaEjecutandose { get; }

    Task IniciarAsync(CancellationToken cancellationToken = default);

    Task DetenerAsync(CancellationToken cancellationToken = default);
}

public interface IAlmacenamientoDocumentosElectronicos
{
    Task GuardarAsync(
        ArtefactoElectronico artefacto,
        Stream contenido,
        CancellationToken cancellationToken = default);

    Task<Stream> AbrirLecturaAsync(
        ArtefactoElectronico artefacto,
        CancellationToken cancellationToken = default);
}

public interface IGeneradorXmlComprobanteElectronico
{
    Task<Stream> GenerarAsync(
        long comprobanteElectronicoId,
        CancellationToken cancellationToken = default);
}

public interface IFirmadorXmlComprobanteElectronico
{
    bool TieneConfiguracionSegura { get; }

    Task<Stream> FirmarAsync(
        Stream xml,
        long empresaId,
        CancellationToken cancellationToken = default);
}

public interface IGeneradorRide
{
    Task<Stream> GenerarAsync(
        long comprobanteElectronicoId,
        Stream xmlAutorizado,
        CancellationToken cancellationToken = default);
}

public interface IClienteSriComprobantesElectronicos
{
    bool TieneConfiguracionSegura { get; }

    Task<ResultadoSri> EnviarAsync(
        string claveAcceso,
        Stream xmlFirmado,
        CancellationToken cancellationToken = default);

    Task<ResultadoSri> ConsultarAutorizacionAsync(
        string claveAcceso,
        CancellationToken cancellationToken = default);
}

public interface IEstadoComprobanteElectronicoService
{
    Task RegistrarResultadoAsync(
        long comprobanteElectronicoId,
        string tipoEvento,
        string? nuevoEstado,
        IReadOnlyCollection<MensajeSri> mensajes,
        string? codigo = null,
        string? mensaje = null,
        string? informacionAdicional = null,
        CancellationToken cancellationToken = default);
}

public interface IProcesadorFacturacionElectronica
{
    Task<int> RecuperarProcesamientosAbandonadosAsync(
        Guid instalacionId,
        TimeSpan antiguedadMinima,
        CancellationToken cancellationToken = default);

    Task<int> ProcesarPendientesAsync(
        Guid instalacionId,
        CancellationToken cancellationToken = default);
}

public interface IWorkerFacturacionElectronica
{
    Task<ResultadoCicloFacturacionElectronica> EjecutarCicloAsync(
        CancellationToken cancellationToken = default);
}
