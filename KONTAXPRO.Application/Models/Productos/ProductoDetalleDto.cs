namespace KONTAXPRO.Application.Models.Productos;

public class ProductoDetalleDto
{
    public long Id { get; set; }

    public long EmpresaId { get; set; }

    public long? CategoriaProductoId { get; set; }

    public long? MarcaId { get; set; }

    public long UnidadMedidaBaseId { get; set; }

    public List<ProductoImpuestoDto> Impuestos { get; set; } = [];

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Modelo { get; set; }

    public string PresentacionNombre { get; set; } = string.Empty;

    public string? CodigoBarras { get; set; }

    public bool CodigoBarrasInterno { get; set; }

    public string? Descripcion { get; set; }

    public string TipoProducto { get; set; } = string.Empty;

    public string TipoControlInventario { get; set; } = string.Empty;

    public bool ManejaInventario { get; set; }

    public bool ManejaLotes { get; set; }

    public bool ManejaSeries { get; set; }

    public bool ManejaFechaCaducidad { get; set; }

    public bool AlertaCaducidad { get; set; }

    public decimal StockMinimo { get; set; }

    public int DiasAlertaCaducidad { get; set; }

    public string? Observacion { get; set; }

    public short Estado { get; set; }

    public List<ProductoPresentacionDto> Presentaciones { get; set; } = [];

    public ProductoCostoDto Costo { get; set; } = new();

    public List<ProductoExistenciaDto> Existencias { get; set; } = [];
}
