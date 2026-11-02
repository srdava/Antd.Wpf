using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Antd.Wpf.Controls;

public enum RowJustification { Start, Center, End, SpaceBetween, SpaceAround, SpaceEvenly }
public enum RowAlignment { Top, Middle, Bottom, Stretch }

/// <summary>A 24-track grid with native column content and source-aligned half gutters.</summary>
public class AntRow : Panel
{
    private readonly List<Line> _lines = new();
    public static readonly DependencyProperty HorizontalGutterProperty = DependencyProperty.Register(
        nameof(HorizontalGutter), typeof(double), typeof(AntRow),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure), IsGutter);
    public static readonly DependencyProperty VerticalGutterProperty = DependencyProperty.Register(
        nameof(VerticalGutter), typeof(double), typeof(AntRow),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure), IsGutter);
    public static readonly DependencyProperty WrapProperty = DependencyProperty.Register(
        nameof(Wrap), typeof(bool), typeof(AntRow), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty JustifyProperty = DependencyProperty.Register(
        nameof(Justify), typeof(RowJustification), typeof(AntRow),
        new FrameworkPropertyMetadata(RowJustification.Start, FrameworkPropertyMetadataOptions.AffectsArrange),
        value => Enum.IsDefined(typeof(RowJustification), value));
    public static readonly DependencyProperty AlignProperty = DependencyProperty.Register(
        nameof(Align), typeof(RowAlignment), typeof(AntRow),
        new FrameworkPropertyMetadata(RowAlignment.Top, FrameworkPropertyMetadataOptions.AffectsArrange),
        value => Enum.IsDefined(typeof(RowAlignment), value));

    public double HorizontalGutter { get => (double)GetValue(HorizontalGutterProperty); set => SetValue(HorizontalGutterProperty, value); }
    public double VerticalGutter { get => (double)GetValue(VerticalGutterProperty); set => SetValue(VerticalGutterProperty, value); }
    public bool Wrap { get => (bool)GetValue(WrapProperty); set => SetValue(WrapProperty, value); }
    public RowJustification Justify { get => (RowJustification)GetValue(JustifyProperty); set => SetValue(JustifyProperty, value); }
    public RowAlignment Align { get => (RowAlignment)GetValue(AlignProperty); set => SetValue(AlignProperty, value); }

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = availableSize.Width;
        double naturalTrack = 0;
        foreach (UIElement child in InternalChildren)
        {
            AntCol col = Column(child);
            col.SetGutter(HorizontalGutter);
            if (col.Visibility == Visibility.Collapsed) { continue; }
            if (double.IsPositiveInfinity(width))
            {
                col.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                naturalTrack = Math.Max(naturalTrack, col.DesiredSize.Width / col.Span);
            }
        }
        if (double.IsPositiveInfinity(width)) { width = Math.Max(0, Math.Min(double.MaxValue, naturalTrack * 24) - HorizontalGutter); }
        double track = Add(width, HorizontalGutter) / 24;
        foreach (UIElement child in InternalChildren)
        {
            AntCol col = Column(child);
            if (col.Visibility != Visibility.Collapsed) { col.Measure(new Size(track * col.Span, double.PositiveInfinity)); }
        }
        BuildLines();
        return new Size(width, NaturalHeight());
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        BuildLines();
        double track = Add(finalSize.Width, HorizontalGutter) / 24;
        double extraHeight = _lines.Count == 0 ? 0 : Math.Max(0, finalSize.Height - NaturalHeight()) / _lines.Count;
        double y = 0;
        foreach (Line line in _lines)
        {
            double free = Math.Max(0, 24 - line.Units) * track;
            double between = Justify == RowJustification.SpaceBetween && line.Count > 1 ? free / (line.Count - 1) :
                Justify == RowJustification.SpaceAround ? free / line.Count :
                Justify == RowJustification.SpaceEvenly ? free / (line.Count + 1) : 0;
            double leading = Justify == RowJustification.Center ? free / 2 : Justify == RowJustification.End ? free :
                Justify == RowJustification.SpaceAround ? between / 2 : Justify == RowJustification.SpaceEvenly ? between : 0;
            double x = leading - HorizontalGutter / 2;
            double height = Add(line.Height, extraHeight);
            for (int index = line.Start; index <= line.End; index++)
            {
                AntCol col = Column(InternalChildren[index]);
                if (col.Visibility == Visibility.Collapsed) { col.Arrange(new Rect()); continue; }
                x = Add(x, col.Offset * track);
                double spare = Math.Max(0, height - col.DesiredSize.Height);
                double offset = Align == RowAlignment.Middle ? spare / 2 : Align == RowAlignment.Bottom ? spare : 0;
                double colHeight = Align == RowAlignment.Stretch ? height : col.DesiredSize.Height;
                col.Arrange(new Rect(x, Add(y, offset), col.Span * track, colHeight));
                x = Add(Add(x, col.Span * track), between);
            }
            y = Add(Add(y, height), VerticalGutter);
        }
        return finalSize;
    }

    private void BuildLines()
    {
        _lines.Clear();
        int start = 0;
        long units = 0;
        double height = 0;
        int count = 0;
        for (int index = 0; index < InternalChildren.Count; index++)
        {
            AntCol col = Column(InternalChildren[index]);
            if (col.Visibility == Visibility.Collapsed) { continue; }
            if (Wrap && count > 0 && units + col.Offset + col.Span > 24)
            {
                _lines.Add(new Line(start, index - 1, units, height, count));
                start = index;
                units = 0;
                height = 0;
                count = 0;
            }
            units += col.Offset + col.Span;
            height = Math.Max(height, col.DesiredSize.Height);
            count++;
        }
        if (count > 0) { _lines.Add(new Line(start, InternalChildren.Count - 1, units, height, count)); }
    }

    private double NaturalHeight()
    {
        double height = 0;
        for (int index = 0; index < _lines.Count; index++)
        {
            if (index > 0) { height = Add(height, VerticalGutter); }
            height = Add(height, _lines[index].Height);
        }
        return height;
    }

    private static AntCol Column(UIElement child) => child as AntCol ?? throw new InvalidOperationException("AntRow children must be AntCol elements.");
    private static bool IsGutter(object value) => (double)value >= 0 && !double.IsNaN((double)value) && !double.IsInfinity((double)value);
    private static double Add(double first, double second) => Math.Min(double.MaxValue, first + second);

    private readonly struct Line
    {
        public Line(int start, int end, long units, double height, int count) { Start = start; End = end; Units = units; Height = height; Count = count; }
        public int Start { get; }
        public int End { get; }
        public long Units { get; }
        public double Height { get; }
        public int Count { get; }
    }
}
