using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace KONTAXPRO.Desktop.Behaviors;

public static class FocusRequestBehavior
{
    public static readonly DependencyProperty RequestTokenProperty =
        DependencyProperty.RegisterAttached(
            "RequestToken",
            typeof(int),
            typeof(FocusRequestBehavior),
            new PropertyMetadata(0, OnRequestTokenChanged));

    public static void SetRequestToken(DependencyObject element, int value) =>
        element.SetValue(RequestTokenProperty, value);

    public static int GetRequestToken(DependencyObject element) =>
        (int)element.GetValue(RequestTokenProperty);

    private static void OnRequestTokenChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not TextBox textBox ||
            args.NewValue is not int token || token <= 0)
            return;

        textBox.Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            textBox.Focus();
            Keyboard.Focus(textBox);
            textBox.SelectAll();
        });
    }
}
