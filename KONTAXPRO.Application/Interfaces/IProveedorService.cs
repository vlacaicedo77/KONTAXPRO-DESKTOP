using KONTAXPRO.Application.Models.Proveedores;

namespace KONTAXPRO.Application.Interfaces;

public interface IProveedorService
{
    Task<ProveedorCatalogoResultadoDto> ObtenerProveedoresAsync(
        ProveedorCatalogoQuery query,
        CancellationToken cancellationToken = default);

    Task<ProveedorDetalleDto?> ObtenerProveedorAsync(
        long terceroId,
        CancellationToken cancellationToken = default);

    Task<ProveedorDetalleDto?> BuscarPorRucAsync(
        string ruc,
        CancellationToken cancellationToken = default);

    Task<ProveedorOperationResult> GuardarAsync(
        ProveedorGuardarRequest request,
        CancellationToken cancellationToken = default);

    Task<ProveedorOperationResult> CambiarEstadoAsync(
        long terceroId,
        int estado,
        uint version,
        CancellationToken cancellationToken = default);
}
