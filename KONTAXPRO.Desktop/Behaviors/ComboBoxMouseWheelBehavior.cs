using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace KONTAXPRO.Desktop.Behaviors;

public static class ComboBoxMouseWheelBehavior
{
    public static readonly DependencyProperty RouteToParentWhenClosedProperty =
        DependencyProperty.RegisterAttached(
            "RouteToParentWhenClosed",
            typeof(bool),
            typeof(ComboBoxMouseWheelBehavior),
            new PropertyMetadata(false, OnRouteToParentWhenClosedChanged));

    public static bool GetRouteToParentWhenClosed(DependencyObject element) =>
        (bool)element.GetValue(RouteToParentWhenClosedProperty);

    public static void SetRouteToParentWhenClosed(
        DependencyObject element, bool value) =>
        element.SetValue(RouteToParentWhenClosedProperty, value);

    private static void OnRouteToParentWhenClosedChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not ComboBox comboBox)
            return;

        comboBox.PreviewMouseWheel -= ComboBox_PreviewMouseWheel;
        if (e.NewValue is true)
            comboBox.PreviewMouseWheel += ComboBox_PreviewMouseWheel;
    }

    private static void ComboBox_PreviewMouseWheel(
        object sender, MouseWheelEventArgs e)
    {
        if (sender is ComboBox comboBox && TryRouteWheel(comboBox, e.Delta))
            e.Handled = true;
    }

    public static bool TryRouteWheel(ComboBox comboBox, int delta)
    {
        if (!ShouldRouteWheel(comboBox.IsDropDownOpen, delta))
            return false;

        var foundScrollViewer = false;
        for (var current = GetParent(comboBox); current is not null;
             current = GetParent(current))
        {
            if (current is not ScrollViewer scrollViewer)
                continue;

            foundScrollViewer = true;
            var scrollingDown = delta < 0;
            var canScroll = scrollingDown
                ? scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight
                : scrollViewer.VerticalOffset > 0;
            if (!canScroll)
                continue;

            scrollViewer.ScrollToVerticalOffset(Math.Clamp(
                scrollViewer.VerticalOffset - delta,
                0,
                scrollViewer.ScrollableHeight));
            return true;
        }

        // Dentro de un área desplazable, una rueda sin recorrido disponible no
        // debe cambiar accidentalmente la selección del combo cerrado.
        return foundScrollViewer;
    }

    public static bool ShouldRouteWheel(bool isDropDownOpen, int delta) =>
        !isDropDownOpen && delta != 0;

    private static DependencyObject? GetParent(DependencyObject current) =>
        current is Visual or System.Windows.Media.Media3D.Visual3D
            ? VisualTreeHelper.GetParent(current)
            : LogicalTreeHelper.GetParent(current);
}
