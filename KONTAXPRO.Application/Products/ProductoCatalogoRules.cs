namespace KONTAXPRO.Application.Products;

public static class ProductoCatalogoRules
{
    public static bool CoincideBusqueda(
        string? busqueda,
        params string?[] valores)
    {
        var texto = busqueda?.Trim();
        return string.IsNullOrEmpty(texto) || valores.Any(x =>
            !string.IsNullOrEmpty(x) &&
            x.Contains(texto, StringComparison.OrdinalIgnoreCase));
    }

    public static bool TieneStockBajo(
        IEnumerable<StockBodegaCatalogo> bodegas) =>
        bodegas.Any(x => x.Operativa && x.Disponible > 0 &&
                         x.StockMinimo > 0 && x.Disponible <= x.StockMinimo);

    public static bool SinStock(
        IEnumerable<StockBodegaCatalogo> bodegas) =>
        bodegas.Where(x => x.Operativa).Sum(x => x.Disponible) <= 0;

    public static bool PorCaducar(
        IEnumerable<LoteCaducidadCatalogo> lotes,
        DateOnly hoy,
        int diasAlerta) =>
        diasAlerta > 0 && lotes.Any(x =>
            x.Activo && x.StockActual > 0 && x.FechaCaducidad.HasValue &&
            x.FechaCaducidad.Value >= hoy &&
            x.FechaCaducidad.Value <= hoy.AddDays(diasAlerta));

    public static int CalcularTotalPaginas(int totalItems, int tamanoPagina) =>
        totalItems <= 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)Math.Max(1, tamanoPagina));

    public static int NormalizarPagina(
        int paginaSolicitada,
        int totalPaginas) =>
        totalPaginas == 0
            ? 1
            : Math.Clamp(paginaSolicitada, 1, totalPaginas);
}

public readonly record struct StockBodegaCatalogo(
    decimal Disponible,
    decimal StockMinimo,
    bool Operativa = true);

public readonly record struct LoteCaducidadCatalogo(
    DateOnly? FechaCaducidad,
    decimal StockActual,
    bool Activo = true);
