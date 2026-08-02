using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KONTAXPRO.Desktop.Behaviors;

public static class DecimalInputBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(DecimalInputBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty DecimalPlacesProperty =
        DependencyProperty.RegisterAttached(
            "DecimalPlaces", typeof(int), typeof(DecimalInputBehavior),
            new PropertyMetadata(6));

    public static readonly DependencyProperty ClearZeroOnFocusProperty =
        DependencyProperty.RegisterAttached(
            "ClearZeroOnFocus", typeof(bool), typeof(DecimalInputBehavior),
            new PropertyMetadata(false));

    public static readonly DependencyProperty TrimTrailingZerosProperty =
        DependencyProperty.RegisterAttached(
            "TrimTrailingZeros", typeof(bool), typeof(DecimalInputBehavior),
            new PropertyMetadata(false));

    public static readonly DependencyProperty MinimumDecimalPlacesProperty =
        DependencyProperty.RegisterAttached(
            "MinimumDecimalPlaces", typeof(int), typeof(DecimalInputBehavior),
            new PropertyMetadata(-1));

    public static readonly DependencyProperty UpdateSourceWhileTypingProperty =
        DependencyProperty.RegisterAttached(
            "UpdateSourceWhileTyping", typeof(bool),
            typeof(DecimalInputBehavior),
            new PropertyMetadata(false));

    public static void SetIsEnabled(DependencyObject element, bool value) =>
        element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) =>
        (bool)element.GetValue(IsEnabledProperty);

    public static void SetDecimalPlaces(DependencyObject element, int value) =>
        element.SetValue(DecimalPlacesProperty, value);

    public static int GetDecimalPlaces(DependencyObject element) =>
        (int)element.GetValue(DecimalPlacesProperty);

    public static void SetClearZeroOnFocus(
        DependencyObject element, bool value) =>
        element.SetValue(ClearZeroOnFocusProperty, value);

    public static bool GetClearZeroOnFocus(DependencyObject element) =>
        (bool)element.GetValue(ClearZeroOnFocusProperty);

    public static void SetTrimTrailingZeros(
        DependencyObject element, bool value) =>
        element.SetValue(TrimTrailingZerosProperty, value);

    public static bool GetTrimTrailingZeros(DependencyObject element) =>
        (bool)element.GetValue(TrimTrailingZerosProperty);

    public static void SetMinimumDecimalPlaces(
        DependencyObject element, int value) =>
        element.SetValue(MinimumDecimalPlacesProperty, value);

    public static int GetMinimumDecimalPlaces(DependencyObject element) =>
        (int)element.GetValue(MinimumDecimalPlacesProperty);

    public static void SetUpdateSourceWhileTyping(
        DependencyObject element, bool value) =>
        element.SetValue(UpdateSourceWhileTypingProperty, value);

    public static bool GetUpdateSourceWhileTyping(DependencyObject element) =>
        (bool)element.GetValue(UpdateSourceWhileTypingProperty);

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
            textBox.GotKeyboardFocus += OnGotKeyboardFocus;
            textBox.LostKeyboardFocus += OnLostKeyboardFocus;
            textBox.TextChanged += OnTextChanged;
        }
        else
        {
            textBox.PreviewTextInput -= PreviewTextInput;
            DataObject.RemovePastingHandler(textBox, OnPasting);
            textBox.GotKeyboardFocus -= OnGotKeyboardFocus;
            textBox.LostKeyboardFocus -= OnLostKeyboardFocus;
            textBox.TextChanged -= OnTextChanged;
        }
    }

    private static void OnTextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox ||
            !GetUpdateSourceWhileTyping(textBox))
            return;

        var value = textBox.Text.Trim();
        if (string.IsNullOrEmpty(value) ||
            value.EndsWith('.') ||
            value.EndsWith(','))
            return;

        var normalized = value.Replace(',', '.');
        if (!decimal.TryParse(
                normalized,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out _))
            return;

        textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }

    private static void OnGotKeyboardFocus(
        object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not TextBox textBox ||
            !GetClearZeroOnFocus(textBox))
            return;

        var normalized = textBox.Text.Trim().Replace(',', '.');
        if (decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var value) &&
            value == 0)
            textBox.Clear();
    }

    private static void PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        var proposed = textBox.Text.Remove(
            textBox.SelectionStart, textBox.SelectionLength)
            .Insert(textBox.SelectionStart, e.Text);
        e.Handled = !IsValidIntermediate(
            proposed, GetDecimalPlaces(textBox));
    }

    private static void OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not TextBox textBox ||
            !e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText))
        {
            e.CancelCommand();
            return;
        }

        var pasted = e.SourceDataObject.GetData(
            DataFormats.UnicodeText) as string ?? string.Empty;
        var proposed = textBox.Text.Remove(
            textBox.SelectionStart, textBox.SelectionLength)
            .Insert(textBox.SelectionStart, pasted);
        if (!IsValidIntermediate(proposed, GetDecimalPlaces(textBox)))
            e.CancelCommand();
    }

    private static bool IsValidIntermediate(string value, int decimalPlaces)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        return Regex.IsMatch(
            value,
            $@"^\d*(?:[\.,]\d{{0,{Math.Max(0, decimalPlaces)}}})?$",
            RegexOptions.CultureInvariant);
    }

    private static void OnLostKeyboardFocus(
        object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        CommitValue(textBox);
    }

    public static void CommitValue(TextBox textBox)
    {
        var normalized = textBox.Text.Trim().Replace(',', '.');
        if (!decimal.TryParse(
                normalized,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var value))
            value = 0;

        var decimals = Math.Max(0, GetDecimalPlaces(textBox));
        var minimumDecimals = GetMinimumDecimalPlaces(textBox);
        var format = minimumDecimals >= 0
            ? BuildVariableDecimalFormat(
                Math.Min(minimumDecimals, decimals),
                decimals)
            : GetTrimTrailingZeros(textBox)
                ? BuildVariableDecimalFormat(0, decimals)
                : $"F{decimals}";
        textBox.Text = value == 0 && GetClearZeroOnFocus(textBox)
            ? "0"
            : value.ToString(format, CultureInfo.InvariantCulture);
        textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }

    private static string BuildVariableDecimalFormat(
        int minimumDecimals,
        int maximumDecimals)
    {
        if (maximumDecimals == 0)
            return "0";

        return $"0.{new string('0', minimumDecimals)}" +
               new string('#', maximumDecimals - minimumDecimals);
    }
}
