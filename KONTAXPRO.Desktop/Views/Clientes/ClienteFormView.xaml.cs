using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using KONTAXPRO.Desktop.ViewModels.Clientes;

namespace KONTAXPRO.Desktop.Views.Clientes;

public partial class ClienteFormView : UserControl
{
    public ClienteFormView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ClienteFormViewModel previous)
            previous.FocusRequested -= OnFocusRequested;
        if (e.NewValue is ClienteFormViewModel current)
            current.FocusRequested += OnFocusRequested;
    }

    private void OnFocusRequested(ClienteFormFocusTarget target) =>
        Dispatcher.BeginInvoke(
            DispatcherPriority.Input,
            () => FocusTextInput(target == ClienteFormFocusTarget.Names
                ? RazonSocialInput
                : DireccionInput));

    private void UserControl_IsVisibleChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            Dispatcher.BeginInvoke(
                DispatcherPriority.Input,
                () =>
                {
                    FormScrollViewer.ScrollToHome();
                    FocusIdentificationInput();
                });
        }
    }

    private void TipoIdentificacion_Click(object sender, RoutedEventArgs e) =>
        Dispatcher.BeginInvoke(
            DispatcherPriority.Input,
            FocusIdentificationInput);

    private void LimpiarIdentificacion_Click(
        object sender,
        RoutedEventArgs e)
    {
        NumeroIdentificacionInput.Clear();
        FocusIdentificationInput();
    }

    private void FocusIdentificationInput()
    {
        if (!NumeroIdentificacionInput.IsVisible ||
            !NumeroIdentificacionInput.IsEnabled)
            return;

        NumeroIdentificacionInput.Focus();
        Keyboard.Focus(NumeroIdentificacionInput);
        NumeroIdentificacionInput.CaretIndex =
            NumeroIdentificacionInput.Text.Length;
    }

    private static void FocusTextInput(TextBox input)
    {
        if (!input.IsVisible || !input.IsEnabled)
            return;

        input.BringIntoView();
        input.Focus();
        Keyboard.Focus(input);
        input.CaretIndex = input.Text.Length;
    }
}
