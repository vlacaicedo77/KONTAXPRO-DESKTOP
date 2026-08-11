namespace KONTAXPRO.Application.Models.Compras;

public sealed class CompraOperationResult
{
    public bool Success { get; init; }
    public bool ConcurrencyConflict { get; init; }
    public long? Id { get; init; }
    public string Message { get; init; } = string.Empty;

    public static CompraOperationResult Ok(long id, string message) => new()
    {
        Success = true,
        Id = id,
        Message = message
    };

    public static CompraOperationResult Fail(string message) => new()
    {
        Message = message
    };

    public static CompraOperationResult Conflict(string message) => new()
    {
        ConcurrencyConflict = true,
        Message = message
    };
}

public sealed class GuardarEquivalenciaProveedorProductoRequest
{
    public long TerceroProveedorId { get; init; }
    public long ProductoPresentacionId { get; init; }
    public string CodigoProveedor { get; init; } = string.Empty;
    public string TipoCodigo { get; init; } = "PRINCIPAL";
    public string? DescripcionOriginal { get; init; }
    public bool ReemplazarExistente { get; init; }
}
