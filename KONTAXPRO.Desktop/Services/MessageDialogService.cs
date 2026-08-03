using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Common;
using KONTAXPRO.Desktop.Views;
using System.Threading;
using System.Windows;

namespace KONTAXPRO.Desktop.Services;

public sealed class MessageDialogService : IMessageDialogService
{
    private int _dialogOpen;

    public Task ShowErrorAsync(string title, string message, string? detail = null) =>
        ShowMessageAsync(new MessageDialogRequest(
            MessageDialogType.Error, title, message, detail));

    public Task ShowWarningAsync(string title, string message, string? detail = null) =>
        ShowMessageAsync(new MessageDialogRequest(
            MessageDialogType.Warning, title, message, detail));

    public Task ShowInfoAsync(string title, string message, string? detail = null) =>
        ShowMessageAsync(new MessageDialogRequest(
            MessageDialogType.Info, title, message, detail));

    public Task ShowSuccessAsync(string title, string message, string? detail = null) =>
        ShowMessageAsync(new MessageDialogRequest(
            MessageDialogType.Success, title, message, detail));

    public Task<bool> ConfirmAsync(
        string title,
        string message,
        string primaryText = "Confirmar",
        string secondaryText = "Cancelar",
        bool isDestructive = false) =>
        ShowAsync(CreateConfirmation(
            MessageDialogType.Confirmation,
            title,
            message,
            primaryText,
            secondaryText,
            isDestructive));

    public Task<bool> ConfirmWarningAsync(
        string title,
        string message,
        string primaryText = "Continuar",
        string secondaryText = "Cancelar") =>
        ShowAsync(CreateConfirmation(
            MessageDialogType.Warning,
            title,
            message,
            primaryText,
            secondaryText,
            false));

    public bool Confirm(
        string title,
        string message,
        string primaryText = "Confirmar",
        string secondaryText = "Cancelar",
        bool isDestructive = false)
    {
        var request = CreateConfirmation(
            MessageDialogType.Confirmation,
            title,
            message,
            primaryText,
            secondaryText,
            isDestructive);

        var application = System.Windows.Application.Current;
        if (application is null)
            return false;

        return application.Dispatcher.CheckAccess()
            ? ShowCore(request)
            : application.Dispatcher.Invoke(() => ShowCore(request));
    }

    private async Task ShowMessageAsync(MessageDialogRequest request) =>
        _ = await ShowAsync(request);

    private Task<bool> ShowAsync(MessageDialogRequest request)
    {
        var application = System.Windows.Application.Current;
        if (application is null)
            return Task.FromResult(false);

        return application.Dispatcher.CheckAccess()
            ? Task.FromResult(ShowCore(request))
            : application.Dispatcher.InvokeAsync(() => ShowCore(request)).Task;
    }

    private bool ShowCore(MessageDialogRequest request)
    {
        if (Interlocked.CompareExchange(ref _dialogOpen, 1, 0) != 0)
            return false;

        try
        {
            var dialog = new MessageDialogWindow(request);
            dialog.AttachTo(WindowOwnerResolver.Resolve());
            return dialog.ShowDialog() == true;
        }
        finally
        {
            Volatile.Write(ref _dialogOpen, 0);
        }
    }

    private static MessageDialogRequest CreateConfirmation(
        MessageDialogType type,
        string title,
        string message,
        string primaryText,
        string secondaryText,
        bool isDestructive) =>
        new(
            type,
            title,
            message,
            PrimaryText: primaryText,
            SecondaryText: secondaryText,
            IsDestructive: isDestructive);
}

