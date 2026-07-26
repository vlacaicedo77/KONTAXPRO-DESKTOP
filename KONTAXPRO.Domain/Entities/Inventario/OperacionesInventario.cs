using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Seguridad;

namespace KONTAXPRO.Domain.Entities.Inventario;

public class TransferenciaInventario
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long EstablecimientoOrigenId { get; set; }
    public long EstablecimientoDestinoId { get; set; }
    public long BodegaOrigenId { get; set; }
    public long BodegaDestinoId { get; set; }
    public long UsuarioId { get; set; }
    public string NumeroTransferencia { get; set; } = string.Empty;
    public DateTime FechaTransferencia { get; set; }
    public string Estado { get; set; } = "CONFIRMADA";
    public long? AnuladoPorUsuarioId { get; set; }
    public DateTime? AnuladaAt { get; set; }
    public string? MotivoAnulacion { get; set; }
    public string? Observacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
    public Establecimiento? EstablecimientoOrigen { get; set; }
    public Establecimiento? EstablecimientoDestino { get; set; }
    public Bodega? BodegaOrigen { get; set; }
    public Bodega? BodegaDestino { get; set; }
    public Usuario? Usuario { get; set; }
    public Usuario? AnuladoPorUsuario { get; set; }
    public ICollection<TransferenciaInventarioDetalle> Detalles { get; set; } = [];
}

public class TransferenciaInventarioDetalle
{
    public long Id { get; set; }
    public long TransferenciaInventarioId { get; set; }
    public long ProductoId { get; set; }
    public long ProductoPresentacionId { get; set; }
    public decimal CantidadPresentacion { get; set; }
    public decimal FactorConversion { get; set; }
    public decimal CantidadBase { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public TransferenciaInventario? TransferenciaInventario { get; set; }
    public Producto? Producto { get; set; }
    public ProductoPresentacion? ProductoPresentacion { get; set; }
}

public class AjusteInventario
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long EstablecimientoId { get; set; }
    public long BodegaId { get; set; }
    public long UsuarioId { get; set; }
    public string NumeroAjuste { get; set; } = string.Empty;
    public string TipoAjuste { get; set; } = string.Empty;
    public DateTime FechaAjuste { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string Estado { get; set; } = "CONFIRMADO";
    public long? AnuladoPorUsuarioId { get; set; }
    public DateTime? AnuladaAt { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
    public Establecimiento? Establecimiento { get; set; }
    public Bodega? Bodega { get; set; }
    public Usuario? Usuario { get; set; }
    public Usuario? AnuladoPorUsuario { get; set; }
    public ICollection<AjusteInventarioDetalle> Detalles { get; set; } = [];
}

public class AjusteInventarioDetalle
{
    public long Id { get; set; }
    public long AjusteInventarioId { get; set; }
    public long ProductoId { get; set; }
    public long ProductoPresentacionId { get; set; }
    public decimal CantidadPresentacion { get; set; }
    public decimal FactorConversion { get; set; }
    public decimal CantidadBase { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public AjusteInventario? AjusteInventario { get; set; }
    public Producto? Producto { get; set; }
    public ProductoPresentacion? ProductoPresentacion { get; set; }
}
