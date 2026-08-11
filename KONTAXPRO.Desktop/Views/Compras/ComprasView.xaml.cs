using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using KONTAXPRO.Application.Models.Compras;
using KONTAXPRO.Desktop.ViewModels.Compras;
using Microsoft.Win32;

namespace KONTAXPRO.Desktop.Views.Compras;

public partial class ComprasView : UserControl
{
    private ComprasViewModel? _subscribed;

    public ComprasView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ComprasViewModel viewModel) return;
        if (!ReferenceEquals(_subscribed, viewModel))
        {
            Unsubscribe();
            _subscribed = viewModel;
            viewModel.SelectXmlRequested += SelectXmlAsync;
            viewModel.FocusDueDateRequested += FocusDueDate;
            viewModel.FocusManualDueDateRequested += FocusManualDueDate;
            viewModel.FocusManualSupplierSearchRequested +=
                FocusManualSupplierSearch;
            viewModel.ProveedorForm.FocusRucRequested += FocusManualSupplierRuc;
            viewModel.ProveedorForm.FocusBusinessNameRequested +=
                FocusManualSupplierBusinessName;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
        await viewModel.InitializeAsync();
    }

    private void OnDataContextChanged(object sender,
        DependencyPropertyChangedEventArgs e)
    {
        Unsubscribe();
        if (e.NewValue is not ComprasViewModel viewModel) return;
        _subscribed = viewModel;
        viewModel.SelectXmlRequested += SelectXmlAsync;
        viewModel.FocusDueDateRequested += FocusDueDate;
        viewModel.FocusManualDueDateRequested += FocusManualDueDate;
        viewModel.FocusManualSupplierSearchRequested +=
            FocusManualSupplierSearch;
        viewModel.ProveedorForm.FocusRucRequested += FocusManualSupplierRuc;
        viewModel.ProveedorForm.FocusBusinessNameRequested +=
            FocusManualSupplierBusinessName;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private async Task SelectXmlAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar factura electrónica",
            Filter = "Archivos XML (*.xml)|*.xml",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog() == true && _subscribed is not null)
            await _subscribed.ImportXmlAsync(dialog.FileName);
    }

    private void ReviewProductsScroll_PreviewMouseWheel(
        object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer || e.Delta == 0)
            return;

        const double cardStep = 56d;
        var direction = e.Delta > 0 ? -1d : 1d;
        var detents = Math.Max(1d, Math.Abs(e.Delta) / 120d);
        var target = Math.Clamp(
            scrollViewer.VerticalOffset + direction * cardStep * detents,
            0d,
            scrollViewer.ScrollableHeight);
        scrollViewer.ScrollToVerticalOffset(target);
        e.Handled = true;
    }

    private void FocusDueDate() => Dispatcher.BeginInvoke(() =>
    {
        DueDateInput.Focus();
        Keyboard.Focus(DueDateInput);
        DueDateInput.SelectAll();
    });

    private void FocusManualDueDate() => Dispatcher.BeginInvoke(() =>
    {
        ManualDueDateInput.Focus();
        Keyboard.Focus(ManualDueDateInput);
        ManualDueDateInput.SelectAll();
    });

    private void FocusManualSupplierSearch() => Dispatcher.BeginInvoke(() =>
    {
        ManualSupplierSearchInput.Focus();
        Keyboard.Focus(ManualSupplierSearchInput);
        ManualSupplierSearchInput.SelectAll();
    });

    private void FocusManualSupplierRuc()
    {
        if (_subscribed?.IsManualSupplierFormOpen != true) return;
        Dispatcher.BeginInvoke(() =>
        {
            ManualSupplierRucInput.Focus();
            Keyboard.Focus(ManualSupplierRucInput);
            ManualSupplierRucInput.CaretIndex =
                ManualSupplierRucInput.Text.Length;
        });
    }

    private void FocusManualSupplierBusinessName()
    {
        if (_subscribed?.IsManualSupplierFormOpen != true) return;
        Dispatcher.BeginInvoke(() =>
        {
            ManualSupplierBusinessNameInput.Focus();
            Keyboard.Focus(ManualSupplierBusinessNameInput);
            ManualSupplierBusinessNameInput.SelectAll();
        });
    }

    private void DigitsOnly_PreviewTextInput(
        object sender, TextCompositionEventArgs e) =>
        e.Handled = e.Text.Any(character => !char.IsDigit(character));

    private void ManualDocumentSegment_LostFocus(
        object sender, RoutedEventArgs e) =>
        _subscribed?.PadManualDocumentSegments();

    private void ManualSupplierSuggestion_SelectionChanged(
        object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0 ||
            e.AddedItems[0] is not CompraProveedorItemDto supplier ||
            _subscribed is null)
            return;
        _subscribed.SelectManualSupplierCommand.Execute(supplier);
        if (sender is ListBox list)
            list.SelectedItem = null;
    }

    private void ManualDescriptionSuggestion_SelectionChanged(
        object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox
            {
                DataContext: CompraManualLineViewModel line
            } list || e.AddedItems.Count == 0 ||
            e.AddedItems[0] is not CompraPresentacionItemDto suggestion)
            return;
        line.SelectDescriptionSuggestionCommand.Execute(suggestion);
        list.SelectedItem = null;
    }

    private void ManualProductSuggestion_SelectionChanged(
        object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox
            {
                DataContext: CompraManualLineViewModel line
            } list || e.AddedItems.Count == 0 ||
            e.AddedItems[0] is not CompraPresentacionItemDto suggestion)
            return;
        line.SelectProductSuggestionCommand.Execute(suggestion);
        list.SelectedItem = null;
    }

    private void ManualLinesGrid_PreviewMouseWheel(
        object sender, MouseWheelEventArgs e)
    {
        var internalScroll = FindVisualChild<ScrollViewer>(ManualLinesGrid);
        if (internalScroll is null) return;
        var atTop = internalScroll.VerticalOffset <= 0.5;
        var atBottom = internalScroll.VerticalOffset >=
            internalScroll.ScrollableHeight - 0.5;
        if ((e.Delta > 0 && !atTop) || (e.Delta < 0 && !atBottom))
            return;

        const double step = 52d;
        var detents = Math.Max(1d, Math.Abs(e.Delta) / 120d);
        var direction = e.Delta > 0 ? -1d : 1d;
        ManualPurchaseScrollViewer.ScrollToVerticalOffset(Math.Clamp(
            ManualPurchaseScrollViewer.VerticalOffset +
            direction * step * detents,
            0d,
            ManualPurchaseScrollViewer.ScrollableHeight));
        e.Handled = true;
    }

    private void OnViewModelPropertyChanged(
        object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(
                ComprasViewModel.IsTraceabilityEditorOpen) ||
            _subscribed?.IsTraceabilityEditorOpen != true)
            return;

        FocusFirstTraceabilityLot();
    }

    private void FocusFirstTraceabilityLot()
    {
        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.ApplicationIdle,
            () =>
            {
                var lot = _subscribed?.TraceabilityLots.FirstOrDefault();
                if (lot is null)
                    return;

                TraceabilityLotsGrid.SelectedItem = lot;
                TraceabilityLotsGrid.ScrollIntoView(
                    lot, TraceabilityLotNumberColumn);
                TraceabilityLotsGrid.UpdateLayout();
                var content = TraceabilityLotNumberColumn
                    .GetCellContent(lot);
                var input = FindVisualChild<TextBox>(
                    content, "ReceiptLotInput");
                if (input is null)
                    return;

                input.BringIntoView();
                input.Focus();
                Keyboard.Focus(input);
                input.SelectAll();
            });
    }

    private void TraceabilityLotSuggestion_SelectionChanged(
        object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0 ||
            sender is not ListBox
            {
                DataContext: CompraReceiptLotEditorViewModel lot
            })
            return;

        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.ApplicationIdle,
            () =>
            {
                lot.ShowSuggestions = false;
                TraceabilityLotsGrid.SelectedItem = lot;
                TraceabilityLotsGrid.ScrollIntoView(
                    lot, TraceabilityLotQuantityColumn);
                TraceabilityLotsGrid.UpdateLayout();

                var content =
                    TraceabilityLotQuantityColumn.GetCellContent(lot);
                var input = FindVisualChild<TextBox>(
                    content, "TraceabilityLotQuantityInput");
                if (input is null)
                    return;

                input.BringIntoView();
                input.Focus();
                Keyboard.Focus(input);
                input.SelectAll();
            });
    }

    private static T? FindVisualChild<T>(
        DependencyObject? parent, string? name = null)
        where T : FrameworkElement
    {
        if (parent is null)
            return null;

        if (parent is T current &&
            (name is null || current.Name == name))
            return current;

        for (var index = 0;
             index < System.Windows.Media.VisualTreeHelper
                 .GetChildrenCount(parent);
             index++)
        {
            var child = System.Windows.Media.VisualTreeHelper
                .GetChild(parent, index);
            if (child is T candidate &&
                (name is null || candidate.Name == name))
                return candidate;

            var nested = FindVisualChild<T>(child, name);
            if (nested is not null)
                return nested;
        }

        return null;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => Unsubscribe();

    private void Unsubscribe()
    {
        if (_subscribed is null) return;
        _subscribed.SelectXmlRequested -= SelectXmlAsync;
        _subscribed.FocusDueDateRequested -= FocusDueDate;
        _subscribed.FocusManualDueDateRequested -= FocusManualDueDate;
        _subscribed.FocusManualSupplierSearchRequested -=
            FocusManualSupplierSearch;
        _subscribed.ProveedorForm.FocusRucRequested -= FocusManualSupplierRuc;
        _subscribed.ProveedorForm.FocusBusinessNameRequested -=
            FocusManualSupplierBusinessName;
        _subscribed.PropertyChanged -= OnViewModelPropertyChanged;
        _subscribed = null;
    }
}
