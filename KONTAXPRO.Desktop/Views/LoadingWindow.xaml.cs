using System.ComponentModel;
using System.Windows;

namespace KONTAXPRO.Desktop.Views;

public partial class LoadingWindow : Window
{
    private bool _canClose;

    public string LoadingTitle { get; }
    public string Message { get; }

    public LoadingWindow(string title, string message)
    {
        LoadingTitle = title;
        Message = message;
        InitializeComponent();
        DataContext = this;
    }

    public void AttachTo(Window? owner)
    {
        if (owner is null || !owner.IsVisible)
            return;

        Owner = owner;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Width = Math.Max(MinWidth, owner.ActualWidth);
        Height = Math.Max(MinHeight, owner.ActualHeight);
        MaxWidth = owner.ActualWidth;
        MaxHeight = owner.ActualHeight;
    }

    public void CloseFromService()
    {
        _canClose = true;
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        var dispatcherShuttingDown =
            Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished;

        if (!_canClose && !dispatcherShuttingDown && Owner?.IsLoaded != false)
            e.Cancel = true;

        base.OnClosing(e);
    }
}
