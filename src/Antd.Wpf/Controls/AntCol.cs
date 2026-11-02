using System;
using System.Windows;
using System.Windows.Controls;

namespace Antd.Wpf.Controls;

/// <summary>A column in an AntRow. Span zero collapses presentation while retaining the child and bindings.</summary>
public class AntCol : Decorator
{
    private double _gutter;

    static AntCol()
    {
        MinHeightProperty.OverrideMetadata(typeof(AntCol), new FrameworkPropertyMetadata(1.0));
        VisibilityProperty.OverrideMetadata(typeof(AntCol), new FrameworkPropertyMetadata(Visibility.Visible,
            FrameworkPropertyMetadataOptions.None, null,
            (sender, value) => ((AntCol)sender).Span == 0 ? Visibility.Collapsed : value));
    }

    public static readonly DependencyProperty SpanProperty = DependencyProperty.Register(
        nameof(Span), typeof(int), typeof(AntCol), new FrameworkPropertyMetadata(24,
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsParentMeasure,
            (sender, _) => sender.CoerceValue(VisibilityProperty)), IsTrackCount);
    public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register(
        nameof(Offset), typeof(int), typeof(AntCol), new FrameworkPropertyMetadata(0,
            FrameworkPropertyMetadataOptions.AffectsParentMeasure), IsTrackCount);

    public int Span { get => (int)GetValue(SpanProperty); set => SetValue(SpanProperty, value); }
    public int Offset { get => (int)GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }

    internal void SetGutter(double value)
    {
        if (_gutter != value) { _gutter = value; InvalidateMeasure(); }
    }

    protected override Size MeasureOverride(Size constraint)
    {
        if (Child == null) { return new Size(_gutter, 0); }
        Child.Measure(new Size(Math.Max(0, constraint.Width - _gutter), constraint.Height));
        return new Size(Math.Min(double.MaxValue, Child.DesiredSize.Width + _gutter), Child.DesiredSize.Height);
    }

    protected override Size ArrangeOverride(Size arrangeSize)
    {
        Child?.Arrange(new Rect(_gutter / 2, 0, Math.Max(0, arrangeSize.Width - _gutter), arrangeSize.Height));
        return arrangeSize;
    }

    protected override void OnVisualParentChanged(DependencyObject oldParent)
    {
        base.OnVisualParentChanged(oldParent);
        if (VisualParent is not AntRow) { SetGutter(0); }
    }

    private static bool IsTrackCount(object value) => (int)value >= 0 && (int)value <= 24;
}
