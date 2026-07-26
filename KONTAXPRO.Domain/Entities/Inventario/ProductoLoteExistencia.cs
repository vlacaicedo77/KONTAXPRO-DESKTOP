namespace KONTAXPRO.Domain.Entities.Inventario;

public class ProductoLoteExistencia
{
    public long Id { get; set; }

    public long ProductoLoteId { get; set; }

    public long BodegaId { get; set; }

    public decimal StockActual { get; set; }

    public decimal StockReservado { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public decimal StockDisponible =>
        StockActual - StockReservado;

    public ProductoLote? ProductoLote { get; set; }

    public Bodega? Bodega { get; set; }
}
