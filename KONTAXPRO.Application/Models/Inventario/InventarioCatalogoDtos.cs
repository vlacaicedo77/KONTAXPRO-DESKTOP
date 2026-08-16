namespace KONTAXPRO.Application.Models.Inventario;

public enum InventarioCatalogoOrden
{
    Producto,
    Stock,
    Reservado,
    Disponible,
    CostoPromedio,
    Valor
}

public sealed class InventarioCatalogoRequest
{
    public long EmpresaId { get; set; }
    public long UsuarioId { get; set; }
    public long? EstablecimientoId { get; set; }
    public long? BodegaId { get; set; }
    public string? Busqueda { get; set; }
    public string Indicador { get; set; } = "TODOS";
    public InventarioCatalogoOrden Orden { get; set; } =
        InventarioCatalogoOrden.Producto;
    public bool OrdenDescendente { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 25;
}

public sealed class InventarioCatalogoDto
{
    public List<InventarioItemDto> Items { get; set; } = [];
    public int Total { get; set; }
    public int Productos { get; set; }
    public int SinStock { get; set; }
    public int StockBajo { get; set; }
    public int PorCaducar { get; set; }
    public decimal ValorInventario { get; set; }
}

public sealed class InventarioItemDto
{
    public long ProductoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Producto { get; set; } = string.Empty;
    public string? Marca { get; set; }
    public string? Categoria { get; set; }
    public string? Modelo { get; set; }
    public string TipoControl { get; set; } = "NORMAL";
    public string ControlVisual => TipoControl;
    public string NombreConMarca => string.IsNullOrWhiteSpace(Marca)
        ? Producto : $"{Producto} · {Marca}";
    public bool TieneCategoria => !string.IsNullOrWhiteSpace(Categoria);
    public bool TieneModelo => !string.IsNullOrWhiteSpace(Modelo);
    public bool MostrarSeparadorCategoriaModelo =>
        TieneCategoria && TieneModelo;
    public string CategoriaModeloTexto => TieneCategoria && TieneModelo
        ? $"{Categoria} · {Modelo}"
        : Categoria ?? Modelo ?? string.Empty;
    public List<InventarioPresentacionResumenDto> Presentaciones { get; set; }
        = [];
    public IReadOnlyList<InventarioPresentacionResumenDto>
        PresentacionesVisibles => Presentaciones.Take(3).ToList();
    public IReadOnlyList<InventarioPresentacionResumenDto>
        PresentacionesRestantes => Presentaciones.Skip(3).ToList();
    public int CantidadPresentacionesAdicionales =>
        Math.Max(0, Presentaciones.Count - 3);
    public bool TienePresentacionesAdicionales =>
        CantidadPresentacionesAdicionales > 0;
    public string TextoPresentacionesAdicionales =>
        $"+{CantidadPresentacionesAdicionales} MÁS";
    public string PresentacionesRestantesTexto => string.Join(
        Environment.NewLine,
        PresentacionesRestantes.Select(x => x.Etiqueta));
    public string PresentacionesTexto => string.Join(" · ",
        Presentaciones.Select(x => x.Etiqueta));
    public decimal StockActual { get; set; }
    public decimal StockReservado { get; set; }
    public decimal StockDisponible => StockActual - StockReservado;
    public decimal StockMinimo { get; set; }
    public decimal CostoPromedio { get; set; }
    public decimal Valor => StockActual * CostoPromedio;
    public int BodegasConStock { get; set; }
    public int LotesPorCaducar { get; set; }
    public bool PuedeCompletarInventarioInicial { get; set; }
    public bool EstaSinStock => StockDisponible <= 0;
    public bool EstaBajo => StockDisponible > 0 && StockMinimo > 0 &&
        StockDisponible <= StockMinimo;
}

public sealed class InventarioPresentacionResumenDto
{
    public string Nombre { get; set; } = string.Empty;
    public decimal Factor { get; set; }
    public bool EsBase { get; set; }
    public bool MostrarSeparador { get; set; }
    public string Etiqueta
    {
        get
        {
            var factor = Factor.ToString("0.######",
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

public sealed class InventarioCatalogosDto
{
    public List<InventarioOpcionDto> Bodegas { get; set; } = [];
    public List<InventarioOpcionDto> TiposMovimiento { get; set; } = [];
    public List<InventarioOpcionDto> TiposOrigen { get; set; } = [];
}

public sealed class InventarioOpcionDto
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public long? EstablecimientoId { get; set; }
    public string Display => string.IsNullOrWhiteSpace(Codigo)
        ? Nombre : $"{Codigo} · {Nombre}";
}

public sealed class InventarioProductoDetalleDto
{
    public long ProductoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Producto { get; set; } = string.Empty;
    public string? Marca { get; set; }
    public string? Categoria { get; set; }
    public string? Modelo { get; set; }
    public string TipoControl { get; set; } = "NORMAL";
    public decimal StockActual { get; set; }
    public decimal StockReservado { get; set; }
    public decimal StockDisponible => StockActual - StockReservado;
    public decimal StockMinimo => Bodegas.Sum(x => x.StockMinimo);
    public decimal CostoPromedio { get; set; }
    public decimal ValorInventario => StockActual * CostoPromedio;
    public decimal UltimoCosto { get; set; }
    public decimal UltimoPrecioCompra { get; set; }
    public List<InventarioPresentacionDto> Presentaciones { get; set; } = [];
    public List<InventarioBodegaDetalleDto> Bodegas { get; set; } = [];
    public List<InventarioLoteDetalleDto> Lotes { get; set; } = [];
    public List<InventarioSerieDetalleDto> Series { get; set; } = [];
    public List<KardexItemDto> MovimientosRecientes { get; set; } = [];
    public string NombreConMarca => string.IsNullOrWhiteSpace(Marca)
        ? Producto : $"{Producto} · {Marca}";
    public bool TieneCategoria => !string.IsNullOrWhiteSpace(Categoria);
    public bool TieneModelo => !string.IsNullOrWhiteSpace(Modelo);
    public bool MostrarSeparadorCategoriaModelo =>
        TieneCategoria && TieneModelo;
    public bool ManejaLotes => TipoControl.Contains("LOTE",
        StringComparison.OrdinalIgnoreCase);
    public bool ManejaSeries => TipoControl.Contains("SERIE",
        StringComparison.OrdinalIgnoreCase);
    public bool TieneBodegas => Bodegas.Count > 0;
    public bool TieneLotes => Lotes.Count > 0;
    public bool TieneSeries => Series.Count > 0;
    public bool TieneMovimientosRecientes => MovimientosRecientes.Count > 0;
    public string PresentacionesTexto => string.Join(" · ",
        Presentaciones.Select(x => x.EtiquetaInventario));
}

public sealed class InventarioPresentacionDto
{
    public long Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Factor { get; set; }
    public bool EsBase { get; set; }
    public string EtiquetaInventario
    {
        get
        {
            var factor = Factor.ToString("0.######",
                System.Globalization.CultureInfo.InvariantCulture);
            var nombre = Nombre.Trim();
            return nombre.EndsWith($"X{factor}",
                       StringComparison.OrdinalIgnoreCase) ||
                   nombre.EndsWith($"×{factor}",
                       StringComparison.OrdinalIgnoreCase)
                ? nombre
                : $"{nombre} X{factor}";
        }
    }
    public string Display => EsBase
        ? $"{Nombre} · BASE"
        : $"{Nombre} · X{Factor:0.######}";
}

public sealed class InventarioBodegaDetalleDto
{
    public long BodegaId { get; set; }
    public string Bodega { get; set; } = string.Empty;
    public decimal StockActual { get; set; }
    public decimal StockReservado { get; set; }
    public decimal Disponible => StockActual - StockReservado;
    public decimal StockMinimo { get; set; }
    public string? Ubicacion { get; set; }
}

public sealed class InventarioLoteDetalleDto
{
    public long LoteId { get; set; }
    public long BodegaId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string Bodega { get; set; } = string.Empty;
    public decimal StockActual { get; set; }
    public decimal StockReservado { get; set; }
    public decimal Disponible => StockActual - StockReservado;
    public DateOnly? Elaboracion { get; set; }
    public DateOnly? Caducidad { get; set; }
}

public sealed class InventarioSerieDetalleDto
{
    public long SerieId { get; set; }
    public long BodegaId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string Bodega { get; set; } = string.Empty;
    public string? Lote { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string EstadoTexto => Estado.Replace('_', ' ').ToUpperInvariant();
}

public sealed class KardexPaginadoRequest
{
    public long EmpresaId { get; set; }
    public long UsuarioId { get; set; }
    public long? ProductoId { get; set; }
    public long? EstablecimientoId { get; set; }
    public long? BodegaId { get; set; }
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public long? TipoMovimientoId { get; set; }
    public long? TipoOrigenId { get; set; }
    public string? Busqueda { get; set; }
    public string Naturaleza { get; set; } = "TODAS";
    public bool FechaDescendente { get; set; } = true;
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 25;
}

public sealed class KardexPaginaDto
{
    public List<KardexItemDto> Items { get; set; } = [];
    public int Total { get; set; }
}

public sealed class InventarioReconciliacionDto
{
    public bool EsConsistente => Hallazgos.Count == 0;
    public DateTime VerificadoAt { get; set; }
    public List<InventarioReconciliacionHallazgoDto> Hallazgos { get; set; } = [];
}

public sealed class InventarioReconciliacionHallazgoDto
{
    public long ProductoId { get; set; }
    public string Producto { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}
