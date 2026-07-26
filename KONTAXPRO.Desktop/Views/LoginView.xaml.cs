using KONTAXPRO.Desktop.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KONTAXPRO.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.LoginSuccessful += OnLoginSuccessful;

        Loaded += LoginWindow_Loaded;
        Closed += LoginWindow_Closed;
    }

    private void LoginWindow_Loaded(
    object sender,
    RoutedEventArgs e)
    {
        Activate();

        NumeroIdentificacionInput.Focus();
        Keyboard.Focus(NumeroIdentificacionInput);
    }

    private void PasswordInput_PasswordChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            _viewModel.Password =
                passwordBox.Password;
        }
    }

    private void OnLoginSuccessful()
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

    private void Window_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void LoginWindow_Closed(
        object? sender,
        EventArgs e)
    {
        _viewModel.LoginSuccessful -= OnLoginSuccessful;

        Loaded -= LoginWindow_Loaded;
        Closed -= LoginWindow_Closed;
    }

    private void NumeroIdentificacion_TextChanged(
    object sender,
    TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        if (textBox.Text.Length == 10)
        {
            PasswordInput.Focus();

            Keyboard.Focus(PasswordInput);
        }
    }
}