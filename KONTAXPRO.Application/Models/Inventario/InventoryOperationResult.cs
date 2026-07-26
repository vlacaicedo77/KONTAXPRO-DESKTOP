namespace KONTAXPRO.Application.Models.Inventario;

public class InventoryOperationResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public long? MovimientoInventarioId { get; set; }

    public static InventoryOperationResult Ok(
        long movimientoInventarioId,
        string message)
    {
        return new InventoryOperationResult
        {
            Success = true,
            MovimientoInventarioId = movimientoInventarioId,
            Message = message
        };
    }

    public static InventoryOperationResult Fail(string message)
    {
        return new InventoryOperationResult
        {
            Success = false,
            Message = message
        };
    }
}