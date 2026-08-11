using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace KONTAXPRO.Application.Compras;

public sealed record MarcaProductoCompra(long Id, string Nombre);

public sealed record ProductoCompraPrellenado(
    string Nombre,
    long? MarcaId,
    string? MarcaNombre,
    string PresentacionNombre)
{
    public string? PresentacionCompraNombre { get; init; }
    public decimal FactorPresentacionCompra { get; init; } = 1;
}

public static partial class DescripcionProductoCompraParser
{
    private sealed record ContainerRule(Regex Pattern, string Name);

    private static readonly IReadOnlyList<ContainerRule> Containers =
    [
        new(ContainerFrascoRegex(), "FRASCO"),
        new(ContainerBotellaRegex(), "BOTELLA"),
        new(ContainerCajaRegex(), "CAJA"),
        new(ContainerFundaRegex(), "FUNDA"),
        new(ContainerBlisterRegex(), "BLÍSTER"),
        new(ContainerSobreRegex(), "SOBRE"),
        new(ContainerTuboRegex(), "TUBO"),
        new(ContainerLataRegex(), "LATA"),
        new(ContainerGalonRegex(), "GALÓN"),
        new(ContainerSacoRegex(), "SACO"),
        new(ContainerBidonRegex(), "BIDÓN"),
        new(ContainerAmpollaRegex(), "AMPOLLA"),
        new(ContainerVialRegex(), "VIAL"),
        new(ContainerPaqueteRegex(), "PAQUETE"),
        new(ContainerRolloRegex(), "ROLLO"),
        new(ContainerTarroRegex(), "TARRO")
    ];

    public static ProductoCompraPrellenado Analizar(
        string? descripcion,
        IEnumerable<MarcaProductoCompra> marcas)
    {
        var normalized = Normalize(descripcion);
        if (normalized.Length == 0)
            return new ProductoCompraPrellenado(
                string.Empty, null, null, "UNIDAD");

        normalized = NormalizeMeasures(normalized);
        var measure = MeasureRegex().Match(normalized);
        var brand = FindBrand(normalized, marcas);
        var container = FindContainer(normalized, measure);
        var pack = PackQuantityRegex().Match(normalized);
        var packFactor = 1m;
        var hasPackPresentation = container is not null && pack.Success &&
                                  decimal.TryParse(
                                      pack.Groups["quantity"].Value
                                          .Replace(',', '.'),
                                      NumberStyles.Number,
                                      CultureInfo.InvariantCulture,
                                      out packFactor) &&
                                  packFactor > 1;

        var productName = normalized;
        if (brand is not null)
            productName = brand.Value.Pattern.Replace(productName, " ", 1);
        if (container is not null)
            productName = container.Value.Rule.Pattern.Replace(
                productName, " ", 1);
        if (hasPackPresentation)
            productName = PackQuantityRegex().Replace(productName, " ", 1);
        productName = CollapseSpaces(productName);
        if (productName.Length == 0) productName = normalized;

        var presentation = hasPackPresentation
            ? "UNIDAD"
            : container is null
            ? "UNIDAD"
            : measure.Success
                ? $"{container.Value.Rule.Name} {measure.Value}"
                : container.Value.Rule.Name;

        return new ProductoCompraPrellenado(
            productName,
            brand?.Brand.Id,
            brand?.Brand.Nombre,
            presentation)
        {
            PresentacionCompraNombre = hasPackPresentation
                ? $"{container!.Value.Rule.Name} X{packFactor:0.######}"
                : null,
            FactorPresentacionCompra = hasPackPresentation ? packFactor : 1
        };
    }

    private static (MarcaProductoCompra Brand, Regex Pattern)? FindBrand(
        string description,
        IEnumerable<MarcaProductoCompra> brands)
    {
        return brands
            .Where(x => x.Id > 0 && !string.IsNullOrWhiteSpace(x.Nombre))
            .Select(x =>
            {
                var normalized = Normalize(x.Nombre);
                var words = normalized.Split(' ',
                    StringSplitOptions.RemoveEmptyEntries);
                var pattern = words.Length == 0
                    ? null
                    : new Regex(
                        $"(?<![A-Z0-9]){string.Join(@"\s+", words.Select(Regex.Escape))}(?![A-Z0-9])",
                        RegexOptions.CultureInvariant);
                return new { Brand = x, Normalized = normalized, Pattern = pattern };
            })
            .Where(x => x.Pattern is not null &&
                        x.Pattern.IsMatch(description))
            .OrderByDescending(x => x.Normalized.Length)
            .Select(x => ((MarcaProductoCompra Brand, Regex Pattern)?)
                (x.Brand, x.Pattern!))
            .FirstOrDefault();
    }

    private static (ContainerRule Rule, Match Match)? FindContainer(
        string description,
        Match measure)
    {
        var matches = Containers
            .Select(x => (Rule: x, Match: x.Pattern.Match(description)))
            .Where(x => x.Match.Success);
        return measure.Success
            ? matches.OrderBy(x => Math.Abs(x.Match.Index - measure.Index))
                .Select(x => ((ContainerRule Rule, Match Match)?)x)
                .FirstOrDefault()
            : matches.Select(x => ((ContainerRule Rule, Match Match)?)x)
                .FirstOrDefault();
    }

    private static string NormalizeMeasures(string value) =>
        MeasureRegex().Replace(value, match =>
        {
            var quantity = match.Groups["quantity"].Value.Replace(',', '.');
            var unit = match.Groups["unit"].Value switch
            {
                "LITRO" or "LITROS" or "LT" or "LTS" => "L",
                "MILILITRO" or "MILILITROS" => "ML",
                "GRAMO" or "GRAMOS" or "GR" or "GRS" => "G",
                "KILOGRAMO" or "KILOGRAMOS" => "KG",
                "MILIGRAMO" or "MILIGRAMOS" => "MG",
                "CENTIMETRO" or "CENTIMETROS" => "CM",
                "MILIMETRO" or "MILIMETROS" => "MM",
                _ => match.Groups["unit"].Value
            };
            return $"{quantity} {unit}";
        });

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var decomposed = value.Trim().ToUpperInvariant()
            .Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) ==
                UnicodeCategory.NonSpacingMark)
                continue;
            builder.Append(char.IsLetterOrDigit(character) ||
                           character is ',' or '.' or '%'
                ? character
                : ' ');
        }
        return CollapseSpaces(builder.ToString());
    }

    private static string CollapseSpaces(string value) =>
        MultipleSpacesRegex().Replace(value, " ").Trim(' ', '.', ',');

    [GeneratedRegex(@"(?<![A-Z0-9])(?<quantity>\d+(?:[.,]\d+)?)\s*(?<unit>ML|MILILITROS?|LITROS?|LTS?|LT|L|MG|MILIGRAMOS?|KG|KILOGRAMOS?|GRS?|GRAMOS?|G|CC|CM|CENTIMETROS?|MM|MILIMETROS?)(?![A-Z])", RegexOptions.CultureInvariant)]
    private static partial Regex MeasureRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex MultipleSpacesRegex();

    [GeneratedRegex(@"(?<![A-Z0-9])(?:X|POR)\s*(?<quantity>\d+(?:[.,]\d+)?)(?![A-Z0-9])", RegexOptions.CultureInvariant)]
    private static partial Regex PackQuantityRegex();

    [GeneratedRegex(@"\bFRASCOS?\b", RegexOptions.CultureInvariant)] private static partial Regex ContainerFrascoRegex();
    [GeneratedRegex(@"\bBOTELLAS?\b", RegexOptions.CultureInvariant)] private static partial Regex ContainerBotellaRegex();
    [GeneratedRegex(@"\bCAJAS?\b", RegexOptions.CultureInvariant)] private static partial Regex ContainerCajaRegex();
    [GeneratedRegex(@"\bFUNDAS?\b", RegexOptions.CultureInvariant)] private static partial Regex ContainerFundaRegex();
    [GeneratedRegex(@"\bBLISTERS?\b", RegexOptions.CultureInvariant)] private static partial Regex ContainerBlisterRegex();
    [GeneratedRegex(@"\bSOBRES?\b", RegexOptions.CultureInvariant)] private static partial Regex ContainerSobreRegex();
    [GeneratedRegex(@"\bTUBOS?\b", RegexOptions.CultureInvariant)] private static partial Regex ContainerTuboRegex();
    [GeneratedRegex(@"\bLATAS?\b", RegexOptions.CultureInvariant)] private static partial Regex ContainerLataRegex();
    [GeneratedRegex(@"\bGALONES?\b", RegexOptions.CultureInvariant)] private static partial Regex ContainerGalonRegex();
    [GeneratedRegex(@"\bSACOS?\b", RegexOptions.CultureInvariant)] private static partial Regex ContainerSacoRegex();
    [GeneratedRegex(@"\bBIDONES?\b", RegexOptions.CultureInvariant)] private static partial Regex ContainerBidonRegex();
    [GeneratedRegex(@"\bAMPOLLAS?\b", RegexOptions.CultureInvariant)] private static partial Regex ContainerAmpollaRegex();
    [GeneratedRegex(@"\bVIALES?\b", RegexOptions.CultureInvariant)] private static partial Regex ContainerVialRegex();
    [GeneratedRegex(@"\bPAQUETES?\b", RegexOptions.CultureInvariant)] private static partial Regex ContainerPaqueteRegex();
    [GeneratedRegex(@"\bROLLOS?\b", RegexOptions.CultureInvariant)] private static partial Regex ContainerRolloRegex();
    [GeneratedRegex(@"\bTARROS?\b", RegexOptions.CultureInvariant)] private static partial Regex ContainerTarroRegex();
}
