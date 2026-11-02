using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using Antd.Wpf.Automation;
using Antd.Wpf.Theming;

namespace Antd.Wpf.Controls;

public enum SiderCollapseReason { Programmatic, Trigger, Automation }

public sealed class SiderCollapsedChangedEventArgs : EventArgs
{
    public SiderCollapsedChangedEventArgs(bool isCollapsed, SiderCollapseReason reason) { IsCollapsed = isCollapsed; Reason = reason; }
    public bool IsCollapsed { get; }
    public SiderCollapseReason Reason { get; }
}

[TemplatePart(Name = "PART_Trigger", Type = typeof(Button))]
[TemplatePart(Name = "PART_ZeroTrigger", Type = typeof(Button))]
public class AntSider : AntThemeScope
{
    private readonly CollapseAction _collapseCommand;
    private Button? _trigger;
    private Button? _zeroTrigger;
    private SiderCollapseReason _reason;
    private static readonly DependencyPropertyKey IsZeroWidthCollapsedPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsZeroWidthCollapsed), typeof(bool), typeof(AntSider), new FrameworkPropertyMetadata(false));
    public static readonly DependencyProperty IsZeroWidthCollapsedProperty = IsZeroWidthCollapsedPropertyKey.DependencyProperty;
    private static readonly DependencyPropertyKey UsesZeroWidthTriggerPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(UsesZeroWidthTrigger), typeof(bool), typeof(AntSider), new FrameworkPropertyMetadata(false));
    public static readonly DependencyProperty UsesZeroWidthTriggerProperty = UsesZeroWidthTriggerPropertyKey.DependencyProperty;

    static AntSider()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AntSider), new FrameworkPropertyMetadata(typeof(AntSider)));
        ModeProperty.OverrideMetadata(typeof(AntSider), new FrameworkPropertyMetadata(ThemeMode.Dark));
        WidthProperty.OverrideMetadata(typeof(AntSider), new FrameworkPropertyMetadata(200.0,
            FrameworkPropertyMetadataOptions.AffectsMeasure, null,
            (sender, value) => ((AntSider)sender).IsCollapsed ? ((AntSider)sender).CollapsedWidth : value));
        IsEnabledProperty.OverrideMetadata(typeof(AntSider), new FrameworkPropertyMetadata(true,
            (sender, _) => ((AntSider)sender)._collapseCommand?.Notify()));
    }

    public AntSider() => _collapseCommand = new CollapseAction(this);

    public static readonly DependencyProperty IsCollapsedProperty = DependencyProperty.Register(
        nameof(IsCollapsed), typeof(bool), typeof(AntSider),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCollapsedChanged));
    public static readonly DependencyProperty CollapsedWidthProperty = DependencyProperty.Register(
        nameof(CollapsedWidth), typeof(double), typeof(AntSider), new FrameworkPropertyMetadata(80.0,
            (sender, _) => ((AntSider)sender).UpdateCollapsedLayout()),
        value => (double)value >= 0 && !double.IsNaN((double)value) && !double.IsInfinity((double)value));
    public static readonly DependencyProperty IsCollapsibleProperty = DependencyProperty.Register(
        nameof(IsCollapsible), typeof(bool), typeof(AntSider), new FrameworkPropertyMetadata(false,
            (sender, _) => ((AntSider)sender)._collapseCommand?.Notify()));
    public static readonly DependencyProperty TriggerTemplateProperty = DependencyProperty.Register(
        nameof(TriggerTemplate), typeof(DataTemplate), typeof(AntSider), new FrameworkPropertyMetadata(null));

    public bool IsCollapsed { get => (bool)GetValue(IsCollapsedProperty); set => SetValue(IsCollapsedProperty, value); }
    public double CollapsedWidth { get => (double)GetValue(CollapsedWidthProperty); set => SetValue(CollapsedWidthProperty, value); }
    public bool IsCollapsible { get => (bool)GetValue(IsCollapsibleProperty); set => SetValue(IsCollapsibleProperty, value); }
    public DataTemplate? TriggerTemplate { get => (DataTemplate?)GetValue(TriggerTemplateProperty); set => SetValue(TriggerTemplateProperty, value); }
    public bool IsZeroWidthCollapsed => (bool)GetValue(IsZeroWidthCollapsedProperty);
    public bool UsesZeroWidthTrigger => (bool)GetValue(UsesZeroWidthTriggerProperty);
    public ICommand CollapseCommand => _collapseCommand;
    public event EventHandler<SiderCollapsedChangedEventArgs>? CollapsedChanged;

    public override void OnApplyTemplate()
    {
        DetachCommand(_trigger);
        DetachCommand(_zeroTrigger);
        _trigger = null;
        _zeroTrigger = null;
        base.OnApplyTemplate();
        _trigger = GetTemplateChild("PART_Trigger") as Button;
        _zeroTrigger = GetTemplateChild("PART_ZeroTrigger") as Button;
        if (IsCollapsible && _zeroTrigger == null)
        {
            Trace.TraceWarning("AntSider: PART_ZeroTrigger is required for keyboard expansion when CollapsedWidth is zero.");
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new AntSiderAutomationPeer(this);

    internal void ChangeCollapsed(bool value, SiderCollapseReason reason)
    {
        _reason = reason;
        try { SetCurrentValue(IsCollapsedProperty, value); }
        finally { _reason = SiderCollapseReason.Programmatic; }
    }

    private static void OnCollapsedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var sider = (AntSider)sender;
        SiderCollapseReason reason = sider._reason;
        sider._reason = SiderCollapseReason.Programmatic;
        sider.UpdateCollapsedLayout();
        if (UIElementAutomationPeer.FromElement(sider) is AntSiderAutomationPeer peer)
        {
            peer.NotifyStateChanged((bool)args.OldValue, (bool)args.NewValue);
        }
        sider.CollapsedChanged?.Invoke(sider, new SiderCollapsedChangedEventArgs((bool)args.NewValue, reason));
    }

    private void UpdateCollapsedLayout()
    {
        bool moveFocus = IsKeyboardFocusWithin;
        bool zeroFocused = _zeroTrigger?.IsKeyboardFocusWithin == true;
        CoerceValue(WidthProperty);
        SetValue(UsesZeroWidthTriggerPropertyKey, CollapsedWidth == 0);
        SetValue(IsZeroWidthCollapsedPropertyKey, IsCollapsed && CollapsedWidth == 0);
        if (moveFocus && IsZeroWidthCollapsed && IsCollapsible) { _zeroTrigger?.Focus(); }
        else if (zeroFocused && !UsesZeroWidthTrigger && IsCollapsible) { _trigger?.Focus(); }
    }

    private void DetachCommand(Button? trigger)
    {
        if (trigger != null && ReferenceEquals(trigger.Command, _collapseCommand)) { trigger.ClearValue(Button.CommandProperty); }
    }

    private sealed class CollapseAction : ICommand
    {
        private readonly AntSider _owner;
        public CollapseAction(AntSider owner) => _owner = owner;
        public bool CanExecute(object? parameter) => _owner.IsCollapsible && _owner.IsEnabled;
        public void Execute(object? parameter)
        {
            if (CanExecute(parameter)) { _owner.ChangeCollapsed(!_owner.IsCollapsed, SiderCollapseReason.Trigger); }
        }
        public event EventHandler? CanExecuteChanged;
        public void Notify() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
