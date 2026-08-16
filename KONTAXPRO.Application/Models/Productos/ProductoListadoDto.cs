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

    public string TipoControl { get; set; } = "NORMAL";

    public string TipoProducto { get; set; } = "PRODUCTO";

    public string ControlVisual => TipoProducto == "SERVICIO"
        ? "SERVICIO"
        : TipoControl;

    public string TarifaImpuesto { get; set; } = string.Empty;

    public string TipoCalculoImpuesto { get; set; } = "NINGUNO";

    public decimal? PorcentajeImpuesto { get; set; }

    public decimal? ValorEspecificoImpuesto { get; set; }

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

    public IReadOnlyList<ProductoPrecioPresentacionDto> PreciosPorPresentacion
        { get; set; } = [];

    public ProductoPrecioPresentacionDto? PrecioPresentacionPrincipal =>
        PreciosPorPresentacion.FirstOrDefault(x => x.EsBase) ??
        PreciosPorPresentacion.FirstOrDefault();

    public ProductoPrecioFinalListaDto? PrecioPrincipalFinal =>
        PrecioPresentacionPrincipal?.Precios.FirstOrDefault();

    public string CondicionTributaria =>
        string.IsNullOrWhiteSpace(TarifaImpuesto)
            ? "SIN IMPUESTO CONFIGURADO"
            : $"{TarifaImpuesto.ToUpperInvariant()} INCLUIDO";

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

    public IReadOnlyList<ProductoPresentacionCatalogoDto> PresentacionesDetalle
        { get; set; } = [];

    public IReadOnlyList<ProductoPresentacionCatalogoDto> PresentacionesDetalleVisibles =>
        PresentacionesDetalle.Take(3).ToList();

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

public sealed class ProductoPresentacionCatalogoDto
{
    public string Nombre { get; init; } = string.Empty;
    public decimal FactorConversion { get; init; }
    public bool EsBase { get; init; }
    public bool MostrarSeparador { get; set; }

    public string Etiqueta
    {
        get
        {
            var factor = FactorConversion.ToString("0.######",
                System.Globalization.CultureInfo.InvariantCulture);
            var nombre = Nombre.Trim();
            return nombre.EndsWith($"X{factor}",
                       StringComparison.OrdinalIgnoreCase) ||
                   nombre.EndsWith($"×{factor}",
                       StringComparison.OrdinalIgnoreCase)
                ? nombre
                : $"{nombre} ×{factor}";
        }
    }
}

public sealed class ProductoPrecioBaseListaDto
{
    public string ListaCodigo { get; init; } = string.Empty;
    public string ListaNombre { get; init; } = string.Empty;
    public int Orden { get; init; }
    public decimal? Precio { get; init; }
    public string ClasificacionPrecio =>
        ListaCodigo.Trim().ToUpperInvariant() switch
        {
            "B" => "B",
            "C" => "C",
            "D" => "D",
            "E" => "E",
            _ => "A"
        };
}

public sealed class ProductoPrecioPresentacionDto
{
    public string Nombre { get; init; } = string.Empty;
    public decimal FactorConversion { get; init; }
    public bool EsBase { get; init; }
    public IReadOnlyList<ProductoPrecioFinalListaDto> Precios { get; init; } = [];

    public string EtiquetaFactor => FactorConversion.ToString(
        "0.######",
        System.Globalization.CultureInfo.InvariantCulture);
}

public sealed class ProductoPrecioFinalListaDto
{
    public string ListaCodigo { get; init; } = string.Empty;
    public string ListaNombre { get; init; } = string.Empty;
    public int Orden { get; init; }
    public decimal? PrecioNeto { get; init; }
    public decimal? PrecioFinal { get; init; }
    public string ClasificacionPrecio =>
        ListaCodigo.Trim().ToUpperInvariant() switch
        {
            "B" => "B",
            "C" => "C",
            "D" => "D",
            "E" => "E",
            _ => "A"
        };
}
