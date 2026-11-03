using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Antd.Wpf.Controls;
using Antd.Wpf.Theming;
using Xunit;

namespace Antd.Wpf.Tests;

[Trait("Category", "UIIntegration")]
public sealed class SwitchTypographyTests
{
    [Theory]
    [InlineData(ControlSize.Small, 12, ThemeMode.Light, FlowDirection.LeftToRight, "开", "关")]
    [InlineData(ControlSize.Middle, 12, ThemeMode.Light, FlowDirection.LeftToRight, "On", "Off")]
    [InlineData(ControlSize.Large, 12, ThemeMode.Dark, FlowDirection.RightToLeft, "开启", "关闭")]
    [InlineData(ControlSize.Middle, 24, ThemeMode.Dark, FlowDirection.LeftToRight, "启用", "停用")]
    public void Switch_LabelsUseNaturalFontMetricsAndStayVerticallyCentered(
        ControlSize size, double fontSize, ThemeMode mode, FlowDirection direction, string on, string off) => Sta.Run(() =>
    {
        var control = new AntSwitch
        {
            Size = size,
            FontSize = fontSize,
            FlowDirection = direction,
            CheckedContent = on,
            UncheckedContent = off,
            UseLayoutRounding = false,
        };
        Window window = Sta.Show(new AntThemeScope { Content = control, Mode = mode, MotionEnabled = false });
        try
        {
            AssertCenteredLabel(control, off);
            control.IsChecked = true;
            Sta.Pump();
            AssertCenteredLabel(control, on);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Switch_CustomContentKeepsItsSizeBindingAndVerticalCenter() => Sta.Run(() =>
    {
        var source = new Border { Tag = 20.0 };
        var on = new Border { Height = 30 };
        on.SetBinding(FrameworkElement.WidthProperty, new Binding("Tag") { Source = source });
        var off = new Border { Width = 12, Height = 10 };
        var control = new AntSwitch { CheckedContent = on, UncheckedContent = off, UseLayoutRounding = false };
        Window window = Sta.Show(new AntThemeScope { Content = control, MotionEnabled = false });
        try
        {
            Assert.Equal(control.ActualHeight / 2, off.TranslatePoint(new Point(0, off.ActualHeight / 2), control).Y, 5);
            control.IsChecked = true;
            source.Tag = 40.0;
            Sta.Pump();
            Assert.Equal(40, on.ActualWidth);
            Assert.Equal(30, on.ActualHeight);
            Assert.True(BindingOperations.IsDataBound(on, FrameworkElement.WidthProperty));
            Assert.Equal(control.ActualHeight / 2, on.TranslatePoint(new Point(0, on.ActualHeight / 2), control).Y, 5);
        }
        finally { window.Close(); }
    });

    private static void AssertCenteredLabel(AntSwitch control, string expected)
    {
        var presenter = (ContentPresenter)control.Template.FindName("Label", control);
        var label = Assert.IsType<TextBlock>(VisualTreeHelper.GetChild(presenter, 0));
        var natural = new TextBlock
        {
            Text = expected,
            FontFamily = control.FontFamily,
            FontSize = control.FontSize,
            FontWeight = control.FontWeight,
            FontStyle = control.FontStyle,
            FontStretch = control.FontStretch,
        };
        natural.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Assert.Equal(expected, label.Text);
        Assert.Equal(natural.DesiredSize.Height, label.ActualHeight, 5);
        Assert.Equal(control.ActualHeight / 2, label.TranslatePoint(new Point(0, label.ActualHeight / 2), control).Y, 5);
        Assert.True(control.ActualHeight >= label.ActualHeight);
    }
}
