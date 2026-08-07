using System.Windows;
using System.Windows.Controls;
using KONTAXPRO.Desktop.ViewModels.Clientes;

namespace KONTAXPRO.Desktop.Views.Clientes;

public partial class ClientesView : UserControl
{
    private bool _initialized;

    public ClientesView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized || DataContext is not ClientesViewModel viewModel)
            return;
        _initialized = true;
        await viewModel.InitializeAsync();
    }
}
