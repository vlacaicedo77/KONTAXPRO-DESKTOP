using KONTAXPRO.Application.Products;
using KONTAXPRO.Application.Models.Productos;

namespace KONTAXPRO.Tests.Products;

public sealed class ProductoCatalogoRulesTests
{
    [Fact]
    public void UnaPresentacion_SeMuestraDirectamenteSinContador()
    {
        var producto = new ProductoListadoDto
        {
            PresentacionesComerciales = ["UNIDAD"]
        };

        Assert.Equal(["UNIDAD"], producto.PresentacionesVisibles);
        Assert.Equal(0, producto.CantidadPresentacionesAdicionales);
        Assert.False(producto.TienePresentacionesAdicionales);
    }

    [Fact]
    public void CincoPresentaciones_MuestraTresYDosAdicionales()
    {
        var producto = new ProductoListadoDto
        {
            PresentacionesComerciales =
                ["UNIDAD", "CAJA X6", "CAJA X12", "PAQUETE X24", "DISPLAY X48"]
        };

        Assert.Equal(
            ["UNIDAD", "CAJA X6", "CAJA X12"],
            producto.PresentacionesVisibles);
        Assert.Equal(2, producto.CantidadPresentacionesAdicionales);
        Assert.True(producto.TienePresentacionesAdicionales);
    }

    [Theory]
    [InlineData("martillo", "Martillo profesional")]
    [InlineData("PRO-001", "PRO-001")]
    [InlineData("x200", "Modelo X200")]
    [InlineData("acme", "ACME")]
    [InlineData("herram", "Herramientas")]
    [InlineData("caja 12", "Caja 12 unidades")]
    [InlineData("7861234567890", "7861234567890")]
    public void Busqueda_CoincideParcialSinDistinguirMayusculas(
        string consulta,
        string valor) =>
        Assert.True(ProductoCatalogoRules.CoincideBusqueda(
            consulta,
            valor));

    [Fact]
    public void Busqueda_ConsideraTodosLosCamposIndexados()
    {
        var valores = new string?[]
        {
            "PRO-002", "Taladro", "TX-5", "ACME", "Herramientas",
            "Caja", "7860000000002"
        };

        Assert.True(ProductoCatalogoRules.CoincideBusqueda(
            "0000002",
            valores));
        Assert.False(ProductoCatalogoRules.CoincideBusqueda(
            "inexistente",
            valores));
    }

    [Fact]
    public void StockBajo_CuentaProductoSiAlgunaBodegaOperativaCumple()
    {
        var bodegas = new[]
        {
            new StockBodegaCatalogo(20, 5),
            new StockBodegaCatalogo(3, 5),
            new StockBodegaCatalogo(1, 10, false)
        };

        Assert.True(ProductoCatalogoRules.TieneStockBajo(bodegas));
    }

    [Fact]
    public void SinStock_SeCalculaConSumaDisponibleDelProducto()
    {
        var conStock = new[]
        {
            new StockBodegaCatalogo(0, 0),
            new StockBodegaCatalogo(4, 0)
        };
        var sinStock = new[]
        {
            new StockBodegaCatalogo(1, 0),
            new StockBodegaCatalogo(-1, 0)
        };

        Assert.False(ProductoCatalogoRules.SinStock(conStock));
        Assert.True(ProductoCatalogoRules.SinStock(sinStock));
    }

    [Fact]
    public void PorCaducar_VariosLotesRepresentanUnSoloProducto()
    {
        var hoy = new DateOnly(2026, 8, 2);
        var lotes = new[]
        {
            new LoteCaducidadCatalogo(hoy.AddDays(3), 4),
            new LoteCaducidadCatalogo(hoy.AddDays(8), 7)
        };

        Assert.True(ProductoCatalogoRules.PorCaducar(lotes, hoy, 10));
    }

    [Fact]
    public void PorCaducar_NoIncluyeLoteAgotado()
    {
        var hoy = new DateOnly(2026, 8, 2);
        var lotes = new[]
        {
            new LoteCaducidadCatalogo(hoy.AddDays(3), 0)
        };

        Assert.False(ProductoCatalogoRules.PorCaducar(lotes, hoy, 10));
    }

    [Theory]
    [InlineData(0, 25, 0)]
    [InlineData(1, 25, 1)]
    [InlineData(25, 25, 1)]
    [InlineData(26, 25, 2)]
    [InlineData(1038, 25, 42)]
    public void TotalPaginas_EsDeterminista(
        int total,
        int tamano,
        int esperado) =>
        Assert.Equal(
            esperado,
            ProductoCatalogoRules.CalcularTotalPaginas(total, tamano));

    [Theory]
    [InlineData(0, 4, 1)]
    [InlineData(1, 4, 1)]
    [InlineData(3, 4, 3)]
    [InlineData(8, 4, 4)]
    public void PaginaSolicitada_SeMantieneDentroDelRango(
        int solicitada,
        int totalPaginas,
        int esperada) =>
        Assert.Equal(
            esperada,
            ProductoCatalogoRules.NormalizarPagina(
                solicitada,
                totalPaginas));
}
