using KONTAXPRO.Application.Models.Inventario;

namespace KONTAXPRO.Application.Interfaces;

public interface IInventoryService
{
    Task<InventoryOperationResult> RegistrarIngresoInicialAsync(
        IngresoInventarioRequest request,
        CancellationToken cancellationToken = default);

    Task<List<KardexItemDto>> ObtenerKardexAsync(
        KardexFiltro filtro,
        CancellationToken cancellationToken = default);

    Task<InventoryOperationResult> RegistrarAjusteAsync(
        AjusteInventarioRequest request,
        CancellationToken cancellationToken = default);

    Task<InventoryOperationResult> CorregirLoteAsync(
        CorregirLoteRequest request,
        CancellationToken cancellationToken = default);

    Task<InventoryOperationResult> CorregirSerieAsync(
        CorregirSerieRequest request,
        CancellationToken cancellationToken = default);

    Task<InventoryOperationResult> ConvertirControlAsync(
        ConversionControlInventarioRequest request,
        CancellationToken cancellationToken = default);

    Task<EstadoControlInventarioDto?> ObtenerEstadoControlAsync(
        long empresaId,
        long usuarioId,
        long productoId,
        CancellationToken cancellationToken = default);

    Task<List<MotivoOperacionInventarioDto>> ObtenerMotivosOperacionAsync(
        long empresaId,
        long usuarioId,
        string tipoOperacion,
        CancellationToken cancellationToken = default);

    Task<InventoryOperationResult> CrearMotivoOperacionAsync(
        CrearMotivoOperacionInventarioRequest request,
        CancellationToken cancellationToken = default);
}
