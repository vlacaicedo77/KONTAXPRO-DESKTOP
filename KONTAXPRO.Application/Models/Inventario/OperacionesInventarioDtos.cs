namespace KONTAXPRO.Application.Models.Inventario;

public sealed class AjusteInventarioRequest
{
    public long EmpresaId { get; set; }
    public long EstablecimientoId { get; set; }
    public long BodegaId { get; set; }
    public long UsuarioId { get; set; }
    public string TipoAjuste { get; set; } = "ENTRADA";
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string Motivo { get; set; } = string.Empty;
    public string? Observacion { get; set; }
    public List<IngresoInventarioDetalleRequest> Detalles { get; set; } = [];
}

public sealed class CorregirLoteRequest
{
    public long EmpresaId { get; set; }
    public long ProductoId { get; set; }
    public long LoteId { get; set; }
    public long UsuarioId { get; set; }
    public string NumeroLote { get; set; } = string.Empty;
    public DateTime? FechaElaboracion { get; set; }
    public DateTime? FechaCaducidad { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

public sealed class CorregirSerieRequest
{
    public long EmpresaId { get; set; }
    public long ProductoId { get; set; }
    public long SerieId { get; set; }
    public long UsuarioId { get; set; }
    public string NumeroSerie { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
}

public sealed class ConversionControlInventarioRequest
{
    public long EmpresaId { get; set; }
    public long ProductoId { get; set; }
    public long UsuarioId { get; set; }
    public string TipoControlAnterior { get; set; } = string.Empty;
    public string TipoControlNuevo { get; set; } = string.Empty;
    public bool ControlCaducidad { get; set; }
    public int DiasAnticipacionCaducidad { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public List<ConversionControlBodegaRequest> Bodegas { get; set; } = [];
}

public sealed class ConversionControlBodegaRequest
{
    public long BodegaId { get; set; }
    public decimal StockEsperado { get; set; }
    public List<ConversionControlLoteRequest> Lotes { get; set; } = [];
    public List<ConversionControlSerieRequest> Series { get; set; } = [];
}

public sealed class ConversionControlLoteRequest
{
    public long? ProductoLoteId { get; set; }
    public string NumeroLote { get; set; } = string.Empty;
    public decimal CantidadBase { get; set; }
    public DateTime? FechaElaboracion { get; set; }
    public DateTime? FechaCaducidad { get; set; }
    public bool EsRegularizacion { get; set; }
}

public sealed class ConversionControlSerieRequest
{
    public long? ProductoSerieId { get; set; }
    public string NumeroSerie { get; set; } = string.Empty;
    public string? NumeroLote { get; set; }
}

public sealed class EstadoControlInventarioDto
{
    public long ProductoId { get; set; }
    public string TipoControl { get; set; } = "NORMAL";
    public List<EstadoControlBodegaDto> Bodegas { get; set; } = [];
}

public sealed class EstadoControlBodegaDto
{
    public long BodegaId { get; set; }
    public string BodegaCodigo { get; set; } = string.Empty;
    public string BodegaNombre { get; set; } = string.Empty;
    public string BodegaDisplay =>
        KONTAXPRO.Application.Models.Productos.BodegaDisplayFormatter.Format(
            BodegaCodigo, BodegaNombre);
    public decimal StockActual { get; set; }
    public decimal StockReservado { get; set; }
    public List<EstadoControlLoteDto> Lotes { get; set; } = [];
    public List<EstadoControlSerieDto> Series { get; set; } = [];
}

public sealed class EstadoControlLoteDto
{
    public long LoteId { get; set; }
    public string NumeroLote { get; set; } = string.Empty;
    public decimal StockActual { get; set; }
    public decimal StockReservado { get; set; }
    public decimal Disponible => StockActual - StockReservado;
    public DateTime? FechaElaboracion { get; set; }
    public DateTime? FechaCaducidad { get; set; }
    public decimal? UltimoCostoUnitarioBase { get; set; }
}

public sealed class EstadoControlSerieDto
{
    public long SerieId { get; set; }
    public string NumeroSerie { get; set; } = string.Empty;
    public string? NumeroLote { get; set; }
    public string? Ubicacion { get; set; }
}
