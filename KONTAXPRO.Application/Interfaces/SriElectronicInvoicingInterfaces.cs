using KONTAXPRO.Application.Models.FacturacionElectronica;
using System.Xml;

namespace KONTAXPRO.Application.Interfaces;

public interface IGeneradorClaveAccesoSri
{
    string Generar(SolicitudClaveAccesoSri solicitud);
    bool EsValida(string claveAcceso);
}

public interface IGeneradorCodigoNumericoSri
{
    string Generar();
}

public interface IGeneradorXmlFacturaSri
{
    XmlDocument Generar(FacturaElectronicaSri factura);
}

public interface IValidadorXmlSri
{
    ResultadoValidacionXmlSri ValidarFactura(XmlDocument documento);
}

public interface IValidadorCertificadoSri
{
    ResultadoValidacionCertificadoSri Validar(
        ReadOnlyMemory<byte> pkcs12,
        ReadOnlySpan<char> password,
        string? rucEsperado = null);

    ResultadoValidacionCertificadoSri ValidarLocal(
        ReadOnlyMemory<byte> pkcs12,
        ReadOnlySpan<char> password,
        string? rucEsperado = null) =>
        Validar(pkcs12, password, rucEsperado);

    Task<ResultadoValidacionCertificadoSri> ValidarAsync(
        ReadOnlyMemory<byte> pkcs12,
        string password,
        string? rucEsperado = null,
        bool forzarConsultaRevocacion = false,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Validar(pkcs12, password.AsSpan(), rucEsperado));
}

public interface IFirmadorXadesSri
{
    XmlDocument Firmar(
        XmlDocument documento,
        ReadOnlyMemory<byte> pkcs12,
        ReadOnlySpan<char> password,
        DateTimeOffset? fechaFirma = null);

    ResultadoVerificacionFirmaSri Verificar(XmlDocument documentoFirmado);
}

public interface IClienteRecepcionSri
{
    Task<ResultadoSri> EnviarAsync(
        int ambiente,
        string claveAcceso,
        ReadOnlyMemory<byte> xmlFirmado,
        CancellationToken cancellationToken = default);
}

public interface IClienteAutorizacionSri
{
    Task<ResultadoSri> ConsultarAsync(
        int ambiente,
        string claveAcceso,
        CancellationToken cancellationToken = default);
}

public interface IDiagnosticoComunicacionSri
{
    Task<ResultadoDiagnosticoComunicacionSri> ProbarAsync(
        int ambiente,
        CancellationToken cancellationToken = default);
}

public interface IProtectorSecretosLocal
{
    byte[] Proteger(ReadOnlySpan<byte> valor, string proposito);
    byte[] Desproteger(ReadOnlySpan<byte> valorProtegido, string proposito);
}

public interface IAlmacenamientoCertificadoSri
{
    Task<string> GuardarAsync(
        long empresaId,
        ReadOnlyMemory<byte> pkcs12,
        string password,
        CancellationToken cancellationToken = default);

    Task<MaterialCertificadoSri> LeerAsync(
        long empresaId,
        string referencia,
        CancellationToken cancellationToken = default);

    Task EliminarAsync(
        long empresaId,
        string referencia,
        CancellationToken cancellationToken = default);
}

public interface IConfiguracionFacturacionElectronicaService
{
    Task<ConfiguracionFacturacionElectronicaDto> ObtenerAsync(
        long empresaId,
        long usuarioId,
        CancellationToken cancellationToken = default);

    Task<ConfiguracionFacturacionElectronicaDto> GuardarAmbienteAsync(
        long empresaId,
        long usuarioId,
        long? establecimientoId,
        long? puntoEmisionId,
        string ambienteCodigo,
        bool habilitada,
        uint versionEsperada,
        CancellationToken cancellationToken = default);

    Task<ConfiguracionFacturacionElectronicaDto> ImportarCertificadoAsync(
        long empresaId,
        long usuarioId,
        string nombreArchivo,
        ReadOnlyMemory<byte> pkcs12,
        string password,
        uint versionEsperada,
        CancellationToken cancellationToken = default);

    Task<ResultadoDiagnosticoFacturacionElectronica> DiagnosticarAsync(
        long empresaId,
        long usuarioId,
        long? establecimientoId,
        long? puntoEmisionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SecuencialComprobanteSriDto>>
        ObtenerNumeracionesAsync(
            long empresaId,
            long usuarioId,
            string ambienteCodigo,
            CancellationToken cancellationToken = default);

    Task ActualizarUltimoSecuencialAsync(
        long empresaId,
        long usuarioId,
        long secuencialComprobanteId,
        int ultimoSecuencialEsperado,
        int nuevoUltimoSecuencial,
        CancellationToken cancellationToken = default);

    Task<AdministracionPuntosEmisionSriDto> ObtenerPuntosEmisionAsync(
        long empresaId,
        long usuarioId,
        CancellationToken cancellationToken = default);

    Task CrearPuntoEmisionAsync(
        long empresaId,
        long usuarioId,
        CrearPuntoEmisionSriDto solicitud,
        CancellationToken cancellationToken = default);

    Task RenombrarPuntoEmisionAsync(
        long empresaId,
        long usuarioId,
        long puntoEmisionId,
        string nombre,
        CancellationToken cancellationToken = default);

    Task EstablecerPuntoEmisionPredeterminadoAsync(
        long empresaId,
        long usuarioId,
        long puntoEmisionId,
        CancellationToken cancellationToken = default);

    Task CambiarEstadoPuntoEmisionAsync(
        long empresaId,
        long usuarioId,
        long puntoEmisionId,
        bool activar,
        CancellationToken cancellationToken = default);
}

public interface IAsignadorSecuencialComprobanteSri
{
    Task<int> AsignarAsync(
        long empresaId,
        long establecimientoId,
        long puntoEmisionId,
        long tipoComprobanteId,
        long tipoAmbienteId,
        CancellationToken cancellationToken = default);
}

public interface IMotorFacturaElectronicaSri
{
    Task<ResultadoMotorFacturaSri> ProcesarAsync(
        FacturaElectronicaSri factura,
        ReadOnlyMemory<byte> pkcs12,
        string password,
        CancellationToken cancellationToken = default);
}
