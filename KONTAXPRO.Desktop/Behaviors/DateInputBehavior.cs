using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KONTAXPRO.Desktop.Behaviors;

public static class DateInputBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(DateInputBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static void SetIsEnabled(DependencyObject element, bool value) =>
        element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) =>
        (bool)element.GetValue(IsEnabledProperty);

    private static void OnIsEnabledChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not TextBox textBox)
            return;
        if ((bool)e.NewValue)
        {
            textBox.PreviewTextInput += PreviewTextInput;
            textBox.LostKeyboardFocus += LostKeyboardFocus;
        }
        else
        {
            textBox.PreviewTextInput -= PreviewTextInput;
            textBox.LostKeyboardFocus -= LostKeyboardFocus;
        }
    }

    private static void PreviewTextInput(
        object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox textBox ||
            e.Text.Any(x => !char.IsDigit(x)))
        {
            e.Handled = true;
            return;
        }

        if (textBox.SelectionLength == textBox.Text.Length)
            textBox.Clear();

        var digits = new string(textBox.Text.Where(char.IsDigit).ToArray());
        if (digits.Length >= 8)
        {
            e.Handled = true;
            return;
        }

        digits += e.Text;
        textBox.Text = digits.Length switch
        {
            <= 2 => digits,
            <= 4 => $"{digits[..2]}/{digits[2..]}",
            _ => $"{digits[..2]}/{digits[2..4]}/{digits[4..]}"
        };
        textBox.CaretIndex = textBox.Text.Length;
        e.Handled = true;
    }

    private static void LostKeyboardFocus(
        object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not TextBox textBox ||
            string.IsNullOrWhiteSpace(textBox.Text))
            return;

        if (DateTime.TryParseExact(
                textBox.Text, "dd/MM/yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
        {
            textBox.Text = date.ToString(
                "dd/MM/yyyy", CultureInfo.InvariantCulture);
            textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        }
    }
}
