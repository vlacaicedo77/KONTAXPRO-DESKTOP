using System.Windows;
using System.Windows.Controls;

namespace KONTAXPRO.Desktop.Controls;

public sealed class AdaptiveGrid : Panel
{
    public static readonly DependencyProperty ColumnsProperty =
        DependencyProperty.Register(
            nameof(Columns),
            typeof(int),
            typeof(AdaptiveGrid),
            new FrameworkPropertyMetadata(
                2,
                FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty CompactBreakpointProperty =
        DependencyProperty.Register(
            nameof(CompactBreakpoint),
            typeof(double),
            typeof(AdaptiveGrid),
            new FrameworkPropertyMetadata(
                760d,
                FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty ColumnSpacingProperty =
        DependencyProperty.Register(
            nameof(ColumnSpacing),
            typeof(double),
            typeof(AdaptiveGrid),
            new FrameworkPropertyMetadata(
                14d,
                FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty RowSpacingProperty =
        DependencyProperty.Register(
            nameof(RowSpacing),
            typeof(double),
            typeof(AdaptiveGrid),
            new FrameworkPropertyMetadata(
                14d,
                FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty SpanProperty =
        DependencyProperty.RegisterAttached(
            "Span",
            typeof(int),
            typeof(AdaptiveGrid),
            new FrameworkPropertyMetadata(
                1,
                FrameworkPropertyMetadataOptions.AffectsParentMeasure));

    public int Columns
    {
        get => (int)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    public double CompactBreakpoint
    {
        get => (double)GetValue(CompactBreakpointProperty);
        set => SetValue(CompactBreakpointProperty, value);
    }

    public double ColumnSpacing
    {
        get => (double)GetValue(ColumnSpacingProperty);
        set => SetValue(ColumnSpacingProperty, value);
    }

    public double RowSpacing
    {
        get => (double)GetValue(RowSpacingProperty);
        set => SetValue(RowSpacingProperty, value);
    }

    public static void SetSpan(DependencyObject element, int value) =>
        element.SetValue(SpanProperty, value);

    public static int GetSpan(DependencyObject element) =>
        (int)element.GetValue(SpanProperty);

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width)
            ? Math.Max(ActualWidth, CompactBreakpoint)
            : availableSize.Width;
        var columns = GetColumnCount(width);
        var cellWidth = Math.Max(
            0,
            (width - ColumnSpacing * (columns - 1)) / columns);
        var rows = BuildRows(columns);
        var totalHeight = 0d;

        foreach (var row in rows)
        {
            var rowHeight = 0d;
            foreach (var cell in row)
            {
                var childWidth = cellWidth * cell.Span +
                                 ColumnSpacing * (cell.Span - 1);
                cell.Child.Measure(new Size(childWidth, double.PositiveInfinity));
                rowHeight = Math.Max(rowHeight, cell.Child.DesiredSize.Height);
            }
            totalHeight += rowHeight;
        }

        if (rows.Count > 1)
            totalHeight += RowSpacing * (rows.Count - 1);
        return new Size(width, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var columns = GetColumnCount(finalSize.Width);
        var cellWidth = Math.Max(
            0,
            (finalSize.Width - ColumnSpacing * (columns - 1)) / columns);
        var rows = BuildRows(columns);
        var y = 0d;

        foreach (var row in rows)
        {
            var rowHeight = row.Max(x => x.Child.DesiredSize.Height);
            var column = 0;
            foreach (var cell in row)
            {
                var childWidth = cellWidth * cell.Span +
                                 ColumnSpacing * (cell.Span - 1);
                cell.Child.Arrange(new Rect(
                    column * (cellWidth + ColumnSpacing),
                    y,
                    childWidth,
                    rowHeight));
                column += cell.Span;
            }
            y += rowHeight + RowSpacing;
        }
        return finalSize;
    }

    private int GetColumnCount(double width) =>
        width < CompactBreakpoint ? 1 : Math.Max(1, Columns);

    private List<List<Cell>> BuildRows(int columns)
    {
        var rows = new List<List<Cell>>();
        var current = new List<Cell>();
        var used = 0;
        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility == Visibility.Collapsed)
                continue;

            var span = columns == 1
                ? 1
                : Math.Clamp(GetSpan(child), 1, columns);
            if (used > 0 && used + span > columns)
            {
                rows.Add(current);
                current = [];
                used = 0;
            }
            current.Add(new Cell(child, span));
            used += span;
            if (used == columns)
            {
                rows.Add(current);
                current = [];
                used = 0;
            }
        }
        if (current.Count > 0)
            rows.Add(current);
        return rows;
    }

    private sealed record Cell(UIElement Child, int Span);
}
