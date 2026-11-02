using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Antd.Wpf.Automation;
using Antd.Wpf.Theming;

namespace Antd.Wpf.Controls;

[TemplatePart(Name = "Track", Type = typeof(Border))]
[TemplatePart(Name = "Thumb", Type = typeof(Border))]
[TemplatePart(Name = "ThumbTranslation", Type = typeof(TranslateTransform))]
public class AntSwitch : ToggleButton
{
    private static readonly KeySpline ThumbEasing = new(0.42, 0, 0.58, 1);
    private Border? _track;
    private Border? _thumb;
    private TranslateTransform? _thumbTranslation;
    private AnimationClock? _thumbMotion;

    static AntSwitch()
    {
        ThumbEasing.Freeze();
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AntSwitch), new FrameworkPropertyMetadata(typeof(AntSwitch)));
        IsThreeStateProperty.OverrideMetadata(typeof(AntSwitch), new FrameworkPropertyMetadata(false, null, (_, _) => false));
        IsCheckedProperty.OverrideMetadata(typeof(AntSwitch), new FrameworkPropertyMetadata(false, null, (_, value) => value ?? false));
    }

    public AntSwitch()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public static readonly DependencyProperty SizeProperty = AntThemeScope.SizeProperty.AddOwner(typeof(AntSwitch));
    public static readonly DependencyProperty CheckedContentProperty = DependencyProperty.Register(
        nameof(CheckedContent), typeof(object), typeof(AntSwitch), new FrameworkPropertyMetadata(null));
    public static readonly DependencyProperty UncheckedContentProperty = DependencyProperty.Register(
        nameof(UncheckedContent), typeof(object), typeof(AntSwitch), new FrameworkPropertyMetadata(null));
    public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(
        nameof(IsLoading), typeof(bool), typeof(AntSwitch), new FrameworkPropertyMetadata(false, OnLoadingChanged));

    public ControlSize Size
    {
        get => (ControlSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public object? CheckedContent
    {
        get => GetValue(CheckedContentProperty);
        set => SetValue(CheckedContentProperty, value);
    }

    public object? UncheckedContent
    {
        get => GetValue(UncheckedContentProperty);
        set => SetValue(UncheckedContentProperty, value);
    }

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    protected override bool IsEnabledCore => base.IsEnabledCore && !IsLoading;

    public override void OnApplyTemplate()
    {
        StopThumbMotion();
        if (_track != null) { _track.SizeChanged -= OnGeometryChanged; }
        if (_thumb != null) { _thumb.SizeChanged -= OnGeometryChanged; }
        base.OnApplyTemplate();
        _track = GetTemplateChild("Track") as Border;
        _thumb = GetTemplateChild("Thumb") as Border;
        _thumbTranslation = GetTemplateChild("ThumbTranslation") as TranslateTransform;
        if (_track != null) { _track.SizeChanged += OnGeometryChanged; }
        if (_thumb != null) { _thumb.SizeChanged += OnGeometryChanged; }
        UpdateThumbPosition(false);
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (args.Property == IsCheckedProperty)
        {
            UpdateThumbPosition(true);
        }
        else if (args.Property == AntThemeScope.MotionEnabledProperty || args.Property == IsVisibleProperty ||
            args.Property == IsEnabledProperty || args.Property == FlowDirectionProperty)
        {
            UpdateThumbPosition(false);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs args) => UpdateThumbPosition(false);
    private void OnUnloaded(object sender, RoutedEventArgs args) => UpdateThumbPosition(false);
    private void OnGeometryChanged(object sender, SizeChangedEventArgs args) => UpdateThumbPosition(_thumbMotion != null);

    private void UpdateThumbPosition(bool animate)
    {
        if (_track == null || _thumb == null || _thumbTranslation == null) { return; }
        double current = _thumbTranslation.X;
        double destination = IsChecked == true ? Math.Max(0, _track.ActualWidth - _thumb.ActualWidth - _thumb.Margin.Left - _thumb.Margin.Right) : 0;
        StopThumbMotion();
        // This named template transform owns X; its base value must survive clock removal.
        _thumbTranslation.X = destination;
        if (!animate || !IsLoaded || !IsVisible || !IsEnabled || !AntThemeScope.GetMotionEnabled(this) || Math.Abs(current - destination) < 0.01)
        {
            return;
        }

        var duration = TimeSpan.FromMilliseconds(200);
        var animation = new DoubleAnimationUsingKeyFrames { Duration = duration, FillBehavior = FillBehavior.Stop };
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(current, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        animation.KeyFrames.Add(new SplineDoubleKeyFrame(destination, KeyTime.FromTimeSpan(duration), ThumbEasing));
        _thumbMotion = (AnimationClock)animation.CreateClock(true);
        _thumbMotion.Completed += OnThumbMotionCompleted;
        _thumbTranslation.ApplyAnimationClock(TranslateTransform.XProperty, _thumbMotion, HandoffBehavior.SnapshotAndReplace);
        // Apply the starting position synchronously so a reversal cannot flash at the endpoint.
        _thumbMotion.Controller?.SeekAlignedToLastTick(TimeSpan.Zero, TimeSeekOrigin.BeginTime);
    }

    private void OnThumbMotionCompleted(object? sender, EventArgs args)
    {
        if (ReferenceEquals(sender, _thumbMotion)) { StopThumbMotion(); }
    }

    private void StopThumbMotion()
    {
        if (_thumbMotion == null) { return; }
        _thumbMotion.Completed -= OnThumbMotionCompleted;
        _thumbMotion.Controller?.Remove();
        _thumbMotion = null;
        _thumbTranslation?.ApplyAnimationClock(TranslateTransform.XProperty, null);
    }

    protected override void OnClick()
    {
        if (IsEnabled)
        {
            base.OnClick();
        }
    }

    protected override void OnToggle()
    {
        if (IsEnabled)
        {
            // A bound null displays as off; the next user action turns it on.
            SetCurrentValue(IsCheckedProperty, IsChecked != true);
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new AntSwitchAutomationPeer(this);

    private static void OnLoadingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var control = (AntSwitch)sender;
        control.CoerceValue(IsEnabledProperty);
        if (AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
        {
            UIElementAutomationPeer.FromElement(control)?.RaisePropertyChangedEvent(
                AutomationElementIdentifiers.ItemStatusProperty,
                (bool)args.OldValue ? "Loading" : AutomationProperties.GetItemStatus(control),
                (bool)args.NewValue ? "Loading" : AutomationProperties.GetItemStatus(control));
        }
    }
}
