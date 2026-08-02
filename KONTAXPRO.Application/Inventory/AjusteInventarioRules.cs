namespace KONTAXPRO.Application.Inventory;

public sealed record LoteAjusteSnapshot(
    string NumeroLote,
    decimal CantidadBase,
    decimal Disponible,
    bool EsExistente);

public sealed record SerieAjusteSnapshot(
    string NumeroSerie,
    string? NumeroLote);

public static class AjusteInventarioRules
{
    public static string NormalizarLoteExacto(string value) =>
        value.Trim().ToUpperInvariant();

    public static string NormalizarLoteComparable(string value) =>
        new(NormalizarLoteExacto(value).Where(char.IsLetterOrDigit).ToArray());

    public static bool EsPosibleEquivalente(string primero, string segundo) =>
        !string.Equals(NormalizarLoteExacto(primero),
            NormalizarLoteExacto(segundo), StringComparison.Ordinal) &&
        string.Equals(NormalizarLoteComparable(primero),
            NormalizarLoteComparable(segundo), StringComparison.Ordinal);

    public static bool CoincideBusquedaLote(string codigo, string consulta)
    {
        var texto = NormalizarLoteExacto(consulta);
        var flexible = NormalizarLoteComparable(consulta);
        return NormalizarLoteExacto(codigo).Contains(texto,
                   StringComparison.OrdinalIgnoreCase) ||
               NormalizarLoteComparable(codigo).Contains(flexible,
                   StringComparison.OrdinalIgnoreCase);
    }

    public static int PrioridadBusquedaLote(string codigo, string consulta)
    {
        var normal = NormalizarLoteExacto(codigo);
        var texto = NormalizarLoteExacto(consulta);
        var flexible = NormalizarLoteComparable(codigo);
        var consultaFlexible = NormalizarLoteComparable(consulta);
        if (normal == texto) return 0;
        if (flexible == consultaFlexible) return 1;
        if (normal.StartsWith(texto, StringComparison.OrdinalIgnoreCase)) return 2;
        if (flexible.StartsWith(consultaFlexible,
                StringComparison.OrdinalIgnoreCase)) return 3;
        if (normal.Contains(texto, StringComparison.OrdinalIgnoreCase)) return 4;
        return 5;
    }

    public static string? ValidarLotes(
        decimal requerido,
        IReadOnlyCollection<LoteAjusteSnapshot> lotes,
        bool esSalida)
    {
        if (lotes.Count == 0 || lotes.Any(x =>
                string.IsNullOrWhiteSpace(x.NumeroLote) || x.CantidadBase <= 0))
            return "Todos los lotes deben tener número y cantidad mayor que cero.";
        if (lotes.Sum(x => x.CantidadBase) != requerido)
            return "La suma de lotes debe coincidir con la cantidad base requerida.";
        if (lotes.Select(x => NormalizarLoteExacto(x.NumeroLote))
                .Distinct(StringComparer.Ordinal).Count() != lotes.Count)
            return "Un mismo lote no puede repetirse dentro del ajuste.";
        if (esSalida && lotes.Any(x => !x.EsExistente ||
                                      x.CantidadBase > x.Disponible))
            return "La salida sólo admite lotes existentes con stock suficiente.";
        return null;
    }

    public static string? ValidarSeries(
        decimal requerido,
        IReadOnlyCollection<SerieAjusteSnapshot> series)
    {
        if (requerido != decimal.Truncate(requerido))
            return "La cantidad base serializada debe ser entera.";
        var validas = series.Where(x => !string.IsNullOrWhiteSpace(x.NumeroSerie))
            .ToList();
        if (validas.Count != (int)requerido)
            return "Debe existir una serie por cada unidad base.";
        if (validas.Select(x => x.NumeroSerie.Trim().ToUpperInvariant())
                .Distinct(StringComparer.Ordinal).Count() != validas.Count)
            return "Las series no pueden repetirse.";
        return null;
    }

    public static IReadOnlyDictionary<string, decimal> DerivarLotesDesdeSeries(
        IEnumerable<SerieAjusteSnapshot> series) =>
        series.GroupBy(x => x.NumeroLote ?? string.Empty,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => (decimal)x.Count(),
                StringComparer.OrdinalIgnoreCase);
}
