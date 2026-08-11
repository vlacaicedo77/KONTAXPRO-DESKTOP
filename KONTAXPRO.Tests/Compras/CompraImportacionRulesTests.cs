using KONTAXPRO.Application.Compras;
using KONTAXPRO.Application.Models.Compras;

namespace KONTAXPRO.Tests.Compras;

public sealed class CompraImportacionRulesTests
{
    [Theory]
    [InlineData("1790012345001", "04", "1790012345001", true)]
    [InlineData("1790012345001", "04", "0999999999001", false)]
    [InlineData("2300257942001", "05", "2300257942", true)]
    [InlineData("2300257942001", "05", "2300257941", false)]
    public void ReceptorCorrespondeEmpresa_UsaIdentidadCanonica(
        string empresa, string tipo, string receptor, bool expected)
    {
        Assert.Equal(expected,
            CompraImportacionRules.ReceptorCorrespondeEmpresa(
                empresa, tipo, receptor));
    }

    [Fact]
    public void ValidarCuadre_AceptaDiferenciaDeUnCentavo()
    {
        var result = CompraImportacionRules.ValidarCuadre(
            Invoice(10m, 1.2m, 11.21m));
        Assert.True(result.Cuadra);
    }

    [Fact]
    public void ValidarCuadre_RechazaDiferenciaMayorATolerancia()
    {
        var result = CompraImportacionRules.ValidarCuadre(
            Invoice(10m, 1.2m, 11.22m));
        Assert.False(result.Cuadra);
    }

    [Theory]
    [InlineData(100.00, 100.01, 0.01)]
    [InlineData(100.01, 100.00, -0.01)]
    [InlineData(100.00, 100.00, 0.00)]
    public void AjusteContable_AceptaSoloUnCentavo(
        decimal debitos, decimal total, decimal expected)
    {
        Assert.Equal(expected,
            CompraImportacionRules.ObtenerAjusteContableRedondeo(
                debitos, total));
    }

    [Fact]
    public void AjusteContable_RechazaDiferenciaMayorAUnCentavo()
    {
        Assert.Throws<ArgumentException>(() =>
            CompraImportacionRules.ObtenerAjusteContableRedondeo(
                100m, 100.02m));
    }

    [Theory]
    [InlineData(" ab-12 / c ", "AB12C")]
    [InlineData(null, "")]
    public void NormalizarCodigoProveedor_EsEstable(string? input,
        string expected) => Assert.Equal(expected,
        CompraImportacionRules.NormalizarCodigoProveedor(input));

    private static FacturaCompraXmlDto Invoice(
        decimal subtotal, decimal tax, decimal total) => new()
    {
        TotalSinImpuestos = subtotal,
        ImporteTotal = total,
        Detalles =
        [
            new DetalleFacturaCompraXmlDto
            {
                PrecioTotalSinImpuesto = subtotal,
                Impuestos =
                [
                    new ImpuestoCompraXmlDto { Valor = tax }
                ]
            }
        ]
    };
}
