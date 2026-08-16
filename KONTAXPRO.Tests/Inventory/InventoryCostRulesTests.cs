using KONTAXPRO.Application.Inventory;

namespace KONTAXPRO.Tests.Inventory;

public sealed class InventoryCostRulesTests
{
    [Fact]
    public void PositiveStock_UsesWeightedAverage()
    {
        var result = InventoryCostRules.CalculateAverageAfterEntry(
            10m, 2m, 5m, 20m);

        Assert.Equal(40m / 15m, result);
    }

    [Fact]
    public void NegativeStock_UsesIncomingUnitCost()
    {
        var result = InventoryCostRules.CalculateAverageAfterEntry(
            -10m, 2m, 5m, 50m);

        Assert.Equal(10m, result);
    }

    [Fact]
    public void NegativeStockCrossingZero_UsesIncomingUnitCost()
    {
        var result = InventoryCostRules.CalculateAverageAfterEntry(
            -10m, 2m, 15m, 150m);

        Assert.Equal(10m, result);
    }
}
