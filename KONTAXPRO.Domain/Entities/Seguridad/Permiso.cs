namespace KONTAXPRO.Domain.Entities.Seguridad;

public class Permiso
{
    public long Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string Modulo { get; set; } = string.Empty;

    public int Estado { get; set; } = 1;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ICollection<RolPermiso> RolesPermisos { get; set; }
        = new List<RolPermiso>();
}
