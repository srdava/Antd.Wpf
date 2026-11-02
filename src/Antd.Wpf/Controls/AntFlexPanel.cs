using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Antd.Wpf.Controls;

public enum FlexJustification { Start, Center, End, SpaceBetween, SpaceAround, SpaceEvenly }
public enum FlexAlignment { Start, Center, End, Stretch }

/// <summary>A layout with optional wrapping and explicit gaps; children retain their native size constraints.</summary>
public class AntFlexPanel : Panel
{
    private readonly List<Line> _lines = new();

    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation), typeof(Orientation), typeof(AntFlexPanel),
        new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsMeasure),
        value => Enum.IsDefined(typeof(Orientation), value));
    public static readonly DependencyProperty JustifyContentProperty = DependencyProperty.Register(
        nameof(JustifyContent), typeof(FlexJustification), typeof(AntFlexPanel),
        new FrameworkPropertyMetadata(FlexJustification.Start, FrameworkPropertyMetadataOptions.AffectsArrange),
        value => Enum.IsDefined(typeof(FlexJustification), value));
    public static readonly DependencyProperty AlignItemsProperty = DependencyProperty.Register(
        nameof(AlignItems), typeof(FlexAlignment), typeof(AntFlexPanel),
        new FrameworkPropertyMetadata(FlexAlignment.Stretch, FrameworkPropertyMetadataOptions.AffectsArrange),
        value => Enum.IsDefined(typeof(FlexAlignment), value));
    public static readonly DependencyProperty RowGapProperty = DependencyProperty.Register(
        nameof(RowGap), typeof(double), typeof(AntFlexPanel),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure), IsGap);
    public static readonly DependencyProperty ColumnGapProperty = DependencyProperty.Register(
        nameof(ColumnGap), typeof(double), typeof(AntFlexPanel),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure), IsGap);
    public static readonly DependencyProperty WrapProperty = DependencyProperty.Register(
        nameof(Wrap), typeof(bool), typeof(AntFlexPanel),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public FlexJustification JustifyContent { get => (FlexJustification)GetValue(JustifyContentProperty); set => SetValue(JustifyContentProperty, value); }
    public FlexAlignment AlignItems { get => (FlexAlignment)GetValue(AlignItemsProperty); set => SetValue(AlignItemsProperty, value); }
    public double RowGap { get => (double)GetValue(RowGapProperty); set => SetValue(RowGapProperty, value); }
    public double ColumnGap { get => (double)GetValue(ColumnGapProperty); set => SetValue(ColumnGapProperty, value); }
    public bool Wrap { get => (bool)GetValue(WrapProperty); set => SetValue(WrapProperty, value); }

    protected override Size MeasureOverride(Size availableSize)
    {
        bool horizontal = Orientation == Orientation.Horizontal;
        var constraint = horizontal ? new Size(double.PositiveInfinity, availableSize.Height) :
            new Size(availableSize.Width, double.PositiveInfinity);
        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility != Visibility.Collapsed) { child.Measure(constraint); }
        }

        BuildLines(Main(availableSize, horizontal), horizontal);
        double main = 0;
        double cross = 0;
        double crossGap = horizontal ? RowGap : ColumnGap;
        for (int index = 0; index < _lines.Count; index++)
        {
            Line line = _lines[index];
            main = Math.Max(main, line.Main);
            if (index > 0) { cross = Add(cross, crossGap); }
            cross = Add(cross, line.Cross);
        }

        return horizontal ? new Size(main, cross) : new Size(cross, main);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        bool horizontal = Orientation == Orientation.Horizontal;
        BuildLines(Main(finalSize, horizontal), horizontal);
        double crossGap = horizontal ? RowGap : ColumnGap;
        double usedCross = 0;
        for (int index = 0; index < _lines.Count; index++)
        {
            if (index > 0) { usedCross = Add(usedCross, crossGap); }
            usedCross = Add(usedCross, _lines[index].Cross);
        }

        // CSS align-content: normal stretches line slots when the cross axis has spare space.
        double extraCross = _lines.Count == 0 ? 0 : Math.Max(0, Cross(finalSize, horizontal) - usedCross) / _lines.Count;
        double crossPosition = 0;
        foreach (Line line in _lines)
        {
            double cross = Add(line.Cross, extraCross);
            ArrangeLine(line, Main(finalSize, horizontal), cross, crossPosition, horizontal);
            crossPosition = Add(Add(crossPosition, cross), crossGap);
        }

        return finalSize;
    }

    private void BuildLines(double limit, bool horizontal)
    {
        _lines.Clear();
        double gap = horizontal ? ColumnGap : RowGap;
        double main = 0;
        double cross = 0;
        int start = 0;
        int count = 0;
        for (int index = 0; index < InternalChildren.Count; index++)
        {
            UIElement child = InternalChildren[index];
            if (child.Visibility == Visibility.Collapsed) { continue; }
            double length = Main(child.DesiredSize, horizontal);
            double next = count == 0 ? length : Add(Add(main, gap), length);
            if (Wrap && count > 0 && next > limit)
            {
                _lines.Add(new Line(start, index, count, main, cross));
                start = index;
                main = 0;
                cross = 0;
                count = 0;
            }

            if (count > 0) { main = Add(main, gap); }
            main = Add(main, length);
            cross = Math.Max(cross, Cross(child.DesiredSize, horizontal));
            count++;
        }
        if (count > 0) { _lines.Add(new Line(start, InternalChildren.Count, count, main, cross)); }
    }

    private void ArrangeLine(Line line, double availableMain, double cross, double crossPosition, bool horizontal)
    {
        double remaining = Math.Max(0, availableMain - line.Main);
        double gap = horizontal ? ColumnGap : RowGap;
        double position = 0;
        switch (JustifyContent)
        {
            case FlexJustification.Center: position = remaining / 2; break;
            case FlexJustification.End: position = remaining; break;
            case FlexJustification.SpaceBetween:
                if (line.Count > 1) { gap = Add(gap, remaining / (line.Count - 1)); }
                break;
            case FlexJustification.SpaceAround:
                double around = remaining / line.Count;
                position = around / 2;
                gap = Add(gap, around);
                break;
            case FlexJustification.SpaceEvenly:
                double evenly = remaining / (line.Count + 1.0);
                position = evenly;
                gap = Add(gap, evenly);
                break;
        }

        bool hasItem = false;
        for (int index = line.Start; index < line.End; index++)
        {
            UIElement child = InternalChildren[index];
            if (child.Visibility == Visibility.Collapsed)
            {
                child.Arrange(new Rect());
                continue;
            }

            if (hasItem) { position = Add(position, gap); }
            double length = horizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
            double breadth = horizontal ? child.DesiredSize.Height : child.DesiredSize.Width;
            double spare = Math.Max(0, cross - breadth);
            double offset = AlignItems == FlexAlignment.Center ? spare / 2 : AlignItems == FlexAlignment.End ? spare : 0;
            if (AlignItems == FlexAlignment.Stretch) { breadth = Math.Max(cross, breadth); }
            child.Arrange(horizontal ? new Rect(position, Add(crossPosition, offset), length, breadth) :
                new Rect(Add(crossPosition, offset), position, breadth, length));
            position = Add(position, length);
            hasItem = true;
        }
    }

    private static double Main(Size size, bool horizontal) => horizontal ? size.Width : size.Height;
    private static double Cross(Size size, bool horizontal) => horizontal ? size.Height : size.Width;
    private static bool IsGap(object value) => (double)value >= 0 && !double.IsNaN((double)value) && !double.IsInfinity((double)value);
    private static double Add(double first, double second) => Math.Min(double.MaxValue, first + second);

    private readonly struct Line
    {
        public Line(int start, int end, int count, double main, double cross)
        { Start = start; End = end; Count = count; Main = main; Cross = cross; }
        public int Start { get; }
        public int End { get; }
        public int Count { get; }
        public double Main { get; }
        public double Cross { get; }
    }
}
