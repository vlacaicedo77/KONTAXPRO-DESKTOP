namespace KONTAXPRO.Application.Models.FacturacionElectronica;

public sealed record MensajeSri(
    string? Codigo,
    string? Mensaje,
    string? InformacionAdicional);

public sealed record ResultadoSri(
    bool Exitoso,
    string Estado,
    string? NumeroAutorizacion,
    DateTime? FechaAutorizacion,
    IReadOnlyList<MensajeSri> Mensajes);

public sealed record ArtefactoElectronico(
    long EmpresaId,
    string RucEmpresa,
    string ClaveAcceso,
    string TipoArtefacto);

public sealed record ResultadoCicloFacturacionElectronica(
    bool Ejecutado,
    int Procesados,
    string Mensaje)
{
    public static ResultadoCicloFacturacionElectronica Omitido(string message) =>
        new(false, 0, message);
}
