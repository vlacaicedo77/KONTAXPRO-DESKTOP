namespace KONTAXPRO.Application.Models.Productos;

public class ProductoGuardarRequest
{
    public long? Id { get; set; }

    public long EmpresaId { get; set; }

    public long? CategoriaProductoId { get; set; }

    public long? MarcaId { get; set; }

    public long UnidadMedidaBaseId { get; set; }

    public long TarifaImpuestoId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Modelo { get; set; }

    public string PresentacionNombre { get; set; } = string.Empty;

    public string? CodigoBarras { get; set; }

    public bool SinCodigoBarras { get; set; }

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
}