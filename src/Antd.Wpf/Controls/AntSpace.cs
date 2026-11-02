using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Antd.Wpf.Controls;

public enum SpaceAlignment { Auto, Start, Center, End }

/// <summary>Spaces direct children, with optional horizontal wrapping and decorative separator templates.</summary>
public class AntSpace : Panel
{
    private readonly List<ContentPresenter> _separators = new();
    private readonly List<Line> _lines = new();

    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation), typeof(Orientation), typeof(AntSpace),
        new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsMeasure),
        value => Enum.IsDefined(typeof(Orientation), value));
    public static readonly DependencyProperty AlignmentProperty = DependencyProperty.Register(
        nameof(Alignment), typeof(SpaceAlignment), typeof(AntSpace),
        new FrameworkPropertyMetadata(SpaceAlignment.Auto, FrameworkPropertyMetadataOptions.AffectsArrange),
        value => Enum.IsDefined(typeof(SpaceAlignment), value));
    public static readonly DependencyProperty GapProperty = DependencyProperty.Register(
        nameof(Gap), typeof(double), typeof(AntSpace),
        new FrameworkPropertyMetadata(8.0, FrameworkPropertyMetadataOptions.AffectsMeasure), IsGap);
    public static readonly DependencyProperty RowGapProperty = DependencyProperty.Register(
        nameof(RowGap), typeof(double?), typeof(AntSpace),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure), value => value == null || IsGap(value));
    public static readonly DependencyProperty ColumnGapProperty = DependencyProperty.Register(
        nameof(ColumnGap), typeof(double?), typeof(AntSpace),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure), value => value == null || IsGap(value));
    public static readonly DependencyProperty WrapProperty = DependencyProperty.Register(
        nameof(Wrap), typeof(bool), typeof(AntSpace), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty SeparatorTemplateProperty = DependencyProperty.Register(
        nameof(SeparatorTemplate), typeof(DataTemplate), typeof(AntSpace),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure,
            (sender, _) => ((AntSpace)sender).ClearSeparators()));

    public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public SpaceAlignment Alignment { get => (SpaceAlignment)GetValue(AlignmentProperty); set => SetValue(AlignmentProperty, value); }
    public double Gap { get => (double)GetValue(GapProperty); set => SetValue(GapProperty, value); }
    public double? RowGap { get => (double?)GetValue(RowGapProperty); set => SetValue(RowGapProperty, value); }
    public double? ColumnGap { get => (double?)GetValue(ColumnGapProperty); set => SetValue(ColumnGapProperty, value); }
    public bool Wrap { get => (bool)GetValue(WrapProperty); set => SetValue(WrapProperty, value); }
    public DataTemplate? SeparatorTemplate { get => (DataTemplate?)GetValue(SeparatorTemplateProperty); set => SetValue(SeparatorTemplateProperty, value); }

    protected override int VisualChildrenCount => base.VisualChildrenCount + _separators.Count;
    protected override Visual GetVisualChild(int index) => index < base.VisualChildrenCount ? base.GetVisualChild(index) :
        _separators[index - base.VisualChildrenCount];
    protected override IEnumerator LogicalChildren => EnumerateLogicalChildren();

    protected override Size MeasureOverride(Size availableSize)
    {
        int visible = 0;
        foreach (UIElement child in InternalChildren) { if (child.Visibility != Visibility.Collapsed) { visible++; } }
        EnsureSeparators(SeparatorTemplate == null ? 0 : Math.Max(0, visible - 1));
        bool horizontal = Orientation == Orientation.Horizontal;
        var constraint = horizontal ? new Size(double.PositiveInfinity, availableSize.Height) :
            new Size(availableSize.Width, double.PositiveInfinity);
        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility != Visibility.Collapsed) { child.Measure(constraint); }
        }
        foreach (ContentPresenter separator in _separators) { separator.Measure(constraint); }
        BuildLines(horizontal ? availableSize.Width : double.PositiveInfinity);
        double main = 0;
        double cross = 0;
        bool hasLine = false;
        foreach (Line line in _lines)
        {
            main = Math.Max(main, line.Main);
            if (hasLine) { cross = Add(cross, RowGap ?? Gap); }
            cross = Add(cross, line.Cross);
            hasLine = true;
        }

        return horizontal ? new Size(main, cross) : new Size(cross, main);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        bool horizontal = Orientation == Orientation.Horizontal;
        BuildLines(horizontal ? finalSize.Width : double.PositiveInfinity);
        double crossPosition = 0;
        double gap = horizontal ? ColumnGap ?? Gap : RowGap ?? Gap;
        int separatorIndex = 0;
        bool hasPrevious = false;
        foreach (Line line in _lines)
        {
            double breadth = horizontal && Wrap ? line.Cross : Math.Max(line.Cross, horizontal ? finalSize.Height : finalSize.Width);
            double position = 0;
            bool first = true;
            for (int index = line.Start; index <= line.End; index++)
            {
                UIElement child = InternalChildren[index];
                if (child.Visibility == Visibility.Collapsed) { child.Arrange(new Rect()); continue; }
                ContentPresenter? separator = hasPrevious && _separators.Count > 0 ? _separators[separatorIndex++] : null;
                if (!first)
                {
                    position = Add(position, gap);
                    if (separator != null)
                    {
                        separator.Visibility = Visibility.Visible;
                        ArrangeItem(separator, position, crossPosition, breadth, horizontal);
                        position = Add(Add(position, Main(separator.DesiredSize, horizontal)), gap);
                    }
                }
                else if (separator != null)
                {
                    // A separator at a wrap boundary has no visible neighbor on its line.
                    separator.Visibility = Visibility.Hidden;
                    separator.Arrange(new Rect());
                }

                ArrangeItem(child, position, crossPosition, breadth, horizontal);
                position = Add(position, Main(child.DesiredSize, horizontal));
                first = false;
                hasPrevious = true;
            }

            crossPosition = Add(Add(crossPosition, breadth), RowGap ?? Gap);
        }

        return finalSize;
    }

    private void BuildLines(double limit)
    {
        _lines.Clear();
        bool horizontal = Orientation == Orientation.Horizontal;
        double gap = horizontal ? ColumnGap ?? Gap : RowGap ?? Gap;
        int start = 0;
        int visible = 0;
        double main = 0;
        double cross = 0;
        bool hasItem = false;
        for (int index = 0; index < InternalChildren.Count; index++)
        {
            UIElement child = InternalChildren[index];
            if (child.Visibility == Visibility.Collapsed) { continue; }
            ContentPresenter? separator = visible > 0 && _separators.Count > 0 ? _separators[visible - 1] : null;
            double addition = Main(child.DesiredSize, horizontal);
            if (hasItem)
            {
                addition = Add(addition, gap);
                if (separator != null) { addition = Add(Add(addition, Main(separator.DesiredSize, horizontal)), gap); }
            }
            if (horizontal && Wrap && hasItem && Add(main, addition) > limit)
            {
                _lines.Add(new Line(start, index - 1, main, cross));
                start = index;
                main = 0;
                cross = 0;
                hasItem = false;
                addition = Main(child.DesiredSize, horizontal);
            }
            main = Add(main, addition);
            cross = Math.Max(cross, horizontal ? child.DesiredSize.Height : child.DesiredSize.Width);
            if (hasItem && separator != null) { cross = Math.Max(cross, horizontal ? separator.DesiredSize.Height : separator.DesiredSize.Width); }
            visible++;
            hasItem = true;
        }
        if (hasItem) { _lines.Add(new Line(start, InternalChildren.Count - 1, main, cross)); }
    }

    private void ArrangeItem(UIElement item, double mainPosition, double crossPosition, double breadth, bool horizontal)
    {
        double desiredCross = horizontal ? item.DesiredSize.Height : item.DesiredSize.Width;
        bool stretch = !horizontal && Alignment == SpaceAlignment.Auto;
        SpaceAlignment align = Alignment == SpaceAlignment.Auto ? SpaceAlignment.Center : Alignment;
        double spare = Math.Max(0, breadth - desiredCross);
        double offset = align == SpaceAlignment.Center ? spare / 2 : align == SpaceAlignment.End ? spare : 0;
        if (stretch) { desiredCross = breadth; offset = 0; }
        item.Arrange(horizontal ? new Rect(mainPosition, crossPosition + offset, item.DesiredSize.Width, desiredCross) :
            new Rect(crossPosition + offset, mainPosition, desiredCross, item.DesiredSize.Height));
    }

    private void EnsureSeparators(int count)
    {
        while (_separators.Count > count) { RemoveSeparator(); }
        while (_separators.Count < count)
        {
            var presenter = new ContentPresenter { ContentTemplate = SeparatorTemplate, IsHitTestVisible = false, Focusable = false };
            KeyboardNavigation.SetTabNavigation(presenter, KeyboardNavigationMode.None);
            presenter.SetBinding(ContentPresenter.ContentProperty, new Binding(nameof(DataContext)) { Source = this });
            _separators.Add(presenter);
            AddLogicalChild(presenter);
            AddVisualChild(presenter);
        }
    }

    private void ClearSeparators() { while (_separators.Count > 0) { RemoveSeparator(); } }
    private void RemoveSeparator()
    {
        ContentPresenter presenter = _separators[_separators.Count - 1];
        _separators.RemoveAt(_separators.Count - 1);
        BindingOperations.ClearBinding(presenter, ContentPresenter.ContentProperty);
        RemoveVisualChild(presenter);
        RemoveLogicalChild(presenter);
    }

    private IEnumerator EnumerateLogicalChildren()
    {
        foreach (UIElement child in InternalChildren) { yield return child; }
        foreach (ContentPresenter separator in _separators) { yield return separator; }
    }

    private static bool IsGap(object value) => (double)value >= 0 && !double.IsNaN((double)value) && !double.IsInfinity((double)value);
    private static double Main(Size size, bool horizontal) => horizontal ? size.Width : size.Height;
    private static double Add(double first, double second) => Math.Min(double.MaxValue, first + second);

    private readonly struct Line
    {
        public Line(int start, int end, double main, double cross) { Start = start; End = end; Main = main; Cross = cross; }
        public int Start { get; }
        public int End { get; }
        public double Main { get; }
        public double Cross { get; }
    }
}
