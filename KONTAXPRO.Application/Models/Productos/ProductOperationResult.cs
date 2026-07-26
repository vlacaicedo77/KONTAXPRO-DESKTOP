namespace KONTAXPRO.Application.Models.Productos;

public class ProductOperationResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public long? ProductoId { get; set; }

    public static ProductOperationResult Ok(
        long productoId,
        string message)
    {
        return new ProductOperationResult
        {
            Success = true,
            ProductoId = productoId,
            Message = message
        };
    }

    public static ProductOperationResult Fail(string message)
    {
        return new ProductOperationResult
        {
            Success = false,
            Message = message
        };
    }
}