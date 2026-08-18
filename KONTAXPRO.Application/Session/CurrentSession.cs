namespace KONTAXPRO.Application.Session;

public sealed class EmpresaActivaChangedEventArgs(
    long? empresaAnteriorId,
    long empresaActualId) : EventArgs
{
    public long? EmpresaAnteriorId { get; } = empresaAnteriorId;
    public long EmpresaActualId { get; } = empresaActualId;
}

public sealed class EstablecimientoActivoChangedEventArgs(
    long? establecimientoAnteriorId,
    long establecimientoActualId) : EventArgs
{
    public long? EstablecimientoAnteriorId { get; } =
        establecimientoAnteriorId;
    public long EstablecimientoActualId { get; } = establecimientoActualId;
}

public class CurrentSession
{
    public event EventHandler<EmpresaActivaChangedEventArgs>?
        EmpresaActivaChanged;
    public event EventHandler<EstablecimientoActivoChangedEventArgs>?
        EstablecimientoActivoChanged;

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

    public int CantidadEstablecimientosDisponibles { get; set; }

    public bool PuedeCambiarEmpresa =>
        CantidadEmpresasDisponibles > 1;

    public bool PuedeCambiarEstablecimiento =>
        EmpresaId.HasValue && CantidadEstablecimientosDisponibles > 1;

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

    public void NotifyEmpresaActivaChanged(long? empresaAnteriorId)
    {
        if (!EmpresaId.HasValue || EmpresaId == empresaAnteriorId)
            return;

        EmpresaActivaChanged?.Invoke(
            this,
            new EmpresaActivaChangedEventArgs(
                empresaAnteriorId,
                EmpresaId.Value));
    }

    public void NotifyEstablecimientoActivoChanged(
        long? establecimientoAnteriorId)
    {
        if (!EstablecimientoId.HasValue ||
            EstablecimientoId == establecimientoAnteriorId)
            return;

        EstablecimientoActivoChanged?.Invoke(
            this,
            new EstablecimientoActivoChangedEventArgs(
                establecimientoAnteriorId,
                EstablecimientoId.Value));
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
        CantidadEstablecimientosDisponibles = 0;
    }
}
