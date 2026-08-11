using KONTAXPRO.Application.Models.Compras;
using KONTAXPRO.Desktop.ViewModels.Compras;

namespace KONTAXPRO.Tests.Compras;

public sealed class CompraReceiptLineViewModelTests
{
    [Fact]
    public void BuildRequest_AceptaVariosLotesQueCuadran()
    {
        var line = Create(lots: true, series: false, factor: 10m,
            pending: 10m);
        line.ReceiveNow = 10m;
        line.LotsText = "A|60|2026-01-01|2027-01-01;B|40||2027-02-01";
        var result = line.BuildRequest();
        Assert.Null(result.Error);
        Assert.Equal(2, result.Request!.Lotes.Count);
        Assert.Equal(100m, result.Request.Lotes.Sum(x => x.CantidadBase));
    }

    [Fact]
    public void BuildRequest_RechazaExcesoSobrePendiente()
    {
        var line = Create(false, false, 1m, 3m);
        line.ReceiveNow = 4m;
        Assert.NotNull(line.BuildRequest().Error);
    }

    [Fact]
    public void BuildRequest_ExigeUnaSeriePorUnidadBase()
    {
        var line = Create(false, true, 2m, 2m);
        line.ReceiveNow = 2m;
        line.SeriesText = "S1;S2;S3";
        Assert.NotNull(line.BuildRequest().Error);
        line.SeriesText = "S1;S2;S3;S4";
        Assert.Null(line.BuildRequest().Error);
    }

    [Fact]
    public void BuildRequest_ExigeLoteEnCadaSerieCuandoCombinaControles()
    {
        var line = Create(true, true, 1m, 2m);
        line.LotsText = "L1|2||2027-01-01";
        line.SeriesText = "S1;S2@L1";
        Assert.Contains("SERIE@LOTE", line.BuildRequest().Error);
    }

    [Fact]
    public void BuildRequest_UsaLaConfiguracionGuiadaDeLotesYSeries()
    {
        var line = Create(true, true, 1m, 2m);
        line.SetTraceability(
        [
            new CompraReceiptLotValue("L1", 2m, null,
                new DateTime(2027, 1, 1), false)
        ],
        [
            new CompraReceiptSeriesValue("S1", "L1"),
            new CompraReceiptSeriesValue("S2", "L1")
        ]);

        var result = line.BuildRequest();

        Assert.Null(result.Error);
        Assert.Equal("L1", result.Request!.Lotes.Single().NumeroLote);
        Assert.Equal(2, result.Request.Series.Count);
    }

    [Fact]
    public void DesdeImportacion_ConservaOrdenCantidadYControlesDeInventario()
    {
        using var imported = new CompraImportLineaViewModel(
            new DetalleFacturaCompraXmlDto
            {
                Orden = 7,
                Descripcion = "Caja por 10",
                Cantidad = 2,
                PrecioUnitario = 12,
                PrecioTotalSinImpuesto = 24
            }, new LineaCompraResueltaDto
            {
                Estado = "RECONOCIDA",
                ProductoId = 10,
                ProductoPresentacionId = 20,
                ProductoNombre = "Producto",
                PresentacionNombre = "Caja x 10",
                FactorConversion = 10,
                ManejaLotes = true,
                ManejaFechaCaducidad = true
            }, (_, _, _) => Task.FromResult<
                IReadOnlyList<CandidatoProductoCompraDto>>([]));

        var line = new CompraReceiptLineViewModel(imported)
        {
            LotsText = "L001|20||2027-08-01"
        };
        var result = line.BuildRequest();

        Assert.Null(result.Error);
        Assert.Equal(7, line.Order);
        Assert.Equal(2m, result.Request!.CantidadPresentacion);
        Assert.Equal(20m, result.Request.Lotes.Single().CantidadBase);
    }

    [Fact]
    public void RestoreDraftFrom_ConservaControlGuiadoCompatible()
    {
        var previous = Create(lots: true, series: false, factor: 2m,
            pending: 3m);
        previous.ReceiveNow = 2m;
        previous.SetTraceability(
        [
            new CompraReceiptLotValue("L-001", 4m, null,
                new DateTime(2027, 8, 1), false)
        ], []);
        var rebuilt = Create(lots: true, series: false, factor: 2m,
            pending: 3m);

        var restored = rebuilt.RestoreDraftFrom(previous);

        Assert.True(restored);
        Assert.True(rebuilt.TraceabilityConfigured);
        Assert.Equal(2m, rebuilt.ReceiveNow);
        Assert.Equal("L-001", rebuilt.TraceabilityLots.Single().Number);
        Assert.Null(rebuilt.BuildRequest().Error);
    }

    [Fact]
    public void DesdeCompraManual_ConservaOrdenYProductoParaConsultarControl()
    {
        var line = new CompraReceiptLineViewModel(new CompraDetalleLineaDto
        {
            Id = 41,
            Orden = 3,
            ProductoId = 27,
            Descripcion = "Producto manual",
            Producto = "Producto",
            CantidadFacturada = 2m,
            FactorConversion = 1m,
            EsInventariable = true,
            ManejaLotes = true
        });

        Assert.Equal(3, line.Order);
        Assert.Equal(27, line.ProductId);
    }

    [Fact]
    public void CambiarCantidad_DescartaControlGuiadoQueYaNoCuadra()
    {
        var line = Create(lots: true, series: false, factor: 1m,
            pending: 3m);
        line.SetTraceability(
        [
            new CompraReceiptLotValue("L-001", 3m, null,
                new DateTime(2027, 8, 1), false)
        ], []);

        line.ReceiveNow = 2m;

        Assert.False(line.TraceabilityConfigured);
        Assert.Empty(line.TraceabilityLots);
        Assert.Contains("pendiente", line.TraceabilitySummary,
            StringComparison.OrdinalIgnoreCase);
    }

    private static CompraReceiptLineViewModel Create(
        bool lots, bool series, decimal factor, decimal pending) => new(
        new CompraDetalleLineaDto
        {
            Id = 1,
            Descripcion = "Producto",
            Producto = "Producto",
            CantidadFacturada = pending,
            CantidadRecibida = 0,
            FactorConversion = factor,
            EsInventariable = true,
            ManejaLotes = lots,
            ManejaSeries = series,
            ManejaFechaCaducidad = lots
        });
}
