using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace KONTAXPRO.Desktop.Views.Inventory;

public partial class InventoryView : UserControl
{
    public InventoryView()
        => InitializeComponent();

    private void CorrectionGrid_PreviewMouseWheel(
        object sender,
        MouseWheelEventArgs e)
    {
        if (sender is not DataGrid grid) return;
        var innerScroll = FindVisualChild<ScrollViewer>(grid);
        if (innerScroll is null) return;

        const double rowStep = 40d;
        var direction = e.Delta > 0 ? -1d : 1d;
        var target = Math.Clamp(innerScroll.VerticalOffset +
            direction * rowStep, 0d, innerScroll.ScrollableHeight);
        if (Math.Abs(target - innerScroll.VerticalOffset) > 0.5)
        {
            innerScroll.ScrollToVerticalOffset(target);
            e.Handled = true;
            return;
        }

        var outerTarget = Math.Clamp(CorrectionContentScroll.VerticalOffset +
            direction * rowStep, 0d,
            CorrectionContentScroll.ScrollableHeight);
        if (Math.Abs(outerTarget - CorrectionContentScroll.VerticalOffset) <=
            0.5) return;
        CorrectionContentScroll.ScrollToVerticalOffset(outerTarget);
        e.Handled = true;
    }

    private static T? FindVisualChild<T>(DependencyObject parent)
        where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) return match;
            var nested = FindVisualChild<T>(child);
            if (nested is not null) return nested;
        }
        return null;
    }
}
