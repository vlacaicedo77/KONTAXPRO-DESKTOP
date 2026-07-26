using KONTAXPRO.Domain.Entities.Configuracion;

namespace KONTAXPRO.Domain.Entities.Seguridad;

public class Auditoria
{
    public long Id { get; set; }
    public long? UsuarioId { get; set; }
    public long? EmpresaId { get; set; }
    public long? EstablecimientoId { get; set; }
    public Guid? InstalacionUuid { get; set; }
    public string? NombreEquipo { get; set; }
    public string? IpEquipo { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? Entidad { get; set; }
    public long? EntidadId { get; set; }
    public string? Descripcion { get; set; }
    public DateTime CreatedAt { get; set; }

    public Usuario? Usuario { get; set; }
    public Empresa? Empresa { get; set; }
    public Establecimiento? Establecimiento { get; set; }
}
