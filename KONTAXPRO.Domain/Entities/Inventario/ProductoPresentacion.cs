using KONTAXPRO.Domain.Entities.Catalogos;

namespace KONTAXPRO.Domain.Entities.Inventario;

public class ProductoPresentacion
{
    public long Id { get; set; }

    public long ProductoId { get; set; }

    public long UnidadMedidaId { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string? CodigoBarras { get; set; }

    public bool CodigoBarrasInterno { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public decimal FactorConversion { get; set; } = 1;

    public bool EsPresentacionBase { get; set; }

    public bool PermiteCompra { get; set; } = true;

    public bool PermiteVenta { get; set; } = true;

    public short Estado { get; set; } = 1;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Producto? Producto { get; set; }

    public UnidadMedida? UnidadMedida { get; set; }

    public ICollection<ProductoPresentacionPrecio> Precios { get; set; }
        = new List<ProductoPresentacionPrecio>();
}