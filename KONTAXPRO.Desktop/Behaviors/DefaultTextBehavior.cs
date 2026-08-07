using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace KONTAXPRO.Desktop.Behaviors;

public static class DefaultTextBehavior
{
    public static readonly DependencyProperty DefaultValueProperty =
        DependencyProperty.RegisterAttached(
            "DefaultValue",
            typeof(string),
            typeof(DefaultTextBehavior),
            new PropertyMetadata(null, OnDefaultValueChanged));

    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached(
            "IsUpdating",
            typeof(bool),
            typeof(DefaultTextBehavior),
            new PropertyMetadata(false));

    public static void SetDefaultValue(DependencyObject element, string? value) =>
        element.SetValue(DefaultValueProperty, value);

    public static string? GetDefaultValue(DependencyObject element) =>
        (string?)element.GetValue(DefaultValueProperty);

    private static void OnDefaultValueChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not TextBox textBox)
            return;

        textBox.Loaded -= OnLoaded;
        textBox.GotKeyboardFocus -= OnGotKeyboardFocus;
        textBox.LostKeyboardFocus -= OnLostKeyboardFocus;
        textBox.TextChanged -= OnTextChanged;

        if (args.NewValue is not string defaultValue ||
            string.IsNullOrWhiteSpace(defaultValue))
        {
            textBox.Opacity = 1;
            return;
        }

        textBox.Loaded += OnLoaded;
        textBox.GotKeyboardFocus += OnGotKeyboardFocus;
        textBox.LostKeyboardFocus += OnLostKeyboardFocus;
        textBox.TextChanged += OnTextChanged;
        if (textBox.IsLoaded)
            RestoreDefaultWhenEmpty(textBox);
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
            RestoreDefaultWhenEmpty(textBox);
    }

    private static void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not TextBox textBox ||
            GetDefaultValue(textBox) is not { } defaultValue ||
            !string.Equals(textBox.Text, defaultValue, StringComparison.OrdinalIgnoreCase))
            return;

        SetText(textBox, string.Empty);
        textBox.Opacity = 1;
    }

    private static void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox textBox)
            RestoreDefaultWhenEmpty(textBox);
    }

    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox ||
            (bool)textBox.GetValue(IsUpdatingProperty))
            return;

        textBox.Opacity = !textBox.IsKeyboardFocusWithin && IsDefaultValue(textBox)
            ? 0.62
            : 1;
    }

    private static void RestoreDefaultWhenEmpty(TextBox textBox)
    {
        if (string.IsNullOrWhiteSpace(textBox.Text) &&
            GetDefaultValue(textBox) is { } defaultValue)
            SetText(textBox, defaultValue);

        textBox.Opacity = IsDefaultValue(textBox) ? 0.62 : 1;
    }

    private static bool IsDefaultValue(TextBox textBox) =>
        GetDefaultValue(textBox) is { } defaultValue &&
        string.Equals(textBox.Text, defaultValue, StringComparison.OrdinalIgnoreCase);

    private static void SetText(TextBox textBox, string value)
    {
        textBox.SetValue(IsUpdatingProperty, true);
        try
        {
            textBox.Text = value;
            textBox.CaretIndex = value.Length;
            textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        }
        finally
        {
            textBox.SetValue(IsUpdatingProperty, false);
        }
    }
}
