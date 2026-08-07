using KONTAXPRO.Application.Models.Clientes;

namespace KONTAXPRO.Application.Interfaces;

public interface IClienteService
{
    Task<ClienteCatalogoResultadoDto> ObtenerClientesAsync(
        ClienteCatalogoQuery query,
        CancellationToken cancellationToken = default);

    Task<ClienteDetalleDto?> ObtenerClienteAsync(
        long terceroId,
        long empresaId,
        CancellationToken cancellationToken = default);

    Task<TerceroIdentificacionDto?> BuscarPorIdentificacionAsync(
        long tipoIdentificacionId,
        string numeroIdentificacion,
        long empresaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TipoIdentificacionClienteDto>>
        ObtenerTiposIdentificacionAsync(
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ListaPrecioClienteDto>> ObtenerListasPrecioAsync(
        long empresaId,
        CancellationToken cancellationToken = default);

    Task<ClienteOperationResult> GuardarAsync(
        ClienteGuardarRequest request,
        CancellationToken cancellationToken = default);

    Task<ClienteOperationResult> CambiarEstadoAsync(
        long terceroId,
        long empresaId,
        int estado,
        uint version,
        CancellationToken cancellationToken = default);
}
