using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using KONTAXPRO.Desktop.Behaviors;
using Xunit;

namespace KONTAXPRO.Tests.Desktop;

public sealed class ComboBoxMouseWheelBehaviorTests
{
    [Fact]
    public void ClosedCombo_RoutesWheelToScrollableParent()
    {
        RunSta(() =>
        {
            var (scrollViewer, comboBox) = CreateScrollableCombo();

            var routed = ComboBoxMouseWheelBehavior.TryRouteWheel(comboBox, -120);
            scrollViewer.UpdateLayout();

            Assert.True(routed);
            Assert.True(scrollViewer.VerticalOffset > 0);
        });
    }

    [Fact]
    public void OpenCombo_KeepsWheelForItsOwnOptions()
    {
        Assert.False(ComboBoxMouseWheelBehavior.ShouldRouteWheel(
            isDropDownOpen: true, delta: -120));
    }

    [Fact]
    public void ClosedCombo_SkipsNonScrollableInnerContainerAndMovesOuterForm()
    {
        RunSta(() =>
        {
            var comboBox = new ComboBox();
            var inner = new ScrollViewer
            {
                Height = 80,
                Content = new Grid { Height = 80, Children = { comboBox } }
            };
            var outerContent = new StackPanel { Height = 700 };
            outerContent.Children.Add(inner);
            var outer = new ScrollViewer
            {
                Width = 300,
                Height = 120,
                Content = outerContent
            };
            Layout(outer, 300, 120);

            var routed = ComboBoxMouseWheelBehavior.TryRouteWheel(comboBox, -120);
            outer.UpdateLayout();

            Assert.True(routed);
            Assert.Equal(0, inner.VerticalOffset);
            Assert.True(outer.VerticalOffset > 0);
        });
    }

    [Fact]
    public void RequestedViews_AreWiredToClosedComboWheelRouting()
    {
        var root = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", ".."));
        var commonStyle = File.ReadAllText(Path.Combine(root,
            "KONTAXPRO.Desktop", "Styles", "ComboBoxStyles.xaml"));
        var productForm = File.ReadAllText(Path.Combine(root,
            "KONTAXPRO.Desktop", "Views", "Products", "ProductFormView.xaml"));
        var clientForm = File.ReadAllText(Path.Combine(root,
            "KONTAXPRO.Desktop", "Views", "Clientes", "ClienteFormView.xaml"));
        var purchases = File.ReadAllText(Path.Combine(root,
            "KONTAXPRO.Desktop", "Views", "Compras", "ComprasView.xaml"));
        var unsupportedOperation = File.ReadAllText(Path.Combine(root,
            "KONTAXPRO.Desktop", "Views", "Tesoreria",
            "OperacionSinSustentoWindow.xaml"));

        Assert.Contains(
            "ComboBoxMouseWheelBehavior.RouteToParentWhenClosed", commonStyle);
        Assert.Contains(
            "ComboBoxMouseWheelBehavior.RouteToParentWhenClosed", productForm);
        Assert.Contains("ModernComboBoxStyle", clientForm);
        Assert.Contains("ModernComboBoxStyle", purchases);
        Assert.Contains("BasedOn=\"{StaticResource ModernComboBoxStyle}\"",
            unsupportedOperation);
    }

    private static (ScrollViewer ScrollViewer, ComboBox ComboBox)
        CreateScrollableCombo()
    {
        var comboBox = new ComboBox();
        var content = new StackPanel { Height = 700 };
        content.Children.Add(comboBox);
        var scrollViewer = new ScrollViewer
        {
            Width = 300,
            Height = 120,
            Content = content
        };
        Layout(scrollViewer, 300, 120);
        Assert.True(scrollViewer.ScrollableHeight > 0);
        return (scrollViewer, comboBox);
    }

    private static void Layout(FrameworkElement element, double width, double height)
    {
        element.Measure(new Size(width, height));
        element.Arrange(new Rect(0, 0, width, height));
        element.UpdateLayout();
    }

    private static void RunSta(Action assertion)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                assertion();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }
}
