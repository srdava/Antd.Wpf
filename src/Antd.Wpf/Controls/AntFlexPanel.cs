using System;
using System.Windows;
using System.Windows.Controls;

namespace Antd.Wpf.Controls;

public enum FlexJustification { Start, Center, End }
public enum FlexAlignment { Start, Center, End, Stretch }

/// <summary>A nonwrapping layout with explicit gaps; children retain their native size constraints.</summary>
public class AntFlexPanel : Panel
{
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

    public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public FlexJustification JustifyContent { get => (FlexJustification)GetValue(JustifyContentProperty); set => SetValue(JustifyContentProperty, value); }
    public FlexAlignment AlignItems { get => (FlexAlignment)GetValue(AlignItemsProperty); set => SetValue(AlignItemsProperty, value); }
    public double RowGap { get => (double)GetValue(RowGapProperty); set => SetValue(RowGapProperty, value); }
    public double ColumnGap { get => (double)GetValue(ColumnGapProperty); set => SetValue(ColumnGapProperty, value); }

    protected override Size MeasureOverride(Size availableSize)
    {
        bool horizontal = Orientation == Orientation.Horizontal;
        double gap = horizontal ? ColumnGap : RowGap;
        var constraint = horizontal ? new Size(double.PositiveInfinity, availableSize.Height) :
            new Size(availableSize.Width, double.PositiveInfinity);
        double main = 0;
        double cross = 0;
        bool hasItem = false;
        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility == Visibility.Collapsed) { continue; }
            child.Measure(constraint);
            Size desired = child.DesiredSize;
            if (hasItem) { main = Add(main, gap); }
            main = Add(main, horizontal ? desired.Width : desired.Height);
            cross = Math.Max(cross, horizontal ? desired.Height : desired.Width);
            hasItem = true;
        }

        return horizontal ? new Size(main, cross) : new Size(cross, main);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        bool horizontal = Orientation == Orientation.Horizontal;
        double gap = horizontal ? ColumnGap : RowGap;
        double main = 0;
        bool hasItem = false;
        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility == Visibility.Collapsed) { continue; }
            if (hasItem) { main = Add(main, gap); }
            main = Add(main, horizontal ? child.DesiredSize.Width : child.DesiredSize.Height);
            hasItem = true;
        }

        double remaining = Math.Max(0, (horizontal ? finalSize.Width : finalSize.Height) - main);
        double position = JustifyContent == FlexJustification.Center ? remaining / 2 :
            JustifyContent == FlexJustification.End ? remaining : 0;
        double cross = horizontal ? finalSize.Height : finalSize.Width;
        hasItem = false;
        foreach (UIElement child in InternalChildren)
        {
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
            child.Arrange(horizontal ? new Rect(position, offset, length, breadth) : new Rect(offset, position, breadth, length));
            position = Add(position, length);
            hasItem = true;
        }

        return finalSize;
    }

    private static bool IsGap(object value) => (double)value >= 0 && !double.IsNaN((double)value) && !double.IsInfinity((double)value);
    private static double Add(double first, double second) => Math.Min(double.MaxValue, first + second);
}
