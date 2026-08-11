namespace KONTAXPRO.Application.Models.Compras;

public enum EstadoProveedorImportacion
{
    ProveedorActivo,
    ProveedorInactivo,
    TerceroSinProveedor,
    NoExiste
}

public sealed class CompraCuadreDto
{
    public bool Cuadra { get; init; }
    public decimal SubtotalDetalles { get; init; }
    public decimal ImpuestosDetalles { get; init; }
    public decimal DiferenciaSubtotal { get; init; }
    public decimal DiferenciaTotal { get; init; }
    public string Mensaje { get; init; } = string.Empty;
}

public sealed class CompraImportacionAnalisisDto
{
    public bool Exito { get; init; }
    public string Mensaje { get; init; } = string.Empty;
    public ErrorLecturaComprobanteCompra? ErrorLectura { get; init; }
    public Guid? ImportacionId { get; init; }
    public FacturaCompraXmlDto? Factura { get; init; }
    public CompraCuadreDto? Cuadre { get; init; }
    public bool EsAmbientePruebas { get; init; }
    public string? AdvertenciaAmbiente { get; init; }
    public bool EsDuplicado { get; init; }
    public long? CompraExistenteId { get; init; }
    public long? DocumentoExistenteId { get; init; }
    public EstadoProveedorImportacion EstadoProveedor { get; init; }
    public long? TerceroId { get; init; }
    public string? RazonSocialProveedorLocal { get; init; }
    public string? DireccionProveedorLocal { get; init; }
    public bool ProveedorCreadoAutomaticamente { get; init; }

    public static CompraImportacionAnalisisDto Fallo(
        string mensaje,
        ErrorLecturaComprobanteCompra? errorLectura = null) => new()
    {
        Mensaje = mensaje,
        ErrorLectura = errorLectura
    };
}
