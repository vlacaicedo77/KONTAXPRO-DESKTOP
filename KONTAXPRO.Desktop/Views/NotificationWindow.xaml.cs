using KONTAXPRO.Application.Models.Common;
using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace KONTAXPRO.Desktop.Views;

public partial class NotificationWindow : Window
{
    private readonly CancellationTokenSource _lifetime = new();
    private readonly MessageDialogType _type;

    public string NotificationTitle { get; }
    public string Message { get; }

    public PackIconKind IconKind => _type switch
    {
        MessageDialogType.Warning => PackIconKind.AlertOutline,
        MessageDialogType.Info => PackIconKind.InformationOutline,
        _ => PackIconKind.CheckCircleOutline
    };

    public Brush AccentBrush => (Brush)System.Windows.Application.Current.FindResource(
        _type switch
        {
            MessageDialogType.Warning => "KontaxDialogWarningBrush",
            MessageDialogType.Info => "KontaxDialogInfoBrush",
            _ => "BrandProBrush"
        });

    public NotificationWindow(
        MessageDialogType type,
        string title,
        string message)
    {
        _type = type;
        NotificationTitle = title;
        Message = message;
        InitializeComponent();
        DataContext = this;
        Closed += NotificationWindow_Closed;
    }

    public void AttachTo(Window? owner)
    {
        if (owner is not null && owner.IsVisible)
            Owner = owner;
        else
            Topmost = true;
    }

    public async void ShowTemporarily(TimeSpan duration)
    {
        Opacity = 0;
        Show();
        PositionWindow();
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)));

        try
        {
            await Task.Delay(duration, _lifetime.Token);
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(220));
            fade.Completed += (_, _) => Close();
            BeginAnimation(OpacityProperty, fade);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void PositionWindow()
    {
        if (Owner is { IsVisible: true } owner)
        {
            Left = owner.Left + owner.ActualWidth - ActualWidth - 22;
            Top = owner.Top + owner.ActualHeight - ActualHeight - 22;
            return;
        }

        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - ActualWidth - 22;
        Top = workArea.Bottom - ActualHeight - 22;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void NotificationWindow_Closed(object? sender, EventArgs e)
    {
        _lifetime.Cancel();
        _lifetime.Dispose();
        Closed -= NotificationWindow_Closed;
    }
}
