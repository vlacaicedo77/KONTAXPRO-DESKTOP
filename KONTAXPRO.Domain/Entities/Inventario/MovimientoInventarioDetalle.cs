namespace KONTAXPRO.Domain.Entities.Inventario;

public class MovimientoInventarioDetalle
{
    public long Id { get; set; }

    public long MovimientoInventarioId { get; set; }

    public long ProductoId { get; set; }

    public long? ProductoPresentacionId { get; set; }

    public long? ProductoLoteId { get; set; }

    public long? ProductoSerieId { get; set; }

    public decimal CantidadPresentacion { get; set; }

    public decimal FactorConversion { get; set; } = 1;

    public decimal CantidadBase { get; set; }

    public decimal CostoUnitarioBase { get; set; }

    public decimal CostoTotal { get; set; }

    public bool EsBonificacion { get; set; }

    public string? Observacion { get; set; }

    public DateTime CreatedAt { get; set; }

    public MovimientoInventario? MovimientoInventario { get; set; }

    public Producto? Producto { get; set; }

    public ProductoPresentacion? ProductoPresentacion { get; set; }

    public ProductoLote? ProductoLote { get; set; }

    public ProductoSerie? ProductoSerie { get; set; }
}
