using KONTAXPRO.Application.Models.Compras;
using KONTAXPRO.Desktop.ViewModels.Compras;

namespace KONTAXPRO.Tests.Compras;

public sealed class CompraManualLineViewModelTests
{
    [Fact]
    public void ProductoRelacionado_UsaSuTarifaYCalculaElValorDelIva()
    {
        var presentation = new CompraPresentacionItemDto
        {
            Id = 11,
            ProductoId = 7,
            Codigo = "UND",
            Producto = "PRODUCTO",
            Presentacion = "UNIDAD",
            FactorConversion = 1m,
            TarifaImpuestoId = 4,
            PorcentajeImpuesto = 15m,
            NombreImpuesto = "IVA 15 %"
        };
        var line = new CompraManualLineViewModel([presentation]);

        line.SelectedPresentation = presentation;
        line.Quantity = 2m;
        line.UnitPrice = 100m;
        line.Discount = 10m;

        Assert.Equal(4, line.TaxRateId);
        Assert.Equal("15 %", line.TaxRateText);
        Assert.Equal(28.50m, line.Tax);
    }

    [Fact]
    public void LineaNoInventariable_PermiteSeleccionarTarifaYCalculaElIva()
    {
        var rate = new CompraTarifaImpuestoItemDto
        {
            Id = 5,
            CodigoSri = "5",
            Nombre = "IVA 5 %",
            Porcentaje = 5m
        };
        var line = new CompraManualLineViewModel(
            taxRates: [rate])
        {
            IsInventory = false,
            Quantity = 1m,
            UnitPrice = 80m,
            SelectedTaxRate = rate
        };

        Assert.Equal(5, line.TaxRateId);
        Assert.Equal("5 %", line.TaxRateText);
        Assert.Equal(4m, line.Tax);
    }
}
