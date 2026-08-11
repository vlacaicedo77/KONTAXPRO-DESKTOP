using KONTAXPRO.Application.Models.Compras;

namespace KONTAXPRO.Application.Interfaces;

public interface ICompraRecepcionService
{
    Task<CompraOperationResult> ConfirmarAsync(
        ConfirmarCompraRecepcionRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompraRecepcionResumenDto>> ObtenerConfirmadasAsync(
        long compraId,
        CancellationToken cancellationToken = default);

    Task<CompraOperationResult> AnularAsync(
        AnularCompraRecepcionRequest request,
        CancellationToken cancellationToken = default);
}
