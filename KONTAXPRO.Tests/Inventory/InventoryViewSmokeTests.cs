using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KONTAXPRO.Application.Models.Inventario;
using KONTAXPRO.Desktop;
using KONTAXPRO.Desktop.ViewModels.Inventory;
using KONTAXPRO.Desktop.Views.Inventory;
using Xunit;

namespace KONTAXPRO.Tests.Inventory;

public sealed class InventoryViewSmokeTests
{
    [Theory]
    [InlineData("FRASCO", 1, "FRASCO X1")]
    [InlineData("CAJA X6", 6, "CAJA X6")]
    [InlineData("CAJA ×12", 12, "CAJA ×12")]
    public void DetailPresentationLabel_DoesNotDuplicateFactor(
        string name, decimal factor, string expected)
    {
        var presentation = new InventarioPresentacionDto
        {
            Nombre = name,
            Factor = factor
        };

        Assert.Equal(expected, presentation.EtiquetaInventario);
    }

    [Fact]
    public void ProductIdentityColumn_CanBeMeasuredWithPresentations()
    {
        var failure = WpfTestHost.Run(() =>
        {
                var app = System.Windows.Application.Current!;
                var globalGrid = new DataGrid
                {
                    Style = Assert.IsType<Style>(
                        app.FindResource("KontaxDataGridStyle"))
                };
                Assert.True(ScrollViewer.GetCanContentScroll(globalGrid));
                Assert.True(VirtualizingPanel.GetIsVirtualizing(globalGrid));
                Assert.Equal(VirtualizationMode.Recycling,
                    VirtualizingPanel.GetVirtualizationMode(globalGrid));
                Assert.Equal(ScrollUnit.Pixel,
                    VirtualizingPanel.GetScrollUnit(globalGrid));

                var view = new InventoryView
                {
                    Width = 1280,
                    Height = 820,
                    DataContext = new InventorySmokeModel()
                };
                view.Measure(new Size(1280, 820));
                view.Arrange(new Rect(0, 0, 1280, 820));
                view.UpdateLayout();

                var inventoryGrid = Assert.IsType<DataGrid>(
                    view.FindName("InventoryMainGrid"));
                Assert.True(ScrollViewer.GetCanContentScroll(inventoryGrid));
                Assert.True(VirtualizingPanel.GetIsVirtualizing(inventoryGrid));
                Assert.Equal(VirtualizationMode.Recycling,
                    VirtualizingPanel.GetVirtualizationMode(inventoryGrid));
                Assert.Equal(ScrollUnit.Pixel,
                    VirtualizingPanel.GetScrollUnit(inventoryGrid));
                Assert.False(inventoryGrid.CanUserSortColumns);
                Assert.All(inventoryGrid.Columns.Take(6), column =>
                    Assert.IsType<Button>(column.Header));
                Assert.Equal("ACCIONES", inventoryGrid.Columns[6].Header);

                var warehouseFilter = Assert.IsType<ComboBox>(
                    view.FindName("InventoryWarehouseFilter"));
                Assert.Equal(3, warehouseFilter.Items.Count);
                var globalWarehouse = Assert.IsType<InventarioOpcionDto>(
                    warehouseFilter.SelectedItem);
                Assert.Equal(0, globalWarehouse.Id);
                Assert.Equal("TODAS LAS BODEGAS", globalWarehouse.Display);

                var warehousesGrid = Assert.IsType<DataGrid>(
                    view.FindName("InventoryWarehousesGrid"));
                Assert.True(ScrollViewer.GetCanContentScroll(warehousesGrid));
                Assert.True(VirtualizingPanel.GetIsVirtualizing(warehousesGrid));
                Assert.Equal(VirtualizationMode.Recycling,
                    VirtualizingPanel.GetVirtualizationMode(warehousesGrid));
                Assert.Equal(ScrollUnit.Pixel,
                    VirtualizingPanel.GetScrollUnit(warehousesGrid));

                var kardex = new InventoryKardexPanel
                {
                    Width = 1280,
                    Height = 820,
                    DataContext = new InventorySmokeModel()
                };
                kardex.Measure(new Size(1280, 820));
                kardex.Arrange(new Rect(0, 0, 1280, 820));
                kardex.UpdateLayout();

                var kardexGrid = Assert.IsType<DataGrid>(
                    kardex.FindName("KardexGrid"));
                Assert.True(ScrollViewer.GetCanContentScroll(kardexGrid));
                Assert.True(VirtualizingPanel.GetIsVirtualizing(kardexGrid));
                Assert.Equal(VirtualizationMode.Recycling,
                    VirtualizingPanel.GetVirtualizationMode(kardexGrid));
                Assert.Equal(ScrollUnit.Pixel,
                    VirtualizingPanel.GetScrollUnit(kardexGrid));

                var dateInput = Assert.IsType<TextBox>(
                    kardex.FindName("KardexFromInput"));
                foreach (var digit in "13082026")
                {
                    var composition = new TextComposition(
                        InputManager.Current, dateInput, digit.ToString());
                    dateInput.RaiseEvent(new TextCompositionEventArgs(
                        Keyboard.PrimaryDevice, composition)
                    {
                        RoutedEvent = TextCompositionManager.PreviewTextInputEvent
                    });
                }
                Assert.Equal("13/08/2026", dateInput.Text);
        });
        Assert.Null(failure);
    }

    private sealed class InventorySmokeModel
    {
        public ObservableCollection<InventarioOpcionDto> WarehouseFilters { get; }
            =
            [
                new() { Id = 0, Nombre = "TODAS LAS BODEGAS" },
                new() { Id = 1, Codigo = "001", Nombre = "PRINCIPAL" },
                new() { Id = 2, Codigo = "002", Nombre = "SECUNDARIA" }
            ];
        public InventarioOpcionDto SelectedWarehouse { get; set; }

        public ObservableCollection<InventarioItemDto> Items { get; } =
        [
            new()
            {
                ProductoId = 1,
                Producto = "PRODUCTO DEMO",
                Marca = "MARCA",
                Categoria = "CATEGORÍA",
                Modelo = "MODELO",
                TipoControl = "LOTE",
                StockActual = 24,
                Presentaciones =
                [
                    Presentation("UNIDAD", 1, true, true),
                    Presentation("CAJA", 6, false, true),
                    Presentation("CAJA", 12, false, true),
                    Presentation("BULTO", 24, false, false)
                ]
            }
        ];

        public bool IsLoading => false;
        public bool CanViewCosts => true;
        public bool HasItems => true;
        public bool IsDetailOpen => true;
        public InventarioItemDto? SelectedItem { get; set; }
        public int SelectedDetailTabIndex { get; set; }
        public bool IsKardexLoading => false;
        public string KardexFromText { get; set; } = string.Empty;
        public string KardexToText { get; set; } = string.Empty;
        public bool HasKardexItems => true;
        public bool HasKardexFilters => false;
        public bool IsKardexAll => true;
        public bool IsKardexEntry => false;
        public bool IsKardexExit => false;
        public string KardexWarehouseText => "001 · BODEGA PRINCIPAL";
        public string KardexPaginationText => "Mostrando 1 de 1 movimiento";
        public string KardexEmptyTitle => "Este producto aún no tiene movimientos";
        public string KardexEmptyDescription => "Los movimientos aparecerán aquí.";
        public IReadOnlyList<int> PageSizes { get; } = [25, 50, 100];
        public int KardexPageSize { get; set; } = 25;
        public ObservableCollection<InventoryPageItem> KardexVisiblePages { get; } =
        [new(1, true)];
        public ObservableCollection<KardexItemDto> KardexItems { get; } =
        [Movement()];
        public KardexItemDto SelectedKardexItem { get; set; } = Movement();
        public InventarioProductoDetalleDto SelectedDetail { get; } = new()
        {
            ProductoId = 1,
            Producto = "PRODUCTO DEMO",
            Marca = "MARCA",
            Categoria = "CATEGORÍA",
            Modelo = "MODELO",
            TipoControl = "LOTE Y SERIE",
            StockActual = 24,
            StockReservado = 2,
            CostoPromedio = 1.25m,
            Presentaciones =
            [
                new() { Nombre = "UNIDAD", Factor = 1, EsBase = true },
                new() { Nombre = "CAJA", Factor = 12 }
            ],
            Bodegas =
            [
                new()
                {
                    BodegaId = 1, Bodega = "BODEGA PRINCIPAL",
                    StockActual = 24, StockReservado = 2, StockMinimo = 5
                }
            ],
            Lotes =
            [
                new()
                {
                    LoteId = 1, BodegaId = 1, Numero = "LOTE-001",
                    Bodega = "BODEGA PRINCIPAL", StockActual = 24,
                    StockReservado = 2, Caducidad = DateOnly.FromDateTime(
                        DateTime.Today.AddMonths(6))
                }
            ],
            Series =
            [
                new()
                {
                    SerieId = 1, BodegaId = 1, Numero = "SERIE-001",
                    Bodega = "BODEGA PRINCIPAL", Lote = "LOTE-001",
                    Estado = "DISPONIBLE"
                }
            ],
            MovimientosRecientes =
            [
                new()
                {
                    MovimientoId = 1, ProductoId = 1,
                    Producto = "PRODUCTO DEMO", Fecha = DateTime.Now,
                    Tipo = "COMPRA", Bodega = "BODEGA PRINCIPAL",
                    EntradaBase = 24, StockNuevo = 24
                }
            ]
        };

        public InventorySmokeModel()
        {
            SelectedWarehouse = WarehouseFilters[0];
            SelectedItem = Items[0];
            SelectedKardexItem = KardexItems[0];
        }

        private static InventarioPresentacionResumenDto Presentation(
            string name, decimal factor, bool isBase, bool separator) => new()
            {
                Nombre = name,
                Factor = factor,
                EsBase = isBase,
                MostrarSeparador = separator
            };
        }

        private static KardexItemDto Movement() => new()
        {
            MovimientoId = 1,
            ProductoId = 1,
            Producto = "PRODUCTO DEMO",
            Fecha = DateTime.Now,
            NumeroMovimiento = "MOV-001",
            Tipo = "COMPRA",
            Origen = "COMPRA",
            Documento = "001-001-000000001",
            Bodega = "BODEGA PRINCIPAL",
            Presentacion = "CAJA X12",
            CantidadPresentacion = 2,
            Factor = 12,
            EntradaBase = 24,
            StockAnterior = 0,
            StockNuevo = 24,
            CostoUnitario = 1.25m,
            CostoTotal = 30,
            CostoPromedioAnterior = 0,
            CostoPromedioNuevo = 1.25m,
            Usuario = "USUARIO DEMO",
            Observacion = "MOVIMIENTO DE PRUEBA"
        };
}
