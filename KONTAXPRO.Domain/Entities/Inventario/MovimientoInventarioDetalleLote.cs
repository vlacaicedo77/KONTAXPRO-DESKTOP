namespace KONTAXPRO.Domain.Entities.Inventario;

public class MovimientoInventarioDetalleLote
{
    public long Id { get; set; }
    public long MovimientoInventarioDetalleId { get; set; }
    public long ProductoLoteId { get; set; }
    public decimal CantidadBase { get; set; }
    public decimal StockLoteAnterior { get; set; }
    public decimal StockLoteNuevo { get; set; }
    public DateTime CreatedAt { get; set; }
    public MovimientoInventarioDetalle? MovimientoInventarioDetalle { get; set; }
    public ProductoLote? ProductoLote { get; set; }
}
