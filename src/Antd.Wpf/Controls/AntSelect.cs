using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Antd.Wpf.Theming;

namespace Antd.Wpf.Controls;

public class AntSelect : ComboBox
{
    private FrameworkElement? _popupRoot;
    private ResourceDictionary? _scopeResources;

    static AntSelect()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AntSelect), new FrameworkPropertyMetadata(typeof(AntSelect)));
        TemplateProperty.OverrideMetadata(typeof(AntSelect), new FrameworkPropertyMetadata(null, OnTemplateChanged));
    }

    public AntSelect()
    {
        Unloaded += OnUnloaded;
    }

    public static readonly DependencyProperty SizeProperty = AntThemeScope.SizeProperty.AddOwner(typeof(AntSelect));

    public ControlSize Size
    {
        get => (ControlSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder), typeof(string), typeof(AntSelect),
        new FrameworkPropertyMetadata(string.Empty, null), null);

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
        nameof(Status), typeof(ControlStatus), typeof(AntSelect),
        new FrameworkPropertyMetadata(global::Antd.Wpf.Controls.ControlStatus.Default, null), value => Enum.IsDefined(typeof(ControlStatus), value));

    public ControlStatus Status
    {
        get => (ControlStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    protected override void OnDropDownOpened(EventArgs args)
    {
        // shortcut: earlier runs exceeded PF05 intermittently; retain native/scoped comparisons until hardware calibration.
        ReleasePopupResources();
        if (GetTemplateChild("PART_Popup") is Popup popup && popup.Child is FrameworkElement root)
        {
            AntThemeScope? scope = FindScope();
            if (scope != null)
            {
                _popupRoot = root;
                _scopeResources = scope.Resources;
                root.Resources.MergedDictionaries.Add(_scopeResources);
            }
        }

        base.OnDropDownOpened(args);
    }

    protected override void OnDropDownClosed(EventArgs args)
    {
        ReleasePopupResources();
        base.OnDropDownClosed(args);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs args)
    {
        if (args.Key == Key.Escape && IsDropDownOpen)
        {
            SetCurrentValue(IsDropDownOpenProperty, false);
            Focus();
            args.Handled = true;
        }

        base.OnPreviewKeyDown(args);
    }

    public override void OnApplyTemplate()
    {
        ReleasePopupResources();
        base.OnApplyTemplate();
    }

    private static void OnTemplateChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var select = (AntSelect)sender;
        if (args.OldValue is ControlTemplate)
        {
            select.SetCurrentValue(IsDropDownOpenProperty, false);
        }

        select.ReleasePopupResources();
    }

    private AntThemeScope? FindScope()
    {
        DependencyObject? current = this;
        while (current != null)
        {
            if (current is AntThemeScope scope)
            {
                return scope;
            }

            DependencyObject? parent = LogicalTreeHelper.GetParent(current);
            if (parent == null && current is Visual)
            {
                parent = VisualTreeHelper.GetParent(current);
            }

            current = parent;
        }

        return null;
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        SetCurrentValue(IsDropDownOpenProperty, false);
        ReleasePopupResources();
    }

    private void ReleasePopupResources()
    {
        if (_popupRoot != null && _scopeResources != null)
        {
            _popupRoot.Resources.MergedDictionaries.Remove(_scopeResources);
        }

        _popupRoot = null;
        _scopeResources = null;
    }
}
