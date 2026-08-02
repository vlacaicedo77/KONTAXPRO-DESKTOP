using KONTAXPRO.Application.Models.Productos;
using KONTAXPRO.Application.Products;

namespace KONTAXPRO.Tests.Products;

public sealed class ProductoInventarioRulesTests
{
    [Fact]
    public void SoloInventarioInicial_PermiteCompletar()
    {
        Assert.True(ProductoInventarioRules.PuedeCompletarInventarioInicial(
            ["INVENTARIO_INICIAL", "INVENTARIO_INICIAL"]));
    }

    [Fact]
    public void MovimientoPosterior_ImpideCompletar()
    {
        Assert.False(ProductoInventarioRules.PuedeCompletarInventarioInicial(
            ["INVENTARIO_INICIAL", "VENTA"]));
    }

    [Fact]
    public void EntradaPendiente_RecalculaCostoBaseYPresentacion()
    {
        var promedio =
            ProductoInventarioRules.CalcularCostoPromedioConPendientes(
                50m,
                1.666667m,
                [new CostoInventarioEntrada(10m, 26.66663m)]);

        Assert.Equal(1.833333m, decimal.Round(promedio, 6));
        Assert.Equal(10.999998m, decimal.Round(promedio * 6m, 6));
    }

    [Fact]
    public void BodegaDisplay_UsaCodigoRealDeBodega()
    {
        Assert.Equal(
            "FAC · PRODUCTOS CON FACTURA",
            BodegaDisplayFormatter.Format("FAC", "PRODUCTOS CON FACTURA"));
        Assert.Equal(
            "SFA · PRODUCTOS SIN FACTURA",
            BodegaDisplayFormatter.Format("SFA", "PRODUCTOS SIN FACTURA"));
    }
}
