namespace KONTAXPRO.Application.Models.Productos;

public sealed class ProductoPresentacionDto
{
    public long? Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal FactorConversion { get; set; } = 1;
    public bool EsPresentacionBase { get; set; }
    public bool PermiteCompra { get; set; } = true;
    public bool PermiteVenta { get; set; } = true;
    public int Estado { get; set; } = 1;
    public List<ProductoPrecioDto> Precios { get; set; } = [];
}

public sealed class ProductoPrecioDto
{
    public long? Id { get; set; }
    public long ListaPrecioId { get; set; }
    public string ListaPrecioNombre { get; set; } = string.Empty;
    public string MetodoCalculo { get; set; } = "PRECIO_FIJO";
    public decimal? Porcentaje { get; set; }
    public decimal? Precio { get; set; }
    public int Estado { get; set; } = 1;
}

public sealed class ProductoImpuestoDto
{
    public long TarifaImpuestoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Estado { get; set; } = 1;
}

public sealed class ProductoExistenciaDto
{
    public long BodegaId { get; set; }
    public string BodegaCodigo { get; set; } = string.Empty;
    public string BodegaNombre { get; set; } = string.Empty;
    public string BodegaDisplay =>
        BodegaDisplayFormatter.Format(BodegaCodigo, BodegaNombre);
    public decimal StockActual { get; set; }
    public decimal StockReservado { get; set; }
    public decimal Disponible => StockActual - StockReservado;
    public decimal StockMinimo { get; set; }
    public string? Ubicacion { get; set; }
}

public static class BodegaDisplayFormatter
{
    public static string Format(string? codigo, string? nombre) =>
        string.IsNullOrWhiteSpace(codigo)
            ? nombre?.Trim() ?? string.Empty
            : $"{codigo.Trim()} · {nombre?.Trim()}";
}

public sealed class ProductoCostoDto
{
    public decimal UltimoPrecioCompra { get; set; }
    public decimal UltimoCostoEfectivo { get; set; }
    public decimal CostoPromedio { get; set; }
}

public sealed class ListaPrecioEditorDto
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool EsListaBase { get; set; }
    public decimal? PorcentajeDescuentoPredeterminado { get; set; }
}
