using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using Antd.Wpf.Automation;
using Antd.Wpf.Theming;

namespace Antd.Wpf.Controls;

public class AntSwitch : ToggleButton
{
    static AntSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AntSwitch), new FrameworkPropertyMetadata(typeof(AntSwitch)));
        IsThreeStateProperty.OverrideMetadata(typeof(AntSwitch), new FrameworkPropertyMetadata(false, null, (_, _) => false));
        IsCheckedProperty.OverrideMetadata(typeof(AntSwitch), new FrameworkPropertyMetadata(false, null, (_, value) => value ?? false));
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
