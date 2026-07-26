using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Comercial;
using KONTAXPRO.Domain.Entities.Compras;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Seguridad;
using KONTAXPRO.Domain.Entities.Ventas;

namespace KONTAXPRO.Domain.Entities.Tributacion;

public sealed class RetencionEmitida
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long EstablecimientoId { get; set; }
    public long PuntoEmisionId { get; set; }
    public long EmpresaTerceroId { get; set; }
    public long UsuarioId { get; set; }
    public long TipoComprobanteId { get; set; }
    public long TipoOrigenRetencionEmitidaId { get; set; }
    public long OrigenId { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public int Secuencial { get; set; }
    public DateOnly FechaEmision { get; set; }
    public string PeriodoFiscal { get; set; } = string.Empty;
    public decimal TotalRetenido { get; set; }
    public string Estado { get; set; } = "EMITIDA";
    public long? AnuladoPorUsuarioId { get; set; }
    public DateTime? AnuladaAt { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
    public Establecimiento? Establecimiento { get; set; }
    public PuntoEmision? PuntoEmision { get; set; }
    public EmpresaTercero? EmpresaTercero { get; set; }
    public Usuario? Usuario { get; set; }
    public TipoComprobante? TipoComprobante { get; set; }
    public TipoOrigenRetencionEmitida? TipoOrigenRetencionEmitida { get; set; }
    public Usuario? AnuladoPorUsuario { get; set; }
    public ICollection<RetencionEmitidaDetalle> Detalles { get; set; } = [];
}

public sealed class RetencionEmitidaDetalle
{
    public long Id { get; set; }
    public long RetencionEmitidaId { get; set; }
    public long ConceptoRetencionId { get; set; }
    public string CodigoImpuestoSri { get; set; } = string.Empty;
    public string CodigoRetencionSri { get; set; } = string.Empty;
    public decimal BaseImponible { get; set; }
    public decimal PorcentajeRetencion { get; set; }
    public decimal ValorRetenido { get; set; }
    public DateTime CreatedAt { get; set; }
    public RetencionEmitida? RetencionEmitida { get; set; }
    public ConceptoRetencion? ConceptoRetencion { get; set; }
}

public sealed class RetencionRecibida
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long EmpresaTerceroId { get; set; }
    public long? DocumentoRecibidoSriId { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public string? ClaveAcceso { get; set; }
    public DateOnly FechaEmision { get; set; }
    public string PeriodoFiscal { get; set; } = string.Empty;
    public decimal TotalRetenido { get; set; }
    public string Estado { get; set; } = "REGISTRADA";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
    public EmpresaTercero? EmpresaTercero { get; set; }
    public DocumentoRecibidoSri? DocumentoRecibidoSri { get; set; }
    public ICollection<RetencionRecibidaDocumento> Documentos { get; set; } = [];
    public ICollection<RetencionRecibidaDetalle> Detalles { get; set; } = [];
}

public sealed class RetencionRecibidaDocumento
{
    public long Id { get; set; }
    public long RetencionRecibidaId { get; set; }
    public long FacturaId { get; set; }
    public decimal ValorRetenidoAplicado { get; set; }
    public DateTime CreatedAt { get; set; }
    public RetencionRecibida? RetencionRecibida { get; set; }
    public Factura? Factura { get; set; }
}

public sealed class RetencionRecibidaDetalle
{
    public long Id { get; set; }
    public long RetencionRecibidaId { get; set; }
    public long? ConceptoRetencionId { get; set; }
    public string CodigoImpuestoSri { get; set; } = string.Empty;
    public string CodigoRetencionSri { get; set; } = string.Empty;
    public decimal BaseImponible { get; set; }
    public decimal PorcentajeRetencion { get; set; }
    public decimal ValorRetenido { get; set; }
    public DateTime CreatedAt { get; set; }
    public RetencionRecibida? RetencionRecibida { get; set; }
    public ConceptoRetencion? ConceptoRetencion { get; set; }
}
