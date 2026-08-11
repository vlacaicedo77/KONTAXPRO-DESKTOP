namespace KONTAXPRO.Application.Models.Compras;

public enum ErrorLecturaComprobanteCompra
{
    Ninguno,
    ArchivoVacio,
    ArchivoDemasiadoGrande,
    XmlInseguro,
    XmlMalFormado,
    TipoNoSoportado,
    DatosObligatoriosFaltantes,
    ClaveAccesoInvalida,
    FirmaElectronicaInvalida,
    ComprobanteNoAutorizado
}

public sealed class LecturaComprobanteCompraResultado
{
    public bool Exito { get; init; }
    public ErrorLecturaComprobanteCompra Error { get; init; }
    public string Mensaje { get; init; } = string.Empty;
    public FacturaCompraXmlDto? Factura { get; init; }

    public static LecturaComprobanteCompraResultado Correcto(
        FacturaCompraXmlDto factura) => new()
    {
        Exito = true,
        Factura = factura
    };

    public static LecturaComprobanteCompraResultado Fallo(
        ErrorLecturaComprobanteCompra error,
        string mensaje) => new()
    {
        Error = error,
        Mensaje = mensaje
    };
}

public sealed class FacturaCompraXmlDto
{
    public string NombreArchivo { get; init; } = string.Empty;
    public long TamanoArchivo { get; init; }
    public string Sha256 { get; init; } = string.Empty;
    public string ComprobanteSha256 { get; init; } = string.Empty;
    public string Ambiente { get; init; } = string.Empty;
    public string TipoEmision { get; init; } = string.Empty;
    public string RazonSocialEmisor { get; init; } = string.Empty;
    public string? NombreComercialEmisor { get; init; }
    public string RucEmisor { get; init; } = string.Empty;
    public string ClaveAcceso { get; init; } = string.Empty;
    public string CodigoDocumento { get; init; } = string.Empty;
    public string Establecimiento { get; init; } = string.Empty;
    public string PuntoEmision { get; init; } = string.Empty;
    public string Secuencial { get; init; } = string.Empty;
    public string NumeroDocumento =>
        $"{Establecimiento}-{PuntoEmision}-{Secuencial}";
    public string? DireccionMatriz { get; init; }
    public DateOnly FechaEmision { get; init; }
    public DateTime? FechaAutorizacion { get; set; }
    public string? DireccionEstablecimiento { get; init; }
    public string? ContribuyenteEspecial { get; init; }
    public string? ObligadoContabilidad { get; init; }
    public string TipoIdentificacionComprador { get; init; } = string.Empty;
    public string RazonSocialComprador { get; init; } = string.Empty;
    public string IdentificacionComprador { get; init; } = string.Empty;
    public decimal TotalSinImpuestos { get; init; }
    public decimal TotalDescuento { get; init; }
    public decimal Propina { get; init; }
    public decimal ImporteTotal { get; init; }
    public string? Moneda { get; init; }
    public bool FirmaPresente { get; set; }
    public bool FirmaValida { get; set; }
    public bool IncluyeAutorizacionSri { get; init; }
    public string? NumeroAutorizacion { get; set; }
    public string? EstadoAutorizacionSri { get; set; }
    public string EstadoValidacion { get; set; } = "VALIDADO_LOCALMENTE";
    public string MensajeValidacion { get; set; } =
        "El documento superó las validaciones locales.";
    public IReadOnlyList<ImpuestoCompraXmlDto> Impuestos { get; init; }
        = [];
    public IReadOnlyList<DetalleFacturaCompraXmlDto> Detalles { get; init; }
        = [];
    public IReadOnlyList<PagoCompraXmlDto> Pagos { get; init; } = [];
    public IReadOnlyDictionary<string, string> InformacionAdicional { get; init; }
        = new Dictionary<string, string>();
}

public enum EstadoConsultaAutorizacionSri
{
    Autorizado,
    NoAutorizado,
    PendienteAnulacion,
    Anulado,
    NoEncontrado,
    NoDisponible
}

public sealed class ConsultaAutorizacionSriDto
{
    public EstadoConsultaAutorizacionSri Estado { get; init; }
    public string? EstadoSri { get; init; }
    public string? NumeroAutorizacion { get; init; }
    public string? RucEmisor { get; init; }
    public DateTime? FechaAutorizacion { get; init; }
    public string? ComprobanteSha256 { get; init; }
    public string Mensaje { get; init; } = string.Empty;
}

public sealed class DetalleFacturaCompraXmlDto
{
    public int Orden { get; init; }
    public string? CodigoPrincipal { get; init; }
    public string? CodigoAuxiliar { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public decimal Cantidad { get; init; }
    public decimal PrecioUnitario { get; init; }
    public decimal Descuento { get; init; }
    public decimal PrecioTotalSinImpuesto { get; init; }
    public IReadOnlyList<ImpuestoCompraXmlDto> Impuestos { get; init; } = [];
}

public sealed class ImpuestoCompraXmlDto
{
    public string Codigo { get; init; } = string.Empty;
    public string CodigoPorcentaje { get; init; } = string.Empty;
    public decimal? Tarifa { get; init; }
    public decimal BaseImponible { get; init; }
    public decimal Valor { get; init; }
}

public sealed class PagoCompraXmlDto
{
    public string FormaPago { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public int? Plazo { get; init; }
    public string? UnidadTiempo { get; init; }
}
