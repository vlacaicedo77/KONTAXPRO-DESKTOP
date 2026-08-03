namespace KONTAXPRO.Application.Products;

public static class ProductoNuevoRules
{
    public static decimal CalcularCantidadBase(
        decimal cantidadPresentacion,
        decimal factorConversion)
    {
        if (cantidadPresentacion < 0)
            throw new ArgumentOutOfRangeException(
                nameof(cantidadPresentacion));
        if (factorConversion <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(factorConversion));

        return cantidadPresentacion * factorConversion;
    }

    public static decimal CalcularCostoTotal(
        decimal cantidadPresentacion,
        decimal costoPresentacion)
    {
        if (cantidadPresentacion < 0)
            throw new ArgumentOutOfRangeException(
                nameof(cantidadPresentacion));
        if (costoPresentacion < 0)
            throw new ArgumentOutOfRangeException(
                nameof(costoPresentacion));

        return cantidadPresentacion * costoPresentacion;
    }

    public static decimal CalcularCostoUnitarioBase(
        decimal costoTotal,
        decimal cantidadBase) =>
        cantidadBase == 0 ? 0 : costoTotal / cantidadBase;

    public static decimal CalcularCostoPromedioPonderado(
        IEnumerable<CostoInventarioEntrada> entradas)
    {
        var materializadas = entradas.ToList();
        var cantidadTotal = materializadas.Sum(x => x.CantidadBase);
        return cantidadTotal == 0
            ? 0
            : materializadas.Sum(x => x.CostoTotal) / cantidadTotal;
    }

    public static bool DistribucionLotesCoincide(
        decimal cantidadBase,
        IEnumerable<decimal> cantidadesLotes) =>
        cantidadesLotes.Sum() == cantidadBase;

    public static bool FechasLoteValidas(
        DateOnly? fechaElaboracion,
        DateOnly? fechaCaducidad) =>
        !fechaElaboracion.HasValue ||
        !fechaCaducidad.HasValue ||
        fechaElaboracion.Value <= fechaCaducidad.Value;

    public static bool CaducidadesCompletas(
        bool controlaCaducidad,
        IEnumerable<DateOnly?> fechasCaducidad) =>
        !controlaCaducidad ||
        fechasCaducidad.All(x => x.HasValue);

    public static bool SeriesCompletasYUnicas(
        decimal cantidadBase,
        IEnumerable<string> numerosSerie)
    {
        if (cantidadBase != decimal.Truncate(cantidadBase))
            return false;

        var series = numerosSerie
            .Select(x => x.Trim())
            .ToList();
        return series.Count == (int)cantidadBase &&
               series.All(x => x.Length > 0) &&
               series.Distinct(StringComparer.OrdinalIgnoreCase).Count() ==
               series.Count;
    }

    public static decimal CalcularPrecioPorcentajeCosto(
        decimal costoPresentacion,
        decimal porcentaje) =>
        costoPresentacion * (1 + porcentaje / 100m);

    public static decimal CalcularPrecioConDescuento(
        decimal precioListaBase,
        decimal porcentajeDescuento) =>
        precioListaBase * (1 - porcentajeDescuento / 100m);

    public static decimal CalcularPrecioEquivalentePresentacion(
        decimal precioPresentacionBase,
        decimal factorConversion)
    {
        if (precioPresentacionBase < 0)
            throw new ArgumentOutOfRangeException(
                nameof(precioPresentacionBase));
        if (factorConversion <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(factorConversion));

        return precioPresentacionBase * factorConversion;
    }

    public static bool PrecioPresentacionSuperaEquivalente(
        decimal precioPresentacion,
        decimal precioPresentacionBase,
        decimal factorConversion) =>
        factorConversion > 1 &&
        precioPresentacionBase > 0 &&
        precioPresentacion > CalcularPrecioEquivalentePresentacion(
            precioPresentacionBase,
            factorConversion);

    public static string CrearCodigoBarrasInterno(
        string prefijoEstablecimiento,
        long presentacionId)
    {
        if (string.IsNullOrWhiteSpace(prefijoEstablecimiento))
            throw new ArgumentException(
                "El prefijo es obligatorio.",
                nameof(prefijoEstablecimiento));
        if (presentacionId <= 0)
            throw new ArgumentOutOfRangeException(nameof(presentacionId));

        return $"KPX-{prefijoEstablecimiento.Trim().ToUpperInvariant()}-" +
               $"{presentacionId:00000000}";
    }
}

public readonly record struct CostoInventarioEntrada(
    decimal CantidadBase,
    decimal CostoTotal);
