namespace KONTAXPRO.Application.Models.Interoperabilidad;

public enum PropositoConsultaIdentificacion
{
    Cliente,
    Proveedor,
    EmpresaCompleta
}

public enum EstadoConsultaIdentificacion
{
    Encontrado,
    NoEncontrado,
    IdentificacionInvalida,
    FuentesNoDisponibles,
    DatosIncompletos
}

public sealed record ConsultaIdentificacionRequest(
    string TipoIdentificacionCodigo,
    string NumeroIdentificacion,
    PropositoConsultaIdentificacion Proposito =
        PropositoConsultaIdentificacion.Cliente,
    Guid FlujoId = default);

public sealed record ConsultaIdentificacionResult
{
    public EstadoConsultaIdentificacion Estado { get; init; }
    public string NumeroNormalizado { get; init; } = string.Empty;
    public string? RazonSocial { get; init; }
    public string? NombreComercial { get; init; }
    public string? Correo { get; init; }
    public string? Direccion { get; init; }
    public string? Fuente { get; init; }
    public string MensajeUsuario { get; init; } = string.Empty;
    public string? DetalleTecnicoSeguro { get; init; }
    public IReadOnlyList<string> SeccionesFaltantes { get; init; } = [];
    public Guid? ConstanciaVerificacionId { get; init; }

    public bool Encontrado => Estado == EstadoConsultaIdentificacion.Encontrado;
}

public enum TipoConstanciaVerificacion
{
    Verificada,
    OfflineAutorizada
}

public sealed record ConstanciaVerificacionIdentificacion(
    Guid Id,
    long UsuarioId,
    string TipoIdentificacionCodigo,
    string NumeroNormalizado,
    PropositoConsultaIdentificacion Proposito,
    TipoConstanciaVerificacion Tipo,
    string? RazonSocial,
    string? NombreComercial,
    string? Fuente,
    DateTimeOffset ExpiraAt);

public sealed class ProveedorIdentificacionResult
{
    public EstadoConsultaIdentificacion Estado { get; init; }
    public string? RazonSocial { get; init; }
    public string? NombreComercial { get; init; }
    public string? Correo { get; init; }
    public string? Direccion { get; init; }
    public string Fuente { get; init; } = string.Empty;
    public string? DetalleTecnicoSeguro { get; init; }
    public IReadOnlyList<string> SeccionesFaltantes { get; init; } = [];

    public bool TieneDatosMinimos =>
        Estado == EstadoConsultaIdentificacion.Encontrado &&
        !string.IsNullOrWhiteSpace(RazonSocial);
}
