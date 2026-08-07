using System.Windows;
using System.Windows.Controls;
using KONTAXPRO.Desktop.ViewModels.Proveedores;

namespace KONTAXPRO.Desktop.Views.Proveedores;

public partial class ProveedoresView : UserControl
{
    private bool _initialized;

    public ProveedoresView() => InitializeComponent();

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized || DataContext is not ProveedoresViewModel viewModel)
            return;
        _initialized = true;
        await viewModel.InitializeAsync();
    }
}
