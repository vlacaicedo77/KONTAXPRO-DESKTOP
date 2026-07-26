namespace KONTAXPRO.Domain.Entities.Inventario;

public class ProductoPresentacionPrecio
{
    public long Id { get; set; }

    public long ProductoPresentacionId { get; set; }

    public long ListaPrecioId { get; set; }

    public string MetodoCalculo { get; set; } = "RECARGO_COSTO";

    public decimal? Porcentaje { get; set; }

    public decimal Precio { get; set; }

    public bool PrecioManual { get; set; }

    public short Estado { get; set; } = 1;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ProductoPresentacion? ProductoPresentacion { get; set; }

    public ListaPrecio? ListaPrecio { get; set; }
}