using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using Antd.Wpf.Controls;
using Antd.Wpf.Theming;
using Xunit;

namespace Antd.Wpf.Tests;

[Trait("Category", "UIIntegration")]
public sealed class FlexTests
{
    [Fact]
    public void Flex_DefaultsHaveNoAddedSpacingOrVisualWrappers() => Sta.Run(() =>
    {
        var panel = new AntFlexPanel();
        var first = new Border { Width = 20, Height = 10 };
        var second = new Border { Width = 30, Height = 15 };
        panel.Children.Add(first);
        panel.Children.Add(second);
        Layout(panel, 100, 40);
        Assert.Equal(new Thickness(0), panel.Margin);
        Assert.Equal(0, panel.RowGap);
        Assert.Equal(0, panel.ColumnGap);
        Assert.Equal(Orientation.Horizontal, panel.Orientation);
        Assert.Equal(FlexAlignment.Stretch, panel.AlignItems);
        Assert.Equal(new Size(50, 15), panel.DesiredSize);
        Assert.Equal(new Rect(0, 0, 20, 40), LayoutInformation.GetLayoutSlot(first));
        Assert.Equal(new Rect(20, 0, 30, 40), LayoutInformation.GetLayoutSlot(second));
        Assert.Equal(2, VisualTreeHelper.GetChildrenCount(panel));
        Assert.Same(first, VisualTreeHelper.GetChild(panel, 0));
        Assert.False(panel.Focusable);
    });

    [Theory]
    [InlineData(Orientation.Horizontal, FlexJustification.Start, 0)]
    [InlineData(Orientation.Horizontal, FlexJustification.Center, 21)]
    [InlineData(Orientation.Horizontal, FlexJustification.End, 42)]
    [InlineData(Orientation.Vertical, FlexJustification.Start, 0)]
    [InlineData(Orientation.Vertical, FlexJustification.Center, 33)]
    [InlineData(Orientation.Vertical, FlexJustification.End, 66)]
    public void Flex_MainAxisAlignmentUsesOnlyRelevantGap(Orientation orientation, FlexJustification justification, double start) => Sta.Run(() =>
    {
        var panel = new AntFlexPanel { Orientation = orientation, JustifyContent = justification, RowGap = 9, ColumnGap = 8 };
        var first = new Border { Width = 20, Height = 10 };
        var second = new Border { Width = 30, Height = 15 };
        panel.Children.Add(first);
        panel.Children.Add(second);
        Layout(panel, 100, 100);
        Rect slot = LayoutInformation.GetLayoutSlot(first);
        Rect next = LayoutInformation.GetLayoutSlot(second);
        bool horizontal = orientation == Orientation.Horizontal;
        Assert.Equal(start, horizontal ? slot.X : slot.Y);
        Assert.Equal(start + (horizontal ? 28 : 19), horizontal ? next.X : next.Y);
        Assert.Equal(horizontal ? new Size(58, 15) : new Size(30, 34), panel.DesiredSize);
    });

    [Theory]
    [InlineData(Orientation.Horizontal, FlexAlignment.Start, 0, 10)]
    [InlineData(Orientation.Horizontal, FlexAlignment.Center, 15, 10)]
    [InlineData(Orientation.Horizontal, FlexAlignment.End, 30, 10)]
    [InlineData(Orientation.Horizontal, FlexAlignment.Stretch, 0, 40)]
    [InlineData(Orientation.Vertical, FlexAlignment.Start, 0, 20)]
    [InlineData(Orientation.Vertical, FlexAlignment.Center, 10, 20)]
    [InlineData(Orientation.Vertical, FlexAlignment.End, 20, 20)]
    [InlineData(Orientation.Vertical, FlexAlignment.Stretch, 0, 40)]
    public void Flex_CrossAxisAlignmentControlsSlotWithoutOverwritingChild(Orientation orientation, FlexAlignment alignment, double offset, double breadth) => Sta.Run(() =>
    {
        var panel = new AntFlexPanel { Orientation = orientation, AlignItems = alignment };
        var child = new Border { Width = 20, Height = 10 };
        panel.Children.Add(child);
        Layout(panel, 40, 40);
        Rect slot = LayoutInformation.GetLayoutSlot(child);
        bool horizontal = orientation == Orientation.Horizontal;
        Assert.Equal(offset, horizontal ? slot.Y : slot.X);
        Assert.Equal(breadth, horizontal ? slot.Height : slot.Width);
        Assert.Equal(20, child.ActualWidth);
        Assert.Equal(10, child.ActualHeight);
        Assert.Equal(HorizontalAlignment.Stretch, child.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Stretch, child.VerticalAlignment);
    });

    [Fact]
    public void Flex_CollapsedHiddenZeroAndRemovedChildrenHaveDistinctGapBehavior() => Sta.Run(() =>
    {
        var panel = new AntFlexPanel { ColumnGap = 8 };
        var first = new Border { Width = 20, Height = 10 };
        var middle = new Border { Width = 30, Height = 10, Visibility = Visibility.Collapsed };
        var last = new Border { Width = 0, Height = 0 };
        panel.Children.Add(first);
        panel.Children.Add(middle);
        panel.Children.Add(last);
        Layout(panel, 150, 40);
        Assert.Equal(28, panel.DesiredSize.Width);
        Assert.Equal(28, LayoutInformation.GetLayoutSlot(last).X);
        middle.Visibility = Visibility.Hidden;
        Layout(panel, 150, 40);
        Assert.Equal(66, panel.DesiredSize.Width);
        Assert.Equal(66, LayoutInformation.GetLayoutSlot(last).X);
        panel.Children.Remove(first);
        Layout(panel, 150, 40);
        Assert.Equal(38, panel.DesiredSize.Width);
        Assert.Equal(0, LayoutInformation.GetLayoutSlot(middle).X);
        panel.Children.Clear();
        Layout(panel, 150, 40);
        Assert.Equal(new Size(), panel.DesiredSize);
    });

    [Fact]
    public void Flex_InfiniteMeasureOverflowAndChildConstraintsAreDeterministic() => Sta.Run(() =>
    {
        var panel = new AntFlexPanel { ColumnGap = 8, JustifyContent = FlexJustification.End, AlignItems = FlexAlignment.Start };
        var first = new Border { MinWidth = 80, MaxHeight = 15, Height = 30 };
        var second = new Border { Width = 40, Height = 10, Margin = new Thickness(2) };
        panel.Children.Add(first);
        panel.Children.Add(second);
        panel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Assert.Equal(new Size(132, 15), panel.DesiredSize);
        panel.Arrange(new Rect(0, 0, 50, 40));
        Assert.Equal(0, LayoutInformation.GetLayoutSlot(first).X);
        Assert.Equal(80, first.ActualWidth);
        Assert.Equal(15, first.ActualHeight);
        Assert.Equal(88, LayoutInformation.GetLayoutSlot(second).X);
        Assert.Equal(40, second.ActualWidth);
        panel.Children.Clear();
        panel.Children.Add(new Border());
        Layout(panel, 0, 0);
        Assert.Equal(new Size(), panel.DesiredSize);
    });

    [Fact]
    public void Flex_RtlMirrorsHorizontalOrderWithoutReorderingChildren() => Sta.Run(() =>
    {
        var panel = new AntFlexPanel { FlowDirection = FlowDirection.RightToLeft, ColumnGap = 8, Height = 40 };
        var first = new Border { Width = 20, Height = 10 };
        var second = new Border { Width = 30, Height = 10 };
        panel.Children.Add(first);
        panel.Children.Add(second);
        var host = new Border { Child = panel };
        Window window = Sta.Show(host);
        try
        {
            double firstCenter = first.TranslatePoint(new Point(10, 5), host).X;
            double secondCenter = second.TranslatePoint(new Point(15, 5), host).X;
            Assert.Equal(33, firstCenter - secondCenter, 5);
            Assert.Same(first, panel.Children[0]);
            Assert.Equal(FlowDirection.RightToLeft, first.FlowDirection);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Flex_AlignmentOnlyChangesAvoidMeasureAndPreserveGapBinding() => Sta.Run(() =>
    {
        var source = new Border { Tag = 8.0 };
        var panel = new AntFlexPanel();
        BindingOperations.SetBinding(panel, AntFlexPanel.ColumnGapProperty, new Binding("Tag") { Source = source });
        var child = new Probe();
        panel.Children.Add(child);
        var next = new Border { Width = 10, Height = 10 };
        panel.Children.Add(next);
        Window window = Sta.Show(new AntThemeScope { Content = panel });
        try
        {
            int measured = child.Measures;
            panel.JustifyContent = FlexJustification.End;
            panel.AlignItems = FlexAlignment.Center;
            Sta.Pump();
            Assert.Equal(measured, child.Measures);
            Assert.Equal(8, panel.ColumnGap);
            double before = LayoutInformation.GetLayoutSlot(next).X - LayoutInformation.GetLayoutSlot(child).X;
            Assert.Equal(28, before);
            source.Tag = 16.0;
            Sta.Pump();
            Assert.Equal(16, panel.ColumnGap);
            Assert.Equal(36, LayoutInformation.GetLayoutSlot(next).X - LayoutInformation.GetLayoutSlot(child).X);
            Assert.True(BindingOperations.IsDataBound(panel, AntFlexPanel.ColumnGapProperty));
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Flex_ContentAndOrientationChangesRelayoutAndPreserveChildFocus() => Sta.Run(() =>
    {
        var panel = new AntFlexPanel { ColumnGap = 8, RowGap = 16, AlignItems = FlexAlignment.Start };
        var first = new AntButton { Content = "First", Width = 80 };
        var second = new AntButton { Content = "Second", Width = 80 };
        panel.Children.Add(first);
        panel.Children.Add(second);
        Window window = Sta.Show(new AntThemeScope { Content = panel });
        try
        {
            Assert.True(first.Focus());
            Assert.Equal(88, LayoutInformation.GetLayoutSlot(second).X);
            first.Width = 100;
            Sta.Pump();
            Assert.Equal(108, LayoutInformation.GetLayoutSlot(second).X);
            panel.Orientation = Orientation.Vertical;
            Sta.Pump();
            Assert.Equal(0, LayoutInformation.GetLayoutSlot(second).X);
            Assert.Equal(first.DesiredSize.Height + 16, LayoutInformation.GetLayoutSlot(second).Y);
            Assert.True(first.IsKeyboardFocused);
            Assert.Same(first, panel.Children[0]);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Flex_LayoutVisitsEachChildOnceWithoutAllocatingContainers() => Sta.Run(() =>
    {
        var panel = new AntFlexPanel { ColumnGap = 1 };
        for (int index = 0; index < 200; index++) { panel.Children.Add(new Probe()); }
        Layout(panel, 5000, 40);
        Assert.Equal(new Size(4199, 10), panel.DesiredSize);
        Assert.Equal(200, VisualTreeHelper.GetChildrenCount(panel));
        foreach (Probe child in panel.Children)
        {
            Assert.Equal(1, child.Measures);
            Assert.Equal(1, child.Arranges);
            Assert.Same(panel, VisualTreeHelper.GetParent(child));
        }
        panel.Children[100].Visibility = Visibility.Collapsed;
        Layout(panel, 5000, 40);
        Assert.Equal(4178, panel.DesiredSize.Width);
        Assert.Equal(200, VisualTreeHelper.GetChildrenCount(panel));
    });

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Flex_RejectsInvalidGaps(double value) => Sta.Run(() =>
    {
        var panel = new AntFlexPanel();
        Assert.Throws<ArgumentException>(() => panel.RowGap = value);
        Assert.Throws<ArgumentException>(() => panel.ColumnGap = value);
        Assert.Equal(0, panel.RowGap);
        Assert.Equal(0, panel.ColumnGap);
    });

    [Fact]
    public void Flex_RejectsUnknownEnumsAndHandlesEmptyUnboundedPanel() => Sta.Run(() =>
    {
        var panel = new AntFlexPanel();
        Assert.Throws<ArgumentException>(() => panel.Orientation = (Orientation)77);
        Assert.Throws<ArgumentException>(() => panel.AlignItems = (FlexAlignment)77);
        Assert.Throws<ArgumentException>(() => panel.JustifyContent = (FlexJustification)77);
        panel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Assert.Equal(new Size(), panel.DesiredSize);
        panel.ColumnGap = double.MaxValue;
        panel.Children.Add(new Border());
        panel.Children.Add(new Border());
        panel.Children.Add(new Border());
        panel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Assert.Equal(double.MaxValue, panel.DesiredSize.Width);
    });

    private static void Layout(AntFlexPanel panel, double width, double height)
    {
        panel.Measure(new Size(width, height));
        panel.Arrange(new Rect(0, 0, width, height));
    }

    private sealed class Probe : FrameworkElement
    {
        public int Measures { get; private set; }
        public int Arranges { get; private set; }
        protected override Size MeasureOverride(Size availableSize) { Measures++; return new Size(20, 10); }
        protected override Size ArrangeOverride(Size finalSize) { Arranges++; return finalSize; }
    }
}
