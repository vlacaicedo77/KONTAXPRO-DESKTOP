using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Comercial;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Contabilidad;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Domain.Entities.Seguridad;

namespace KONTAXPRO.Domain.Entities.Compras;

public sealed class DocumentoRecibidoSri
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long? TerceroId { get; set; }
    public long TipoComprobanteId { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public string ClaveAcceso { get; set; } = string.Empty;
    public DateOnly FechaEmision { get; set; }
    public DateTime? FechaAutorizacion { get; set; }
    public string Ambiente { get; set; } = string.Empty;
    public string TipoEmision { get; set; } = string.Empty;
    public string RucEmisor { get; set; } = string.Empty;
    public string RazonSocialEmisor { get; set; } = string.Empty;
    public string? NombreComercialEmisor { get; set; }
    public string? DireccionMatriz { get; set; }
    public string? DireccionEstablecimiento { get; set; }
    public string EstablecimientoCodigo { get; set; } = string.Empty;
    public string PuntoEmisionCodigo { get; set; } = string.Empty;
    public string Secuencial { get; set; } = string.Empty;
    public string IdentificacionReceptor { get; set; } = string.Empty;
    public string RazonSocialReceptor { get; set; } = string.Empty;
    public decimal ValorSinImpuestos { get; set; }
    public decimal Iva { get; set; }
    public decimal Propina { get; set; }
    public decimal ImporteTotal { get; set; }
    public string? Moneda { get; set; }
    public bool FirmaPresente { get; set; }
    public string EstadoValidacion { get; set; } = "ADVERTENCIA";
    public string ArchivoRutaRelativa { get; set; } = string.Empty;
    public string ArchivoSha256 { get; set; } = string.Empty;
    public long ArchivoTamano { get; set; }
    public string? NumeroDocumentoModificado { get; set; }
    public string? Clasificacion { get; set; }
    public string EstadoProcesamiento { get; set; } = "PENDIENTE";
    public DateTime? XmlObtenidoAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
    public Tercero? Tercero { get; set; }
    public TipoComprobante? TipoComprobante { get; set; }
    public ICollection<DocumentoRecibidoSriPago> PagosDeclarados { get; set; }
        = [];
}

public sealed class Compra
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long EstablecimientoId { get; set; }
    public long EmpresaTerceroId { get; set; }
    public long UsuarioId { get; set; }
    public long? DocumentoRecibidoSriId { get; set; }
    public long? CompraSustituidaId { get; set; }
    public string TipoCompra { get; set; } = string.Empty;
    public long? TipoComprobanteId { get; set; }
    public string? NumeroDocumento { get; set; }
    public string ProveedorIdentificacion { get; set; } = string.Empty;
    public string ProveedorRazonSocial { get; set; } = string.Empty;
    public DateOnly FechaEmision { get; set; }
    public DateTime FechaIngreso { get; set; }
    public DateOnly? FechaVencimiento { get; set; }
    public decimal SubtotalSinImpuestos { get; set; }
    public decimal DescuentoTotal { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ImpuestoTotal { get; set; }
    public decimal Total { get; set; }
    public bool EsCredito { get; set; }
    public string Estado { get; set; } = "BORRADOR";
    public long? AnuladoPorUsuarioId { get; set; }
    public DateTime? AnuladaAt { get; set; }
    public string? MotivoAnulacion { get; set; }
    public string? Observacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public uint Version { get; set; }
    public Empresa? Empresa { get; set; }
    public Establecimiento? Establecimiento { get; set; }
    public EmpresaTercero? EmpresaTercero { get; set; }
    public Usuario? Usuario { get; set; }
    public DocumentoRecibidoSri? DocumentoRecibidoSri { get; set; }
    public Compra? CompraSustituida { get; set; }
    public Compra? CompraSustituta { get; set; }
    public TipoComprobante? TipoComprobante { get; set; }
    public Usuario? AnuladoPorUsuario { get; set; }
    public ICollection<CompraDetalle> Detalles { get; set; } = [];
    public ICollection<CompraRecepcion> Recepciones { get; set; } = [];
}

public sealed class CompraDetalle
{
    public long Id { get; set; }
    public long CompraId { get; set; }
    public long EmpresaId { get; set; }
    public int Orden { get; set; }
    public string? CodigoPrincipalProveedor { get; set; }
    public string? CodigoAuxiliarProveedor { get; set; }
    public string EstadoReconocimiento { get; set; } = "NO_RECONOCIDA";
    public bool EsInventariable { get; set; }
    public string ClasificacionContable { get; set; } = "INVENTARIO";
    public long CuentaContableId { get; set; }
    public long? ProductoId { get; set; }
    public long? ProductoPresentacionId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal CantidadPresentacion { get; set; }
    public decimal FactorConversion { get; set; }
    public decimal CantidadBase { get; set; }
    public decimal PrecioUnitarioCompra { get; set; }
    public decimal DescuentoPorcentaje { get; set; }
    public decimal DescuentoValor { get; set; }
    public decimal PrecioTotalSinImpuesto { get; set; }
    public decimal CostoTotalLinea { get; set; }
    public decimal CostoUnitarioBase { get; set; }
    public bool EsBonificacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Compra? Compra { get; set; }
    public PlanCuenta? CuentaContable { get; set; }
    public Producto? Producto { get; set; }
    public ProductoPresentacion? ProductoPresentacion { get; set; }
    public ICollection<CompraDetalleImpuesto> Impuestos { get; set; } = [];
    public ICollection<CompraRecepcionDetalle> RecepcionesDetalles
        { get; set; } = [];
}

public sealed class DocumentoRecibidoSriPago
{
    public long Id { get; set; }
    public long DocumentoRecibidoSriId { get; set; }
    public string CodigoFormaPagoSri { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int? Plazo { get; set; }
    public string? UnidadTiempo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DocumentoRecibidoSri? DocumentoRecibidoSri { get; set; }
}

public sealed class CompraDetalleImpuesto
{
    public long Id { get; set; }
    public long CompraDetalleId { get; set; }
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
    public CompraDetalle? CompraDetalle { get; set; }
    public TarifaImpuesto? TarifaImpuesto { get; set; }
}

public sealed class LiquidacionCompra
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long EstablecimientoId { get; set; }
    public long PuntoEmisionId { get; set; }
    public long EmpresaTerceroId { get; set; }
    public long UsuarioId { get; set; }
    public long TipoComprobanteId { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public int Secuencial { get; set; }
    public DateOnly FechaEmision { get; set; }
    public decimal SubtotalSinImpuestos { get; set; }
    public decimal DescuentoTotal { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ImpuestoTotal { get; set; }
    public decimal Total { get; set; }
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
    public Usuario? AnuladoPorUsuario { get; set; }
    public ICollection<LiquidacionCompraDetalle> Detalles { get; set; } = [];
}

public sealed class LiquidacionCompraDetalle
{
    public long Id { get; set; }
    public long LiquidacionCompraId { get; set; }
    public long? ProductoId { get; set; }
    public long? ProductoPresentacionId { get; set; }
    public long? BodegaId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal CantidadPresentacion { get; set; }
    public decimal FactorConversion { get; set; }
    public decimal CantidadBase { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal DescuentoPorcentaje { get; set; }
    public decimal DescuentoValor { get; set; }
    public decimal Subtotal { get; set; }
    public decimal CostoTotalLinea { get; set; }
    public decimal CostoUnitarioBase { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public LiquidacionCompra? LiquidacionCompra { get; set; }
    public Producto? Producto { get; set; }
    public ProductoPresentacion? ProductoPresentacion { get; set; }
    public Bodega? Bodega { get; set; }
    public ICollection<LiquidacionCompraDetalleImpuesto> Impuestos { get; set; } = [];
}

public sealed class LiquidacionCompraDetalleImpuesto
{
    public long Id { get; set; }
    public long LiquidacionCompraDetalleId { get; set; }
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
    public LiquidacionCompraDetalle? LiquidacionCompraDetalle { get; set; }
    public TarifaImpuesto? TarifaImpuesto { get; set; }
}
