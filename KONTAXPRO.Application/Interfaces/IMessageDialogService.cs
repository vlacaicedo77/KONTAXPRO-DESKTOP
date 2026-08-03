using KONTAXPRO.Application.Models.Common;

namespace KONTAXPRO.Application.Interfaces;

public interface IMessageDialogService
{
    Task ShowErrorAsync(string title, string message, string? detail = null);

    Task ShowWarningAsync(string title, string message, string? detail = null);

    Task ShowInfoAsync(string title, string message, string? detail = null);

    Task ShowSuccessAsync(string title, string message, string? detail = null);

    Task<bool> ConfirmAsync(
        string title,
        string message,
        string primaryText = "Confirmar",
        string secondaryText = "Cancelar",
        bool isDestructive = false);

    Task<bool> ConfirmWarningAsync(
        string title,
        string message,
        string primaryText = "Continuar",
        string secondaryText = "Cancelar");

    bool Confirm(
        string title,
        string message,
        string primaryText = "Confirmar",
        string secondaryText = "Cancelar",
        bool isDestructive = false);
}

