using KONTAXPRO.Desktop.Views;
using System.Windows;

namespace KONTAXPRO.Desktop.Services;

internal static class WindowOwnerResolver
{
    public static Window? Resolve()
    {
        var application = System.Windows.Application.Current;
        if (application is null)
            return null;

        var windows = application.Windows
            .OfType<Window>()
            .Where(window =>
                window.IsVisible &&
                window is not MessageDialogWindow &&
                window is not NotificationWindow)
            .ToList();

        return windows.LastOrDefault(window => window.IsActive)
            ?? windows.LastOrDefault(window => window.Owner?.IsActive == true)
            ?? (application.MainWindow?.IsVisible == true
                ? application.MainWindow
                : windows.LastOrDefault());
    }
}

