namespace KONTAXPRO.Application.Models.Compras;

public sealed class ResolverProductosCompraRequest
{
    public long? TerceroProveedorId { get; init; }
    public IReadOnlyList<LineaCompraAResolverDto> Lineas { get; init; } = [];
}

public sealed class LineaCompraAResolverDto
{
    public int Orden { get; init; }
    public string? CodigoPrincipal { get; init; }
    public string? CodigoAuxiliar { get; init; }
    public string Descripcion { get; init; } = string.Empty;
}

public sealed class LineaCompraResueltaDto
{
    public int Orden { get; init; }
    public string Estado { get; init; } = "NO_RECONOCIDA";
    public string Origen { get; init; } = "NINGUNO";
    public string Mensaje { get; init; } = string.Empty;
    public long? ProductoId { get; init; }
    public long? ProductoPresentacionId { get; init; }
    public string? ProductoNombre { get; init; }
    public string? PresentacionNombre { get; init; }
    public decimal? FactorConversion { get; init; }
    public bool ManejaLotes { get; init; }
    public bool ManejaSeries { get; init; }
    public bool ManejaFechaCaducidad { get; init; }
    public IReadOnlyList<CandidatoProductoCompraDto> Candidatos { get; init; }
        = [];
}

public sealed class CandidatoProductoCompraDto
{
    public long ProductoId { get; init; }
    public long ProductoPresentacionId { get; init; }
    public string ProductoNombre { get; init; } = string.Empty;
    public string? MarcaNombre { get; init; }
    public string PresentacionNombre { get; init; } = string.Empty;
    public string PresentacionCodigo { get; init; } = string.Empty;
    public string? CodigoBarras { get; init; }
    public decimal FactorConversion { get; init; }
    public bool EsPresentacionBase { get; init; }
    public bool ManejaLotes { get; init; }
    public bool ManejaSeries { get; init; }
    public bool ManejaFechaCaducidad { get; init; }
    public decimal CostoPromedio { get; init; }
    public decimal Confianza { get; init; }
    public string Motivo { get; init; } = string.Empty;
    public string ProductoConMarca => string.IsNullOrWhiteSpace(MarcaNombre)
        ? ProductoNombre
        : $"{ProductoNombre} · {MarcaNombre}";
    public string ConversionDisplay => EsPresentacionBase
        ? "BASE · ×1"
        : $"×{FactorConversion:0.######}";
    public string PresentationBadgeDisplay => EsPresentacionBase
        ? ConversionDisplay
        : $"{PresentacionCodigo} · {ConversionDisplay}";
    public decimal CostoPromedioPresentacion =>
        CostoPromedio * FactorConversion;
    public string CostoPromedioDisplay =>
        $"Costo prom. presentación: $ {CostoPromedioPresentacion:N2}";
}
