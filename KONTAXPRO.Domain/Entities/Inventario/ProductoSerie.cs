using KONTAXPRO.Domain.Entities.Catalogos;

namespace KONTAXPRO.Domain.Entities.Inventario;

public class ProductoSerie
{
    public long Id { get; set; }
    public long ProductoId { get; set; }
    public long? ProductoLoteId { get; set; }
    public long BodegaId { get; set; }
    public string NumeroSerie { get; set; } = string.Empty;
    public long EstadoSerieId { get; set; }
    public string? Ubicacion { get; set; }
    public string? Observacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Producto? Producto { get; set; }
    public ProductoLote? ProductoLote { get; set; }
    public Bodega? Bodega { get; set; }
    public EstadoSerie? EstadoSerie { get; set; }
}
