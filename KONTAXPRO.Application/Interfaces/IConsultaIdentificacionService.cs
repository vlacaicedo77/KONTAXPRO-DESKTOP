using KONTAXPRO.Application.Models.Interoperabilidad;

namespace KONTAXPRO.Application.Interfaces;

public interface IConsultaIdentificacionService
{
    Task<ConsultaIdentificacionResult> ConsultarAsync(
        ConsultaIdentificacionRequest request,
        CancellationToken cancellationToken = default);
}

public interface IProveedorConsultaIdentificacion
{
    string Fuente { get; }

    Task<ProveedorIdentificacionResult> ConsultarAsync(
        ConsultaIdentificacionRequest request,
        string numeroNormalizado,
        CancellationToken cancellationToken = default);
}

public interface IConstanciaVerificacionIdentificacionStore
{
    bool TryTake(
        Guid constanciaId,
        long usuarioId,
        string tipoIdentificacionCodigo,
        string numeroNormalizado,
        PropositoConsultaIdentificacion proposito,
        out ConstanciaVerificacionIdentificacion? constancia);
}
