namespace KONTAXPRO.Application.Compras;

public static class CompraBodegaRules
{
    public static bool EsCompatible(
        string tipoCompra,
        bool permiteVentaFacturada) =>
        tipoCompra.Trim().ToUpperInvariant() switch
        {
            "FACTURADA" => permiteVentaFacturada,
            _ => false
        };
}
