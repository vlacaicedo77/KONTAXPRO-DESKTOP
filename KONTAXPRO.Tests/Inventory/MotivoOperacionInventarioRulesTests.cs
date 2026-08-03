using KONTAXPRO.Application.Inventory;

namespace KONTAXPRO.Tests.Inventory;

public class MotivoOperacionInventarioRulesTests
{
    [Fact]
    public void MotivoGlobalActivo_DelTipoSolicitado_EsVisible() =>
        Assert.True(MotivoOperacionInventarioRules.EsVisibleParaEmpresa(
            null, 1, "AJUSTE_ENTRADA", 7, "ajuste_entrada"));

    [Fact]
    public void MotivoPrivado_SoloEsVisibleParaSuEmpresa()
    {
        Assert.True(MotivoOperacionInventarioRules.EsVisibleParaEmpresa(
            7, 1, "AJUSTE_SALIDA", 7, "AJUSTE_SALIDA"));
        Assert.False(MotivoOperacionInventarioRules.EsVisibleParaEmpresa(
            7, 1, "AJUSTE_SALIDA", 8, "AJUSTE_SALIDA"));
    }

    [Fact]
    public void MotivoInactivo_NoEsVisible() =>
        Assert.False(MotivoOperacionInventarioRules.EsVisibleParaEmpresa(
            null, 0, "CONVERSION_CONTROL", 7, "CONVERSION_CONTROL"));

    [Fact]
    public void MotivoDeOtroTipo_NoEsCompatible() =>
        Assert.False(MotivoOperacionInventarioRules.EsVisibleParaEmpresa(
            null, 1, "AJUSTE_ENTRADA", 7, "AJUSTE_SALIDA"));

    [Fact]
    public void NombreRepetidoEnMismoTipoYEmpresa_EsDuplicado() =>
        Assert.True(MotivoOperacionInventarioRules.EsDuplicadoEnAlcance(
            7, "AJUSTE_ENTRADA", "Daño en recepción",
            7, "ajuste_entrada", "  DAÑO EN RECEPCIÓN "));

    [Fact]
    public void MismoNombreEnOtraEmpresa_NoEsDuplicado() =>
        Assert.False(MotivoOperacionInventarioRules.EsDuplicadoEnAlcance(
            7, "AJUSTE_ENTRADA", "CONTEO FÍSICO",
            8, "AJUSTE_ENTRADA", "CONTEO FÍSICO"));

    [Theory]
    [InlineData("INVENTARIO_INICIAL_ADICIONAL")]
    [InlineData("AJUSTE_ENTRADA")]
    [InlineData("AJUSTE_SALIDA")]
    [InlineData("CONVERSION_CONTROL")]
    [InlineData("CORRECCION_LOTE_SERIE")]
    public void TiposAprobados_SonValidos(string tipo) =>
        Assert.True(MotivoOperacionInventarioRules.EsTipoValido(tipo));

    [Fact]
    public void Otro_NoEsUnTipoAprobado() =>
        Assert.False(MotivoOperacionInventarioRules.EsTipoValido("OTRO"));

    [Fact]
    public void Snapshot_ConservaTextoNormalizadoAunqueCambieElCatalogo()
    {
        var snapshot = MotivoOperacionInventarioRules.CrearSnapshot(
            "  Corrección de identificación  ");
        var nombreCatalogoPosterior = "NOMBRE MODIFICADO";

        Assert.Equal("CORRECCIÓN DE IDENTIFICACIÓN", snapshot);
        Assert.NotEqual(nombreCatalogoPosterior, snapshot);
    }
}
