using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Comercial;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Domain.Entities.Seguridad;

namespace KONTAXPRO.Domain.Entities.Compras;

public sealed class DevolucionCompra
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long EstablecimientoId { get; set; }
    public long EmpresaTerceroId { get; set; }
    public long TipoOrigenDevolucionCompraId { get; set; }
    public long OrigenId { get; set; }
    public long UsuarioId { get; set; }
    public string NumeroDevolucion { get; set; } = string.Empty;
    public DateTime FechaDevolucion { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "CONFIRMADA";
    public long? AnuladoPorUsuarioId { get; set; }
    public DateTime? AnuladaAt { get; set; }
    public string? MotivoAnulacion { get; set; }
    public string? Observacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
    public Establecimiento? Establecimiento { get; set; }
    public EmpresaTercero? EmpresaTercero { get; set; }
    public TipoOrigenDevolucionCompra? TipoOrigenDevolucionCompra { get; set; }
    public Usuario? Usuario { get; set; }
    public Usuario? AnuladoPorUsuario { get; set; }
    public ICollection<DevolucionCompraDetalle> Detalles { get; set; } = [];
}

public sealed class DevolucionCompraDetalle
{
    public long Id { get; set; }
    public long DevolucionCompraId { get; set; }
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
    public DevolucionCompra? DevolucionCompra { get; set; }
    public Producto? Producto { get; set; }
    public ProductoPresentacion? ProductoPresentacion { get; set; }
    public Bodega? Bodega { get; set; }
}

public sealed class AjusteCompra
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long EmpresaTerceroId { get; set; }
    public long CompraId { get; set; }
    public long? DocumentoRecibidoSriId { get; set; }
    public long? DevolucionCompraId { get; set; }
    public long UsuarioId { get; set; }
    public string TipoAjuste { get; set; } = string.Empty;
    public decimal ValorSinImpuestos { get; set; }
    public decimal ImpuestoTotal { get; set; }
    public decimal ValorTotal { get; set; }
    public string Estado { get; set; } = "CONFIRMADO";
    public string? Observacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
    public EmpresaTercero? EmpresaTercero { get; set; }
    public Compra? Compra { get; set; }
    public DocumentoRecibidoSri? DocumentoRecibidoSri { get; set; }
    public DevolucionCompra? DevolucionCompra { get; set; }
    public Usuario? Usuario { get; set; }
    public ICollection<AjusteCompraDetalle> Detalles { get; set; } = [];
}

public sealed class AjusteCompraDetalle
{
    public long Id { get; set; }
    public long AjusteCompraId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal ValorSinImpuestos { get; set; }
    public decimal ImpuestoTotal { get; set; }
    public decimal ValorTotal { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public AjusteCompra? AjusteCompra { get; set; }
    public ICollection<AjusteCompraDetalleImpuesto> Impuestos { get; set; } = [];
}

public sealed class AjusteCompraDetalleImpuesto
{
    public long Id { get; set; }
    public long AjusteCompraDetalleId { get; set; }
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
    public AjusteCompraDetalle? AjusteCompraDetalle { get; set; }
    public TarifaImpuesto? TarifaImpuesto { get; set; }
}
