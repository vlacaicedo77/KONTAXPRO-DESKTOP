namespace KONTAXPRO.Application.Interfaces;

public interface INotificationService
{
    Task ShowSuccessAsync(string message, string? title = null);

    Task ShowInfoAsync(string message, string? title = null);

    Task ShowWarningAsync(string message, string? title = null);
}
