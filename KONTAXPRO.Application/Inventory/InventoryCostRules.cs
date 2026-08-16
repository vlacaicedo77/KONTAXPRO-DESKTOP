namespace KONTAXPRO.Application.Inventory;

public static class InventoryCostRules
{
    public static decimal CalculateAverageAfterEntry(
        decimal stockBefore,
        decimal averageBefore,
        decimal entryQuantity,
        decimal entryTotalCost)
    {
        if (entryQuantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(entryQuantity));
        if (entryTotalCost < 0)
            throw new ArgumentOutOfRangeException(nameof(entryTotalCost));

        var entryUnitCost = entryTotalCost / entryQuantity;
        var stockAfter = stockBefore + entryQuantity;

        // El stock negativo representa unidades vendidas antes de registrar su
        // entrada. No debe valorarse como una existencia positiva dentro del
        // promedio ponderado porque distorsionaría o volvería negativo el costo.
        if (stockBefore <= 0)
            return stockAfter == 0 ? 0 : entryUnitCost;

        return stockAfter == 0
            ? 0
            : ((stockBefore * averageBefore) + entryTotalCost) / stockAfter;
    }
}
