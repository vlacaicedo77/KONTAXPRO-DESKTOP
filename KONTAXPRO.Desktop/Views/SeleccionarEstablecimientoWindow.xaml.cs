using System.Windows;
using KONTAXPRO.Desktop.ViewModels;

namespace KONTAXPRO.Desktop.Views;

public partial class SeleccionarEstablecimientoWindow : Window
{
    private readonly SeleccionarEstablecimientoViewModel _viewModel;

    public SeleccionarEstablecimientoWindow(
        SeleccionarEstablecimientoViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += OnLoaded;
        Closed += OnClosed;
        viewModel.EstablecimientoSeleccionado += OnSelected;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.CargarAsync();

    private void OnSelected()
    {
        DialogResult = true;
        Close();
    }

    private void Cerrar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Loaded -= OnLoaded;
        Closed -= OnClosed;
        _viewModel.EstablecimientoSeleccionado -= OnSelected;
    }
}
