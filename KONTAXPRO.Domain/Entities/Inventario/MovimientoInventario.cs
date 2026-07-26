using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Seguridad;

namespace KONTAXPRO.Domain.Entities.Inventario;

public class MovimientoInventario
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public string NumeroMovimiento { get; set; } = string.Empty;
    public long TipoMovimientoId { get; set; }
    public DateTime FechaMovimiento { get; set; }
    public long BodegaId { get; set; }
    public long OrigenTipoId { get; set; }
    public long OrigenId { get; set; }
    public string? NumeroDocumento { get; set; }
    public string? Referencia { get; set; }
    public string? Observacion { get; set; }
    public long UsuarioId { get; set; }
    public string Estado { get; set; } = "CONFIRMADO";
    public long? AnuladoPorUsuarioId { get; set; }
    public DateTime? AnuladoAt { get; set; }
    public string? MotivoAnulacion { get; set; }
    public long? MovimientoReversoId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Empresa? Empresa { get; set; }
    public TipoMovimientoInventario? TipoMovimiento { get; set; }
    public Bodega? Bodega { get; set; }
    public TipoOrigenMovimientoInventario? OrigenTipo { get; set; }
    public Usuario? Usuario { get; set; }
    public Usuario? AnuladoPorUsuario { get; set; }
    public MovimientoInventario? MovimientoReverso { get; set; }
    public MovimientoInventario? MovimientoOrigenReversado { get; set; }
    public ICollection<MovimientoInventarioDetalle> Detalles { get; set; } = [];
}
