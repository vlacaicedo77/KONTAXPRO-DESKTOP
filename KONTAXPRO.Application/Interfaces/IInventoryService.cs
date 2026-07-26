using KONTAXPRO.Application.Models.Inventario;

namespace KONTAXPRO.Application.Interfaces;

public interface IInventoryService
{
    Task<InventoryOperationResult> RegistrarIngresoInicialAsync(
        IngresoInventarioRequest request,
        CancellationToken cancellationToken = default);
}