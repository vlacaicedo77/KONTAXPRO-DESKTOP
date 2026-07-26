using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Configuracion;

namespace KONTAXPRO.Domain.Entities.Inventario;

public class Producto
{
    public long Id { get; set; }

    public long EmpresaId { get; set; }

    public long? CategoriaProductoId { get; set; }

    public long? MarcaId { get; set; }

    public long UnidadMedidaBaseId { get; set; }

    public long TarifaImpuestoId { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Modelo { get; set; }

    public string? Descripcion { get; set; }

    public string TipoProducto { get; set; } = "PRODUCTO";

    public string TipoControlInventario { get; set; } = "NORMAL";

    public bool ManejaInventario { get; set; } = true;

    public bool PermiteVentaSinStock { get; set; }

    public bool ManejaLotes { get; set; }

    public bool ManejaSeries { get; set; }

    public bool ManejaFechaCaducidad { get; set; }

    public bool AlertaStockMinimo { get; set; } = true;

    public bool AlertaCaducidad { get; set; } = true;

    public decimal StockMinimo { get; set; }

    public int DiasAlertaCaducidad { get; set; } = 30;

    public string? Observacion { get; set; }

    public short Estado { get; set; } = 1;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Empresa? Empresa { get; set; }

    public CategoriaProducto? CategoriaProducto { get; set; }

    public Marca? Marca { get; set; }

    public UnidadMedida? UnidadMedidaBase { get; set; }

    public TarifaImpuesto? TarifaImpuesto { get; set; }

    public ICollection<ProductoPresentacion> Presentaciones { get; set; }
        = new List<ProductoPresentacion>();

    public ICollection<ProductoExistencia> Existencias { get; set; }
    = new List<ProductoExistencia>();

    public ICollection<ProductoLote> Lotes { get; set; }
        = new List<ProductoLote>();

    public ICollection<ProductoSerie> Series { get; set; }
        = new List<ProductoSerie>();

    public ProductoCosto? Costo { get; set; }

    public ICollection<MovimientoInventarioDetalle> MovimientosInventarioDetalles
    { get; set; }
        = new List<MovimientoInventarioDetalle>();
}