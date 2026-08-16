using KONTAXPRO.Application.Models.Compras;
using KONTAXPRO.Desktop.ViewModels.Compras;

namespace KONTAXPRO.Tests.Compras;

public sealed class CompraManualLineViewModelTests
{
    [Fact]
    public void NuevaLinea_IniciaCantidadEnCero()
    {
        var line = new CompraManualLineViewModel();

        Assert.Equal(0m, line.Quantity);
    }

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

    [Fact]
    public void SugerenciaDesdeDescripcion_FormalizaProductoYPresentacion()
    {
        var presentation = Presentation();
        var line = new CompraManualLineViewModel([presentation])
        {
            Description = "producto buscado"
        };

        line.SelectDescriptionSuggestionCommand.Execute(presentation);

        Assert.Same(presentation, line.SelectedPresentation);
        Assert.Equal("PRODUCTO · CAJA X12", line.Description);
    }

    [Fact]
    public void SugerenciaDesdeProducto_ConservaDescripcionTranscrita()
    {
        var presentation = Presentation();
        var line = new CompraManualLineViewModel([presentation])
        {
            Description = "DETALLE ESPECIAL DE LA FACTURA"
        };

        line.SelectProductSuggestionCommand.Execute(presentation);

        Assert.Equal("DETALLE ESPECIAL DE LA FACTURA", line.Description);
    }

    [Fact]
    public void LimpiarDescripcion_ReiniciaRelacionYValoresDependientes()
    {
        var presentation = Presentation();
        var line = new CompraManualLineViewModel([presentation]);
        line.SelectDescriptionSuggestionCommand.Execute(presentation);
        line.Quantity = 2m;
        line.UnitPrice = 10m;
        line.Discount = 1m;
        line.IsBonus = true;

        line.ClearDescriptionCommand.Execute(null);

        Assert.Empty(line.Description);
        Assert.Null(line.SelectedPresentation);
        Assert.Empty(line.ProductSearchText);
        Assert.Equal(0m, line.Quantity);
        Assert.Equal(0m, line.UnitPrice);
        Assert.Equal(0m, line.Discount);
        Assert.Equal(0m, line.Tax);
        Assert.False(line.IsBonus);
    }

    private static CompraPresentacionItemDto Presentation() => new()
    {
        Id = 11,
        ProductoId = 7,
        Codigo = "CJ12",
        Producto = "PRODUCTO",
        Presentacion = "CAJA X12",
        FactorConversion = 12m,
        TarifaImpuestoId = 4,
        PorcentajeImpuesto = 15m,
        NombreImpuesto = "IVA 15 %"
    };
}
