namespace KONTAXPRO.Domain.Entities.Seguridad;

public class UsuarioEmpresaRol
{
    public long Id { get; set; }

    public long UsuarioEmpresaId { get; set; }

    public long RolId { get; set; }

    public DateTime CreatedAt { get; set; }

    public UsuarioEmpresa? UsuarioEmpresa { get; set; }

    public Rol? Rol { get; set; }
}
