using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Input;
using KONTAXPRO.Desktop.ViewModels.Products;

namespace KONTAXPRO.Desktop.Views.Products;

public partial class ProductsView : UserControl
{
    public ProductsView()
    {
        InitializeComponent();

        DataContextChanged += ProductsView_DataContextChanged;
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
