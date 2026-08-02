namespace KONTAXPRO.Application.Products;

public static class ProductoInventarioRules
{
    public static bool PuedeCompletarInventarioInicial(
        IEnumerable<string> tiposMovimiento) =>
        tiposMovimiento.All(x => string.Equals(
            x,
            "INVENTARIO_INICIAL",
            StringComparison.OrdinalIgnoreCase));

    public static decimal CalcularCostoPromedioConPendientes(
        decimal stockConfirmado,
        decimal costoPromedioConfirmado,
        IEnumerable<CostoInventarioEntrada> entradasPendientes)
    {
        var pendientes = entradasPendientes.ToList();
        var cantidadPendiente = pendientes.Sum(x => x.CantidadBase);
        var cantidadTotal = stockConfirmado + cantidadPendiente;
        return cantidadTotal == 0
            ? 0
            : ((stockConfirmado * costoPromedioConfirmado) +
               pendientes.Sum(x => x.CostoTotal)) / cantidadTotal;
    }
}
