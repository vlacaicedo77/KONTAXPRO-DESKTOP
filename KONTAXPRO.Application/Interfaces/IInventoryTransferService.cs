using KONTAXPRO.Application.Models.Inventario;

namespace KONTAXPRO.Application.Interfaces;

public interface IInventoryTransferService
{
    Task<InventoryOperationResult> RegistrarAsync(
        TransferenciaInventarioRequest request,
        CancellationToken cancellationToken = default);

    Task<InventoryOperationResult> CorregirBodegaInventarioInicialAsync(
        CorreccionBodegaInventarioInicialRequest request,
        CancellationToken cancellationToken = default);
}
