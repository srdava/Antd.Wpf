using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using Antd.Wpf.Controls;
using Antd.Wpf.Theming;
using Xunit;

namespace Antd.Wpf.Tests;

[Trait("Category", "UIIntegration")]
public sealed class SpaceTests
{
    [Fact]
    public void Space_DefaultGapAndHorizontalCenterHaveNoWrappers() => Sta.Run(() =>
    {
        var space = new AntSpace();
        var first = Box(20, 10);
        var second = Box(30, 20);
        space.Children.Add(first);
        space.Children.Add(second);
        Layout(space, 100, 40);
        Assert.Equal(8, space.Gap);
        Assert.Null(space.RowGap);
        Assert.Null(space.ColumnGap);
        Assert.Equal(new Thickness(0), space.Margin);
        Assert.Equal(new Size(58, 20), space.DesiredSize);
        Assert.Equal(new Rect(0, 15, 20, 10), Slot(first));
        Assert.Equal(new Rect(28, 10, 30, 20), Slot(second));
        Assert.Equal(2, VisualTreeHelper.GetChildrenCount(space));
        Assert.Same(first, VisualTreeHelper.GetChild(space, 0));
        Assert.False(space.Focusable);
    });

    [Theory]
    [InlineData(Orientation.Horizontal, SpaceAlignment.Start, 0)]
    [InlineData(Orientation.Horizontal, SpaceAlignment.Center, 15)]
    [InlineData(Orientation.Horizontal, SpaceAlignment.End, 30)]
    [InlineData(Orientation.Vertical, SpaceAlignment.Start, 0)]
    [InlineData(Orientation.Vertical, SpaceAlignment.Center, 10)]
    [InlineData(Orientation.Vertical, SpaceAlignment.End, 20)]
    public void Space_ExplicitCrossAlignmentPreservesNativeChildSize(Orientation orientation, SpaceAlignment alignment, double offset) => Sta.Run(() =>
    {
        var space = new AntSpace { Orientation = orientation, Alignment = alignment };
        var child = Box(20, 10);
        space.Children.Add(child);
        Layout(space, 40, 40);
        Assert.Equal(offset, orientation == Orientation.Horizontal ? Slot(child).Y : Slot(child).X);
        Assert.Equal(20, Slot(child).Width);
        Assert.Equal(10, Slot(child).Height);
        Assert.Equal(20, child.ActualWidth);
        Assert.Equal(10, child.ActualHeight);
    });

    [Fact]
    public void Space_VerticalAutoStretchesSlotsAndIgnoresWrap() => Sta.Run(() =>
    {
        var space = new AntSpace { Orientation = Orientation.Vertical, Wrap = true, RowGap = 16, ColumnGap = 99 };
        var first = Box(20, 10);
        var second = Box(30, 20);
        space.Children.Add(first);
        space.Children.Add(second);
        space.Measure(new Size(100, double.PositiveInfinity));
        space.Arrange(new Rect(0, 0, 100, 100));
        Assert.Equal(new Size(30, 46), space.DesiredSize);
        Assert.Equal(new Rect(0, 0, 100, 10), Slot(first));
        Assert.Equal(new Rect(0, 26, 100, 20), Slot(second));
        Assert.Equal(20, first.ActualWidth);
    });

    [Theory]
    [InlineData(58, 0, 26, 46)]
    [InlineData(57.99, 26, 52, 72)]
    [InlineData(120, 0, 0, 20)]
    public void Space_WrapHonorsExactBoundaryAndIndependentRowGap(double width, double secondY, double thirdY, double height) => Sta.Run(() =>
    {
        var space = new AntSpace { Wrap = true, ColumnGap = 8, RowGap = 16, Alignment = SpaceAlignment.Start };
        space.Children.Add(Box(20, 10));
        space.Children.Add(Box(30, 10));
        space.Children.Add(Box(40, 20));
        Layout(space, width, 100);
        Assert.Equal(secondY, Slot(space.Children[1]).Y);
        Assert.Equal(thirdY, Slot(space.Children[2]).Y);
        Assert.Equal(height, space.DesiredSize.Height);
        Assert.Equal(thirdY == 0 ? 66 : 0, Slot(space.Children[2]).X);
    });

    [Fact]
    public void Space_CollapsedHiddenRemovalAndZeroSizeKeepVisibleAdjacency() => Sta.Run(() =>
    {
        var space = new AntSpace { SeparatorTemplate = Separator() };
        var first = Box(20, 10);
        var middle = Box(30, 10);
        var last = Box(0, 0);
        middle.Visibility = Visibility.Collapsed;
        space.Children.Add(first);
        space.Children.Add(middle);
        space.Children.Add(last);
        Layout(space, 200, 40);
        Assert.Equal(38, Slot(last).X);
        Assert.Equal(4, VisualTreeHelper.GetChildrenCount(space));
        middle.Visibility = Visibility.Hidden;
        Layout(space, 200, 40);
        Assert.Equal(86, Slot(last).X);
        Assert.Equal(5, VisualTreeHelper.GetChildrenCount(space));
        space.Children.Remove(first);
        Layout(space, 200, 40);
        Assert.Equal(48, Slot(last).X);
        Assert.Equal(3, VisualTreeHelper.GetChildrenCount(space));
        middle.Visibility = Visibility.Collapsed;
        Layout(space, 200, 40);
        Assert.Equal(0, Slot(last).X);
        Assert.Equal(2, VisualTreeHelper.GetChildrenCount(space));
        space.Children.Clear();
        Layout(space, 200, 40);
        Assert.Equal(new Size(), space.DesiredSize);
        Assert.Equal(0, VisualTreeHelper.GetChildrenCount(space));
    });

    [Fact]
    public void Space_SeparatorsHaveTwoGapsAndDisappearAtWrapBoundary() => Sta.Run(() =>
    {
        var space = new AntSpace { Wrap = true, SeparatorTemplate = Separator(), RowGap = 16, Alignment = SpaceAlignment.Start };
        space.Children.Add(Box(20, 10));
        space.Children.Add(Box(30, 10));
        space.Children.Add(Box(40, 20));
        Layout(space, 68, 100);
        var firstSeparator = (ContentPresenter)VisualTreeHelper.GetChild(space, 3);
        var secondSeparator = (ContentPresenter)VisualTreeHelper.GetChild(space, 4);
        Assert.Equal(38, Slot(space.Children[1]).X);
        Assert.Equal(28, Slot(firstSeparator).X);
        Assert.Equal(Visibility.Visible, firstSeparator.Visibility);
        Assert.Equal(Visibility.Hidden, secondSeparator.Visibility);
        Assert.Equal(0, Slot(secondSeparator).Width);
        Assert.Equal(new Rect(0, 26, 40, 20), Slot(space.Children[2]));
        Layout(space, 200, 100);
        Assert.Equal(Visibility.Visible, secondSeparator.Visibility);
        Assert.Equal(86, Slot(space.Children[2]).X);
        Assert.Equal(5, VisualTreeHelper.GetChildrenCount(space));
        Assert.False(firstSeparator.IsHitTestVisible);
        Assert.Equal(KeyboardNavigationMode.None, KeyboardNavigation.GetTabNavigation(firstSeparator));
    });

    [Fact]
    public void Space_SeparatorTemplateReplacementDetachesOldVisualAndLogicalChildren() => Sta.Run(() =>
    {
        var space = new AntSpace { SeparatorTemplate = Separator() };
        space.Children.Add(Box(20, 10));
        space.Children.Add(Box(30, 10));
        Layout(space, 200, 40);
        var old = (ContentPresenter)VisualTreeHelper.GetChild(space, 2);
        space.SeparatorTemplate = Separator(6);
        Layout(space, 200, 40);
        Assert.Null(VisualTreeHelper.GetParent(old));
        Assert.Null(LogicalTreeHelper.GetParent(old));
        Assert.False(BindingOperations.IsDataBound(old, ContentPresenter.ContentProperty));
        Assert.Equal(42, Slot(space.Children[1]).X);
        space.SeparatorTemplate = null;
        Layout(space, 200, 40);
        Assert.Equal(2, VisualTreeHelper.GetChildrenCount(space));
        Assert.Equal(28, Slot(space.Children[1]).X);
    });

    [Fact]
    public void Space_SeparatorContextAndThemeUpdateWithoutReplacingContent() => Sta.Run(() =>
    {
        var space = new AntSpace { SeparatorTemplate = Separator(), DataContext = "First" };
        var first = new AntButton { Content = "First" };
        var second = new AntButton { Content = "Second" };
        space.Children.Add(first);
        space.Children.Add(second);
        var scope = new AntThemeScope { Content = space };
        Window window = Sta.Show(scope);
        try
        {
            var separator = (ContentPresenter)VisualTreeHelper.GetChild(space, 2);
            var border = (Border)VisualTreeHelper.GetChild(separator, 0);
            Color before = ((SolidColorBrush)border.Background).Color;
            Assert.Equal("First", separator.Content);
            Assert.True(first.Focus());
            space.DataContext = "Second";
            scope.Mode = ThemeMode.Dark;
            Sta.Pump();
            Assert.Equal("Second", separator.Content);
            Assert.NotEqual(before, ((SolidColorBrush)border.Background).Color);
            Assert.Same(first, space.Children[0]);
            Assert.Same(separator, VisualTreeHelper.GetChild(space, 2));
            Assert.True(first.IsKeyboardFocused);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Space_GapFallbackBindingAndContentChangesUpdatePositions() => Sta.Run(() =>
    {
        var source = new Border { Tag = 8.0 };
        var space = new AntSpace { ColumnGap = 16 };
        space.SetBinding(AntSpace.GapProperty, new Binding("Tag") { Source = source });
        var first = Box(20, 10);
        space.Children.Add(first);
        space.Children.Add(Box(30, 10));
        Window window = Sta.Show(space);
        try
        {
            Assert.Equal(36, Slot(space.Children[1]).X);
            source.Tag = 24.0;
            space.ColumnGap = null;
            Sta.Pump();
            Assert.Equal(44, Slot(space.Children[1]).X);
            first.Width = 40;
            Sta.Pump();
            Assert.Equal(64, Slot(space.Children[1]).X);
            Assert.True(BindingOperations.IsDataBound(space, AntSpace.GapProperty));
            space.Orientation = Orientation.Vertical;
            Sta.Pump();
            Assert.Equal(34, Slot(space.Children[1]).Y);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Space_RtlWrapMirrorsRowsWithoutReorderingChildren() => Sta.Run(() =>
    {
        var space = new AntSpace { Wrap = true, Width = 58, RowGap = 16, FlowDirection = FlowDirection.RightToLeft, Alignment = SpaceAlignment.Start };
        var first = Box(20, 10);
        var second = Box(30, 10);
        var third = Box(40, 10);
        space.Children.Add(first);
        space.Children.Add(second);
        space.Children.Add(third);
        var host = new Border { Child = space };
        Window window = Sta.Show(host);
        try
        {
            Assert.Equal(33, first.TranslatePoint(new Point(10, 5), host).X - second.TranslatePoint(new Point(15, 5), host).X, 5);
            Assert.Equal(26, Slot(third).Y);
            Assert.Equal(0, Slot(third).X);
            Assert.Same(first, space.Children[0]);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Space_UnboundedOversizedZeroAndEmptyLayoutsRemainDeterministic() => Sta.Run(() =>
    {
        var space = new AntSpace { Wrap = true };
        space.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Assert.Equal(new Size(), space.DesiredSize);
        space.Children.Add(Box(80, 10));
        space.Children.Add(Box(0, 0));
        space.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Assert.Equal(new Size(88, 10), space.DesiredSize);
        Layout(space, 0, 100);
        Assert.Equal(0, Slot(space.Children[0]).X);
        Assert.Equal(80, ((Border)space.Children[0]).ActualWidth);
        Assert.Equal(18, Slot(space.Children[1]).Y);
        space.Children.Clear();
        space.Children.Add(Box(0, 0));
        space.Children.Add(Box(0, 0));
        Layout(space, 0, 100);
        Assert.Equal(8, space.DesiredSize.Height);
        Assert.Equal(8, Slot(space.Children[1]).Y);
    });

    [Fact]
    public void Space_StableLayoutReusesBoundedSeparatorsAndAlignmentAvoidsMeasure() => Sta.Run(() =>
    {
        var space = new AntSpace { SeparatorTemplate = Separator(), Wrap = true };
        for (int index = 0; index < 200; index++) { space.Children.Add(new Probe()); }
        Window window = Sta.Show(space);
        try
        {
            Assert.Equal(399, VisualTreeHelper.GetChildrenCount(space));
            DependencyObject firstSeparator = VisualTreeHelper.GetChild(space, 200);
            var probe = (Probe)space.Children[0];
            int measures = probe.Measures;
            for (int index = 0; index < 30; index++)
            {
                space.Alignment = index % 2 == 0 ? SpaceAlignment.Start : SpaceAlignment.End;
                Sta.Pump();
            }
            Assert.Equal(measures, probe.Measures);
            Assert.Equal(399, VisualTreeHelper.GetChildrenCount(space));
            Assert.Same(firstSeparator, VisualTreeHelper.GetChild(space, 200));
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Space_InvalidGapInputsAreRejected(double value) => Sta.Run(() =>
    {
        var space = new AntSpace();
        Assert.Throws<ArgumentException>(() => space.Gap = value);
        Assert.Throws<ArgumentException>(() => space.RowGap = value);
        Assert.Throws<ArgumentException>(() => space.ColumnGap = value);
    });

    [Fact]
    public void Space_UnknownEnumsAreRejectedAndZeroGapsAreAllowed() => Sta.Run(() =>
    {
        var space = new AntSpace { Gap = 0, RowGap = 0, ColumnGap = 0 };
        Assert.Throws<ArgumentException>(() => space.Alignment = (SpaceAlignment)42);
        Assert.Throws<ArgumentException>(() => space.Orientation = (Orientation)42);
        space.Children.Add(Box(20, 10));
        space.Children.Add(Box(30, 10));
        Layout(space, 100, 40);
        Assert.Equal(20, Slot(space.Children[1]).X);
    });

    private static Border Box(double width, double height) => new() { Width = width, Height = height };
    private static Rect Slot(UIElement element) => LayoutInformation.GetLayoutSlot((FrameworkElement)element);
    private static DataTemplate Separator(int width = 2) => (DataTemplate)XamlReader.Parse(
        "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Border Width='" + width +
        "' Height='10' Background='{DynamicResource Antd.Brush.Container}' /></DataTemplate>");
    private static void Layout(AntSpace space, double width, double height)
    {
        space.Measure(new Size(width, height));
        space.Arrange(new Rect(0, 0, width, height));
    }
    private sealed class Probe : FrameworkElement
    {
        public int Measures { get; private set; }
        protected override Size MeasureOverride(Size availableSize) { Measures++; return new Size(20, 10); }
    }
}
