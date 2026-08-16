namespace KONTAXPRO.Application.Models.Proveedores;

public enum ProveedorCatalogoKpi
{
    Todos,
    Activos,
    PendientesVerificar,
    SinCorreo,
    SinContactoDigital
}

public enum ProveedorEstadoFiltro
{
    Todos,
    Activos,
    Inactivos
}

public enum ProveedorVerificacionFiltro
{
    Todos,
    Verificados,
    NoVerificados
}

public enum ProveedorCatalogoOrden
{
    Ruc,
    RazonSocial,
    Estado
}

public sealed class ProveedorCatalogoQuery
{
    public string? Busqueda { get; init; }
    public ProveedorCatalogoKpi Kpi { get; init; }
    public ProveedorEstadoFiltro Estado { get; init; }
    public ProveedorVerificacionFiltro Verificacion { get; init; }
    public ProveedorCatalogoOrden Orden { get; init; } =
        ProveedorCatalogoOrden.RazonSocial;
    public bool OrdenDescendente { get; init; }
    public int Pagina { get; init; } = 1;
    public int TamanoPagina { get; init; } = 25;
}

public sealed class ProveedorCatalogoItemDto
{
    public long TerceroId { get; init; }
    public string Ruc { get; init; } = string.Empty;
    public string RazonSocial { get; init; } = string.Empty;
    public string? Direccion { get; init; }
    public string? Correo { get; init; }
    public string? Telefono { get; init; }
    public bool Verificado { get; init; }
    public string? FuenteVerificacion { get; init; }
    public int Estado { get; init; }
    public uint Version { get; init; }
    public bool Activo => Estado == 1;
    public string EstadoTexto => Activo ? "ACTIVO" : "INACTIVO";
    public string VerificacionTexto => Verificado
        ? "VERIFICADO"
        : "PENDIENTE";
}

public sealed class ProveedorCatalogoKpisDto
{
    public int Activos { get; init; }
    public int PendientesVerificar { get; init; }
    public int SinCorreo { get; init; }
    public int SinContactoDigital { get; init; }
}

public sealed class ProveedorCatalogoResultadoDto
{
    public IReadOnlyList<ProveedorCatalogoItemDto> Items { get; init; } = [];
    public ProveedorCatalogoKpisDto Kpis { get; init; } = new();
    public int Total { get; init; }
    public int Pagina { get; init; }
    public int TamanoPagina { get; init; }
}

public sealed class ProveedorDetalleDto
{
    public long TerceroId { get; init; }
    public string Ruc { get; init; } = string.Empty;
    public string RazonSocial { get; init; } = string.Empty;
    public string? Direccion { get; init; }
    public string? Correo { get; init; }
    public string? Telefono { get; init; }
    public bool Verificado { get; init; }
    public string? FuenteVerificacion { get; init; }
    public DateTime? VerificadoAt { get; init; }
    public bool EsProveedor { get; init; }
    public int Estado { get; init; } = 1;
    public uint Version { get; init; }
}

public sealed record ProveedorGuardarRequest
{
    public long? TerceroId { get; init; }
    public uint? Version { get; init; }
    public string Ruc { get; init; } = string.Empty;
    public string RazonSocial { get; init; } = string.Empty;
    public string? Direccion { get; init; }
    public string? Correo { get; init; }
    public string? Telefono { get; init; }
    public Guid? ConstanciaVerificacionId { get; init; }
    public int Estado { get; init; } = 1;
}

public sealed record ProveedorOperationResult(
    bool Success,
    string Message,
    long? TerceroId = null,
    bool ConcurrencyConflict = false)
{
    public static ProveedorOperationResult Ok(string message, long terceroId) =>
        new(true, message, terceroId);

    public static ProveedorOperationResult Fail(string message) =>
        new(false, message);

    public static ProveedorOperationResult Conflict(string message) =>
        new(false, message, ConcurrencyConflict: true);
}
