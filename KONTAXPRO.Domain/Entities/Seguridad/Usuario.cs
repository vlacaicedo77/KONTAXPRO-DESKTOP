namespace KONTAXPRO.Domain.Entities.Seguridad;

public class Usuario
{
    public long Id { get; set; }
    public string NumeroIdentificacion { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public bool RequiereCambioClave { get; set; }
    public DateTime? UltimoAccesoAt { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<UsuarioEmpresa> UsuariosEmpresas { get; set; } =
        new List<UsuarioEmpresa>();
}
