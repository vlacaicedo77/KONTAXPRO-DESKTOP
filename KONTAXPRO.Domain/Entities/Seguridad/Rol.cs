namespace KONTAXPRO.Domain.Entities.Seguridad;

public class Rol
{
    public long Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public bool EsSistema { get; set; }

    public int Estado { get; set; } = 1;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ICollection<RolPermiso> RolesPermisos { get; set; }
        = new List<RolPermiso>();

    public ICollection<UsuarioEmpresaRol> UsuariosEmpresasRoles { get; set; }
    = new List<UsuarioEmpresaRol>();
}
