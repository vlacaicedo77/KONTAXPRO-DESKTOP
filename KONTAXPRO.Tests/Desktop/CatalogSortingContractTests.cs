using KONTAXPRO.Application.Models.Clientes;
using KONTAXPRO.Application.Models.Inventario;
using KONTAXPRO.Application.Models.Proveedores;
using KONTAXPRO.Application.Models.Tesoreria;
using Xunit;

namespace KONTAXPRO.Tests.Desktop;

public sealed class CatalogSortingContractTests
{
    [Fact]
    public void Catalogs_DefaultToTheirApprovedBusinessOrder()
    {
        Assert.Equal(ClienteCatalogoOrden.RazonSocial,
            new ClienteCatalogoQuery().Orden);
        Assert.False(new ClienteCatalogoQuery().OrdenDescendente);
        Assert.Equal(ProveedorCatalogoOrden.RazonSocial,
            new ProveedorCatalogoQuery().Orden);
        Assert.False(new ProveedorCatalogoQuery().OrdenDescendente);

        var operation = new OperacionSinSustentoCatalogoRequest();
        Assert.Equal(OperacionSinSustentoCatalogoOrden.Fecha,
            operation.Orden);
        Assert.True(operation.OrdenDescendente);
        Assert.True(new KardexPaginadoRequest().FechaDescendente);
    }

    [Theory]
    [InlineData("Clientes", "ClientesView.xaml", "OrdenarClientesCommand")]
    [InlineData("Proveedores", "ProveedoresView.xaml",
        "OrdenarProveedoresCommand")]
    [InlineData("Compras", "ComprasView.xaml", "OrdenarComprasCommand")]
    [InlineData("Tesoreria", "OperacionesSinSustentoView.xaml",
        "SortOperationsCommand")]
    [InlineData("Inventory", "InventoryKardexPanel.xaml",
        "SortKardexByDateCommand")]
    public void ApprovedCatalogs_UseExplicitSortCommands(
        string folder, string file, string command)
    {
        var root = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", ".."));
        var xaml = File.ReadAllText(Path.Combine(root,
            "KONTAXPRO.Desktop", "Views", folder, file));

        Assert.Contains(command, xaml);
        Assert.Contains("CanUserSortColumns=\"False\"", xaml);
    }
}
