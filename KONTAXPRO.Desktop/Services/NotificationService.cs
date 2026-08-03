using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Common;
using KONTAXPRO.Desktop.Views;
using System.Windows;

namespace KONTAXPRO.Desktop.Services;

public sealed class NotificationService : INotificationService
{
    private NotificationWindow? _currentNotification;

    public Task ShowSuccessAsync(string message, string? title = null) =>
        ShowAsync(MessageDialogType.Success, title ?? "Operación completada", message);

    public Task ShowInfoAsync(string message, string? title = null) =>
        ShowAsync(MessageDialogType.Info, title ?? "Información", message);

    public Task ShowWarningAsync(string message, string? title = null) =>
        ShowAsync(MessageDialogType.Warning, title ?? "Advertencia", message);

    private Task ShowAsync(MessageDialogType type, string title, string message)
    {
        var application = System.Windows.Application.Current;
        if (application is null)
            return Task.CompletedTask;

        if (application.Dispatcher.CheckAccess())
        {
            ShowCore(type, title, message);
            return Task.CompletedTask;
        }

        return application.Dispatcher.InvokeAsync(
            () => ShowCore(type, title, message)).Task;
    }

    private void ShowCore(MessageDialogType type, string title, string message)
    {
        if (_currentNotification is { IsLoaded: true })
            _currentNotification.Close();

        var notification = new NotificationWindow(type, title, message);
        notification.AttachTo(WindowOwnerResolver.Resolve());
        notification.Closed += (_, _) =>
        {
            if (ReferenceEquals(_currentNotification, notification))
                _currentNotification = null;
        };

        _currentNotification = notification;
        notification.ShowTemporarily(TimeSpan.FromSeconds(3.5));
    }
}

