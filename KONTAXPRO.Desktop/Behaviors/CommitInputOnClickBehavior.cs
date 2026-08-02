using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KONTAXPRO.Desktop.Behaviors;

public static class CommitInputOnClickBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(CommitInputOnClickBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static void SetIsEnabled(DependencyObject element, bool value) =>
        element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) =>
        (bool)element.GetValue(IsEnabledProperty);

    private static void OnIsEnabledChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not Button button)
            return;

        if ((bool)e.NewValue)
            button.PreviewMouseLeftButtonUp += CommitFocusedInput;
        else
            button.PreviewMouseLeftButtonUp -= CommitFocusedInput;
    }

    private static void CommitFocusedInput(
        object sender,
        MouseButtonEventArgs e)
    {
        if (Keyboard.FocusedElement is not TextBox textBox)
            return;

        DecimalInputBehavior.CommitValue(textBox);
    }
}
