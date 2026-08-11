namespace KONTAXPRO.Application.Compras;

public static class CodigoBarrasCompraRules
{
    private static readonly HashSet<int> GtinLengths = [8, 12, 13, 14];

    public static bool EsGtinValido(string? value)
    {
        var code = value?.Trim() ?? string.Empty;
        if (!GtinLengths.Contains(code.Length) ||
            code.Any(x => !char.IsAsciiDigit(x)) ||
            code.Distinct().Count() == 1)
            return false;

        var sum = 0;
        var multiplyByThree = true;
        for (var index = code.Length - 2; index >= 0; index--)
        {
            var digit = code[index] - '0';
            sum += digit * (multiplyByThree ? 3 : 1);
            multiplyByThree = !multiplyByThree;
        }
        var expectedCheckDigit = (10 - sum % 10) % 10;
        return expectedCheckDigit == code[^1] - '0';
    }

    public static IReadOnlyList<string> ObtenerCandidatos(
        string? codigoPrincipal,
        string? codigoAuxiliar) =>
        new[] { codigoPrincipal, codigoAuxiliar }
            .Select(x => x?.Trim())
            .Where(x => EsGtinValido(x))
            .Select(x => x!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
}
