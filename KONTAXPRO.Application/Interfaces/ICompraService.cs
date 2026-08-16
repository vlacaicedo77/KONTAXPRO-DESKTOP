using KONTAXPRO.Application.Models.Compras;

namespace KONTAXPRO.Application.Interfaces;

public interface ICompraService
{
    Task<CompraCatalogoDto> ObtenerCatalogoAsync(
        string? busqueda = null,
        string? estado = null,
        int pagina = 1,
        int tamanoPagina = 25,
        CompraCatalogoOrden orden = CompraCatalogoOrden.Fecha,
        bool ordenDescendente = true,
        CancellationToken cancellationToken = default);

    Task<CompraDetalleDto?> ObtenerDetalleAsync(
        long compraId,
        CancellationToken cancellationToken = default);

    Task<CompraFormularioCatalogosDto> ObtenerCatalogosFormularioAsync(
        CancellationToken cancellationToken = default);

    Task<CompraOperationResult> GuardarImportadaAsync(
        GuardarCompraImportadaRequest request,
        CancellationToken cancellationToken = default);

    Task<CompraOperationResult> GuardarManualAsync(
        GuardarCompraManualRequest request,
        CancellationToken cancellationToken = default);

    Task<CompraManualEdicionDto?> ObtenerManualParaEdicionAsync(
        long compraId,
        CancellationToken cancellationToken = default);

    Task<CompraOperationResult> SustituirManualAsync(
        long compraId,
        GuardarCompraManualRequest request,
        CancellationToken cancellationToken = default);

    Task<CompraOperationResult> AnularAsync(
        long compraId,
        string motivo,
        CancellationToken cancellationToken = default);
}
