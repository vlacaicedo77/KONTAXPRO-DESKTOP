using KONTAXPRO.Application.Compras;

namespace KONTAXPRO.Tests.Compras;

public sealed class TextoCompraSimilarityTests
{
    [Fact]
    public void Normalizar_QuitaAcentosYPuntuacion() =>
        Assert.Equal("CAFÉ MOLIDO 500G".Replace('É', 'E'),
            TextoCompraSimilarity.Normalizar(" Café, molido 500g "));

    [Fact]
    public void Calcular_DaMayorPuntajeACoincidenciaRelevante()
    {
        var close = TextoCompraSimilarity.Calcular(
            "CAFE MOLIDO PREMIUM 500G", "Cafe premium molido 500 g");
        var far = TextoCompraSimilarity.Calcular(
            "CAFE MOLIDO PREMIUM 500G", "Aceite vegetal 1 litro");
        Assert.True(close > far);
        Assert.True(close >= 0.65m);
    }
}
