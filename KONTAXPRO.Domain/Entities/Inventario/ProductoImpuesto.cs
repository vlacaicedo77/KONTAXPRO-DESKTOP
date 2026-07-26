using KONTAXPRO.Domain.Entities.Catalogos;

namespace KONTAXPRO.Domain.Entities.Inventario;

public class ProductoImpuesto
{
    public long Id { get; set; }
    public long ProductoId { get; set; }
    public long TarifaImpuestoId { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Producto? Producto { get; set; }
    public TarifaImpuesto? TarifaImpuesto { get; set; }
}
