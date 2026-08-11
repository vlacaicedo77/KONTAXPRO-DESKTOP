using KONTAXPRO.Application.Compras;

namespace KONTAXPRO.Tests.Compras;

public sealed class DescripcionProductoCompraParserTests
{
    private static readonly MarcaProductoCompra[] Brands =
    [
        new(1, "FARBIOVET"),
        new(2, "Laboratorios Life"),
        new(3, "LIFE")
    ];

    [Fact]
    public void Analizar_SeparaMarcaEnvaseYMedida()
    {
        var result = DescripcionProductoCompraParser.Analizar(
            "IVERMEC 100ML FARBIOVET FRASCO", Brands);

        Assert.Equal("IVERMEC 100 ML", result.Nombre);
        Assert.Equal(1, result.MarcaId);
        Assert.Equal("FARBIOVET", result.MarcaNombre);
        Assert.Equal("FRASCO 100 ML", result.PresentacionNombre);
    }

    [Fact]
    public void Analizar_SinEnvaseNormalizaMedidaSinInferirPresentacion()
    {
        var result = DescripcionProductoCompraParser.Analizar(
            "ACETAMINOFEN 500MG", Brands);

        Assert.Equal("ACETAMINOFEN 500 MG", result.Nombre);
        Assert.Null(result.MarcaId);
        Assert.Equal("UNIDAD", result.PresentacionNombre);
    }

    [Fact]
    public void Analizar_PrefiereLaMarcaExactaMasLarga()
    {
        var result = DescripcionProductoCompraParser.Analizar(
            "VITAMINA C LABORATORIOS LIFE CAJA 20 MG", Brands);

        Assert.Equal("VITAMINA C 20 MG", result.Nombre);
        Assert.Equal(2, result.MarcaId);
        Assert.Equal("CAJA 20 MG", result.PresentacionNombre);
    }

    [Fact]
    public void Analizar_NoConfundeUnaMarcaIncluidaEnOtraPalabra()
    {
        var result = DescripcionProductoCompraParser.Analizar(
            "LIFENOL 20MG FRASCO", Brands);

        Assert.Equal("LIFENOL 20 MG", result.Nombre);
        Assert.Null(result.MarcaId);
    }

    [Fact]
    public void Analizar_NoAsignaMarcaPorErrorOrtografico()
    {
        var result = DescripcionProductoCompraParser.Analizar(
            "IVERMEC 100ML FARVIOBET FRASCO", Brands);

        Assert.Equal("IVERMEC 100 ML FARVIOBET", result.Nombre);
        Assert.Null(result.MarcaId);
        Assert.Equal("FRASCO 100 ML", result.PresentacionNombre);
    }

    [Fact]
    public void Analizar_CajaPorCantidad_SeparaBaseYPresentacionCompra()
    {
        var result = DescripcionProductoCompraParser.Analizar(
            "GUANTES VETERINARIOS LARGOS CAJA X100", Brands);

        Assert.Equal("GUANTES VETERINARIOS LARGOS", result.Nombre);
        Assert.Equal("UNIDAD", result.PresentacionNombre);
        Assert.Equal("CAJA X100", result.PresentacionCompraNombre);
        Assert.Equal(100, result.FactorPresentacionCompra);
    }
}
