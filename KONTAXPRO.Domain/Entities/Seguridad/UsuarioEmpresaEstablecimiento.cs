using KONTAXPRO.Domain.Entities.Configuracion;

namespace KONTAXPRO.Domain.Entities.Seguridad;

public class UsuarioEmpresaEstablecimiento
{
    public long Id { get; set; }
    public long UsuarioEmpresaId { get; set; }
    public long EstablecimientoId { get; set; }
    public DateTime CreatedAt { get; set; }

    public UsuarioEmpresa? UsuarioEmpresa { get; set; }
    public Establecimiento? Establecimiento { get; set; }
}
