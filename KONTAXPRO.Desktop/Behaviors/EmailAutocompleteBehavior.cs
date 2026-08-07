using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace KONTAXPRO.Desktop.Behaviors;

public static class EmailAutocompleteBehavior
{
    private static readonly string[] PopularDomains =
    [
        "gmail.com",
        "hotmail.com",
        "outlook.com",
        "yahoo.com",
        "icloud.com",
        "live.com"
    ];

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(EmailAutocompleteBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyProperty StateProperty =
        DependencyProperty.RegisterAttached(
            "State",
            typeof(AutocompleteState),
            typeof(EmailAutocompleteBehavior));

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
            if (textBox.GetValue(StateProperty) is AutocompleteState)
                return;

            var state = CreateState(textBox);
            textBox.SetValue(StateProperty, state);
            textBox.TextChanged += OnTextChanged;
            textBox.PreviewKeyDown += OnPreviewKeyDown;
            textBox.Unloaded += OnUnloaded;
            state.Suggestions.PreviewMouseLeftButtonUp += OnSuggestionClicked;
        }
        else
        {
            Detach(textBox);
        }
    }

    private static AutocompleteState CreateState(TextBox textBox)
    {
        var suggestions = new ListBox
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            FontSize = 13,
            MaxHeight = 220
        };
        suggestions.SetResourceReference(Control.ForegroundProperty, "PrimaryTextBrush");

        var itemStyle = new Style(typeof(ListBoxItem));
        itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(11, 7, 11, 7)));
        itemStyle.Setters.Add(new Setter(FrameworkElement.CursorProperty, Cursors.Hand));
        itemStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        suggestions.ItemContainerStyle = itemStyle;

        var border = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(4),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 14,
                ShadowDepth = 3,
                Opacity = 0.2
            },
            Child = suggestions
        };
        border.SetResourceReference(Border.BackgroundProperty, "ElevatedSurfaceBrush");
        border.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");

        var popup = new Popup
        {
            PlacementTarget = textBox,
            Placement = PlacementMode.Bottom,
            VerticalOffset = 3,
            AllowsTransparency = true,
            PopupAnimation = PopupAnimation.Fade,
            StaysOpen = false,
            Child = border
        };

        return new AutocompleteState(popup, border, suggestions);
    }

    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox ||
            textBox.GetValue(StateProperty) is not AutocompleteState state ||
            state.IsApplyingSuggestion)
            return;

        var parts = GetEmailParts(textBox.Text);
        if (parts is null)
        {
            state.Popup.IsOpen = false;
            return;
        }

        var matches = PopularDomains
            .Where(domain => domain.StartsWith(parts.Value.Domain, StringComparison.OrdinalIgnoreCase))
            .Where(domain => !domain.Equals(parts.Value.Domain, StringComparison.OrdinalIgnoreCase))
            .Select(domain => $"@{domain}")
            .ToArray();

        if (matches.Length == 0)
        {
            state.Popup.IsOpen = false;
            return;
        }

        state.Suggestions.ItemsSource = matches;
        state.Suggestions.SelectedIndex = 0;
        state.Container.Width = Math.Max(230, textBox.ActualWidth);
        state.Popup.IsOpen = true;
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox ||
            textBox.GetValue(StateProperty) is not AutocompleteState state ||
            !state.Popup.IsOpen)
            return;

        switch (e.Key)
        {
            case Key.Down:
                state.Suggestions.SelectedIndex = Math.Min(
                    state.Suggestions.Items.Count - 1,
                    state.Suggestions.SelectedIndex + 1);
                state.Suggestions.ScrollIntoView(state.Suggestions.SelectedItem);
                e.Handled = true;
                break;
            case Key.Up:
                state.Suggestions.SelectedIndex = Math.Max(0, state.Suggestions.SelectedIndex - 1);
                state.Suggestions.ScrollIntoView(state.Suggestions.SelectedItem);
                e.Handled = true;
                break;
            case Key.Enter:
                ApplySelectedSuggestion(textBox, state);
                e.Handled = true;
                break;
            case Key.Tab:
                ApplySelectedSuggestion(textBox, state);
                e.Handled = true;
                textBox.MoveFocus(new TraversalRequest(
                    e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift)
                        ? FocusNavigationDirection.Previous
                        : FocusNavigationDirection.Next));
                break;
            case Key.Escape:
                state.Popup.IsOpen = false;
                e.Handled = true;
                break;
        }
    }

    private static void OnSuggestionClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBox suggestions ||
            FindOwner(suggestions) is not { } textBox ||
            textBox.GetValue(StateProperty) is not AutocompleteState state)
            return;

        suggestions.SelectedItem = ItemsControl.ContainerFromElement(
            suggestions,
            e.OriginalSource as DependencyObject) is ListBoxItem item
                ? item.DataContext
                : suggestions.SelectedItem;
        ApplySelectedSuggestion(textBox, state);
        textBox.Focus();
        e.Handled = true;
    }

    private static TextBox? FindOwner(ListBox suggestions)
    {
        return suggestions.Tag as TextBox;
    }

    private static void ApplySelectedSuggestion(TextBox textBox, AutocompleteState state)
    {
        if (state.Suggestions.SelectedItem is not string suggestion ||
            GetEmailParts(textBox.Text) is not { } parts)
            return;

        state.IsApplyingSuggestion = true;
        try
        {
            textBox.Text = $"{parts.Local}{suggestion}".ToLowerInvariant();
            textBox.CaretIndex = textBox.Text.Length;
            textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            state.Popup.IsOpen = false;
        }
        finally
        {
            state.IsApplyingSuggestion = false;
        }
    }

    private static (string Local, string Domain)? GetEmailParts(string value)
    {
        var atIndex = value.IndexOf('@');
        if (atIndex <= 0 || atIndex != value.LastIndexOf('@'))
            return null;

        var local = value[..atIndex].Trim();
        var domain = value[(atIndex + 1)..].Trim();
        if (local.Length == 0 || local.Any(char.IsWhiteSpace) || domain.Any(char.IsWhiteSpace))
            return null;

        return (local, domain);
    }

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox &&
            textBox.GetValue(StateProperty) is AutocompleteState state)
            state.Popup.IsOpen = false;
    }

    private static void Detach(TextBox textBox)
    {
        if (textBox.GetValue(StateProperty) is not AutocompleteState state)
            return;

        textBox.TextChanged -= OnTextChanged;
        textBox.PreviewKeyDown -= OnPreviewKeyDown;
        textBox.Unloaded -= OnUnloaded;
        state.Suggestions.PreviewMouseLeftButtonUp -= OnSuggestionClicked;
        state.Popup.IsOpen = false;
        textBox.ClearValue(StateProperty);
    }

    private sealed class AutocompleteState(
        Popup popup,
        Border container,
        ListBox suggestions)
    {
        public Popup Popup { get; } = popup;
        public Border Container { get; } = container;
        public ListBox Suggestions { get; } = InitializeSuggestions(suggestions, popup);
        public bool IsApplyingSuggestion { get; set; }

        private static ListBox InitializeSuggestions(ListBox suggestions, Popup popup)
        {
            suggestions.Tag = popup.PlacementTarget;
            return suggestions;
        }
    }
}
