namespace KONTAXPRO.Infrastructure.Interoperabilidad;

public sealed class InteroperabilidadOptions
{
    public GuiaOptions Guia { get; init; } = new();
    public SifaeOptions Sifae { get; init; } = new();
}

public sealed class GuiaOptions
{
    public string TokenUrl { get; set; } = string.Empty;
    public string ServicioUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 15;

    public bool HasCredentials =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);
}

public sealed class SifaeOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 15;
}

public static class InteroperabilidadHttpClients
{
    public const string GuiaToken = "KONTAXPRO.GUIA.Token";
    public const string GuiaServicio = "KONTAXPRO.GUIA.Servicio";
    public const string Sifae = "KONTAXPRO.SIFAE";
}

public static class GuiaClasificaciones
{
    public const string Cedula = "Cédula";
    public const string RucNatural = "Natural";
    public const string AntMatriculaLicencia = "AntMatriculaLicencia";
}
