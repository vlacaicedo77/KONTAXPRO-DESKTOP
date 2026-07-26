using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Configuracion;

namespace KONTAXPRO.Domain.Entities.Inventario;

public class Producto
{
    public long Id { get; set; }
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long EmpresaId { get; set; }
    public long? CategoriaProductoId { get; set; }
    public long? MarcaId { get; set; }
    public long UnidadMedidaBaseId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Modelo { get; set; }
    public string TipoProducto { get; set; } = "PRODUCTO";
    public bool ManejaInventario { get; set; } = true;
    public bool ManejaLotes { get; set; }
    public bool ManejaSeries { get; set; }
    public bool ManejaFechaCaducidad { get; set; }
    public bool AlertaCaducidad { get; set; }
    public int? DiasAlertaCaducidad { get; set; }
    public string? Observacion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Empresa? Empresa { get; set; }
    public CategoriaProducto? CategoriaProducto { get; set; }
    public Marca? Marca { get; set; }
    public UnidadMedida? UnidadMedidaBase { get; set; }
    public ICollection<ProductoPresentacion> Presentaciones { get; set; } = [];
    public ICollection<ProductoImpuesto> Impuestos { get; set; } = [];
    public ICollection<ProductoExistencia> Existencias { get; set; } = [];
    public ICollection<ProductoLote> Lotes { get; set; } = [];
    public ICollection<ProductoSerie> Series { get; set; } = [];
    public ProductoCosto? Costo { get; set; }
    public ICollection<MovimientoInventarioDetalle> MovimientosDetalles
        { get; set; } = [];
}
