using System.Windows;
using System.Windows.Controls;
using KONTAXPRO.Desktop.ViewModels.Products;

namespace KONTAXPRO.Desktop.Views.Products;

public partial class ProductsView : UserControl
{
    private bool _initialized;

    public ProductsView()
    {
        InitializeComponent();

        Loaded += ProductsView_Loaded;
    }

    private async void ProductsView_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (_initialized)
        {
            return;
        }

        if (DataContext is not ProductsViewModel viewModel)
        {
            return;
        }

        _initialized = true;

        await viewModel.InitializeAsync();
    }
}