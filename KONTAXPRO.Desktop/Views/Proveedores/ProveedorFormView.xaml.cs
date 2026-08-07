using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KONTAXPRO.Desktop.ViewModels.Proveedores;

namespace KONTAXPRO.Desktop.Views.Proveedores;

public partial class ProveedorFormView : UserControl
{
    private ProveedorFormViewModel? _subscribedViewModel;

    public ProveedorFormView() => InitializeComponent();

    private void UserControl_IsVisibleChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        SubscribeToViewModel();
        if (IsVisible)
        {
            FormScroll.ScrollToTop();
            Dispatcher.BeginInvoke(FocusRuc);
        }
    }

    private void SubscribeToViewModel()
    {
        if (ReferenceEquals(_subscribedViewModel, DataContext))
            return;
        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.FocusRucRequested -= FocusRuc;
            _subscribedViewModel.FocusBusinessNameRequested -= FocusBusinessName;
            _subscribedViewModel.FocusAddressRequested -= FocusAddress;
        }
        _subscribedViewModel = DataContext as ProveedorFormViewModel;
        if (_subscribedViewModel is null) return;
        _subscribedViewModel.FocusRucRequested += FocusRuc;
        _subscribedViewModel.FocusBusinessNameRequested += FocusBusinessName;
        _subscribedViewModel.FocusAddressRequested += FocusAddress;
    }

    private void FocusRuc()
    {
        if (!RucInput.IsEnabled) return;
        RucInput.Focus();
        Keyboard.Focus(RucInput);
        RucInput.CaretIndex = RucInput.Text.Length;
    }

    private void FocusBusinessName()
    {
        BusinessNameInput.Focus();
        Keyboard.Focus(BusinessNameInput);
        BusinessNameInput.CaretIndex = BusinessNameInput.Text.Length;
    }

    private void FocusAddress()
    {
        AddressInput.Focus();
        Keyboard.Focus(AddressInput);
        AddressInput.CaretIndex = AddressInput.Text.Length;
    }

    private void RucInput_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
        e.Handled = e.Text.Any(character => !char.IsDigit(character));

    private void IntegerInput_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
        e.Handled = e.Text.Any(character => !char.IsDigit(character));
}
