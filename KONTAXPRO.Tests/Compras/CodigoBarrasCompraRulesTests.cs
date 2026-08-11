using KONTAXPRO.Application.Compras;

namespace KONTAXPRO.Tests.Compras;

public sealed class CodigoBarrasCompraRulesTests
{
    [Theory]
    [InlineData("96385074")]
    [InlineData("036000291452")]
    [InlineData("4006381333931")]
    [InlineData("10012345000017")]
    public void EsGtinValido_AceptaEstandaresGs1(string code) =>
        Assert.True(CodigoBarrasCompraRules.EsGtinValido(code));

    [Theory]
    [InlineData("96385075")]
    [InlineData("036000291451")]
    [InlineData("4006381333932")]
    [InlineData("10012345000018")]
    [InlineData("12345678")]
    [InlineData("11111111")]
    [InlineData("ABC1234567890")]
    [InlineData("123-45678")]
    public void EsGtinValido_RechazaCodigosInternosOChecksumIncorrecto(
        string code) =>
        Assert.False(CodigoBarrasCompraRules.EsGtinValido(code));

    [Fact]
    public void ObtenerCandidatos_RevisaPrincipalYAuxiliarSinDuplicar()
    {
        var candidates = CodigoBarrasCompraRules.ObtenerCandidatos(
            "COD-INTERNO", "4006381333931");
        Assert.Equal(["4006381333931"], candidates);

        candidates = CodigoBarrasCompraRules.ObtenerCandidatos(
            "4006381333931", "4006381333931");
        Assert.Single(candidates);
    }

    [Fact]
    public void ObtenerCandidatos_ConservaAmbosCuandoSonValidosYDiferentes()
    {
        var candidates = CodigoBarrasCompraRules.ObtenerCandidatos(
            "4006381333931", "036000291452");

        Assert.Equal(2, candidates.Count);
        Assert.Equal("4006381333931", candidates[0]);
        Assert.Equal("036000291452", candidates[1]);
    }
}
