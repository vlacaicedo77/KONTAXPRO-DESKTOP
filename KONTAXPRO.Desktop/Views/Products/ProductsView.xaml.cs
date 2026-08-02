using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Input;
using KONTAXPRO.Desktop.ViewModels.Products;

namespace KONTAXPRO.Desktop.Views.Products;

public partial class ProductsView : UserControl
{
    private bool _initialized;

    public ProductsView()
    {
        InitializeComponent();

        Loaded += ProductsView_Loaded;
        DataContextChanged += ProductsView_DataContextChanged;
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

    private void ProductsView_DataContextChanged(
        object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyPropertyChanged oldValue)
            oldValue.PropertyChanged -= ViewModel_PropertyChanged;
        if (e.NewValue is INotifyPropertyChanged newValue)
            newValue.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(
        object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(
                ProductsViewModel.IsNewProductCheckOpen) ||
            sender is not ProductsViewModel
            {
                IsNewProductCheckOpen: true
            })
            return;

        Dispatcher.BeginInvoke(() =>
        {
            CodigoBarrasVerificacionInput.Focus();
            Keyboard.Focus(CodigoBarrasVerificacionInput);
        });
    }
}
