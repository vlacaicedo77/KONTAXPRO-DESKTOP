namespace KONTAXPRO.Application.Inventory;

public static class MotivoOperacionInventarioRules
{
    public static readonly IReadOnlySet<string> TiposOperacionValidos =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "INVENTARIO_INICIAL_ADICIONAL",
            "AJUSTE_ENTRADA",
            "AJUSTE_SALIDA",
            "CONVERSION_CONTROL",
            "CORRECCION_LOTE_SERIE"
        };

    public static string NormalizarTipo(string? tipoOperacion) =>
        (tipoOperacion ?? string.Empty).Trim().ToUpperInvariant();

    public static string NormalizarNombre(string? nombre) =>
        (nombre ?? string.Empty).Trim().ToUpperInvariant();

    public static bool EsTipoValido(string? tipoOperacion) =>
        TiposOperacionValidos.Contains(NormalizarTipo(tipoOperacion));

    public static bool EsVisibleParaEmpresa(
        long? motivoEmpresaId,
        int estado,
        string motivoTipoOperacion,
        long empresaId,
        string tipoOperacion) =>
        estado == 1 &&
        (motivoEmpresaId is null || motivoEmpresaId == empresaId) &&
        string.Equals(NormalizarTipo(motivoTipoOperacion),
            NormalizarTipo(tipoOperacion), StringComparison.Ordinal);

    public static bool EsDuplicadoEnAlcance(
        long? motivoEmpresaId,
        string motivoTipoOperacion,
        string motivoNombre,
        long? empresaId,
        string tipoOperacion,
        string nombre) =>
        motivoEmpresaId == empresaId &&
        string.Equals(NormalizarTipo(motivoTipoOperacion),
            NormalizarTipo(tipoOperacion), StringComparison.Ordinal) &&
        string.Equals(NormalizarNombre(motivoNombre),
            NormalizarNombre(nombre), StringComparison.Ordinal);

    public static string CrearSnapshot(string nombre) =>
        NormalizarNombre(nombre);
}
