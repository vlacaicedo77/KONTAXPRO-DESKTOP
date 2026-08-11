namespace KONTAXPRO.Application.Models.Compras;

public sealed class GuardarCompraImportadaRequest
{
    public Guid ImportacionId { get; init; }
    public long TerceroProveedorId { get; init; }
    public long EstablecimientoId { get; init; }
    public bool EsCredito { get; init; }
    public DateOnly? FechaVencimiento { get; init; }
    public string? Observacion { get; init; }
    public GuardarCompraRecepcionInmediataRequest? RecepcionInmediata { get; init; }
    public IReadOnlyList<GuardarLineaCompraImportadaRequest> Lineas { get; init; }
        = [];
}

public sealed class GuardarCompraRecepcionInmediataRequest
{
    public Guid OperacionUuid { get; init; }
    public long BodegaId { get; init; }
    public DateTime FechaRecepcion { get; init; } = DateTime.Now;
    public string? Observacion { get; init; }
    public IReadOnlyList<GuardarCompraRecepcionInmediataLineaRequest> Lineas
        { get; init; } = [];
}

public sealed class GuardarCompraRecepcionInmediataLineaRequest
{
    public int Orden { get; init; }
    public decimal CantidadPresentacion { get; init; }
    public IReadOnlyList<CompraRecepcionLoteRequest> Lotes { get; init; } = [];
    public IReadOnlyList<CompraRecepcionSerieRequest> Series { get; init; } = [];
}

public sealed class GuardarLineaCompraImportadaRequest
{
    public int Orden { get; init; }
    public bool EsInventariable { get; init; }
    public long? ProductoPresentacionId { get; init; }
    public string ClasificacionContable { get; init; } = "INVENTARIO";
    public long? CuentaContableId { get; init; }
    public bool EsBonificacion { get; init; }
}
