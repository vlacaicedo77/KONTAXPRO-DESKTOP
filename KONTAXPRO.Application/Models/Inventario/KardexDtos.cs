namespace KONTAXPRO.Application.Models.Inventario;

public sealed class KardexFiltro
{
    public long EmpresaId { get; set; }
    public long UsuarioId { get; set; }
    public long ProductoId { get; set; }
    public long? EstablecimientoId { get; set; }
    public long? BodegaId { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public long? TipoMovimientoId { get; set; }
    public string? NumeroDocumento { get; set; }
    public long? OrigenTipoId { get; set; }
}

public sealed class KardexItemDto
{
    public long MovimientoId { get; set; }
    public long ProductoId { get; set; }
    public string Producto { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string NumeroMovimiento { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Origen { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public string Bodega { get; set; } = string.Empty;
    public string Presentacion { get; set; } = string.Empty;
    public decimal CantidadPresentacion { get; set; }
    public decimal Factor { get; set; }
    public decimal EntradaBase { get; set; }
    public decimal SalidaBase { get; set; }
    public decimal StockAnterior { get; set; }
    public decimal StockNuevo { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal CostoTotal { get; set; }
    public decimal CostoPromedioAnterior { get; set; }
    public decimal CostoPromedioNuevo { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string? Observacion { get; set; }
    public bool EsEntrada => EntradaBase > 0;
    public bool EsSalida => SalidaBase > 0;
    public string Naturaleza => EsEntrada ? "ENTRADA" : "SALIDA";
    public string DocumentoVisual => string.IsNullOrWhiteSpace(Documento)
        ? NumeroMovimiento
        : Documento;
    public string MovimientoVisual => string.IsNullOrWhiteSpace(NumeroMovimiento)
        ? Tipo
        : $"{Tipo} · {NumeroMovimiento}";
    public string ObservacionVisual => string.IsNullOrWhiteSpace(Observacion)
        ? "Sin observaciones"
        : Observacion;
}
