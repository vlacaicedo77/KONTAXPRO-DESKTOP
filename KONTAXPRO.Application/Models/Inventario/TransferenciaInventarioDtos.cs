namespace KONTAXPRO.Application.Models.Inventario;

public sealed class TransferenciaInventarioRequest
{
    public long EmpresaId { get; set; }
    public long UsuarioId { get; set; }
    public long EstablecimientoOrigenId { get; set; }
    public long EstablecimientoDestinoId { get; set; }
    public long BodegaOrigenId { get; set; }
    public long BodegaDestinoId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string Motivo { get; set; } = string.Empty;
    public string? Observacion { get; set; }
    public List<TransferenciaInventarioLineaRequest> Detalles { get; set; } = [];
}

public sealed class TransferenciaInventarioLineaRequest
{
    public long ProductoId { get; set; }
    public long ProductoPresentacionId { get; set; }
    public decimal Cantidad { get; set; }
    public List<IngresoInventarioLoteRequest> Lotes { get; set; } = [];
    public List<IngresoInventarioSerieRequest> Series { get; set; } = [];
}

public sealed class CorreccionBodegaInventarioInicialRequest
{
    public long EmpresaId { get; set; }
    public long UsuarioId { get; set; }
    public long ProductoId { get; set; }
    public long BodegaOrigenId { get; set; }
    public long BodegaDestinoId { get; set; }
    public decimal CantidadBase { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string Motivo { get; set; } = string.Empty;
    public List<IngresoInventarioLoteRequest> Lotes { get; set; } = [];
    public List<IngresoInventarioSerieRequest> Series { get; set; } = [];
}
