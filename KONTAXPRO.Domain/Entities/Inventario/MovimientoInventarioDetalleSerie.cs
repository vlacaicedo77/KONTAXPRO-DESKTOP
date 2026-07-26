namespace KONTAXPRO.Domain.Entities.Inventario;

public class MovimientoInventarioDetalleSerie
{
    public long Id { get; set; }
    public long MovimientoInventarioDetalleId { get; set; }
    public long ProductoSerieId { get; set; }
    public DateTime CreatedAt { get; set; }
    public MovimientoInventarioDetalle? MovimientoInventarioDetalle { get; set; }
    public ProductoSerie? ProductoSerie { get; set; }
}
