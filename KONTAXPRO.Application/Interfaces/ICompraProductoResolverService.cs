using KONTAXPRO.Application.Models.Compras;

namespace KONTAXPRO.Application.Interfaces;

public interface ICompraProductoResolverService
{
    Task<IReadOnlyList<LineaCompraResueltaDto>> ResolverAsync(
        ResolverProductosCompraRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CandidatoProductoCompraDto>> BuscarPresentacionesAsync(
        string? busqueda = null,
        long? productoId = null,
        int limite = 20,
        CancellationToken cancellationToken = default);

    Task<CompraOperationResult> GuardarEquivalenciaAsync(
        GuardarEquivalenciaProveedorProductoRequest request,
        CancellationToken cancellationToken = default);
}
