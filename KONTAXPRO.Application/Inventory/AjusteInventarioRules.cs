namespace KONTAXPRO.Application.Inventory;

public sealed record LoteAjusteSnapshot(
    string NumeroLote,
    decimal CantidadBase,
    decimal Disponible,
    bool EsExistente);

public sealed record SerieAjusteSnapshot(
    string NumeroSerie,
    string? NumeroLote);

public sealed record LoteEntradaAjusteSnapshot(
    Guid FilaId,
    long? ProductoLoteId,
    string NumeroLote,
    decimal CantidadBase,
    DateTime? FechaElaboracion = null,
    DateTime? FechaCaducidad = null,
    bool TieneConflictoSimilar = false);

public sealed record SerieEntradaAjusteSnapshot(
    string NumeroSerie,
    Guid? LoteFilaId);

public sealed record EntradaLoteYSerieAjusteSnapshot(
    bool IsSaving,
    long? BodegaId,
    long? PresentacionId,
    decimal CantidadPresentacion,
    decimal FactorConversion,
    decimal CostoPresentacion,
    string Motivo,
    bool ManejaFechaCaducidad,
    IReadOnlyCollection<LoteEntradaAjusteSnapshot> Lotes,
    IReadOnlyCollection<SerieEntradaAjusteSnapshot> Series,
    IReadOnlySet<string> SeriesExistentes);

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
        var lotesAplicados = esSalida
            ? lotes.Where(x => x.CantidadBase > 0).ToList()
            : lotes.ToList();
        if (lotesAplicados.Count == 0 || lotesAplicados.Any(x =>
                string.IsNullOrWhiteSpace(x.NumeroLote) || x.CantidadBase <= 0))
            return "Todos los lotes deben tener número y cantidad mayor que cero.";
        if (lotesAplicados.Sum(x => x.CantidadBase) != requerido)
            return "La suma de lotes debe coincidir con la cantidad base requerida.";
        if (lotesAplicados.Select(x => NormalizarLoteExacto(x.NumeroLote))
                .Distinct(StringComparer.Ordinal).Count() != lotesAplicados.Count)
            return "Un mismo lote no puede repetirse dentro del ajuste.";
        if (esSalida && lotesAplicados.Any(x => !x.EsExistente ||
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

    public static string? ValidarSeriesNuevas(
        decimal requerido,
        IReadOnlyCollection<SerieAjusteSnapshot> series,
        IReadOnlySet<string> seriesExistentes)
    {
        if (series.Any(x => string.IsNullOrWhiteSpace(x.NumeroSerie)))
            return "Complete o elimine las filas de serie vacías.";
        var error = ValidarSeries(requerido, series);
        if (error is not null) return error;
        var existente = series.FirstOrDefault(x => seriesExistentes.Contains(
            x.NumeroSerie.Trim().ToUpperInvariant()));
        return existente is null
            ? null
            : $"La serie '{existente.NumeroSerie.Trim().ToUpperInvariant()}' ya existe para este producto.";
    }

    public static IReadOnlyDictionary<string, decimal> DerivarLotesDesdeSeries(
        IEnumerable<SerieAjusteSnapshot> series) =>
        series.GroupBy(x => x.NumeroLote ?? string.Empty,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => (decimal)x.Count(),
                StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> ObtenerRazonesEntradaLoteYSerieNoValida(
        EntradaLoteYSerieAjusteSnapshot ajuste)
    {
        var razones = new List<string>();
        if (ajuste.IsSaving)
            razones.Add("El ajuste se está procesando.");
        if (!ajuste.BodegaId.HasValue)
            razones.Add("Seleccione una bodega.");
        if (!ajuste.PresentacionId.HasValue)
            razones.Add("Seleccione una presentación.");
        if (ajuste.CantidadPresentacion <= 0)
            razones.Add("La cantidad debe ser mayor que cero.");
        if (ajuste.FactorConversion <= 0)
            razones.Add("La presentación no tiene un factor de conversión válido.");
        if (ajuste.CostoPresentacion < 0)
            razones.Add("El costo no puede ser negativo.");
        if (string.IsNullOrWhiteSpace(ajuste.Motivo))
            razones.Add("Escriba el motivo del ajuste.");

        var cantidadBase = ajuste.CantidadPresentacion * ajuste.FactorConversion;
        var errorLotes = ValidarLotes(cantidadBase,
            ajuste.Lotes.Select(x => new LoteAjusteSnapshot(
                x.NumeroLote, x.CantidadBase, 0,
                x.ProductoLoteId.HasValue)).ToList(), false);
        if (errorLotes is not null)
            razones.Add(errorLotes);
        if (ajuste.Lotes.Any(x => x.TieneConflictoSimilar))
            razones.Add("Resuelva la advertencia de lote posiblemente equivalente antes de continuar.");
        if (ajuste.ManejaFechaCaducidad && ajuste.Lotes.Any(x =>
                !x.ProductoLoteId.HasValue && !x.FechaCaducidad.HasValue))
            razones.Add("Ingrese la caducidad de cada lote nuevo.");
        if (ajuste.Lotes.Any(x => !x.ProductoLoteId.HasValue &&
                x.FechaElaboracion.HasValue && x.FechaCaducidad.HasValue &&
                x.FechaElaboracion.Value.Date > x.FechaCaducidad.Value.Date))
            razones.Add("La elaboración no puede ser posterior a la caducidad.");

        var lotesPorId = ajuste.Lotes.ToDictionary(x => x.FilaId);
        var series = ajuste.Series.Select(x => new SerieAjusteSnapshot(
            x.NumeroSerie,
            x.LoteFilaId.HasValue && lotesPorId.TryGetValue(
                x.LoteFilaId.Value, out var lote)
                    ? lote.NumeroLote
                    : null)).ToList();
        var errorSeries = ValidarSeriesNuevas(
            cantidadBase, series, ajuste.SeriesExistentes);
        if (errorSeries is not null)
            razones.Add(errorSeries);

        var seriesCompletas = ajuste.Series.Where(x =>
            !string.IsNullOrWhiteSpace(x.NumeroSerie)).ToList();
        if (seriesCompletas.Any(x => !x.LoteFilaId.HasValue ||
                                     !lotesPorId.ContainsKey(x.LoteFilaId.Value)))
            razones.Add("Asocie cada serie nueva a uno de los lotes del ajuste.");
        else
            foreach (var lote in ajuste.Lotes)
                if (seriesCompletas.Count(x => x.LoteFilaId == lote.FilaId) !=
                    lote.CantidadBase)
                    razones.Add(
                        $"Las series asociadas al lote '{lote.NumeroLote}' deben coincidir con su cantidad.");

        return razones.Distinct(StringComparer.Ordinal).ToList();
    }
}
