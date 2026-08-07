using System.Windows;
using System.Windows.Controls;

namespace KONTAXPRO.Desktop.Behaviors;

public enum DataGridColumnPriority
{
    Always,
    Medium,
    Wide
}

public static class ResponsiveDataGridBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(ResponsiveDataGridBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty PriorityProperty =
        DependencyProperty.RegisterAttached(
            "Priority",
            typeof(DataGridColumnPriority),
            typeof(ResponsiveDataGridBehavior),
            new PropertyMetadata(DataGridColumnPriority.Always));

    public static void SetIsEnabled(DependencyObject element, bool value) =>
        element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) =>
        (bool)element.GetValue(IsEnabledProperty);

    public static void SetPriority(
        DependencyObject element,
        DataGridColumnPriority value) => element.SetValue(PriorityProperty, value);

    public static DataGridColumnPriority GetPriority(DependencyObject element) =>
        (DataGridColumnPriority)element.GetValue(PriorityProperty);

    private static void OnIsEnabledChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not DataGrid grid)
            return;

        grid.Loaded -= OnGridLoaded;
        grid.Unloaded -= OnGridUnloaded;
        grid.SizeChanged -= OnGridSizeChanged;
        if (args.NewValue is true)
        {
            grid.Loaded += OnGridLoaded;
            grid.Unloaded += OnGridUnloaded;
            if (grid.IsLoaded)
            {
                grid.SizeChanged += OnGridSizeChanged;
                Apply(grid);
            }
        }
    }

    private static void OnGridLoaded(object sender, RoutedEventArgs args)
    {
        if (sender is not DataGrid grid)
            return;
        grid.SizeChanged -= OnGridSizeChanged;
        grid.SizeChanged += OnGridSizeChanged;
        Apply(grid);
    }

    private static void OnGridUnloaded(object sender, RoutedEventArgs args)
    {
        if (sender is DataGrid grid)
            grid.SizeChanged -= OnGridSizeChanged;
    }

    private static void OnGridSizeChanged(object sender, SizeChangedEventArgs args)
    {
        if (sender is DataGrid grid)
            Apply(grid);
    }

    private static void Apply(DataGrid grid)
    {
        var width = grid.ActualWidth;
        foreach (var column in grid.Columns)
        {
            var priority = GetPriority(column);
            column.Visibility = priority switch
            {
                DataGridColumnPriority.Always => Visibility.Visible,
                DataGridColumnPriority.Medium when width >= 780 =>
                    Visibility.Visible,
                DataGridColumnPriority.Wide when width >= 1150 =>
                    Visibility.Visible,
                _ => Visibility.Collapsed
            };
        }
    }
}
