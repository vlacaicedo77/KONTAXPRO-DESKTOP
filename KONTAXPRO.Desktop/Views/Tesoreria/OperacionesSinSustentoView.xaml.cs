using System.Windows;
using System.Windows.Controls;
using KONTAXPRO.Desktop.ViewModels.Tesoreria;

namespace KONTAXPRO.Desktop.Views.Tesoreria;

public partial class OperacionesSinSustentoView : UserControl
{
    public OperacionesSinSustentoView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is OperacionesSinSustentoViewModel viewModel)
            await viewModel.InitializeAsync();
    }
}
