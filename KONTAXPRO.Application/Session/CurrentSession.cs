namespace KONTAXPRO.Application.Session;

public class CurrentSession
{
    public long UsuarioId { get; set; }

    public string NumeroIdentificacion { get; set; } = string.Empty;

    public string NombreCompleto { get; set; } = string.Empty;

    public long? EmpresaId { get; set; }

    public string? RazonSocial { get; set; }

    public List<string> Roles { get; set; } = new();

    public List<string> Permisos { get; set; } = new();

    public long? EstablecimientoId { get; set; }

    public string? EstablecimientoCodigo { get; set; }

    public string? EstablecimientoNombre { get; set; }

    public long? PuntoEmisionId { get; set; }

    public string? PuntoEmisionCodigo { get; set; }

    public string? PuntoEmisionNombre { get; set; }

    public long? BodegaId { get; set; }

    public string? BodegaNombre { get; set; }

    public long? CajaId { get; set; }

    public string? CajaNombre { get; set; }

    public long? CajaSesionId { get; set; }

    public bool IsAuthenticated =>
        UsuarioId > 0;

    public int CantidadEmpresasDisponibles { get; set; }

    public bool PuedeCambiarEmpresa =>
        CantidadEmpresasDisponibles > 1;

    public bool HasPermission(string permiso)
    {
        if (Roles.Contains(
            "ADMINISTRADOR",
            StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return Permisos.Contains(
            permiso,
            StringComparer.OrdinalIgnoreCase);
    }

    public void Clear()
    {
        UsuarioId = 0;
        NombreCompleto = string.Empty;

        EmpresaId = null;
        RazonSocial = null;

        Roles.Clear();
        Permisos.Clear();

        EstablecimientoId = null;
        EstablecimientoCodigo = null;
        EstablecimientoNombre = null;

        PuntoEmisionId = null;
        PuntoEmisionCodigo = null;
        PuntoEmisionNombre = null;

        BodegaId = null;
        BodegaNombre = null;

        CajaId = null;
        CajaNombre = null;
        CajaSesionId = null;
        NumeroIdentificacion = string.Empty;
        CantidadEmpresasDisponibles = 0;
    }
}
