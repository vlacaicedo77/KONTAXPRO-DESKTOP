namespace KONTAXPRO.Application.Models.Productos;

public class ProductoListadoDto
{
    public long Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Modelo { get; set; }

    public string? Categoria { get; set; }

    public string? Marca { get; set; }

    public string UnidadBase { get; set; } = string.Empty;

    public string TarifaImpuesto { get; set; } = string.Empty;

    public decimal StockDisponible { get; set; }

    public decimal StockActual
    {
        get => StockDisponible;
        set => StockDisponible = value;
    }

    public decimal StockMinimo { get; set; }

    public decimal CostoPromedio { get; set; }

    public decimal? PrecioBase { get; set; }

    public string ListaPrecioBaseCodigo { get; set; } = string.Empty;

    public IReadOnlyList<ProductoPrecioBaseListaDto> PreciosBasePorLista
        { get; set; } = [];

    public decimal PrecioPrincipal
    {
        get => PrecioBase ?? 0;
        set => PrecioBase = value;
    }

    public int DiasAlertaCaducidad { get; set; }

    public bool PorCaducar { get; set; }

    public decimal CantidadPorCaducar { get; set; }

    public DateOnly? ProximaCaducidad { get; set; }

    public short Estado { get; set; }

    public int CantidadPresentaciones { get; set; }

    public IReadOnlyList<string> PresentacionesComerciales { get; set; } = [];

    public IReadOnlyList<string> PresentacionesVisibles =>
        PresentacionesComerciales.Take(3).ToList();

    public int CantidadPresentacionesAdicionales =>
        Math.Max(0, PresentacionesComerciales.Count - 3);

    public bool TienePresentacionesAdicionales =>
        CantidadPresentacionesAdicionales > 0;

    public bool TieneStockBajo { get; set; }

    public bool SinStock { get; set; }

    public bool Activo =>
        Estado == 1;

    public string NombreConMarca => string.IsNullOrWhiteSpace(Marca)
        ? Nombre
        : $"{Nombre} · {Marca}";

    public string EstadoTexto => Activo ? "ACTIVO" : "INACTIVO";
}

public sealed class ProductoPrecioBaseListaDto
{
    public string ListaCodigo { get; init; } = string.Empty;
    public int Orden { get; init; }
    public decimal? Precio { get; init; }
}
