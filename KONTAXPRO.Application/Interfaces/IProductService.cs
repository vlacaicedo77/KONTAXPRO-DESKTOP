using KONTAXPRO.Application.Models.Productos;

namespace KONTAXPRO.Application.Interfaces;

public interface IProductService
{
    Task<List<ProductoListadoDto>> ObtenerProductosAsync(
        long empresaId,
        string? busqueda = null,
        long? categoriaId = null,
        short? estado = 1,
        CancellationToken cancellationToken = default);

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

    Task<ProductoCodigoBarrasDto?> BuscarPorCodigoBarrasAsync(
    string codigoBarras,
    CancellationToken cancellationToken = default);

    Task<List<ProductoSugerenciaDto>> BuscarSimilaresAsync(
    long empresaId,
    string? nombre,
    string? modelo,
    int limite = 5,
    CancellationToken cancellationToken = default);

}