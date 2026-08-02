using KONTAXPRO.Application.Inventory;

namespace KONTAXPRO.Tests.Inventory;

public class AjusteInventarioRulesTests
{
    [Theory]
    [InlineData(" lt510 ", "LT510")]
    [InlineData("Lt-510", "LT-510")]
    public void NormalizacionExacta_RecortaYConvierteAMayusculas(
        string entrada, string esperado) =>
        Assert.Equal(esperado,
            AjusteInventarioRules.NormalizarLoteExacto(entrada));

    [Theory]
    [InlineData("LT 510", "LT510")]
    [InlineData("LT-510", "LT510")]
    [InlineData("LT.510", "LT510")]
    public void NormalizacionComparable_IgnoraSeparadoresSimples(
        string entrada, string esperado) =>
        Assert.Equal(esperado,
            AjusteInventarioRules.NormalizarLoteComparable(entrada));

    [Fact]
    public void LoteConSeparadorDistinto_SeDetectaComoPosibleEquivalente() =>
        Assert.True(AjusteInventarioRules.EsPosibleEquivalente(
            "LT510", "LT 510"));

    [Theory]
    [InlineData("LT510", "510")]
    [InlineData("LT510", "LT5")]
    [InlineData("LT510", "lt510")]
    [InlineData("LT-510", "LT 510")]
    [InlineData("ABC-2026-510", "510")]
    public void BusquedaParcialFlexible_EncuentraLote(
        string codigo, string consulta) =>
        Assert.True(AjusteInventarioRules.CoincideBusquedaLote(
            codigo, consulta));

    [Fact]
    public void Busqueda_PriorizaCoincidenciaExacta()
    {
        var codigos = new[] { "ABC-510", "LT510", "510", "LT-510" };
        var resultado = codigos.OrderBy(x =>
            AjusteInventarioRules.PrioridadBusquedaLote(x, "510")).ToList();
        Assert.Equal("510", resultado[0]);
    }

    [Fact]
    public void DosLotesEntrada_QueCompletanCantidad_SonValidos()
    {
        var error = AjusteInventarioRules.ValidarLotes(5,
            [new("LT510", 3, 18, true), new("LT620", 2, 0, false)], false);
        Assert.Null(error);
    }

    [Fact]
    public void DistribucionLotesIncompleta_SeRechaza()
    {
        var error = AjusteInventarioRules.ValidarLotes(5,
            [new("LT510", 3, 18, true)], false);
        Assert.Contains("coincidir", error);
    }

    [Fact]
    public void SalidaConDosLotesYStockSuficiente_EsValida()
    {
        var error = AjusteInventarioRules.ValidarLotes(5,
            [new("A", 3, 3, true), new("B", 2, 8, true)], true);
        Assert.Null(error);
    }

    [Fact]
    public void SalidaDeLoteSinStock_SeRechaza()
    {
        var error = AjusteInventarioRules.ValidarLotes(2,
            [new("A", 2, 1, true)], true);
        Assert.Contains("stock suficiente", error);
    }

    [Fact]
    public void Salida_NoPermiteLoteNuevo()
    {
        var error = AjusteInventarioRules.ValidarLotes(1,
            [new("NUEVO", 1, 0, false)], true);
        Assert.Contains("existentes", error);
    }

    [Fact]
    public void TresSeriesParaTresUnidades_SonValidas()
    {
        var error = AjusteInventarioRules.ValidarSeries(3,
            [new("SN1", null), new("SN2", null), new("SN3", null)]);
        Assert.Null(error);
    }

    [Fact]
    public void SeriesDuplicadas_SeRechazan()
    {
        var error = AjusteInventarioRules.ValidarSeries(2,
            [new("sn1", null), new("SN1", null)]);
        Assert.Contains("repetirse", error);
    }

    [Fact]
    public void CantidadSerializadaFraccionaria_SeRechaza()
    {
        var error = AjusteInventarioRules.ValidarSeries(1.5m,
            [new("SN1", null)]);
        Assert.Contains("entera", error);
    }

    [Fact]
    public void LoteYSerieSalida_DerivaDistribucionDesdeSeries()
    {
        var lotes = AjusteInventarioRules.DerivarLotesDesdeSeries(
            [new("SN1", "A"), new("SN2", "B"), new("SN3", "A")]);
        Assert.Equal(2, lotes["A"]);
        Assert.Equal(1, lotes["B"]);
    }

    [Fact]
    public void LoteRepetidoConDiferenciaDeMayusculas_SeRechaza()
    {
        var error = AjusteInventarioRules.ValidarLotes(2,
            [new("LT1", 1, 3, true), new("lt1", 1, 3, true)], false);
        Assert.Contains("repetirse", error);
    }
}
