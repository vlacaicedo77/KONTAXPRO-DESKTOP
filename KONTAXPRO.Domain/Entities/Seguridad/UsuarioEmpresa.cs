using KONTAXPRO.Domain.Entities.Configuracion;

namespace KONTAXPRO.Domain.Entities.Seguridad;

public class UsuarioEmpresa
{
    public long Id { get; set; }

    public long UsuarioId { get; set; }

    public long EmpresaId { get; set; }

    public int Estado { get; set; } = 1;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Usuario? Usuario { get; set; }

    public Empresa? Empresa { get; set; }

    public ICollection<UsuarioEmpresaRol> UsuariosEmpresasRoles { get; set; }
        = new List<UsuarioEmpresaRol>();

    public ICollection<UsuarioEmpresaEstablecimiento>
        UsuariosEmpresasEstablecimientos { get; set; } =
        new List<UsuarioEmpresaEstablecimiento>();
}
