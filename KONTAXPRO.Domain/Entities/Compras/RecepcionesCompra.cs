using KONTAXPRO.Domain.Entities.Comercial;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Domain.Entities.Seguridad;

namespace KONTAXPRO.Domain.Entities.Compras;

public sealed class ProveedorProductoEquivalencia
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long TerceroId { get; set; }
    public string CodigoProveedor { get; set; } = string.Empty;
    public string CodigoProveedorNormalizado { get; set; } = string.Empty;
    public string TipoCodigo { get; set; } = "PRINCIPAL";
    public long ProductoPresentacionId { get; set; }
    public string? DescripcionOriginal { get; set; }
    public long CreadoPorUsuarioId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Empresa? Empresa { get; set; }
    public Tercero? Tercero { get; set; }
    public ProductoPresentacion? ProductoPresentacion { get; set; }
    public Usuario? CreadoPorUsuario { get; set; }
}

public sealed class CompraRecepcion
{
    public long Id { get; set; }
    public Guid OperacionUuid { get; set; }
    public long CompraId { get; set; }
    public long EmpresaId { get; set; }
    public long EstablecimientoId { get; set; }
    public long BodegaId { get; set; }
    public long UsuarioId { get; set; }
    public string NumeroRecepcion { get; set; } = string.Empty;
    public DateTime FechaRecepcion { get; set; }
    public string Estado { get; set; } = "CONFIRMADA";
    public long? MovimientoInventarioId { get; set; }
    public long? AnuladaPorUsuarioId { get; set; }
    public DateTime? AnuladaAt { get; set; }
    public string? MotivoAnulacion { get; set; }
    public string? Observacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public uint Version { get; set; }

    public Compra? Compra { get; set; }
    public Empresa? Empresa { get; set; }
    public Establecimiento? Establecimiento { get; set; }
    public Bodega? Bodega { get; set; }
    public Usuario? Usuario { get; set; }
    public Usuario? AnuladaPorUsuario { get; set; }
    public MovimientoInventario? MovimientoInventario { get; set; }
    public ICollection<CompraRecepcionDetalle> Detalles { get; set; } = [];
}

public sealed class CompraRecepcionDetalle
{
    public long Id { get; set; }
    public long CompraRecepcionId { get; set; }
    public long CompraId { get; set; }
    public long EmpresaId { get; set; }
    public long CompraDetalleId { get; set; }
    public long ProductoId { get; set; }
    public long ProductoPresentacionId { get; set; }
    public long BodegaId { get; set; }
    public long? MovimientoInventarioDetalleId { get; set; }
    public decimal CantidadPresentacion { get; set; }
    public decimal FactorConversion { get; set; }
    public decimal CantidadBase { get; set; }
    public decimal CostoUnitarioBase { get; set; }
    public decimal CostoTotal { get; set; }
    public bool EsBonificacion { get; set; }
    public decimal? UltimoPrecioCompraAnterior { get; set; }
    public decimal? UltimoCostoEfectivoAnterior { get; set; }
    public DateTime CreatedAt { get; set; }

    public CompraRecepcion? CompraRecepcion { get; set; }
    public CompraDetalle? CompraDetalle { get; set; }
    public Producto? Producto { get; set; }
    public ProductoPresentacion? ProductoPresentacion { get; set; }
    public MovimientoInventarioDetalle? MovimientoInventarioDetalle { get; set; }
    public ICollection<CompraRecepcionDetalleLote> Lotes { get; set; } = [];
    public ICollection<CompraRecepcionDetalleSerie> Series { get; set; } = [];
}

public sealed class CompraRecepcionDetalleLote
{
    public long Id { get; set; }
    public long CompraRecepcionDetalleId { get; set; }
    public long EmpresaId { get; set; }
    public long ProductoId { get; set; }
    public long ProductoLoteId { get; set; }
    public string NumeroLote { get; set; } = string.Empty;
    public decimal CantidadBase { get; set; }
    public DateOnly? FechaElaboracion { get; set; }
    public DateOnly? FechaCaducidad { get; set; }
    public DateTime CreatedAt { get; set; }

    public CompraRecepcionDetalle? CompraRecepcionDetalle { get; set; }
    public ProductoLote? ProductoLote { get; set; }
}

public sealed class CompraRecepcionDetalleSerie
{
    public long Id { get; set; }
    public long CompraRecepcionDetalleId { get; set; }
    public long EmpresaId { get; set; }
    public long ProductoId { get; set; }
    public long BodegaId { get; set; }
    public long ProductoSerieId { get; set; }
    public string NumeroSerie { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public CompraRecepcionDetalle? CompraRecepcionDetalle { get; set; }
    public ProductoSerie? ProductoSerie { get; set; }
}
