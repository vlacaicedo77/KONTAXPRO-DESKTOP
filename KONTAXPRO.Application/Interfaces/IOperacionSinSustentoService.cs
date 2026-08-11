using KONTAXPRO.Application.Models.Tesoreria;

namespace KONTAXPRO.Application.Interfaces;

public interface IOperacionSinSustentoService
{
    Task<OperacionSinSustentoCatalogoDto> ListarAsync(
        OperacionSinSustentoCatalogoRequest request,
        CancellationToken cancellationToken = default);

    Task<OperacionSinSustentoDetalleDto?> ObtenerAsync(
        long empresaId, long usuarioId, long id,
        CancellationToken cancellationToken = default);
    Task<OperacionSinSustentoCatalogosDto> ObtenerCatalogosAsync(
        long empresaId,
        long establecimientoId,
        long usuarioId,
        CancellationToken cancellationToken = default);

    Task<OperacionSinSustentoResult> RegistrarAsync(
        OperacionSinSustentoRequest request,
        CancellationToken cancellationToken = default);

    Task<OperacionSinSustentoResult> CorregirAsync(
        OperacionSinSustentoRequest request,
        CancellationToken cancellationToken = default);

    Task<OperacionSinSustentoResult> AnularAsync(
        AnularOperacionSinSustentoRequest request,
        CancellationToken cancellationToken = default);

    Task<SoporteSinSustentoContenidoDto> ObtenerEvidenciaAsync(
        long empresaId, long usuarioId, long operacionId,
        CancellationToken cancellationToken = default);
}
