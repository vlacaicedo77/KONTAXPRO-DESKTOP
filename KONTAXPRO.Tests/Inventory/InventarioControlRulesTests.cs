using KONTAXPRO.Application.Inventory;

namespace KONTAXPRO.Tests.Inventory;

public class InventarioControlRulesTests
{
    [Fact]
    public void NormalALote_ValidaDistribucionPorBodega()
    {
        var error = InventarioControlRules.ValidarConversion(
            "NORMAL", "LOTE",
            [new(1, 5, 0), new(2, 3, 0)],
            [new(1, 5, 0), new(2, 2, 0)]);
        Assert.Contains("cada bodega", error);
    }

    [Fact]
    public void Conversion_SeBloqueaConReservas()
    {
        var error = InventarioControlRules.ValidarConversion(
            "NORMAL", "LOTE",
            [new(1, 5, 1)],
            [new(1, 5, 0)]);
        Assert.Contains("reservas", error);
    }

    [Theory]
    [InlineData("NORMAL", "SERIE")]
    [InlineData("NORMAL", "LOTE_Y_SERIE")]
    [InlineData("LOTE", "LOTE_Y_SERIE")]
    public void ControlConSeries_RequiereUnaSeriePorUnidad(
        string anterior, string nuevo)
    {
        var error = InventarioControlRules.ValidarConversion(
            anterior, nuevo,
            [new(1, 2, 0)],
            [new(1, 2, 1)]);
        Assert.Contains("una serie", error);
    }

    [Fact]
    public void ControlConSeries_RechazaStockFraccionario()
    {
        var error = InventarioControlRules.ValidarConversion(
            "NORMAL", "SERIE",
            [new(1, 1.5m, 0)],
            [new(1, 1.5m, 1)]);
        Assert.Contains("fraccionario", error);
    }

    [Fact]
    public void DistribucionCompletaSinReservas_EsValida()
    {
        var error = InventarioControlRules.ValidarConversion(
            "SERIE", "LOTE_Y_SERIE",
            [new(1, 2, 0), new(2, 1, 0)],
            [new(1, 2, 2), new(2, 1, 1)]);
        Assert.Null(error);
    }

    [Fact]
    public void ReduccionConHistoria_SeBloquea()
    {
        var error = InventarioControlRules.ValidarReduccionControl(
            "LOTE", "NORMAL", true, [new(1, 0, 0)]);
        Assert.Contains("sin historia", error);
    }

    [Theory]
    [InlineData("LOTE", "SERIE")]
    [InlineData("SERIE", "LOTE")]
    public void TransicionesLaterales_NoEstanSoportadas(
        string anterior, string nuevo)
    {
        var error = InventarioControlRules.ValidarConversion(
            anterior, nuevo, [], []);
        Assert.Contains("no está soportada", error);
    }

    [Fact]
    public void NormalALote_DosBodegasCompletas_EsValido()
    {
        var error = InventarioControlRules.ValidarConversion(
            "NORMAL", "LOTE",
            [new(10, 4.5m, 0), new(20, 7, 0)],
            [new(10, 4.5m, 0), new(20, 7, 0)]);
        Assert.Null(error);
    }

    [Fact]
    public void LoteYSerieALote_ConStockSeBloqueaComoReduccion()
    {
        var error = InventarioControlRules.ValidarReduccionControl(
            "LOTE_Y_SERIE", "LOTE", false, [new(1, 2, 0)]);
        Assert.Contains("stock cero", error);
    }

    [Fact]
    public void ReduccionSinStockNiHistoria_EsValida()
    {
        var error = InventarioControlRules.ValidarReduccionControl(
            "SERIE", "NORMAL", false, [new(1, 0, 0)]);
        Assert.Null(error);
    }
}
