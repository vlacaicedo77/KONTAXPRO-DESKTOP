using KONTAXPRO.Application.Models.Productos;

namespace KONTAXPRO.Application.Interfaces;

public interface IProductService
{
    Task<ProductoDetalleDto?> ObtenerProductoAsync(
        long productoId,
        long empresaId,
        CancellationToken cancellationToken = default);

    Task<ProductOperationResult> GuardarProductoAsync(
        ProductoGuardarRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductOperationResult> CambiarEstadoAsync(
        long productoId,
        long empresaId,
        short nuevoEstado,
        CancellationToken cancellationToken = default);

    Task<ProductOperationResult> EliminarBorradorContextualAsync(
        long productoId,
        long empresaId,
        CancellationToken cancellationToken = default);

    Task<ProductoCodigoBarrasDto?> BuscarPorCodigoBarrasAsync(
        long empresaId,
        string codigoBarras,
        CancellationToken cancellationToken = default);

    Task<ProductoCatalogoResultadoDto> ObtenerCatalogoProductosAsync(
        ProductoCatalogoQuery query,
        CancellationToken cancellationToken = default);

    Task<List<ProductoSugerenciaDto>> BuscarSimilaresAsync(
    long empresaId,
    string? nombre,
    string? modelo,
    int limite = 5,
    CancellationToken cancellationToken = default);

}
