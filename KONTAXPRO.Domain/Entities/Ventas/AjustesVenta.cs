using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Comercial;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Domain.Entities.Seguridad;

namespace KONTAXPRO.Domain.Entities.Ventas;

public sealed class DevolucionVenta : DocumentoVentaBase
{
    public long TipoOrigenDevolucionVentaId { get; set; }
    public long OrigenId { get; set; }
    public string NumeroDevolucion { get; set; } = string.Empty;
    public DateTime FechaDevolucion { get; set; }
    public TipoOrigenDevolucionVenta? TipoOrigenDevolucionVenta { get; set; }
    public ICollection<DevolucionVentaDetalle> Detalles { get; set; } = [];
}

public sealed class DevolucionVentaDetalle
{
    public long Id { get; set; }
    public long DevolucionVentaId { get; set; }
    public long OrigenDetalleId { get; set; }
    public long ProductoId { get; set; }
    public long ProductoPresentacionId { get; set; }
    public long BodegaId { get; set; }
    public decimal CantidadPresentacion { get; set; }
    public decimal FactorConversion { get; set; }
    public decimal CantidadBase { get; set; }
    public decimal CostoUnitarioBase { get; set; }
    public decimal CostoTotal { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DevolucionVenta? DevolucionVenta { get; set; }
    public Producto? Producto { get; set; }
    public ProductoPresentacion? ProductoPresentacion { get; set; }
    public Bodega? Bodega { get; set; }
}

public sealed class NotaCredito : DocumentoVentaBase
{
    public long PuntoEmisionId { get; set; }
    public long TipoComprobanteId { get; set; }
    public long FacturaId { get; set; }
    public long? DevolucionVentaId { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public int Secuencial { get; set; }
    public DateTime FechaEmision { get; set; }
    public decimal SubtotalSinImpuestos { get; set; }
    public decimal ImpuestoTotal { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public PuntoEmision? PuntoEmision { get; set; }
    public TipoComprobante? TipoComprobante { get; set; }
    public Factura? Factura { get; set; }
    public DevolucionVenta? DevolucionVenta { get; set; }
    public ICollection<NotaCreditoDetalle> Detalles { get; set; } = [];
}

public sealed class NotaCreditoDetalle
{
    public long Id { get; set; }
    public long NotaCreditoId { get; set; }
    public long FacturaDetalleId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal CantidadPresentacion { get; set; }
    public decimal FactorConversion { get; set; }
    public decimal CantidadBase { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal DescuentoValor { get; set; }
    public decimal Subtotal { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public NotaCredito? NotaCredito { get; set; }
    public FacturaDetalle? FacturaDetalle { get; set; }
    public ICollection<NotaCreditoDetalleImpuesto> Impuestos { get; set; } = [];
}

public sealed class NotaCreditoDetalleImpuesto
{
    public long Id { get; set; }
    public long NotaCreditoDetalleId { get; set; }
    public string CodigoImpuestoSri { get; set; } = string.Empty;
    public string CodigoPorcentajeSri { get; set; } = string.Empty;
    public string NombreImpuesto { get; set; } = string.Empty;
    public string TipoCalculo { get; set; } = string.Empty;
    public decimal? Porcentaje { get; set; }
    public decimal? ValorEspecifico { get; set; }
    public decimal BaseImponible { get; set; }
    public decimal ValorImpuesto { get; set; }
    public DateTime CreatedAt { get; set; }
    public NotaCreditoDetalle? NotaCreditoDetalle { get; set; }
}

public sealed class NotaDebito : DocumentoVentaBase
{
    public long PuntoEmisionId { get; set; }
    public long TipoComprobanteId { get; set; }
    public long FacturaId { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public int Secuencial { get; set; }
    public DateTime FechaEmision { get; set; }
    public decimal SubtotalSinImpuestos { get; set; }
    public decimal ImpuestoTotal { get; set; }
    public PuntoEmision? PuntoEmision { get; set; }
    public TipoComprobante? TipoComprobante { get; set; }
    public Factura? Factura { get; set; }
    public ICollection<NotaDebitoDetalle> Detalles { get; set; } = [];
}

public sealed class NotaDebitoDetalle
{
    public long Id { get; set; }
    public long NotaDebitoId { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public NotaDebito? NotaDebito { get; set; }
    public ICollection<NotaDebitoDetalleImpuesto> Impuestos { get; set; } = [];
}

public sealed class NotaDebitoDetalleImpuesto
{
    public long Id { get; set; }
    public long NotaDebitoDetalleId { get; set; }
    public string CodigoImpuestoSri { get; set; } = string.Empty;
    public string CodigoPorcentajeSri { get; set; } = string.Empty;
    public string NombreImpuesto { get; set; } = string.Empty;
    public string TipoCalculo { get; set; } = string.Empty;
    public decimal? Porcentaje { get; set; }
    public decimal? ValorEspecifico { get; set; }
    public decimal BaseImponible { get; set; }
    public decimal ValorImpuesto { get; set; }
    public DateTime CreatedAt { get; set; }
    public NotaDebitoDetalle? NotaDebitoDetalle { get; set; }
}

public sealed class GuiaRemision
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long EstablecimientoId { get; set; }
    public long PuntoEmisionId { get; set; }
    public long? EmpresaTerceroId { get; set; }
    public long UsuarioId { get; set; }
    public long TipoComprobanteId { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public int Secuencial { get; set; }
    public long TipoOrigenGuiaRemisionId { get; set; }
    public long? OrigenId { get; set; }
    public DateTime FechaEmision { get; set; }
    public DateOnly FechaInicioTraslado { get; set; }
    public DateOnly FechaFinTraslado { get; set; }
    public string MotivoTraslado { get; set; } = string.Empty;
    public string DireccionPartida { get; set; } = string.Empty;
    public string DireccionDestino { get; set; } = string.Empty;
    public string TransportistaTipoIdentificacion { get; set; } = string.Empty;
    public string TransportistaNumeroIdentificacion { get; set; } = string.Empty;
    public string TransportistaRazonSocial { get; set; } = string.Empty;
    public string? Placa { get; set; }
    public string Estado { get; set; } = "EMITIDA";
    public long? AnuladoPorUsuarioId { get; set; }
    public DateTime? AnuladaAt { get; set; }
    public string? MotivoAnulacion { get; set; }
    public string? Observacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
    public Establecimiento? Establecimiento { get; set; }
    public PuntoEmision? PuntoEmision { get; set; }
    public EmpresaTercero? EmpresaTercero { get; set; }
    public Usuario? Usuario { get; set; }
    public TipoComprobante? TipoComprobante { get; set; }
    public TipoOrigenGuiaRemision? TipoOrigenGuiaRemision { get; set; }
    public Usuario? AnuladoPorUsuario { get; set; }
    public ICollection<GuiaRemisionDetalle> Detalles { get; set; } = [];
}

public sealed class GuiaRemisionDetalle
{
    public long Id { get; set; }
    public long GuiaRemisionId { get; set; }
    public long ProductoId { get; set; }
    public long ProductoPresentacionId { get; set; }
    public decimal CantidadPresentacion { get; set; }
    public decimal FactorConversion { get; set; }
    public decimal CantidadBase { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public GuiaRemision? GuiaRemision { get; set; }
    public Producto? Producto { get; set; }
    public ProductoPresentacion? ProductoPresentacion { get; set; }
}
