using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Comercial;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Domain.Entities.Seguridad;

namespace KONTAXPRO.Domain.Entities.Ventas;

public abstract class DocumentoVentaBase
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long EstablecimientoId { get; set; }
    public long EmpresaTerceroId { get; set; }
    public long UsuarioId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DescuentoTotal { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "CONFIRMADO";
    public long? AnuladoPorUsuarioId { get; set; }
    public DateTime? AnuladaAt { get; set; }
    public string? MotivoAnulacion { get; set; }
    public string? Observacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
    public Establecimiento? Establecimiento { get; set; }
    public EmpresaTercero? EmpresaTercero { get; set; }
    public Usuario? Usuario { get; set; }
    public Usuario? AnuladoPorUsuario { get; set; }
}

public abstract class DetalleComercialBase
{
    public long Id { get; set; }
    public long ProductoId { get; set; }
    public long ProductoPresentacionId { get; set; }
    public long BodegaId { get; set; }
    public string OrigenFacturable { get; set; } = string.Empty;
    public decimal CantidadPresentacion { get; set; }
    public decimal FactorConversion { get; set; }
    public decimal CantidadBase { get; set; }
    public decimal PrecioReferencia { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal DescuentoPorcentaje { get; set; }
    public decimal DescuentoValor { get; set; }
    public decimal PrecioFinalUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public decimal CostoUnitarioBase { get; set; }
    public long? PrecioModificadoPorUsuarioId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Producto? Producto { get; set; }
    public ProductoPresentacion? ProductoPresentacion { get; set; }
    public Bodega? Bodega { get; set; }
    public Usuario? PrecioModificadoPorUsuario { get; set; }
}

public sealed class VentaXf : DocumentoVentaBase
{
    public string NumeroXf { get; set; } = string.Empty;
    public DateTime FechaVenta { get; set; }
    public ICollection<VentaXfDetalle> Detalles { get; set; } = [];
}

public sealed class VentaXfDetalle : DetalleComercialBase
{
    public long VentaXfId { get; set; }
    public VentaXf? VentaXf { get; set; }
    public ICollection<NotaEntregaXfDetalle> NotasEntrega { get; set; } = [];
    public ICollection<FacturaXfDetalle> Facturas { get; set; } = [];
}

public sealed class NotaEntrega : DocumentoVentaBase
{
    public long? ProformaId { get; set; }
    public string NumeroNota { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public bool EsCredito { get; set; }
    public Proforma? Proforma { get; set; }
    public ICollection<NotaEntregaDetalle> Detalles { get; set; } = [];
}

public sealed class NotaEntregaDetalle : DetalleComercialBase
{
    public long NotaEntregaId { get; set; }
    public NotaEntrega? NotaEntrega { get; set; }
    public ICollection<NotaEntregaXfDetalle> OrigenesXf { get; set; } = [];
    public ICollection<FacturaNotaEntregaDetalle> Facturas { get; set; } = [];
}

public sealed class NotaEntregaXfDetalle
{
    public long Id { get; set; }
    public long NotaEntregaDetalleId { get; set; }
    public long VentaXfDetalleId { get; set; }
    public decimal CantidadBase { get; set; }
    public DateTime CreatedAt { get; set; }
    public NotaEntregaDetalle? NotaEntregaDetalle { get; set; }
    public VentaXfDetalle? VentaXfDetalle { get; set; }
}

public sealed class Factura : DocumentoVentaBase
{
    public long PuntoEmisionId { get; set; }
    public long TipoComprobanteId { get; set; }
    public long? ProformaId { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public int Secuencial { get; set; }
    public DateTime FechaEmision { get; set; }
    public string OrigenFacturacion { get; set; } = "DIRECTA";
    public decimal SubtotalSinImpuestos { get; set; }
    public decimal ImpuestoTotal { get; set; }
    public bool EsCredito { get; set; }
    public long? ReceptorDistintoAutorizadoPorUsuarioId { get; set; }
    public string? MotivoReceptorDistinto { get; set; }
    public PuntoEmision? PuntoEmision { get; set; }
    public TipoComprobante? TipoComprobante { get; set; }
    public Proforma? Proforma { get; set; }
    public Usuario? ReceptorDistintoAutorizadoPorUsuario { get; set; }
    public ICollection<FacturaDetalle> Detalles { get; set; } = [];
    public ICollection<FacturaFormaPago> FormasPago { get; set; } = [];
}

public sealed class FacturaDetalle : DetalleComercialBase
{
    public long FacturaId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public Factura? Factura { get; set; }
    public ICollection<FacturaDetalleImpuesto> Impuestos { get; set; } = [];
    public ICollection<FacturaNotaEntregaDetalle> OrigenesNotaEntrega { get; set; } = [];
    public ICollection<FacturaXfDetalle> OrigenesXf { get; set; } = [];
}

public sealed class FacturaDetalleImpuesto
{
    public long Id { get; set; }
    public long FacturaDetalleId { get; set; }
    public long? TarifaImpuestoId { get; set; }
    public string CodigoImpuestoSri { get; set; } = string.Empty;
    public string CodigoPorcentajeSri { get; set; } = string.Empty;
    public string NombreImpuesto { get; set; } = string.Empty;
    public string TipoCalculo { get; set; } = string.Empty;
    public decimal? Porcentaje { get; set; }
    public decimal? ValorEspecifico { get; set; }
    public decimal BaseImponible { get; set; }
    public decimal ValorImpuesto { get; set; }
    public DateTime CreatedAt { get; set; }
    public FacturaDetalle? FacturaDetalle { get; set; }
    public TarifaImpuesto? TarifaImpuesto { get; set; }
}

public sealed class FacturaNotaEntregaDetalle
{
    public long Id { get; set; }
    public long FacturaDetalleId { get; set; }
    public long NotaEntregaDetalleId { get; set; }
    public decimal CantidadBase { get; set; }
    public DateTime CreatedAt { get; set; }
    public FacturaDetalle? FacturaDetalle { get; set; }
    public NotaEntregaDetalle? NotaEntregaDetalle { get; set; }
}

public sealed class FacturaXfDetalle
{
    public long Id { get; set; }
    public long FacturaDetalleId { get; set; }
    public long VentaXfDetalleId { get; set; }
    public decimal CantidadBase { get; set; }
    public DateTime CreatedAt { get; set; }
    public FacturaDetalle? FacturaDetalle { get; set; }
    public VentaXfDetalle? VentaXfDetalle { get; set; }
}

public sealed class FacturaFormaPago
{
    public long Id { get; set; }
    public long FacturaId { get; set; }
    public long MedioPagoId { get; set; }
    public string CodigoFormaPagoSri { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int? Plazo { get; set; }
    public string? UnidadTiempo { get; set; }
    public DateTime CreatedAt { get; set; }
    public Factura? Factura { get; set; }
    public MedioPago? MedioPago { get; set; }
}

public sealed class Proforma
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long EstablecimientoId { get; set; }
    public long EmpresaTerceroId { get; set; }
    public long UsuarioId { get; set; }
    public string NumeroProforma { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public DateOnly? FechaVigencia { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DescuentoTotal { get; set; }
    public decimal ImpuestoTotal { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "ABIERTA";
    public string? Observacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
    public Establecimiento? Establecimiento { get; set; }
    public EmpresaTercero? EmpresaTercero { get; set; }
    public Usuario? Usuario { get; set; }
    public ICollection<ProformaDetalle> Detalles { get; set; } = [];
}

public sealed class ProformaDetalle
{
    public long Id { get; set; }
    public long ProformaId { get; set; }
    public long ProductoId { get; set; }
    public long ProductoPresentacionId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal CantidadPresentacion { get; set; }
    public decimal FactorConversion { get; set; }
    public decimal CantidadBase { get; set; }
    public decimal PrecioReferencia { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal DescuentoPorcentaje { get; set; }
    public decimal DescuentoValor { get; set; }
    public decimal PrecioFinalUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public long? PrecioModificadoPorUsuarioId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Proforma? Proforma { get; set; }
    public Producto? Producto { get; set; }
    public ProductoPresentacion? ProductoPresentacion { get; set; }
    public Usuario? PrecioModificadoPorUsuario { get; set; }
    public ICollection<ProformaDetalleImpuesto> Impuestos { get; set; } = [];
}

public sealed class ProformaDetalleImpuesto
{
    public long Id { get; set; }
    public long ProformaDetalleId { get; set; }
    public long? TarifaImpuestoId { get; set; }
    public string CodigoImpuestoSri { get; set; } = string.Empty;
    public string CodigoPorcentajeSri { get; set; } = string.Empty;
    public string NombreImpuesto { get; set; } = string.Empty;
    public string TipoCalculo { get; set; } = string.Empty;
    public decimal? Porcentaje { get; set; }
    public decimal? ValorEspecifico { get; set; }
    public decimal BaseImponible { get; set; }
    public decimal ValorImpuesto { get; set; }
    public DateTime CreatedAt { get; set; }
    public ProformaDetalle? ProformaDetalle { get; set; }
    public TarifaImpuesto? TarifaImpuesto { get; set; }
}
