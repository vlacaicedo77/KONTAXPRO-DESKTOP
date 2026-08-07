using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KONTAXPRO.Desktop.Behaviors;

public static class PhoneInputBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(PhoneInputBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyProperty IsFormattingProperty =
        DependencyProperty.RegisterAttached(
            "IsFormatting",
            typeof(bool),
            typeof(PhoneInputBehavior),
            new PropertyMetadata(false));

    public static void SetIsEnabled(DependencyObject element, bool value) =>
        element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) =>
        (bool)element.GetValue(IsEnabledProperty);

    private static void OnIsEnabledChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not TextBox textBox)
            return;

        if ((bool)args.NewValue)
        {
            textBox.PreviewTextInput += PreviewTextInput;
            DataObject.AddPastingHandler(textBox, OnPasting);
            textBox.TextChanged += OnTextChanged;
        }
        else
        {
            textBox.PreviewTextInput -= PreviewTextInput;
            DataObject.RemovePastingHandler(textBox, OnPasting);
            textBox.TextChanged -= OnTextChanged;
        }
    }

    private static void PreviewTextInput(
        object sender,
        TextCompositionEventArgs e) =>
        e.Handled = e.Text.Any(character =>
            !char.IsDigit(character) && character != '+');

    private static void OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText))
        {
            e.CancelCommand();
            return;
        }

        var value = e.SourceDataObject.GetData(DataFormats.UnicodeText)
            as string ?? string.Empty;
        if (value.Any(character =>
                !char.IsDigit(character) &&
                character is not ('+' or ' ' or '-' or '(' or ')')))
            e.CancelCommand();
    }

    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox ||
            (bool)textBox.GetValue(IsFormattingProperty))
            return;

        var formatted = Format(textBox.Text);
        if (formatted == textBox.Text)
            return;

        textBox.SetValue(IsFormattingProperty, true);
        try
        {
            textBox.Text = formatted;
            textBox.CaretIndex = formatted.Length;
            textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        }
        finally
        {
            textBox.SetValue(IsFormattingProperty, false);
        }
    }

    internal static string Format(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();
        var digits = new string(trimmed.Where(char.IsDigit).Take(15).ToArray());
        if (digits.Length == 0)
            return trimmed.StartsWith('+') ? "+" : string.Empty;

        if (trimmed.StartsWith("00"))
        {
            var international = digits[2..];
            return international.StartsWith("593")
                ? FormatEcuadorInternational(international[3..])
                : $"+{GroupFromRight(international)}";
        }

        if (trimmed.StartsWith('+') || digits.StartsWith("593"))
        {
            var international = digits.StartsWith("593")
                ? digits[3..]
                : digits;
            if (digits.StartsWith("593"))
                return FormatEcuadorInternational(international);
            return $"+{GroupFromRight(international)}";
        }

        if (digits.StartsWith("09"))
            return JoinParts(digits, 3, 3, 4);
        if (digits.StartsWith('0'))
            return JoinParts(digits, 2, 3, 4);

        return GroupFromRight(digits);
    }

    private static string FormatEcuadorInternational(string national) =>
        national.StartsWith('9')
            ? $"+593 {JoinParts(national, 2, 3, 4)}"
            : $"+593 {JoinParts(national, 1, 3, 4)}";

    private static string JoinParts(string digits, params int[] sizes)
    {
        var parts = new List<string>();
        var offset = 0;
        foreach (var size in sizes)
        {
            if (offset >= digits.Length)
                break;
            var length = Math.Min(size, digits.Length - offset);
            parts.Add(digits.Substring(offset, length));
            offset += length;
        }

        if (offset < digits.Length)
            parts.Add(digits[offset..]);
        return string.Join(' ', parts);
    }

    private static string GroupFromRight(string digits)
    {
        var groups = new List<string>();
        for (var end = digits.Length; end > 0; end -= 3)
        {
            var start = Math.Max(0, end - 3);
            groups.Insert(0, digits[start..end]);
        }
        return string.Join(' ', groups);
    }
}
