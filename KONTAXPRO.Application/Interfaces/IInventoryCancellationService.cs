using KONTAXPRO.Application.Models.Inventario;

namespace KONTAXPRO.Application.Interfaces;

public interface IInventoryCancellationService
{
    Task<InventoryOperationResult> AnularAjusteAsync(
        InventoryCancellationRequest request,
        CancellationToken cancellationToken = default);

    Task<InventoryOperationResult> AnularInventarioInicialAsync(
        InventoryCancellationRequest request,
        CancellationToken cancellationToken = default);

    Task<InventoryOperationResult> AnularTransferenciaAsync(
        InventoryCancellationRequest request,
        CancellationToken cancellationToken = default);
}
