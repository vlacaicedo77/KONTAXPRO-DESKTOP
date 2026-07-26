namespace KONTAXPRO.Domain.Entities.Inventario;

public class ProductoExistencia
{
    public long Id { get; set; }

    public long ProductoId { get; set; }

    public long BodegaId { get; set; }

    public decimal StockActual { get; set; }

    public decimal StockReservado { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public decimal StockDisponible =>
        StockActual - StockReservado;

    public Producto? Producto { get; set; }

    public Bodega? Bodega { get; set; }
}