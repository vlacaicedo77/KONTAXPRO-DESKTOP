namespace KONTAXPRO.Domain.Entities.Inventario;

public class ProductoSerie
{
    public long Id { get; set; }

    public long ProductoId { get; set; }

    public long? ProductoLoteId { get; set; }

    public long? BodegaId { get; set; }

    public string NumeroSerie { get; set; } = string.Empty;

    public decimal? CostoUnitarioBase { get; set; }

    public string EstadoSerie { get; set; } = "DISPONIBLE";

    public string? Observacion { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Producto? Producto { get; set; }

    public ProductoLote? ProductoLote { get; set; }

    public Bodega? Bodega { get; set; }
}
