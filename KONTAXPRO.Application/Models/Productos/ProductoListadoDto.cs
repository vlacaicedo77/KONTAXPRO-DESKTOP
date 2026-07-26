namespace KONTAXPRO.Application.Models.Productos;

public class ProductoListadoDto
{
    public long Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Categoria { get; set; }

    public string? Marca { get; set; }

    public string UnidadBase { get; set; } = string.Empty;

    public string TarifaImpuesto { get; set; } = string.Empty;

    public decimal StockActual { get; set; }

    public decimal StockMinimo { get; set; }

    public decimal CostoPromedio { get; set; }

    public decimal PrecioPrincipal { get; set; }

    public int DiasAlertaCaducidad { get; set; }

    public bool PorCaducar { get; set; }

    public DateOnly? ProximaCaducidad { get; set; }

    public short Estado { get; set; }

    public bool Activo =>
        Estado == 1;

    public bool TieneStockBajo =>
        StockMinimo > 0 &&
        StockActual > 0 &&
        StockActual <= StockMinimo;

    public bool SinStock =>
        StockActual <= 0;
}