namespace KONTAXPRO.Application.Inventory;

public sealed record ExistenciaControlSnapshot(
    long BodegaId,
    decimal StockActual,
    decimal StockReservado);

public sealed record DistribucionControlSnapshot(
    long BodegaId,
    decimal CantidadBase,
    int CantidadSeries);

public static class InventarioControlRules
{
    public static readonly string[] TiposControl =
        ["NORMAL", "LOTE", "SERIE", "LOTE_Y_SERIE"];

    public static string? ValidarConversion(
        string tipoAnterior,
        string tipoNuevo,
        IReadOnlyCollection<ExistenciaControlSnapshot> existencias,
        IReadOnlyCollection<DistribucionControlSnapshot> distribuciones)
    {
        if (!TiposControl.Contains(tipoAnterior) ||
            !TiposControl.Contains(tipoNuevo) ||
            tipoAnterior == tipoNuevo)
            return "Seleccione una conversión de control válida.";

        var transicionSoportada = tipoAnterior switch
        {
            "NORMAL" => tipoNuevo is "LOTE" or "SERIE" or "LOTE_Y_SERIE",
            "LOTE" => tipoNuevo is "NORMAL" or "LOTE_Y_SERIE",
            "SERIE" => tipoNuevo is "NORMAL" or "LOTE_Y_SERIE",
            "LOTE_Y_SERIE" => tipoNuevo is "NORMAL" or "LOTE" or "SERIE",
            _ => false
        };
        if (!transicionSoportada)
            return $"La transición {tipoAnterior} → {tipoNuevo} no está soportada.";

        if (existencias.Any(x => x.StockReservado > 0))
            return "No es posible convertir el control mientras existan reservas.";

        foreach (var existencia in existencias)
        {
            var cantidadDistribuida = distribuciones
                .Where(x => x.BodegaId == existencia.BodegaId)
                .Sum(x => x.CantidadBase);
            if (cantidadDistribuida != existencia.StockActual)
                return "La distribución debe coincidir con el stock actual de cada bodega.";

            if (tipoNuevo is "SERIE" or "LOTE_Y_SERIE")
            {
                if (existencia.StockActual != decimal.Truncate(existencia.StockActual))
                    return "Un producto con stock fraccionario no puede controlarse por serie.";
                var series = distribuciones
                    .Where(x => x.BodegaId == existencia.BodegaId)
                    .Sum(x => x.CantidadSeries);
                if (series != (int)existencia.StockActual)
                    return "Debe registrar una serie por cada unidad base de la bodega.";
            }
        }

        return null;
    }

    public static string? ValidarReduccionControl(
        string tipoAnterior,
        string tipoNuevo,
        bool tieneHistoria,
        IReadOnlyCollection<ExistenciaControlSnapshot> existencias)
    {
        var esReduccion = tipoAnterior != "NORMAL" &&
                          (tipoNuevo == "NORMAL" ||
                           tipoAnterior == "LOTE_Y_SERIE");
        if (esReduccion &&
            (tieneHistoria || existencias.Any(x =>
                x.StockActual != 0 || x.StockReservado != 0)))
            return "La conversión hacia un control menos estricto requiere stock cero, sin reservas y sin historia relevante.";
        return null;
    }
}
