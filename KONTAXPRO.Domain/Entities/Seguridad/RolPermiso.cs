namespace KONTAXPRO.Domain.Entities.Seguridad;

public class RolPermiso
{
    public long Id { get; set; }

    public long RolId { get; set; }

    public long PermisoId { get; set; }

    public DateTime CreatedAt { get; set; }

    public Rol? Rol { get; set; }

    public Permiso? Permiso { get; set; }
}