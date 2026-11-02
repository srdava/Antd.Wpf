using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Antd.Wpf.Theming;

namespace Antd.Wpf.Controls;

public class AntButton : Button
{
    private static readonly DependencyPropertyKey IsLoadingVisiblePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsLoadingVisible), typeof(bool), typeof(AntButton), new PropertyMetadata(false));
    public static readonly DependencyProperty IsLoadingVisibleProperty = IsLoadingVisiblePropertyKey.DependencyProperty;
    private DispatcherTimer? _loadingTimer;

    static AntButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AntButton), new FrameworkPropertyMetadata(typeof(AntButton)));
    }

    public AntButton()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public bool IsLoadingVisible => (bool)GetValue(IsLoadingVisibleProperty);
    protected override bool IsEnabledCore => base.IsEnabledCore && !IsLoading;

    public static readonly DependencyProperty SizeProperty = AntThemeScope.SizeProperty.AddOwner(typeof(AntButton));

    public ControlSize Size
    {
        get => (ControlSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(ButtonVariant), typeof(AntButton),
        new FrameworkPropertyMetadata(global::Antd.Wpf.Controls.ButtonVariant.Outlined, null), value => Enum.IsDefined(typeof(ButtonVariant), value));

    public ButtonVariant Variant
    {
        get => (ButtonVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public static readonly DependencyProperty SemanticColorProperty = DependencyProperty.Register(
        nameof(SemanticColor), typeof(SemanticColor), typeof(AntButton),
        new FrameworkPropertyMetadata(global::Antd.Wpf.Controls.SemanticColor.Default, null), value => Enum.IsDefined(typeof(SemanticColor), value));

    public SemanticColor SemanticColor
    {
        get => (SemanticColor)GetValue(SemanticColorProperty);
        set => SetValue(SemanticColorProperty, value);
    }

    public static readonly DependencyProperty ShapeProperty = DependencyProperty.Register(
        nameof(Shape), typeof(ButtonShape), typeof(AntButton),
        new FrameworkPropertyMetadata(global::Antd.Wpf.Controls.ButtonShape.Default, null), value => Enum.IsDefined(typeof(ButtonShape), value));

    public ButtonShape Shape
    {
        get => (ButtonShape)GetValue(ShapeProperty);
        set => SetValue(ShapeProperty, value);
    }

    public static readonly DependencyProperty IconPlacementProperty = DependencyProperty.Register(
        nameof(IconPlacement), typeof(IconPlacement), typeof(AntButton),
        new FrameworkPropertyMetadata(global::Antd.Wpf.Controls.IconPlacement.Start, null), value => Enum.IsDefined(typeof(IconPlacement), value));

    public IconPlacement IconPlacement
    {
        get => (IconPlacement)GetValue(IconPlacementProperty);
        set => SetValue(IconPlacementProperty, value);
    }

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon), typeof(object), typeof(AntButton),
        new FrameworkPropertyMetadata(null, null), null);

    public object? Icon
    {
        get => (object?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly DependencyProperty IsGhostProperty = DependencyProperty.Register(
        nameof(IsGhost), typeof(bool), typeof(AntButton),
        new FrameworkPropertyMetadata(false, null), null);

    public bool IsGhost
    {
        get => (bool)GetValue(IsGhostProperty);
        set => SetValue(IsGhostProperty, value);
    }

    public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(
        nameof(IsLoading), typeof(bool), typeof(AntButton),
        new FrameworkPropertyMetadata(false, OnLoadingChanged), null);

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public static readonly DependencyProperty LoadingDelayProperty = DependencyProperty.Register(
        nameof(LoadingDelay), typeof(TimeSpan), typeof(AntButton),
        new FrameworkPropertyMetadata(TimeSpan.Zero, OnLoadingChanged), value => (TimeSpan)value >= TimeSpan.Zero && (TimeSpan)value <= TimeSpan.FromDays(1));

    public TimeSpan LoadingDelay
    {
        get => (TimeSpan)GetValue(LoadingDelayProperty);
        set => SetValue(LoadingDelayProperty, value);
    }

    protected override void OnClick()
    {
        if (!IsLoading)
        {
            base.OnClick();
        }
    }

    private static void OnLoadingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var button = (AntButton)sender;
        button.CoerceValue(IsEnabledProperty);
        button.ScheduleLoading();
    }

    private void OnLoaded(object sender, RoutedEventArgs args) => ScheduleLoading();
    private void OnUnloaded(object sender, RoutedEventArgs args) => StopLoading();

    private void ScheduleLoading()
    {
        StopLoading();
        if (!IsLoading || !IsLoaded)
        {
            return;
        }

        if (LoadingDelay == TimeSpan.Zero)
        {
            SetValue(IsLoadingVisiblePropertyKey, true);
            return;
        }

        _loadingTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
        {
            Interval = LoadingDelay,
        };
        _loadingTimer.Tick += OnLoadingTick;
        _loadingTimer.Start();
    }

    private void OnLoadingTick(object? sender, EventArgs args)
    {
        StopLoading();
        if (IsLoading && IsLoaded)
        {
            SetValue(IsLoadingVisiblePropertyKey, true);
        }
    }

    private void StopLoading()
    {
        if (_loadingTimer != null)
        {
            _loadingTimer.Stop();
            _loadingTimer.Tick -= OnLoadingTick;
            _loadingTimer = null;
        }

        SetValue(IsLoadingVisiblePropertyKey, false);
    }
}
