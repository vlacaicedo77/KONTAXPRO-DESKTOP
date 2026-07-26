namespace KONTAXPRO.Domain.Entities.Inventario;

public class ProductoLoteExistencia
{
    public long Id { get; set; }
    public long LoteId { get; set; }
    public long BodegaId { get; set; }
    public decimal StockActual { get; set; }
    public decimal StockReservado { get; set; }
    public string? Ubicacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public decimal StockDisponible => StockActual - StockReservado;
    public ProductoLote? Lote { get; set; }
    public Bodega? Bodega { get; set; }
}
