using KONTAXPRO.Desktop.ViewModels;
using System.Windows;

namespace KONTAXPRO.Desktop.Views;

public partial class SeleccionarEmpresaWindow : Window
{
    private readonly SeleccionarEmpresaViewModel _viewModel;

    public SeleccionarEmpresaWindow(
        SeleccionarEmpresaViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.EmpresaSeleccionada +=
            OnEmpresaSeleccionada;

        Loaded +=
            SeleccionarEmpresaWindow_Loaded;

        Closed +=
            SeleccionarEmpresaWindow_Closed;
    }

    private async void SeleccionarEmpresaWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await _viewModel.CargarEmpresasAsync();
    }

    private void OnEmpresaSeleccionada()
    {
        DialogResult = true;
        Close();
    }

    private void Cerrar_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void SeleccionarEmpresaWindow_Closed(
        object? sender,
        System.EventArgs e)
    {
        _viewModel.EmpresaSeleccionada -=
            OnEmpresaSeleccionada;

        Loaded -=
            SeleccionarEmpresaWindow_Loaded;

        Closed -=
            SeleccionarEmpresaWindow_Closed;
    }
}