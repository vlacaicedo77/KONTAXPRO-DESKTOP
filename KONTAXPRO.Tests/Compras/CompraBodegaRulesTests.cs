using KONTAXPRO.Application.Compras;

namespace KONTAXPRO.Tests.Compras;

public sealed class CompraBodegaRulesTests
{
    [Theory]
    [InlineData("FACTURADA", true, true)]
    [InlineData("FACTURADA", false, false)]
    [InlineData("SIN_FACTURA", false, false)]
    [InlineData("SIN_FACTURA", true, false)]
    [InlineData("DESCONOCIDA", false, false)]
    public void EsCompatible_AplicaPoliticaDelTipoCompra(
        string tipoCompra,
        bool permiteVentaFacturada,
        bool esperado)
    {
        Assert.Equal(esperado, CompraBodegaRules.EsCompatible(
            tipoCompra, permiteVentaFacturada));
    }
}
