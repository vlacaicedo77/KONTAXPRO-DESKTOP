namespace KONTAXPRO.Application.Models;

public class EmpresaDisponible
{
    public long UsuarioEmpresaId { get; set; }

    public long EmpresaId { get; set; }

    public string NumeroIdentificacion { get; set; } = string.Empty;

    public string RazonSocial { get; set; } = string.Empty;

    public string NombreComercial { get; set; } = string.Empty;
}