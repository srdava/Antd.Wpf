using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Antd.Wpf.Controls;

namespace Antd.Wpf.Internal;

internal sealed class DividerPresenter : Decorator
{
    private static readonly DashStyle Dashed = CreateDash(3, 3);
    private static readonly DashStyle Dotted = CreateDash(0, 2);
    private Pen? _pen;
    private Brush? _brush;
    private DividerVariant _variant;
    private double _leading;
    private double _trailing;
    private AntDivider? Owner => TemplatedParent as AntDivider;

    protected override Size MeasureOverride(Size constraint)
    {
        AntDivider? owner = Owner;
        if (owner == null || Child == null) { return new Size(); }
        double stroke = owner.Orientation == Orientation.Horizontal ? owner.BorderThickness.Top : owner.BorderThickness.Left;
        if (!owner.HasTitle)
        {
            Child.Measure(new Size(0, 0));
            return owner.Orientation == Orientation.Horizontal ? new Size(0, stroke) : new Size(stroke, 0);
        }

        Thickness requested = owner.Padding;
        Child.Measure(new Size(constraint.Width,
            Math.Max(0, constraint.Height - requested.Top - requested.Bottom)));
        Thickness padding = GetPadding(owner, constraint.Width, Child.DesiredSize.Width);
        return new Size(Math.Min(constraint.Width, Child.DesiredSize.Width + padding.Left + padding.Right),
            Math.Max(stroke, Child.DesiredSize.Height + padding.Top + padding.Bottom));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        AntDivider? owner = Owner;
        if (owner == null || Child == null)
        {
            UpdateRails(finalSize.Width, finalSize.Width);
            return finalSize;
        }
        if (!owner.HasTitle || (Child.DesiredSize.Width == 0 && Child.DesiredSize.Height == 0))
        {
            Child.Arrange(new Rect());
            UpdateRails(finalSize.Width, finalSize.Width);
            return finalSize;
        }

        Thickness padding = GetPadding(owner, finalSize.Width, Child.DesiredSize.Width);
        double width = Math.Max(0, finalSize.Width - padding.Left - padding.Right);
        double titleWidth = Math.Min(width, Child.DesiredSize.Width);
        double remaining = Math.Max(0, width - titleWidth);
        double ratio = owner.TitlePlacement == DividerTitlePlacement.Center ? 0.5 :
            owner.TitleInset.HasValue ? (owner.TitlePlacement == DividerTitlePlacement.Start ? 0 : 1) :
            owner.TitlePlacement == DividerTitlePlacement.Start ? 0.05 : 0.95;
        double leading = remaining * ratio;
        double trailing = Math.Min(finalSize.Width, leading + padding.Left + titleWidth + padding.Right);
        double height = Math.Max(0, finalSize.Height - padding.Top - padding.Bottom);
        Child.Arrange(new Rect(Math.Min(finalSize.Width, leading + padding.Left), padding.Top, titleWidth, height));
        UpdateRails(leading, trailing);
        return finalSize;
    }

    private void UpdateRails(double leading, double trailing)
    {
        if (_leading != leading || _trailing != trailing)
        {
            _leading = leading;
            _trailing = trailing;
            // A title can resize without changing this presenter's RenderSize.
            InvalidateVisual();
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        AntDivider? owner = Owner;
        if (owner == null) { return; }
        if (owner.Background != null) { drawingContext.DrawRectangle(owner.Background, null, new Rect(RenderSize)); }
        double stroke = owner.Orientation == Orientation.Horizontal ? owner.BorderThickness.Top : owner.BorderThickness.Left;
        if (owner.BorderBrush == null || stroke <= 0)
        {
            _pen = null;
            _brush = null;
            return;
        }
        if (_pen == null || _brush != owner.BorderBrush || _pen.Thickness != stroke || _variant != owner.Variant)
        {
            _brush = owner.BorderBrush;
            _variant = owner.Variant;
            _pen = new Pen(_brush, stroke)
            {
                DashStyle = _variant == DividerVariant.Dashed ? Dashed : _variant == DividerVariant.Dotted ? Dotted : DashStyles.Solid,
                DashCap = _variant == DividerVariant.Dotted ? PenLineCap.Round : PenLineCap.Flat,
            };
            // Freeze only when the caller's brush is already frozen; mutable brushes retain their ownership and updates.
            if (_brush.IsFrozen) { _pen.Freeze(); }
        }

        if (owner.Orientation == Orientation.Vertical)
        {
            drawingContext.DrawLine(_pen, new Point(RenderSize.Width / 2, 0), new Point(RenderSize.Width / 2, RenderSize.Height));
        }
        else
        {
            double y = RenderSize.Height / 2;
            if (_leading > 0) { drawingContext.DrawLine(_pen, new Point(0, y), new Point(_leading, y)); }
            if (_trailing < RenderSize.Width) { drawingContext.DrawLine(_pen, new Point(_trailing, y), new Point(RenderSize.Width, y)); }
        }
    }

    private static Thickness GetPadding(AntDivider owner, double width, double titleWidth)
    {
        Thickness padding = owner.Padding;
        if (owner.TitleInset.HasValue && owner.TitlePlacement != DividerTitlePlacement.Center)
        {
            if (owner.TitlePlacement == DividerTitlePlacement.Start) { padding.Left = owner.TitleInset.Value; }
            else { padding.Right = owner.TitleInset.Value; }
        }

        double available = Math.Max(0, width - titleWidth);
        double gap = padding.Left + padding.Right;
        if (gap > available)
        {
            double ratio = available / gap;
            padding.Left *= ratio;
            padding.Right *= ratio;
        }

        return padding;
    }

    private static DashStyle CreateDash(double dash, double gap)
    {
        var result = new DashStyle(new[] { dash, gap }, 0);
        result.Freeze();
        return result;
    }
}
