using System;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Antd.Wpf.Automation;
using Antd.Wpf.Internal;
using Antd.Wpf.Theming;

namespace Antd.Wpf.Controls;

public enum DividerVariant { Solid, Dashed, Dotted }
public enum DividerTitlePlacement { Start, Center, End }

[TemplatePart(Name = "PART_Layout", Type = typeof(Decorator))]
public class AntDivider : ContentControl
{
    private DividerPresenter? _presenter;
    private static readonly DependencyPropertyKey HasTitlePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HasTitle), typeof(bool), typeof(AntDivider), new FrameworkPropertyMetadata(false));
    public static readonly DependencyProperty HasTitleProperty = HasTitlePropertyKey.DependencyProperty;

    static AntDivider()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AntDivider), new FrameworkPropertyMetadata(typeof(AntDivider)));
        FocusableProperty.OverrideMetadata(typeof(AntDivider), new FrameworkPropertyMetadata(false));
        IsTabStopProperty.OverrideMetadata(typeof(AntDivider), new FrameworkPropertyMetadata(false));
        // A void block keeps this overload a change callback, rather than a coercion callback returning null.
        TemplateProperty.OverrideMetadata(typeof(AntDivider), new FrameworkPropertyMetadata(null,
            (sender, _) => { ((AntDivider)sender)._presenter = null; }));
    }

    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation), typeof(Orientation), typeof(AntDivider),
        new FrameworkPropertyMetadata(Orientation.Horizontal, OnOrientationChanged), value => Enum.IsDefined(typeof(Orientation), value));
    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(DividerVariant), typeof(AntDivider), new FrameworkPropertyMetadata(DividerVariant.Solid),
        value => Enum.IsDefined(typeof(DividerVariant), value));
    public static readonly DependencyProperty TitlePlacementProperty = DependencyProperty.Register(
        nameof(TitlePlacement), typeof(DividerTitlePlacement), typeof(AntDivider), new FrameworkPropertyMetadata(DividerTitlePlacement.Center),
        value => Enum.IsDefined(typeof(DividerTitlePlacement), value));
    public static readonly DependencyProperty TitleInsetProperty = DependencyProperty.Register(
        nameof(TitleInset), typeof(double?), typeof(AntDivider), new FrameworkPropertyMetadata(null),
        value => value == null || ((double)value >= 0 && !double.IsInfinity((double)value) && !double.IsNaN((double)value)));
    public static readonly DependencyProperty IsPlainProperty = DependencyProperty.Register(
        nameof(IsPlain), typeof(bool), typeof(AntDivider), new FrameworkPropertyMetadata(false));
    public static readonly DependencyProperty SizeProperty = AntThemeScope.SizeProperty.AddOwner(typeof(AntDivider),
        new FrameworkPropertyMetadata(ControlSize.Large, FrameworkPropertyMetadataOptions.Inherits));

    public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public DividerVariant Variant { get => (DividerVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public DividerTitlePlacement TitlePlacement { get => (DividerTitlePlacement)GetValue(TitlePlacementProperty); set => SetValue(TitlePlacementProperty, value); }
    public double? TitleInset { get => (double?)GetValue(TitleInsetProperty); set => SetValue(TitleInsetProperty, value); }
    public bool IsPlain { get => (bool)GetValue(IsPlainProperty); set => SetValue(IsPlainProperty, value); }
    public ControlSize Size { get => (ControlSize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public bool HasTitle => (bool)GetValue(HasTitleProperty);

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _presenter = GetTemplateChild("PART_Layout") as DividerPresenter;
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        UpdateTitle();
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (_presenter == null) { return; }
        if (args.Property == OrientationProperty || args.Property == TitlePlacementProperty || args.Property == TitleInsetProperty ||
            args.Property == PaddingProperty || args.Property == FontSizeProperty || args.Property == BorderThicknessProperty || args.Property == HasTitleProperty)
        {
            _presenter.InvalidateMeasure();
            _presenter.InvalidateVisual();
        }
        else if (args.Property == VariantProperty || args.Property == BorderBrushProperty || args.Property == BackgroundProperty)
        {
            _presenter.InvalidateVisual();
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new AntDividerAutomationPeer(this);

    private static void OnOrientationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) => ((AntDivider)sender).UpdateTitle();
    private void UpdateTitle()
    {
        SetValue(HasTitlePropertyKey, Orientation == Orientation.Horizontal && Content != null && !(Content is string text && text.Length == 0));
        UIElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
}
