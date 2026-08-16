using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Globalization;
using System.Text.RegularExpressions;
using KONTAXPRO.Application.Models.Productos;

namespace KONTAXPRO.Desktop.Views.Products;

public partial class ProductFormView : UserControl
{
    private bool _presentationNameFocusPending;
    private bool _lotNumberFocusPending;
    private bool _serialNumberFocusPending;
    private global::KONTAXPRO.Desktop.ViewModels.Products.ConversionBodegaEditorViewModel?
        _conversionLotFocusBodega;
    private global::KONTAXPRO.Desktop.ViewModels.Products.ConversionBodegaEditorViewModel?
        _conversionSerialFocusBodega;

    public ProductFormView()
    {
        InitializeComponent();
        Loaded += ProductFormView_Loaded;
        DataContextChanged += ProductFormView_DataContextChanged;
    }

    private void ProductFormView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        ScrollToTop();
    }

    private void ProductFormView_DataContextChanged(
        object sender,
        System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is global::KONTAXPRO.Desktop.ViewModels.Products.ProductFormViewModel oldViewModel)
        {
            oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            oldViewModel.PresentationAdded -= ViewModel_PresentationAdded;
            oldViewModel.InitialFocusRequested -= ViewModel_InitialFocusRequested;
            oldViewModel.InventoryOperationFocusRequested -=
                ViewModel_InventoryOperationFocusRequested;
        }

        if (e.NewValue is global::KONTAXPRO.Desktop.ViewModels.Products.ProductFormViewModel newViewModel)
        {
            newViewModel.PropertyChanged += ViewModel_PropertyChanged;
            newViewModel.PresentationAdded += ViewModel_PresentationAdded;
            newViewModel.InitialFocusRequested += ViewModel_InitialFocusRequested;
            newViewModel.InventoryOperationFocusRequested +=
                ViewModel_InventoryOperationFocusRequested;
        }

        ActualizarColumnasInventario();
    }

    private void ViewModel_InventoryOperationFocusRequested()
    {
        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Loaded,
            () => InventoryOperationsSection.BringIntoView());
    }

    private void ViewModel_InitialFocusRequested()
    {
        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Input,
            () =>
            {
                ScrollToTop();
                NombreProductoInput.Focus();
                Keyboard.Focus(NombreProductoInput);
                NombreProductoInput.CaretIndex =
                    NombreProductoInput.Text.Length;
            });
    }

    private void ViewModel_PresentationAdded()
    {
        _presentationNameFocusPending = true;
        FocusLastPresentationName();
    }

    private void AgregarPresentacionButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _presentationNameFocusPending = true;
        FocusLastPresentationName();
    }

    private void PresentacionAdicionalNombre_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (!_presentationNameFocusPending ||
            sender is not TextBox input ||
            !ReferenceEquals(
                input.DataContext,
                PresentacionesAdicionalesGrid.Items
                    .Cast<object>()
                    .LastOrDefault()))
            return;

        FocusPresentationNameInput(input);
    }

    private void FocusLastPresentationName()
    {
        Dispatcher.BeginInvoke(() =>
        {
            var item = PresentacionesAdicionalesGrid.Items
                .Cast<object>()
                .LastOrDefault();
            if (item is null)
                return;

            PresentacionesAdicionalesGrid.SelectedItem = item;
            PresentacionesAdicionalesGrid.ScrollIntoView(
                item,
                NombrePresentacionAdicionalColumn);
            PresentacionesAdicionalesGrid.UpdateLayout();

            Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.ContextIdle,
                () =>
            {
                PresentacionesAdicionalesGrid.ScrollIntoView(
                    item,
                    NombrePresentacionAdicionalColumn);
                PresentacionesAdicionalesGrid.UpdateLayout();

                if (NombrePresentacionAdicionalColumn.GetCellContent(item)
                    is not TextBox input)
                    return;

                FocusPresentationNameInput(input);
            });
        });
    }

    private void FocusPresentationNameInput(TextBox input)
    {
        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Input,
            () =>
            {
                input.BringIntoView();
                input.Focus();
                Keyboard.Focus(input);
                input.CaretIndex = input.Text.Length;
                _presentationNameFocusPending = false;
            });
    }

    private void AgregarLoteButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _lotNumberFocusPending = true;
        FocusLastLotNumber();
    }

    private void NumeroLoteInicial_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (!_lotNumberFocusPending ||
            sender is not TextBox input ||
            !ReferenceEquals(
                input.DataContext,
                LotesInventarioInicialGrid.Items
                    .Cast<object>()
                    .LastOrDefault()))
            return;

        FocusLotNumberInput(input);
    }

    private void FocusLastLotNumber()
    {
        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.ContextIdle,
            () =>
            {
                var item = LotesInventarioInicialGrid.Items
                    .Cast<object>()
                    .LastOrDefault();
                if (item is null)
                    return;

                LotesInventarioInicialGrid.SelectedItem = item;
                LotesInventarioInicialGrid.ScrollIntoView(
                    item,
                    NumeroLoteInicialColumn);
                LotesInventarioInicialGrid.UpdateLayout();

                if (NumeroLoteInicialColumn.GetCellContent(item)
                    is TextBox input)
                    FocusLotNumberInput(input);
                else if (NumeroLoteInicialColumn.GetCellContent(item)
                         is Border border &&
                         border.Child is TextBox borderedInput)
                    FocusLotNumberInput(borderedInput);
            });
    }

    private void FocusLotNumberInput(TextBox input)
    {
        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Input,
            () =>
            {
                input.BringIntoView();
                input.Focus();
                Keyboard.Focus(input);
                input.CaretIndex = input.Text.Length;
                _lotNumberFocusPending = false;
            });
    }

    private void AgregarSerieButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _serialNumberFocusPending = true;
        FocusLastSerialNumber();
    }

    private void AgregarLoteConversionButton_Click(
        object sender, RoutedEventArgs e)
    {
        if (sender is Button
            {
                DataContext: global::KONTAXPRO.Desktop.ViewModels.Products
                    .ConversionBodegaEditorViewModel bodega
            })
            _conversionLotFocusBodega = bodega;
    }

    private void NumeroLoteConversion_Loaded(
        object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox input ||
            _conversionLotFocusBodega is not { } bodega ||
            !ReferenceEquals(input.DataContext, bodega.Lotes.LastOrDefault()))
            return;

        _conversionLotFocusBodega = null;
        FocusConversionInput(input);
    }

    private void AgregarSerieConversionButton_Click(
        object sender, RoutedEventArgs e)
    {
        if (sender is Button
            {
                DataContext: global::KONTAXPRO.Desktop.ViewModels.Products
                    .ConversionBodegaEditorViewModel bodega
            })
            _conversionSerialFocusBodega = bodega;
    }

    private void NumeroSerieConversion_Loaded(
        object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox input ||
            _conversionSerialFocusBodega is not { } bodega ||
            !ReferenceEquals(input.DataContext, bodega.Series.LastOrDefault()))
            return;

        _conversionSerialFocusBodega = null;
        FocusConversionInput(input);
    }

    private void FocusConversionInput(TextBox input)
    {
        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Input,
            () =>
            {
                input.BringIntoView();
                input.Focus();
                Keyboard.Focus(input);
                input.SelectAll();
            });
    }

    private void ZeroNumericTextBox_GotKeyboardFocus(
        object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not TextBox input)
            return;
        if (decimal.TryParse(input.Text, NumberStyles.Number,
                CultureInfo.CurrentCulture, out var value) && value == 0)
            input.Clear();
        else
            input.SelectAll();
    }

    private void ZeroNumericTextBox_LostKeyboardFocus(
        object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox input && string.IsNullOrWhiteSpace(input.Text))
        {
            input.Text = "0";
            input.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        }
    }

    private void PrecioListaMetodo_SelectionChanged(
        object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox comboBox ||
            !comboBox.IsKeyboardFocusWithin ||
            e.RemovedItems.Count == 0 ||
            comboBox.Parent is not DependencyObject priceEditor)
            return;

        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Input,
            () =>
            {
                var input = FindVisualChildren<TextBox>(priceEditor)
                    .FirstOrDefault(x =>
                        x.IsVisible &&
                        x.Name is "PrecioListaPorcentajeInput" or
                            "PrecioListaPrecioInput");
                if (input is null)
                    return;

                input.BringIntoView();
                input.Focus();
                Keyboard.Focus(input);
                input.SelectAll();
            });
    }

    private void AjusteLoteEntradaInput_LostKeyboardFocus(
        object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not TextBox
            {
                DataContext: global::KONTAXPRO.Desktop.ViewModels.Products
                    .AjusteLoteEditorViewModel lote
            })
            return;

        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.ContextIdle,
            () => lote.MostrarSugerencias = false);
    }

    private void AjusteLoteSugerencia_SelectionChanged(
        object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0 ||
            sender is not ListBox
            {
                DataContext: global::KONTAXPRO.Desktop.ViewModels.Products
                    .AjusteLoteEditorViewModel lote
            })
            return;

        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.ApplicationIdle,
            () =>
            {
                lote.MostrarSugerencias = false;
                AjusteLotesGrid.SelectedItem = lote;
                AjusteLotesGrid.ScrollIntoView(lote, AjusteLoteCantidadColumn);
                AjusteLotesGrid.UpdateLayout();

                var content = AjusteLoteCantidadColumn.GetCellContent(lote);
                var input = content as TextBox ??
                    (content is null
                        ? null
                        : FindVisualChildren<TextBox>(content).FirstOrDefault(x =>
                            x.Name == "AjusteLoteCantidadInput"));
                if (input is null)
                    return;

                input.BringIntoView();
                input.Focusable = true;
                input.Focus();
                Keyboard.Focus(input);
                input.SelectAll();
            });
    }

    private void AgregarLoteAjusteButton_Click(
        object sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.ContextIdle,
            () =>
            {
                var item = AjusteLotesGrid.Items.Cast<object>().LastOrDefault();
                if (item is null)
                    return;

                AjusteLotesGrid.SelectedItem = item;
                AjusteLotesGrid.ScrollIntoView(item, AjusteLoteNumeroColumn);
                AjusteLotesGrid.UpdateLayout();

                var content = AjusteLoteNumeroColumn.GetCellContent(item);
                var input = content as TextBox ??
                    (content is null
                        ? null
                        : FindVisualChildren<TextBox>(content).FirstOrDefault(x =>
                            x.Name == "AjusteLoteEntradaInput"));
                if (input is null)
                    return;

                input.BringIntoView();
                input.Focus();
                Keyboard.Focus(input);
                input.CaretIndex = input.Text.Length;
            });
    }

    private void AgregarSerieAjusteButton_Click(
        object sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.ContextIdle,
            () =>
            {
                var item = AjusteSeriesNuevasGrid.Items
                    .Cast<object>()
                    .LastOrDefault();
                if (item is null)
                    return;

                AjusteSeriesNuevasGrid.SelectedItem = item;
                AjusteSeriesNuevasGrid.ScrollIntoView(
                    item, AjusteSerieNumeroColumn);
                AjusteSeriesNuevasGrid.UpdateLayout();

                var content = AjusteSerieNumeroColumn.GetCellContent(item);
                var input = content as TextBox ??
                    (content is null
                        ? null
                        : FindVisualChildren<TextBox>(content).FirstOrDefault());
                if (input is null)
                    return;

                input.BringIntoView();
                input.Focus();
                Keyboard.Focus(input);
                input.CaretIndex = input.Text.Length;
            });
    }

    private void NumeroSerieInicial_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (!_serialNumberFocusPending ||
            sender is not TextBox input ||
            !ReferenceEquals(
                input.DataContext,
                SeriesInventarioInicialGrid.Items
                    .Cast<object>()
                    .LastOrDefault()))
            return;

        FocusSerialNumberInput(input);
    }

    private void FocusLastSerialNumber()
    {
        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.ContextIdle,
            () =>
            {
                var item = SeriesInventarioInicialGrid.Items
                    .Cast<object>()
                    .LastOrDefault();
                if (item is null)
                    return;

                SeriesInventarioInicialGrid.SelectedItem = item;
                SeriesInventarioInicialGrid.ScrollIntoView(
                    item,
                    NumeroSerieInicialColumn);
                SeriesInventarioInicialGrid.UpdateLayout();

                if (NumeroSerieInicialColumn.GetCellContent(item)
                    is TextBox input)
                    FocusSerialNumberInput(input);
            });
    }

    private void FocusSerialNumberInput(TextBox input)
    {
        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Input,
            () =>
            {
                input.BringIntoView();
                input.Focus();
                Keyboard.Focus(input);
                input.CaretIndex = input.Text.Length;
                _serialNumberFocusPending = false;
            });
    }

    private void PresentacionAdicionalNombre_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (sender is not TextBox
            {
                DataContext: ProductoPresentacionDto presentacion
            })
            return;

        var matches = Regex.Matches(
            presentacion.Nombre ?? string.Empty,
            @"X\s*(\d+(?:[.,]\d{1,6})?)",
            RegexOptions.IgnoreCase);
        if (matches.Count > 0 &&
            decimal.TryParse(
                matches[^1].Groups[1].Value.Replace(',', '.'),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var factor) &&
            factor > 0)
        {
            presentacion.FactorConversion = factor;
            if (FactorPresentacionAdicionalColumn.GetCellContent(presentacion)
                is TextBlock factorText)
            {
                factorText.GetBindingExpression(TextBlock.TextProperty)
                    ?.UpdateTarget();
            }
        }

        if (DataContext is
            global::KONTAXPRO.Desktop.ViewModels.Products.ProductFormViewModel
            viewModel)
            viewModel.SincronizarPresentacionAdicional(presentacion);
    }

    private void PresentacionesAdicionalesGrid_CellEditEnding(
        object sender,
        DataGridCellEditEndingEventArgs e)
    {
        if (e.Row.Item is not ProductoPresentacionDto presentacion)
            return;

        Dispatcher.BeginInvoke(() =>
        {
            if (DataContext is
                global::KONTAXPRO.Desktop.ViewModels.Products.ProductFormViewModel
                viewModel)
                viewModel.SincronizarPresentacionAdicional(presentacion);
        });
    }

    private void ViewModel_PropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(
                global::KONTAXPRO.Desktop.ViewModels.Products.ProductFormViewModel.TipoControlInventario)
            or nameof(
                global::KONTAXPRO.Desktop.ViewModels.Products.ProductFormViewModel.AlertaCaducidad)
            or nameof(
                global::KONTAXPRO.Desktop.ViewModels.Products.ProductFormViewModel.AjusteManejaFechaCaducidad)
            or nameof(
                global::KONTAXPRO.Desktop.ViewModels.Products.ProductFormViewModel.AjusteTipo))
            ActualizarColumnasInventario();

        if (e.PropertyName is nameof(
                global::KONTAXPRO.Desktop.ViewModels.Products.ProductFormViewModel.ConversionControlCaducidad)
            or nameof(
                global::KONTAXPRO.Desktop.ViewModels.Products.ProductFormViewModel.ConversionTipoNuevo)
            or nameof(
                global::KONTAXPRO.Desktop.ViewModels.Products.ProductFormViewModel.IsConversionOpen))
            Dispatcher.BeginInvoke(() => ActualizarColumnasConversion());

        if (e.PropertyName != nameof(
                global::KONTAXPRO.Desktop.ViewModels.Products.ProductFormViewModel.IsQuickCatalogOpen) ||
            sender is not global::KONTAXPRO.Desktop.ViewModels.Products.ProductFormViewModel
            {
                IsQuickCatalogOpen: true
            })
            return;

        Dispatcher.BeginInvoke(() =>
        {
            QuickCatalogNameInput.Focus();
            Keyboard.Focus(QuickCatalogNameInput);
        });
    }

    private void ScrollToTop()
    {
        Dispatcher.BeginInvoke(() =>
        {
            FormScrollViewer.ScrollToHome();
        });
    }

    private void FormScrollViewer_PreviewMouseWheel(
        object sender,
        MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer outerScrollViewer)
        {
            return;
        }

        if (e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        var innerScrollViewer = FindAncestorScrollViewer(source);
        if (innerScrollViewer is null)
        {
            return;
        }

        var dataGrid = FindAncestorDataGrid(source);
        if (dataGrid is null)
        {
            return;
        }

        var scrollingDown = e.Delta < 0;
        var canScrollDown =
            innerScrollViewer.VerticalOffset < innerScrollViewer.ScrollableHeight;
        var canScrollUp =
            innerScrollViewer.VerticalOffset > 0;

        if (scrollingDown ? canScrollDown : canScrollUp)
        {
            return;
        }

        e.Handled = true;
        outerScrollViewer.ScrollToVerticalOffset(
            Math.Clamp(
                outerScrollViewer.VerticalOffset - e.Delta,
                0,
                outerScrollViewer.ScrollableHeight));
    }

    private void ScrollableDataGrid_PreviewMouseWheel(
        object sender,
        MouseWheelEventArgs e)
    {
        if (sender is not DataGrid dataGrid)
        {
            return;
        }

        var dataGridScrollViewer = FindDescendantScrollViewer(dataGrid);
        if (dataGridScrollViewer is not null)
        {
            var scrollingDown = e.Delta < 0;
            var canScrollDown =
                dataGridScrollViewer.VerticalOffset < dataGridScrollViewer.ScrollableHeight;
            var canScrollUp =
                dataGridScrollViewer.VerticalOffset > 0;

            if ((scrollingDown && canScrollDown) || (!scrollingDown && canScrollUp))
            {
                return;
            }
        }

        e.Handled = true;

        var routedArgs = new MouseWheelEventArgs(
            e.MouseDevice,
            e.Timestamp,
            e.Delta)
        {
            RoutedEvent = UIElement.MouseWheelEvent,
            Source = sender
        };

        FormScrollViewer.RaiseEvent(routedArgs);
    }

    private static ScrollViewer? FindAncestorScrollViewer(
        DependencyObject? current)
    {
        while (current is not null)
        {
            if (current is ScrollViewer scrollViewer)
            {
                return scrollViewer;
            }

            current = GetParent(current);
        }

        return null;
    }

    private static ScrollViewer? FindDescendantScrollViewer(
        DependencyObject? parent)
    {
        if (parent is null)
        {
            return null;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is ScrollViewer scrollViewer)
            {
                return scrollViewer;
            }

            var nested = FindDescendantScrollViewer(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private static DataGrid? FindAncestorDataGrid(DependencyObject? current)
    {
        while (current is not null)
        {
            if (current is DataGrid dataGrid)
            {
                return dataGrid;
            }

            current = GetParent(current);
        }

        return null;
    }

    private static DependencyObject? GetParent(DependencyObject current)
    {
        if (current is Visual ||
            current is System.Windows.Media.Media3D.Visual3D)
        {
            return VisualTreeHelper.GetParent(current);
        }

        if (current is FrameworkContentElement contentElement)
        {
            return contentElement.Parent;
        }

        return LogicalTreeHelper.GetParent(current);
    }

    private void ActualizarColumnasInventario()
    {
        if (DataContext is not
            global::KONTAXPRO.Desktop.ViewModels.Products.ProductFormViewModel
            viewModel)
            return;

        ElaboracionLoteColumn.Visibility =
            viewModel.MostrarColumnaCaducidad
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;
        var visibilidadCaducidad =
            viewModel.MostrarColumnaCaducidad
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;
        CaducidadLoteColumn.Visibility = visibilidadCaducidad;
        ElaboracionCorreccionLoteColumn.Visibility = visibilidadCaducidad;
        CaducidadCorreccionLoteColumn.Visibility = visibilidadCaducidad;
        LoteAsociadoSerieColumn.Visibility =
            viewModel.MostrarLoteAsociadoSeries
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;
        AjusteSerieNuevaLoteColumn.Visibility =
            viewModel.MostrarLoteAsociadoSeries
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;
        var visibilidadCaducidadAjuste =
            viewModel.AjusteManejaFechaCaducidad
                ? Visibility.Visible
                : Visibility.Collapsed;
        AjusteLoteElaboracionColumn.Visibility = visibilidadCaducidadAjuste;
        AjusteLoteCaducidadColumn.Visibility = visibilidadCaducidadAjuste;
        AjusteLoteAccionesColumn.Visibility = viewModel.AjusteEsEntrada
            ? Visibility.Visible
            : Visibility.Collapsed;
        LotesInventarioAgregadoColumn.Visibility =
            viewModel.TipoControlInventario is "LOTE" or "LOTE_Y_SERIE"
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;
        SeriesInventarioAgregadoColumn.Visibility =
            viewModel.TipoControlInventario is "SERIE" or "LOTE_Y_SERIE"
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;
    }

    private void ConversionLotesGrid_Loaded(
        object sender, RoutedEventArgs e)
    {
        if (sender is DataGrid dataGrid)
            ActualizarColumnasConversion(dataGrid);
    }

    private void ActualizarColumnasConversion()
    {
        foreach (var dataGrid in FindVisualChildren<DataGrid>(this)
                     .Where(x => string.Equals(
                         x.Tag?.ToString(), "LOTES_CONVERSION",
                         StringComparison.Ordinal)))
            ActualizarColumnasConversion(dataGrid);
    }

    private void ActualizarColumnasConversion(DataGrid dataGrid)
    {
        if (dataGrid.Columns.Count < 4 || DataContext is not
            global::KONTAXPRO.Desktop.ViewModels.Products.ProductFormViewModel
            viewModel)
            return;

        var visibility = viewModel.MostrarColumnasConversionFecha
            ? Visibility.Visible
            : Visibility.Collapsed;
        dataGrid.Columns[2].Visibility = visibility;
        dataGrid.Columns[3].Visibility = visibility;
    }

    private static IEnumerable<T> FindVisualChildren<T>(
        DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
                yield return match;
            foreach (var nested in FindVisualChildren<T>(child))
                yield return nested;
        }
    }
}
