namespace KONTAXPRO.Application.Models.Clientes;

public enum ClienteCatalogoKpi
{
    Todos,
    PendientesVerificar,
    SinCredito,
    SinContactoDigital
}

public enum ClienteEstadoFiltro
{
    Todos,
    Activos,
    Inactivos
}

public enum ClienteVerificacionFiltro
{
    Todos,
    Verificados,
    NoVerificados
}

public enum ClienteCreditoFiltro
{
    Todos,
    ConCredito,
    SinCredito
}

public enum ClienteCatalogoOrden
{
    Identificacion,
    RazonSocial,
    Clasificacion,
    Credito,
    Estado
}

public enum EstadoVerificacionCliente
{
    Verificado,
    NoVerificado,
    NoAplica
}

public sealed class ClienteCatalogoQuery
{
    public long EmpresaId { get; init; }
    public string? Busqueda { get; init; }
    public ClienteCatalogoKpi Kpi { get; init; }
    public ClienteEstadoFiltro Estado { get; init; }
    public ClienteVerificacionFiltro Verificacion { get; init; }
    public ClienteCreditoFiltro Credito { get; init; }
    public ClienteCatalogoOrden Orden { get; init; } =
        ClienteCatalogoOrden.RazonSocial;
    public bool OrdenDescendente { get; init; }
    public int Pagina { get; init; } = 1;
    public int TamanoPagina { get; init; } = 25;
}

public sealed class ClienteCatalogoItemDto
{
    public long? EmpresaTerceroId { get; init; }
    public long TerceroId { get; init; }
    public string TipoIdentificacionCodigo { get; init; } = string.Empty;
    public string TipoIdentificacionNombre { get; init; } = string.Empty;
    public string NumeroIdentificacion { get; init; } = string.Empty;
    public string RazonSocial { get; init; } = string.Empty;
    public string? NombreComercial { get; init; }
    public string? Direccion { get; init; }
    public string? Correo { get; init; }
    public string? Telefono { get; init; }
    public EstadoVerificacionCliente Verificacion { get; init; }
    public string? FuenteVerificacion { get; init; }
    public string? ListaPrecioCodigo { get; init; }
    public string? ListaPrecioNombre { get; init; }
    public bool UsaListaBasePredeterminada { get; init; }
    public bool CreditoHabilitado { get; init; }
    public int Estado { get; init; }
    public uint Version { get; init; }
    public bool Activo => Estado == 1;
    public string EstadoTexto => Activo ? "ACTIVO" : "INACTIVO";
    public string CreditoTexto =>
        CreditoHabilitado ? "Crédito habilitado" : "Sin crédito";
    public string ClasificacionPrecio =>
        ListaPrecioCodigo?.Trim().ToUpperInvariant() switch
        {
            "B" => "B",
            "C" => "C",
            "D" => "D",
            "E" => "E",
            _ => "A"
        };
    public string VerificacionTexto => Verificacion switch
    {
        EstadoVerificacionCliente.Verificado => "Identificación verificada",
        EstadoVerificacionCliente.NoAplica => "Verificación no aplicable",
        _ => "Identificación no verificada"
    };
}

public sealed class ClienteCatalogoResultadoDto
{
    public IReadOnlyList<ClienteCatalogoItemDto> Items { get; init; } = [];
    public ClienteCatalogoKpisDto Kpis { get; init; } = new();
    public int Total { get; init; }
    public int Pagina { get; init; }
    public int TamanoPagina { get; init; }
}

public sealed class ClienteCatalogoKpisDto
{
    public int Clientes { get; init; }
    public int PendientesVerificar { get; init; }
    public int SinCredito { get; init; }
    public int SinContactoDigital { get; init; }
}

public sealed class ClienteDetalleDto
{
    public long? EmpresaTerceroId { get; init; }
    public long TerceroId { get; init; }
    public long TipoIdentificacionId { get; init; }
    public string TipoIdentificacionCodigo { get; init; } = string.Empty;
    public string NumeroIdentificacion { get; init; } = string.Empty;
    public string RazonSocial { get; init; } = string.Empty;
    public string? NombreComercial { get; init; }
    public string? Direccion { get; init; }
    public string? Correo { get; init; }
    public string? Telefono { get; init; }
    public EstadoVerificacionCliente Verificacion { get; init; }
    public string? FuenteVerificacion { get; init; }
    public DateTime? VerificadoAt { get; init; }
    public long? ListaPrecioId { get; init; }
    public bool CreditoHabilitado { get; init; }
    public string? Observacion { get; init; }
    public int Estado { get; init; }
    public uint Version { get; init; }
}

public sealed class TerceroIdentificacionDto
{
    public long TerceroId { get; init; }
    public long TipoIdentificacionId { get; init; }
    public string TipoIdentificacionCodigo { get; init; } = string.Empty;
    public string NumeroIdentificacion { get; init; } = string.Empty;
    public long? EmpresaTerceroId { get; init; }
    public bool EsCliente { get; init; }
    public bool TieneConfiguracionEmpresaActual { get; init; }
    public string RazonSocial { get; init; } = string.Empty;
    public string? NombreComercial { get; init; }
    public string? Direccion { get; init; }
    public string? Correo { get; init; }
    public string? Telefono { get; init; }
    public EstadoVerificacionCliente Verificacion { get; init; }
    public string? FuenteVerificacion { get; init; }
    public long? ListaPrecioId { get; init; }
    public bool CreditoHabilitado { get; init; }
    public string? Observacion { get; init; }
    public int Estado { get; init; }
    public uint Version { get; init; }
}

public sealed class TipoIdentificacionClienteDto
{
    public long Id { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public int LongitudMinima { get; init; }
    public int LongitudMaxima { get; init; }
    public bool PermiteConsulta => Codigo is "CEDULA" or "RUC";
}

public sealed class ListaPrecioClienteDto
{
    public long Id { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public bool EsListaBase { get; init; }
}

public sealed record ClienteGuardarRequest
{
    public long EmpresaId { get; init; }
    public long? EmpresaTerceroId { get; init; }
    public long? TerceroId { get; init; }
    public uint? Version { get; init; }
    public long TipoIdentificacionId { get; init; }
    public string NumeroIdentificacion { get; init; } = string.Empty;
    public string RazonSocial { get; init; } = string.Empty;
    public string? NombreComercial { get; init; }
    public string? Direccion { get; init; }
    public string? Correo { get; init; }
    public string? Telefono { get; init; }
    public Guid? ConstanciaVerificacionId { get; init; }
    public long? ListaPrecioId { get; init; }
    public bool CreditoHabilitado { get; init; }
    public string? Observacion { get; init; }
    public int Estado { get; init; } = 1;
}

public sealed record ClienteOperationResult(
    bool Success,
    string Message,
    long? EmpresaTerceroId = null,
    long? TerceroId = null,
    bool ConcurrencyConflict = false)
{
    public static ClienteOperationResult Ok(
        string message,
        long? empresaTerceroId,
        long terceroId) =>
        new(true, message, empresaTerceroId, terceroId);

    public static ClienteOperationResult Fail(string message) =>
        new(false, message);

    public static ClienteOperationResult Conflict(string message) =>
        new(false, message, ConcurrencyConflict: true);
}
