using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace KONTAXPRO.Desktop.Behaviors;

public static class ScrollViewerResetBehavior
{
    public static readonly DependencyProperty ResetOnHideProperty =
        DependencyProperty.RegisterAttached(
            "ResetOnHide",
            typeof(bool),
            typeof(ScrollViewerResetBehavior),
            new PropertyMetadata(false, OnResetOnHideChanged));

    public static bool GetResetOnHide(DependencyObject element) =>
        (bool)element.GetValue(ResetOnHideProperty);

    public static void SetResetOnHide(
        DependencyObject element,
        bool value) =>
        element.SetValue(ResetOnHideProperty, value);

    private static void OnResetOnHideChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not ScrollViewer scrollViewer)
            return;

        scrollViewer.IsVisibleChanged -= ScrollViewer_IsVisibleChanged;
        if (e.NewValue is true)
            scrollViewer.IsVisibleChanged += ScrollViewer_IsVisibleChanged;
    }

    private static void ScrollViewer_IsVisibleChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
            return;

        if (e.NewValue is false)
        {
            ResetPosition(scrollViewer);
            return;
        }

        scrollViewer.Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            () =>
            {
                if (scrollViewer.IsVisible)
                    ResetPosition(scrollViewer);
            });
    }

    private static void ResetPosition(ScrollViewer scrollViewer)
    {
        scrollViewer.ScrollToVerticalOffset(0);
        scrollViewer.ScrollToHorizontalOffset(0);
    }
}
