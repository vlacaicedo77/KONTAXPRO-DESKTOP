namespace KONTAXPRO.Domain.Entities.Inventario;

public class MovimientoInventarioDetalle
{
    public long Id { get; set; }
    public long MovimientoInventarioId { get; set; }
    public long ProductoId { get; set; }
    public long ProductoPresentacionId { get; set; }
    public decimal CantidadPresentacion { get; set; }
    public decimal FactorConversion { get; set; }
    public decimal CantidadBase { get; set; }
    public decimal CostoUnitarioBase { get; set; }
    public decimal CostoTotal { get; set; }
    public decimal StockAnterior { get; set; }
    public decimal StockNuevo { get; set; }
    public decimal CostoPromedioAnterior { get; set; }
    public decimal CostoPromedioNuevo { get; set; }
    public bool EsBonificacion { get; set; }
    public string? Observacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public MovimientoInventario? MovimientoInventario { get; set; }
    public Producto? Producto { get; set; }
    public ProductoPresentacion? ProductoPresentacion { get; set; }
    public ICollection<MovimientoInventarioDetalleLote> Lotes { get; set; } = [];
    public ICollection<MovimientoInventarioDetalleSerie> Series { get; set; } = [];
}
