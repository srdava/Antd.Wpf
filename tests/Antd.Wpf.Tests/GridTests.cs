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
public sealed class GridTests
{
    [Fact]
    public void Grid_SpansOffsetsAndWrapUse24Tracks() => Sta.Run(() =>
    {
        var row = new AntRow();
        for (int index = 0; index < 3; index++) { row.Children.Add(Col(8)); }
        Layout(row, 240);
        Assert.True(row.Wrap);
        Assert.Equal(new Thickness(0), row.Margin);
        Assert.Equal(new Rect(0, 0, 80, 10), Slot(row.Children[0]));
        Assert.Equal(new Rect(80, 0, 80, 10), Slot(row.Children[1]));
        Assert.Equal(new Rect(160, 0, 80, 10), Slot(row.Children[2]));
        row.Children.Clear();
        row.Children.Add(Col(12));
        row.Children.Add(Col(16));
        row.VerticalGutter = 6;
        Layout(row, 240);
        Assert.Equal(new Size(240, 26), row.DesiredSize);
        Assert.Equal(new Rect(0, 16, 160, 10), Slot(row.Children[1]));
        ((AntCol)row.Children[1]).Span = 13;
        Layout(row, 240);
        Assert.Equal(16, Slot(row.Children[1]).Y);
        ((AntCol)row.Children[1]).Span = 12;
        Layout(row, 240);
        Assert.Equal(new Rect(120, 0, 120, 10), Slot(row.Children[1]));
        ((AntCol)row.Children[1]).Span = 16;
        row.Wrap = false;
        Layout(row, 240);
        Assert.Equal(new Rect(120, 0, 160, 10), Slot(row.Children[1]));
        row.Children.Clear();
        row.Children.Add(new AntCol { Span = 6, Offset = 6, Child = new Border { Height = 10 } });
        row.Children.Add(Col(12));
        Layout(row, 240);
        Assert.Equal(60, Slot(row.Children[0]).X);
        Assert.Equal(120, Slot(row.Children[1]).X);
        row.Wrap = true;
        row.Justify = RowJustification.Center;
        Layout(row, 240);
        Assert.Equal(60, Slot(row.Children[0]).X);
        Assert.Equal(120, Slot(row.Children[1]).X);
        row.Children.Add(Col(1));
        Layout(row, 240);
        Assert.Equal(16, Slot(row.Children[2]).Y);
    });

    [Fact]
    public void Grid_GutterUsesOfficialNegativeRowAndColumnHalfPadding() => Sta.Run(() =>
    {
        var row = new AntRow { HorizontalGutter = 16 };
        for (int index = 0; index < 3; index++) { row.Children.Add(Col(8)); }
        Layout(row, 224);
        for (int index = 0; index < 3; index++)
        {
            var col = (AntCol)row.Children[index];
            Assert.Equal(new Rect(-8 + index * 80, 0, 80, 10), Slot(col));
            Assert.Equal(new Rect(8, 0, 64, 10), Slot(col.Child));
            Assert.Equal(64, ((Border)col.Child).ActualWidth);
            Assert.Equal(index * 80, col.Child.TranslatePoint(new Point(), row).X, 5);
            Assert.Equal(new Thickness(0), col.Margin);
        }
        Assert.Equal(224, row.RenderSize.Width);
        Assert.Equal(224, ((AntCol)row.Children[2]).Child.TranslatePoint(new Point(64, 0), row).X, 5);
        Assert.Equal(new Thickness(0), row.Margin);
    });

    [Theory]
    [InlineData(RowJustification.Start, 0, 40)]
    [InlineData(RowJustification.Center, 80, 120)]
    [InlineData(RowJustification.End, 160, 200)]
    [InlineData(RowJustification.SpaceBetween, 0, 200)]
    [InlineData(RowJustification.SpaceAround, 40, 160)]
    [InlineData(RowJustification.SpaceEvenly, 53.333333333333, 146.666666666667)]
    public void Grid_JustificationDistributesOnlyFreeTracks(RowJustification justify, double first, double second) => Sta.Run(() =>
    {
        var row = new AntRow { Justify = justify };
        row.Children.Add(Col(4));
        row.Children.Add(Col(4));
        Layout(row, 240);
        Assert.Equal(first, Slot(row.Children[0]).X, 5);
        Assert.Equal(second, Slot(row.Children[1]).X, 5);
        Assert.Equal(40, Slot(row.Children[0]).Width);
        row.Children.Clear();
        row.Children.Add(Col(4));
        Layout(row, 240);
        double single = justify == RowJustification.Start || justify == RowJustification.SpaceBetween ? 0 :
            justify == RowJustification.End ? 200 : 100;
        Assert.Equal(single, Slot(row.Children[0]).X, 5);
    });

    [Theory]
    [InlineData(RowAlignment.Top, 0, 0, 10, 0, 49, 10)]
    [InlineData(RowAlignment.Middle, 45, 40, 10, 15.5, 64.5, 10)]
    [InlineData(RowAlignment.Bottom, 90, 80, 10, 31, 80, 10)]
    [InlineData(RowAlignment.Stretch, 0, 0, 100, 0, 49, 41)]
    public void Grid_AlignUsesNaturalLineHeight(RowAlignment align, double firstY, double secondY, double height,
        double wrappedFirstY, double wrappedSecondY, double wrappedHeight) => Sta.Run(() =>
    {
        var row = new AntRow { Align = align };
        var first = Col(12, 10);
        var second = Col(12, 20);
        row.Children.Add(first);
        row.Children.Add(second);
        row.Measure(new Size(240, double.PositiveInfinity));
        Assert.Equal(20, row.DesiredSize.Height);
        row.Arrange(new Rect(0, 0, 240, 100));
        Assert.Equal(firstY, Slot(first).Y);
        Assert.Equal(secondY, Slot(second).Y);
        Assert.Equal(height, Slot(first).Height);
        Assert.Equal(10, ((Border)first.Child).ActualHeight);
        Assert.Equal(20, ((Border)second.Child).ActualHeight);
        second.Span = 24;
        row.VerticalGutter = 8;
        row.Measure(new Size(240, double.PositiveInfinity));
        Assert.Equal(38, row.DesiredSize.Height);
        row.Arrange(new Rect(0, 0, 240, 100));
        Assert.Equal(wrappedFirstY, Slot(first).Y);
        Assert.Equal(wrappedSecondY, Slot(second).Y);
        Assert.Equal(wrappedHeight, Slot(first).Height);
        Assert.Equal(align == RowAlignment.Stretch ? 51 : 20, Slot(second).Height);
    });

    [Fact]
    public void Col_ZeroSpanCoercesVisibilityWithoutMeasuringOrLosingBindings() => Sta.Run(() =>
    {
        var source = new Border { Tag = 0, Visibility = Visibility.Visible };
        var probe = new Probe();
        var col = new AntCol { Child = probe, Offset = 12 };
        col.SetBinding(AntCol.SpanProperty, new Binding("Tag") { Source = source });
        col.SetBinding(UIElement.VisibilityProperty, new Binding("Visibility") { Source = source });
        var row = new AntRow { VerticalAlignment = VerticalAlignment.Top };
        row.Children.Add(col);
        row.Children.Add(Col(24));
        Window window = Sta.Show(row);
        try
        {
            Assert.Equal(Visibility.Collapsed, col.Visibility);
            Assert.Equal(Visibility.Visible, source.Visibility);
            Assert.Equal(0, probe.Measures);
            Assert.Equal(0, Slot(row.Children[1]).X);
            source.Tag = 12;
            Sta.Pump();
            Assert.Equal(Visibility.Visible, col.Visibility);
            Assert.True(probe.Measures > 0);
            Assert.Same(probe, col.Child);
            source.Visibility = Visibility.Hidden;
            source.Tag = 0;
            Sta.Pump();
            Assert.Equal(Visibility.Collapsed, col.Visibility);
            source.Tag = 12;
            Sta.Pump();
            Assert.Equal(Visibility.Hidden, col.Visibility);
            Assert.Equal(10, Slot(row.Children[1]).Y);
            Assert.True(BindingOperations.IsDataBound(col, AntCol.SpanProperty));
            Assert.True(BindingOperations.IsDataBound(col, UIElement.VisibilityProperty));
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Col_ReparentingClearsOnlyOwnedGutter() => Sta.Run(() =>
    {
        var source = new Border { Tag = 100.0, Margin = new Thickness(2) };
        var col = Col(24);
        col.SetBinding(FrameworkElement.WidthProperty, new Binding("Tag") { Source = source });
        col.SetBinding(FrameworkElement.MarginProperty, new Binding("Margin") { Source = source });
        var row = new AntRow { HorizontalGutter = 16 };
        row.Children.Add(col);
        Layout(row, 224);
        Assert.Equal(8, Slot(col.Child).X);
        Assert.Equal(84, Slot(col.Child).Width);
        row.Children.Remove(col);
        var host = new Border { Child = col };
        host.Measure(new Size(200, 40));
        host.Arrange(new Rect(0, 0, 200, 40));
        Assert.Equal(0, Slot(col.Child).X);
        Assert.Equal(100, Slot(col.Child).Width);
        Assert.Equal(new Thickness(2), col.Margin);
        Assert.True(BindingOperations.IsDataBound(col, FrameworkElement.WidthProperty));
        Assert.True(BindingOperations.IsDataBound(col, FrameworkElement.MarginProperty));
    });

    [Fact]
    public void Grid_RtlMirrorsOffsetAndGutter() => Sta.Run(() =>
    {
        var row = new AntRow { Width = 240, HorizontalGutter = 16, FlowDirection = FlowDirection.RightToLeft };
        var first = Col(6);
        first.Offset = 6;
        var second = Col(12);
        row.Children.Add(first);
        row.Children.Add(second);
        var host = new Border { Child = row };
        Window window = Sta.Show(host);
        try
        {
            double firstCenter = first.Child.TranslatePoint(new Point(((Border)first.Child).ActualWidth / 2, 5), host).X;
            double secondCenter = second.Child.TranslatePoint(new Point(((Border)second.Child).ActualWidth / 2, 5), host).X;
            Assert.Equal(96, firstCenter - secondCenter, 5);
            Assert.Equal(56, Slot(first).X, 5);
            Assert.Same(first, row.Children[0]);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Grid_UnboundedAndZeroLayoutsRemainFinite() => Sta.Run(() =>
    {
        var row = new AntRow { HorizontalGutter = 16 };
        row.Children.Add(new AntCol { Span = 12, Child = new Border { Width = 40, Height = 10 } });
        row.Children.Add(new AntCol { Span = 12, Child = new Border { Width = 80, Height = 20 } });
        row.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Assert.Equal(new Size(176, 20), row.DesiredSize);
        row.Children.Clear();
        row.HorizontalGutter = 0;
        Layout(row, 0);
        Assert.Equal(new Size(), row.DesiredSize);
        var col = new AntCol();
        row.Children.Add(col);
        Layout(row, 0);
        Assert.Equal(24, col.Span);
        Assert.Equal(1, row.DesiredSize.Height);
        Assert.Equal(0, Slot(col).Width);
        col.Offset = 24;
        Layout(row, 240);
        Assert.Equal(240, Slot(col).X);
        Assert.Equal(240, Slot(col).Width);
    });

    [Fact]
    public void Grid_ResizeAndGutterBindingsPreserveContentAndFocus() => Sta.Run(() =>
    {
        var source = new Border { Tag = 16.0 };
        var input = new AntInput { Text = "保留内容" };
        var col = new AntCol { Span = 12, Child = input };
        var row = new AntRow { Width = 240 };
        row.SetBinding(AntRow.HorizontalGutterProperty, new Binding("Tag") { Source = source });
        row.Children.Add(col);
        row.Children.Add(Col(12));
        var scope = new AntThemeScope { Content = row };
        Window window = Sta.Show(scope);
        try
        {
            Assert.True(input.Focus());
            Assert.Equal(112, Slot(input).Width);
            row.Width = 480;
            source.Tag = 24.0;
            scope.Mode = ThemeMode.Dark;
            Sta.Pump();
            Assert.Equal(228, Slot(input).Width);
            Assert.Equal(-12, Slot(col).X);
            Assert.Equal("保留内容", input.Text);
            Assert.True(input.IsKeyboardFocused);
            Assert.Same(input, col.Child);
            Assert.True(BindingOperations.IsDataBound(row, AntRow.HorizontalGutterProperty));
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Grid_TextWrappingRecomputesLineHeightsAfterResize() => Sta.Run(() =>
    {
        var text = new TextBlock { Text = new string('W', 30), TextWrapping = TextWrapping.Wrap, FontSize = 14, LineHeight = 20 };
        var col = new AntCol { Span = 12, Child = text };
        var row = new AntRow { Width = 240, HorizontalGutter = 16, VerticalGutter = 8, VerticalAlignment = VerticalAlignment.Top };
        row.Children.Add(col);
        var next = Col(24);
        row.Children.Add(next);
        Window window = Sta.Show(row);
        try
        {
            double before = text.ActualHeight;
            Assert.True(before > 20);
            Assert.Equal(col.DesiredSize.Height + 8, Slot(next).Y);
            row.Width = 480;
            Sta.Pump();
            Assert.True(text.ActualHeight < before);
            Assert.Equal(col.DesiredSize.Height + 8, Slot(next).Y);
            Assert.Equal(new string('W', 30), text.Text);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Grid_LinearLayoutKeepsDirectColumnsAndAvoidsRemeasure() => Sta.Run(() =>
    {
        var row = new MeasuredRow { Width = 240 };
        for (int index = 0; index < 200; index++) { row.Children.Add(new AntCol { Span = 1, Child = new Probe() }); }
        Window window = Sta.Show(row);
        try
        {
            Assert.Equal(90, row.DesiredSize.Height);
            Assert.Equal(200, VisualTreeHelper.GetChildrenCount(row));
            var measurements = new int[200];
            int rowMeasures = row.Measures;
            for (int index = 0; index < 200; index++)
            {
                var col = (AntCol)row.Children[index];
                var probe = (Probe)col.Child;
                Assert.Same(row, VisualTreeHelper.GetParent(col));
                Assert.Same(probe, VisualTreeHelper.GetChild(col, 0));
                Assert.Equal(1, VisualTreeHelper.GetChildrenCount(col));
                Assert.Equal(1, probe.Measures);
                measurements[index] = probe.Measures;
            }
            for (int index = 0; index < 30; index++)
            {
                row.Align = index % 2 == 0 ? RowAlignment.Middle : RowAlignment.Bottom;
                row.Justify = index % 2 == 0 ? RowJustification.Center : RowJustification.End;
                Sta.Pump();
            }
            for (int index = 0; index < 200; index++)
            {
                Assert.Equal(measurements[index], ((Probe)((AntCol)row.Children[index]).Child).Measures);
            }
            Assert.Equal(200, VisualTreeHelper.GetChildrenCount(row));
            Assert.Equal(rowMeasures, row.Measures);
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(-1)]
    [InlineData(25)]
    public void Col_InvalidTrackCountsAreRejected(int value) => Sta.Run(() =>
    {
        var col = new AntCol();
        Assert.Throws<ArgumentException>(() => col.Span = value);
        Assert.Throws<ArgumentException>(() => col.Offset = value);
        Assert.Equal(24, col.Span);
        Assert.Equal(0, col.Offset);
    });

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Grid_InvalidGuttersAreRejected(double value) => Sta.Run(() =>
    {
        var row = new AntRow();
        Assert.Throws<ArgumentException>(() => row.HorizontalGutter = value);
        Assert.Throws<ArgumentException>(() => row.VerticalGutter = value);
    });

    [Fact]
    public void Grid_InvalidParametersAndNonColumnChildrenAreRejected() => Sta.Run(() =>
    {
        var row = new AntRow();
        Assert.Throws<ArgumentException>(() => row.Align = (RowAlignment)99);
        Assert.Throws<ArgumentException>(() => row.Justify = (RowJustification)99);
        row.Children.Add(new Border());
        var exception = Assert.Throws<InvalidOperationException>(() => row.Measure(new Size(240, 40)));
        Assert.Contains("AntCol", exception.Message);
    });

    private static AntCol Col(int span, double height = 10) => new() { Span = span, Child = new Border { Height = height } };
    private static Rect Slot(UIElement element) => LayoutInformation.GetLayoutSlot((FrameworkElement)element);
    private static void Layout(AntRow row, double width)
    {
        row.Measure(new Size(width, double.PositiveInfinity));
        row.Arrange(new Rect(0, 0, width, row.DesiredSize.Height));
    }
    private sealed class Probe : FrameworkElement
    {
        public int Measures { get; private set; }
        protected override Size MeasureOverride(Size availableSize) { Measures++; return new Size(20, 10); }
    }
    private sealed class MeasuredRow : AntRow
    {
        public int Measures { get; private set; }
        protected override Size MeasureOverride(Size availableSize) { Measures++; return base.MeasureOverride(availableSize); }
    }
}
