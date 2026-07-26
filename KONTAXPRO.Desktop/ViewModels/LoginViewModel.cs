using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KONTAXPRO.Application.Interfaces;

namespace KONTAXPRO.Desktop.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthenticationService _authenticationService;

    public event Action? LoginSuccessful;

    public LoginViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [ObservableProperty]
    private string numeroIdentificacion = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string mensaje = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsLoading)
            return;

        try
        {
            IsLoading = true;
            Mensaje = string.Empty;

            var result = await _authenticationService.LoginAsync(
                NumeroIdentificacion.Trim(),
                Password);

            if (!result.Success)
            {
                Mensaje = result.Message ?? "No fue posible iniciar sesión.";
                return;
            }

            LoginSuccessful?.Invoke();
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al iniciar sesión: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}