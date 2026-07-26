using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Domain.Entities.Seguridad;

namespace KONTAXPRO.Domain.Entities.Configuracion;

public class UsuarioConfiguracionEmpresa
{
    public long Id { get; set; }
    public long UsuarioId { get; set; }
    public long EmpresaId { get; set; }
    public long EstablecimientoId { get; set; }
    public long? PuntoEmisionId { get; set; }
    public long? BodegaId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Usuario? Usuario { get; set; }
    public Empresa? Empresa { get; set; }
    public Establecimiento? Establecimiento { get; set; }
    public PuntoEmision? PuntoEmision { get; set; }
    public Bodega? Bodega { get; set; }
}
