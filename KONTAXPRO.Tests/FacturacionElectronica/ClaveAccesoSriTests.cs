using KONTAXPRO.Application.FacturacionElectronica;
using KONTAXPRO.Application.Models.FacturacionElectronica;
using KONTAXPRO.Infrastructure.FacturacionElectronica;
using Xunit;

namespace KONTAXPRO.Tests.FacturacionElectronica;

public sealed class ClaveAccesoSriTests
{
    [Fact]
    public void Modulo11_CoincideConVectorOficialSri()
    {
        Assert.Equal(6,
            GeneradorClaveAccesoSri.CalcularDigitoVerificador("41261533"));
    }

    [Fact]
    public void Generar_CoincideConClavePublicadaPorSri()
    {
        var sut = new GeneradorClaveAccesoSri();

        var clave = sut.Generar(new SolicitudClaveAccesoSri(
            new DateOnly(2012, 3, 5),
            "01",
            "1760013210001",
            1,
            "001",
            "003",
            990064,
            "12345678"));

        Assert.Equal(
            "0503201201176001321000110010030009900641234567814",
            clave);
        Assert.True(sut.EsValida(clave));
    }

    [Fact]
    public void EsValida_RechazaClaveAlterada()
    {
        var sut = new GeneradorClaveAccesoSri();
        Assert.False(sut.EsValida(
            "0503201201176001321000110010030009900641234567815"));
    }

    [Theory]
    [InlineData("123", "001", "001", "12345678")]
    [InlineData("1760013210001", "01", "001", "12345678")]
    [InlineData("1760013210001", "001", "1A1", "12345678")]
    [InlineData("1760013210001", "001", "001", "1234567A")]
    public void Generar_RechazaComponentesInvalidos(
        string ruc, string establecimiento, string punto, string codigo)
    {
        var sut = new GeneradorClaveAccesoSri();

        Assert.ThrowsAny<ArgumentException>(() => sut.Generar(
            new SolicitudClaveAccesoSri(
                new DateOnly(2026, 8, 16), "01", ruc, 1,
                establecimiento, punto, 1, codigo)));
    }

    [Fact]
    public void CodigoNumerico_UsaSiempreOchoDigitos()
    {
        var sut = new GeneradorCodigoNumericoSri();

        for (var index = 0; index < 50; index++)
        {
            var codigo = sut.Generar();
            Assert.Equal(8, codigo.Length);
            Assert.All(codigo, character => Assert.InRange(character, '0', '9'));
        }
    }
}
