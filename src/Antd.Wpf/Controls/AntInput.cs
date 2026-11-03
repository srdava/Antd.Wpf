using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Antd.Wpf.Theming;
using Antd.Wpf.Automation;
using Antd.Wpf.Internal;

namespace Antd.Wpf.Controls;

[TemplatePart(Name = ClearButtonPartName, Type = typeof(Button))]
public class AntInput : TextBox
{
    private const string ClearButtonPartName = "PART_ClearButton";
    private Button? _clearButton;

    static AntInput()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AntInput), new FrameworkPropertyMetadata(typeof(AntInput)));
        TemplateProperty.OverrideMetadata(typeof(AntInput), new FrameworkPropertyMetadata(null, OnTemplateChanged));
    }

    public static readonly DependencyProperty SizeProperty = AntThemeScope.SizeProperty.AddOwner(typeof(AntInput));

    public ControlSize Size
    {
        get => (ControlSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder), typeof(string), typeof(AntInput),
        new FrameworkPropertyMetadata(string.Empty, null), null);

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public static readonly DependencyProperty PrefixProperty = DependencyProperty.Register(
        nameof(Prefix), typeof(object), typeof(AntInput),
        new FrameworkPropertyMetadata(null, null), null);

    public object? Prefix
    {
        get => (object?)GetValue(PrefixProperty);
        set => SetValue(PrefixProperty, value);
    }

    public static readonly DependencyProperty SuffixProperty = DependencyProperty.Register(
        nameof(Suffix), typeof(object), typeof(AntInput),
        new FrameworkPropertyMetadata(null, null), null);

    public object? Suffix
    {
        get => (object?)GetValue(SuffixProperty);
        set => SetValue(SuffixProperty, value);
    }

    public static readonly DependencyProperty AllowClearProperty = DependencyProperty.Register(
        nameof(AllowClear), typeof(bool), typeof(AntInput),
        new FrameworkPropertyMetadata(false, null), null);

    public bool AllowClear
    {
        get => (bool)GetValue(AllowClearProperty);
        set => SetValue(AllowClearProperty, value);
    }

    public static readonly DependencyProperty ShowCountProperty = DependencyProperty.Register(
        nameof(ShowCount), typeof(bool), typeof(AntInput),
        new FrameworkPropertyMetadata(false, null), null);

    public bool ShowCount
    {
        get => (bool)GetValue(ShowCountProperty);
        set => SetValue(ShowCountProperty, value);
    }

    private static readonly DependencyPropertyKey IsOverMaxLengthPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsOverMaxLength), typeof(bool), typeof(AntInput), new FrameworkPropertyMetadata(false, OnOverflowChanged));

    public static readonly DependencyProperty IsOverMaxLengthProperty = IsOverMaxLengthPropertyKey.DependencyProperty;

    public bool IsOverMaxLength => (bool)GetValue(IsOverMaxLengthProperty);

    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(InputVariant), typeof(AntInput),
        new FrameworkPropertyMetadata(global::Antd.Wpf.Controls.InputVariant.Outlined, null), value => Enum.IsDefined(typeof(InputVariant), value));

    public InputVariant Variant
    {
        get => (InputVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
        nameof(Status), typeof(ControlStatus), typeof(AntInput),
        new FrameworkPropertyMetadata(global::Antd.Wpf.Controls.ControlStatus.Default, OnStatusChanged), value => Enum.IsDefined(typeof(ControlStatus), value));

    public ControlStatus Status
    {
        get => (ControlStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public override void OnApplyTemplate()
    {
        DetachClearButton();
        base.OnApplyTemplate();
        ResetContentHostPadding();
        (GetTemplateChild("PART_ContentHost") as InputScrollViewer)?.UpdateLineBounds();
        _clearButton = GetTemplateChild(ClearButtonPartName) as Button;
        if (_clearButton != null)
        {
            _clearButton.Click += OnClearClick;
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new AntInputAutomationPeer(this);

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (args.Property == TextProperty || args.Property == MaxLengthProperty)
        {
            bool overflow = MaxLength > 0 && Text.Length > MaxLength;
            if (overflow != IsOverMaxLength)
            {
                SetValue(IsOverMaxLengthPropertyKey, overflow);
            }
        }
        if (args.Property == PaddingProperty)
        {
            ResetContentHostPadding();
        }
        if (args.Property == MinLinesProperty || args.Property == MaxLinesProperty ||
            args.Property == AcceptsReturnProperty || args.Property == TextBlock.LineHeightProperty ||
            args.Property == HeightProperty || args.Property == MinHeightProperty || args.Property == MaxHeightProperty)
        {
            (GetTemplateChild("PART_ContentHost") as InputScrollViewer)?.UpdateLineBounds();
        }
    }

    private void ResetContentHostPadding()
    {
        // TextBoxBase copies Padding into the ScrollViewer; our Chrome already owns that inset.
        if (GetTemplateChild("PART_ContentHost") is ScrollViewer viewer)
        {
            viewer.SetCurrentValue(PaddingProperty, new Thickness(0));
        }
    }

    private static void OnTemplateChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        ((AntInput)sender).DetachClearButton();

    private static void OnStatusChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var input = (AntInput)sender;
        AutomationPeer? peer = UIElementAutomationPeer.FromElement(input);
        if (peer != null && AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged) && !Validation.GetHasError(input) && !input.IsOverMaxLength)
        {
            string fallback = AutomationProperties.GetItemStatus(input);
            peer.RaisePropertyChangedEvent(AutomationElementIdentifiers.ItemStatusProperty,
                (ControlStatus)args.OldValue == ControlStatus.Default ? fallback : args.OldValue.ToString(),
                (ControlStatus)args.NewValue == ControlStatus.Default ? fallback : args.NewValue.ToString());
        }
    }

    private static void OnOverflowChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var input = (AntInput)sender;
        AutomationPeer? peer = UIElementAutomationPeer.FromElement(input);
        if (peer != null && AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged) &&
            !Validation.GetHasError(input) && input.Status != ControlStatus.Error)
        {
            string fallback = input.Status == ControlStatus.Default ? AutomationProperties.GetItemStatus(input) : input.Status.ToString();
            peer.RaisePropertyChangedEvent(AutomationElementIdentifiers.ItemStatusProperty,
                (bool)args.OldValue ? "Error" : fallback, (bool)args.NewValue ? "Error" : fallback);
        }
    }

    private void DetachClearButton()
    {
        if (_clearButton != null)
        {
            _clearButton.Click -= OnClearClick;
            _clearButton = null;
        }
    }

    private void OnClearClick(object sender, RoutedEventArgs args)
    {
        if (IsEnabled && !IsReadOnly)
        {
            Clear();
            Focus();
            args.Handled = true;
        }
    }
}
