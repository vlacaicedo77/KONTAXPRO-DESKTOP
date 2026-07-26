using KONTAXPRO.Application.Models.Common;

namespace KONTAXPRO.Application.Interfaces;

public interface IProductCatalogService
{
    Task<List<CatalogItemDto>> ObtenerCategoriasAsync(
        long empresaId,
        CancellationToken cancellationToken = default);

    Task<List<CatalogItemDto>> ObtenerMarcasAsync(
        CancellationToken cancellationToken = default);

    Task<List<CatalogItemDto>> ObtenerUnidadesMedidaAsync(
        CancellationToken cancellationToken = default);

    Task<List<CatalogItemDto>> ObtenerTarifasImpuestoAsync(
        CancellationToken cancellationToken = default);

    Task<CatalogCreateResult> CrearCategoriaAsync(
        long empresaId,
        string nombre,
        CancellationToken cancellationToken = default);

    Task<CatalogCreateResult> CrearMarcaAsync(
        string nombre,
        CancellationToken cancellationToken = default);
}