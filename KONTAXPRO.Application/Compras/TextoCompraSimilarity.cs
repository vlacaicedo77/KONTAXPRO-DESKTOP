using System.Globalization;
using System.Text;

namespace KONTAXPRO.Application.Compras;

public static class TextoCompraSimilarity
{
    private static readonly HashSet<string> StopWords =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "DE", "DEL", "LA", "LAS", "EL", "LOS", "Y", "EN", "CON",
            "PARA", "POR", "UN", "UNA"
        };

    public static string Normalizar(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var decomposed = value.Trim().ToUpperInvariant()
            .Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var previousSpace = false;
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) ==
                UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousSpace = false;
            }
            else if (!previousSpace)
            {
                builder.Append(' ');
                previousSpace = true;
            }
        }
        return builder.ToString().Trim();
    }

    public static IReadOnlyList<string> Tokens(string? value) =>
        Normalizar(value).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(x => x.Length >= 3 && !StopWords.Contains(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static decimal Calcular(string source, string candidate)
    {
        var sourceTokens = Tokens(source);
        var candidateTokens = Tokens(candidate);
        if (sourceTokens.Count == 0 || candidateTokens.Count == 0) return 0;
        var intersection = sourceTokens.Intersect(candidateTokens,
            StringComparer.OrdinalIgnoreCase).Count();
        var union = sourceTokens.Union(candidateTokens,
            StringComparer.OrdinalIgnoreCase).Count();
        var jaccard = union == 0 ? 0m : (decimal)intersection / union;
        var coverage = (decimal)intersection / sourceTokens.Count;
        return decimal.Round((jaccard * 0.4m) + (coverage * 0.6m), 4);
    }
}
