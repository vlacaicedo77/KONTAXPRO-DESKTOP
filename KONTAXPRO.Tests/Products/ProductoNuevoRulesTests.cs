using KONTAXPRO.Application.Products;

namespace KONTAXPRO.Tests.Products;

public sealed class ProductoNuevoRulesTests
{
    [Fact]
    public void CajaX6_CalculaCantidadYCostoBaseEsperados()
    {
        var cantidadBase =
            ProductoNuevoRules.CalcularCantidadBase(2m, 6m);
        var costoTotal =
            ProductoNuevoRules.CalcularCostoTotal(2m, 48m);
        var costoBase =
            ProductoNuevoRules.CalcularCostoUnitarioBase(
                costoTotal,
                cantidadBase);

        Assert.Equal(12m, cantidadBase);
        Assert.Equal(96m, costoTotal);
        Assert.Equal(8m, costoBase);
    }

    [Fact]
    public void VariasEntradas_CalculanPromedioPonderado()
    {
        CostoInventarioEntrada[] entradas =
        [
            new(50m, 50m),
            new(50m, 60m)
        ];

        var promedio =
            ProductoNuevoRules.CalcularCostoPromedioPonderado(entradas);

        Assert.Equal(1.1m, promedio);
    }

    [Fact]
    public void Lotes_DebenCoincidirConCantidadBase()
    {
        Assert.True(
            ProductoNuevoRules.DistribucionLotesCoincide(
                12m,
                [5m, 7m]));
        Assert.False(
            ProductoNuevoRules.DistribucionLotesCoincide(
                12m,
                [5m, 6m]));
    }

    [Fact]
    public void ElaboracionOpcional_EsValida()
    {
        Assert.True(
            ProductoNuevoRules.FechasLoteValidas(
                null,
                new DateOnly(2028, 7, 25)));
    }

    [Fact]
    public void ElaboracionPosteriorACaducidad_EsInvalida()
    {
        Assert.False(
            ProductoNuevoRules.FechasLoteValidas(
                new DateOnly(2028, 7, 26),
                new DateOnly(2028, 7, 25)));
    }

    [Fact]
    public void Caducidad_SoloEsObligatoriaCuandoSeControla()
    {
        DateOnly?[] incompletas =
        [
            new DateOnly(2028, 7, 25),
            null
        ];

        Assert.True(
            ProductoNuevoRules.CaducidadesCompletas(
                false,
                incompletas));
        Assert.False(
            ProductoNuevoRules.CaducidadesCompletas(
                true,
                incompletas));
    }

    [Fact]
    public void Series_RequierenUnaUnicaPorUnidadBase()
    {
        Assert.True(
            ProductoNuevoRules.SeriesCompletasYUnicas(
                3m,
                ["S1", "S2", "S3"]));
        Assert.False(
            ProductoNuevoRules.SeriesCompletasYUnicas(
                3m,
                ["S1", "S1", "S3"]));
        Assert.False(
            ProductoNuevoRules.SeriesCompletasYUnicas(
                2.5m,
                ["S1", "S2"]));
    }

    [Fact]
    public void Precios_AplicanMargenYDescuentosSobreListaA()
    {
        var listaA =
            ProductoNuevoRules.CalcularPrecioPorcentajeCosto(8m, 25m);
        var listaB =
            ProductoNuevoRules.CalcularPrecioConDescuento(listaA, 5m);
        var listaC =
            ProductoNuevoRules.CalcularPrecioConDescuento(listaA, 10m);

        Assert.Equal(10m, listaA);
        Assert.Equal(9.5m, listaB);
        Assert.Equal(9m, listaC);
    }

    [Fact]
    public void CajaX6_AdvierteSiSuperaElPrecioDeSeisUnidades()
    {
        var sugerido =
            ProductoNuevoRules.CalcularPrecioEquivalentePresentacion(2m, 6m);

        Assert.Equal(12m, sugerido);
        Assert.False(
            ProductoNuevoRules.PrecioPresentacionSuperaEquivalente(
                12m, 2m, 6m));
        Assert.True(
            ProductoNuevoRules.PrecioPresentacionSuperaEquivalente(
                12.01m, 2m, 6m));
    }

    [Fact]
    public void CodigoKpx_UsaPrefijoEIdentidadEstable()
    {
        var codigo =
            ProductoNuevoRules.CrearCodigoBarrasInterno("001", 42);

        Assert.Equal("KPX-001-00000042", codigo);
    }
}
