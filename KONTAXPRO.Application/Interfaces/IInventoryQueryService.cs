using KONTAXPRO.Application.Models.Inventario;

namespace KONTAXPRO.Application.Interfaces;

public interface IInventoryQueryService
{
    Task<InventarioCatalogoDto> ListarAsync(
        InventarioCatalogoRequest request,
        CancellationToken cancellationToken = default);

    Task<InventarioCatalogosDto> ObtenerCatalogosAsync(
        long empresaId,
        long usuarioId,
        CancellationToken cancellationToken = default);

    Task<InventarioProductoDetalleDto?> ObtenerDetalleAsync(
        long empresaId,
        long usuarioId,
        long productoId,
        CancellationToken cancellationToken = default);

    Task<KardexPaginaDto> ObtenerKardexPaginadoAsync(
        KardexPaginadoRequest request,
        CancellationToken cancellationToken = default);

    Task<InventarioReconciliacionDto> ReconciliarAsync(
        long empresaId,
        long usuarioId,
        CancellationToken cancellationToken = default);
}
