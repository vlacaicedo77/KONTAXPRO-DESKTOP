namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

public sealed class SriEndpointsOptions
{
    public const string SectionName = "Sri:FacturacionElectronica";

    public string RecepcionPruebas { get; set; } =
        "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline";
    public string AutorizacionPruebas { get; set; } =
        "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline";
    public string RecepcionProduccion { get; set; } =
        "https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline";
    public string AutorizacionProduccion { get; set; } =
        "https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline";
    public int TimeoutSeconds { get; set; } = 45;

    public Uri Recepcion(int ambiente) => UriSegura(
        ambiente == 1 ? RecepcionPruebas : RecepcionProduccion, ambiente,
        "RecepcionComprobantesOffline");

    public Uri Autorizacion(int ambiente) => UriSegura(
        ambiente == 1 ? AutorizacionPruebas : AutorizacionProduccion, ambiente,
        "AutorizacionComprobantesOffline");

    private static Uri UriSegura(
        string value,
        int ambiente,
        string servicio)
    {
        if (ambiente is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(ambiente));
        var uri = new Uri(value, UriKind.Absolute);
        if (uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Los endpoints SRI deben usar HTTPS.");
        var expectedHost = ambiente == 1
            ? "celcer.sri.gob.ec"
            : "cel.sri.gob.ec";
        if (!string.Equals(uri.Host, expectedHost,
                StringComparison.OrdinalIgnoreCase) ||
            !uri.IsDefaultPort ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.Equals(uri.AbsolutePath,
                $"/comprobantes-electronicos-ws/{servicio}",
                StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"El endpoint del ambiente {ambiente} no pertenece al servicio oficial del SRI.");
        return uri;
    }
}
