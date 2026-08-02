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
    [InlineData("PBJ-208-TJ-A", "208")]
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
    public void SalidaConLotesPrecargados_IgnoraCantidadesEnCero()
    {
        var error = AjusteInventarioRules.ValidarLotes(2,
            [new("A", 0, 8, true), new("B", 2, 3, true)], true);
        Assert.Null(error);
    }

    [Fact]
    public void SalidaConTodosLosLotesEnCero_SeRechaza()
    {
        var error = AjusteInventarioRules.ValidarLotes(2,
            [new("A", 0, 8, true), new("B", 0, 3, true)], true);
        Assert.NotNull(error);
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
    public void SerieYaExistenteEnProducto_SeRechaza()
    {
        HashSet<string> existentes = new(StringComparer.OrdinalIgnoreCase)
        {
            "SN001"
        };
        var error = AjusteInventarioRules.ValidarSeriesNuevas(1,
            [new(" sn001 ", null)], existentes);
        Assert.Contains("ya existe", error);
    }

    [Fact]
    public void FilaDeSerieVacia_SeRechaza()
    {
        var error = AjusteInventarioRules.ValidarSeriesNuevas(1,
            [new("SN001", null), new("", null)], new HashSet<string>());
        Assert.Contains("vacías", error);
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

    [Fact]
    public void EntradaLoteYSerie_Completa_HabilitaRegistro()
    {
        var ajuste = CrearEntradaLoteYSerieValida();

        var razones = AjusteInventarioRules
            .ObtenerRazonesEntradaLoteYSerieNoValida(ajuste);

        Assert.Empty(razones);
    }

    [Fact]
    public void EntradaLoteYSerie_ConUnaSerieFaltante_NoHabilitaRegistro()
    {
        var baseValida = CrearEntradaLoteYSerieValida();
        var ajuste = baseValida with
        {
            Series = baseValida.Series.Take(1).ToList()
        };

        var razones = AjusteInventarioRules
            .ObtenerRazonesEntradaLoteYSerieNoValida(ajuste);

        Assert.NotEmpty(razones);
    }

    [Fact]
    public void EntradaLoteYSerie_SinCantidadDeLote_NoHabilitaRegistro()
    {
        var baseValida = CrearEntradaLoteYSerieValida();
        var ajuste = baseValida with
        {
            Lotes = baseValida.Lotes.Select(x =>
                x with { CantidadBase = 0 }).ToList()
        };

        var razones = AjusteInventarioRules
            .ObtenerRazonesEntradaLoteYSerieNoValida(ajuste);

        Assert.Contains(razones, x => x.Contains("cantidad mayor que cero"));
    }

    [Fact]
    public void EntradaLoteYSerie_ConSerieSinLote_NoHabilitaRegistro()
    {
        var baseValida = CrearEntradaLoteYSerieValida();
        var ajuste = baseValida with
        {
            Series = baseValida.Series.Select((x, index) => index == 0
                ? x with { LoteFilaId = null }
                : x).ToList()
        };

        var razones = AjusteInventarioRules
            .ObtenerRazonesEntradaLoteYSerieNoValida(ajuste);

        Assert.Contains(razones, x => x.Contains("Asocie cada serie"));
    }

    [Fact]
    public void EntradaLoteYSerie_ConDistribucionIncorrecta_NoHabilitaRegistro()
    {
        var loteAId = Guid.NewGuid();
        var loteBId = Guid.NewGuid();
        var ajuste = CrearEntradaLoteYSerieValida() with
        {
            Lotes = new List<LoteEntradaAjusteSnapshot>
            {
                new(loteAId, 10, "PJB-2608-A", 1),
                new(loteBId, 11, "PJB-2608-B", 1)
            },
            Series = new List<SerieEntradaAjusteSnapshot>
            {
                new("SA-001", loteAId),
                new("SA-002", loteAId)
            }
        };

        var razones = AjusteInventarioRules
            .ObtenerRazonesEntradaLoteYSerieNoValida(ajuste);

        Assert.Contains(razones, x => x.Contains("deben coincidir con su cantidad"));
    }

    [Fact]
    public void EntradaLoteYSerie_ObservacionVacia_SigueHabilitandoRegistro()
    {
        // La observación no forma parte del estado validable: continúa opcional.
        var razones = AjusteInventarioRules
            .ObtenerRazonesEntradaLoteYSerieNoValida(
                CrearEntradaLoteYSerieValida());

        Assert.Empty(razones);
    }

    [Fact]
    public void EntradaLoteYSerie_ConCantidadCero_NoHabilitaRegistro()
    {
        var ajuste = CrearEntradaLoteYSerieValida() with
        {
            CantidadPresentacion = 0
        };

        var razones = AjusteInventarioRules
            .ObtenerRazonesEntradaLoteYSerieNoValida(ajuste);

        Assert.Contains(razones, x => x.Contains("cantidad debe ser mayor"));
    }

    [Fact]
    public void EntradaLoteYSerie_ConMotivoVacio_NoHabilitaRegistro()
    {
        var ajuste = CrearEntradaLoteYSerieValida() with { Motivo = "   " };

        var razones = AjusteInventarioRules
            .ObtenerRazonesEntradaLoteYSerieNoValida(ajuste);

        Assert.Contains(razones, x => x.Contains("motivo"));
    }

    [Fact]
    public void EntradaLoteYSerie_SeVuelveValidaTrasUltimaAsociacion()
    {
        var loteId = Guid.NewGuid();
        var lotes = new List<LoteEntradaAjusteSnapshot>();
        var series = new List<SerieEntradaAjusteSnapshot>();
        var ajuste = CrearEntradaLoteYSerieValida() with
        {
            Lotes = lotes,
            Series = series
        };
        Assert.NotEmpty(AjusteInventarioRules
            .ObtenerRazonesEntradaLoteYSerieNoValida(ajuste));

        lotes.Add(new(loteId, 77, "PJB-2608-A", 2));
        series.Add(new("SA-001", loteId));
        series.Add(new("SA-002", null));
        Assert.NotEmpty(AjusteInventarioRules
            .ObtenerRazonesEntradaLoteYSerieNoValida(ajuste));

        series[1] = series[1] with { LoteFilaId = loteId };

        Assert.Empty(AjusteInventarioRules
            .ObtenerRazonesEntradaLoteYSerieNoValida(ajuste));
    }

    private static EntradaLoteYSerieAjusteSnapshot
        CrearEntradaLoteYSerieValida()
    {
        var loteId = Guid.NewGuid();
        return new EntradaLoteYSerieAjusteSnapshot(
            false,
            1,
            2,
            2,
            1,
            330m,
            "AJUSTE DE PRUEBA",
            false,
            new List<LoteEntradaAjusteSnapshot>
            {
                new(loteId, 77, "PJB-2608-A", 2)
            },
            new List<SerieEntradaAjusteSnapshot>
            {
                new("SA-001", loteId),
                new("SA-002", loteId)
            },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }
}
