using KONTAXPRO.Application.Models.Common;
using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace KONTAXPRO.Desktop.Views;

public partial class MessageDialogWindow : Window
{
    private readonly MessageDialogRequest _request;

    public MessageDialogType Type => _request.Type;
    public string DialogTitle => _request.Title;
    public string Message => _request.Message;
    public string? Detail => _request.Detail;
    public string PrimaryText => _request.PrimaryText;
    public string? SecondaryText => _request.SecondaryText;
    public bool HasDetail => !string.IsNullOrWhiteSpace(Detail);
    public bool HasSecondaryAction => !string.IsNullOrWhiteSpace(SecondaryText);
    public bool IsDestructive => _request.IsDestructive;

    public string TypeLabel => Type switch
    {
        MessageDialogType.Error => "ERROR",
        MessageDialogType.Warning => "ADVERTENCIA",
        MessageDialogType.Info => "INFORMACIÓN",
        MessageDialogType.Success => "OPERACIÓN COMPLETADA",
        _ => "CONFIRMACIÓN"
    };

    public PackIconKind IconKind => Type switch
    {
        MessageDialogType.Error => PackIconKind.AlertCircleOutline,
        MessageDialogType.Warning => PackIconKind.AlertOutline,
        MessageDialogType.Info => PackIconKind.InformationOutline,
        MessageDialogType.Success => PackIconKind.CheckCircleOutline,
        _ => PackIconKind.HelpCircleOutline
    };

    public Brush AccentBrush => FindBrush(Type switch
    {
        MessageDialogType.Error => "DangerButtonBackgroundBrush",
        MessageDialogType.Warning => "KontaxDialogWarningBrush",
        MessageDialogType.Info => "KontaxDialogInfoBrush",
        MessageDialogType.Success => "PrimaryDarkBrush",
        _ => "KontaxDialogQuestionBrush"
    });

    public Brush AccentSoftBrush => FindBrush(Type switch
    {
        MessageDialogType.Error => "DangerSoftBrush",
        MessageDialogType.Warning => "WarningSoftBrush",
        MessageDialogType.Info => "InfoSoftBrush",
        MessageDialogType.Success => "SuccessSoftBrush",
        _ => "SuccessSoftBrush"
    });

    public MessageDialogWindow(MessageDialogRequest request)
    {
        _request = request;
        InitializeComponent();
        DataContext = this;

        PrimaryButton.Style = (Style)FindResource(
            IsDestructive ? "DangerButtonStyle" : "PrimaryButtonStyle");
        PrimaryButton.IsDefault = !IsDestructive;
        SecondaryButton.IsDefault = IsDestructive && HasSecondaryAction;
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

    private Brush FindBrush(string key) =>
        (Brush)System.Windows.Application.Current.FindResource(key);

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var initialButton = IsDestructive && HasSecondaryAction
            ? SecondaryButton
            : PrimaryButton;

        initialButton.Focus();
        Keyboard.Focus(initialButton);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        e.Handled = true;
        Complete(false);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        Complete(false);

    private void SecondaryButton_Click(object sender, RoutedEventArgs e) =>
        Complete(false);

    private void PrimaryButton_Click(object sender, RoutedEventArgs e) =>
        Complete(true);

    private void Complete(bool result)
    {
        DialogResult = result;
        Close();
    }
}
