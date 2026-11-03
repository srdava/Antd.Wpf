using System;
using System.Collections;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using Antd.Wpf.Theming;

namespace Antd.Wpf.Controls;

[TemplatePart(Name = HostPartName, Type = typeof(ContentPresenter))]
[TemplatePart(Name = ClearPartName, Type = typeof(Button))]
public class AntPasswordBox : Control
{
    private const string HostPartName = "PART_PasswordHost";
    private const string ClearPartName = "PART_ClearButton";
    private readonly PasswordBox _editor = new();
    private ContentPresenter? _host;
    private Button? _clearButton;

    static AntPasswordBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AntPasswordBox), new FrameworkPropertyMetadata(typeof(AntPasswordBox)));
        TemplateProperty.OverrideMetadata(typeof(AntPasswordBox), new FrameworkPropertyMetadata(null, OnTemplateChanged));
    }

    public AntPasswordBox()
    {
        AddLogicalChild(_editor);
        _editor.PasswordChanged += OnPasswordChanged;
        Unloaded += (_, _) => Clear();
    }

    protected override IEnumerator LogicalChildren => new[] { _editor }.GetEnumerator();

    public static readonly DependencyProperty SizeProperty = AntThemeScope.SizeProperty.AddOwner(typeof(AntPasswordBox));
    public ControlSize Size { get => (ControlSize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder), typeof(string), typeof(AntPasswordBox), new FrameworkPropertyMetadata(string.Empty));
    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    public static readonly DependencyProperty AllowClearProperty = DependencyProperty.Register(
        nameof(AllowClear), typeof(bool), typeof(AntPasswordBox), new FrameworkPropertyMetadata(false));
    public bool AllowClear { get => (bool)GetValue(AllowClearProperty); set => SetValue(AllowClearProperty, value); }

    public static readonly DependencyProperty MaxLengthProperty = DependencyProperty.Register(
        nameof(MaxLength), typeof(int), typeof(AntPasswordBox), new FrameworkPropertyMetadata(0), value => (int)value >= 0);
    public int MaxLength { get => (int)GetValue(MaxLengthProperty); set => SetValue(MaxLengthProperty, value); }

    public static readonly DependencyProperty PasswordCharProperty = DependencyProperty.Register(
        nameof(PasswordChar), typeof(char), typeof(AntPasswordBox),
        new FrameworkPropertyMetadata(PasswordBox.PasswordCharProperty.DefaultMetadata.DefaultValue));
    public char PasswordChar { get => (char)GetValue(PasswordCharProperty); set => SetValue(PasswordCharProperty, value); }

    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(InputVariant), typeof(AntPasswordBox), new FrameworkPropertyMetadata(InputVariant.Outlined),
        value => Enum.IsDefined(typeof(InputVariant), value));
    public InputVariant Variant { get => (InputVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
        nameof(Status), typeof(ControlStatus), typeof(AntPasswordBox), new FrameworkPropertyMetadata(ControlStatus.Default),
        value => Enum.IsDefined(typeof(ControlStatus), value));
    public ControlStatus Status { get => (ControlStatus)GetValue(StatusProperty); set => SetValue(StatusProperty, value); }

    private static readonly DependencyPropertyKey IsEmptyPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsEmpty), typeof(bool), typeof(AntPasswordBox), new FrameworkPropertyMetadata(true));
    public static readonly DependencyProperty IsEmptyProperty = IsEmptyPropertyKey.DependencyProperty;
    public bool IsEmpty => (bool)GetValue(IsEmptyProperty);

    // The native control returns a copy; the caller owns and disposes it.
    public SecureString SecurePassword { get { VerifyAccess(); return _editor.SecurePassword; } }
    public event EventHandler? PasswordChanged;

    public void Clear() { VerifyAccess(); _editor.Clear(); }
    public bool FocusPassword() { VerifyAccess(); ApplyTemplate(); return _editor.Focus(); }

    public override void OnApplyTemplate()
    {
        DetachTemplate();
        base.OnApplyTemplate();
        _host = GetTemplateChild(HostPartName) as ContentPresenter
            ?? throw new InvalidOperationException("AntPasswordBox requires a ContentPresenter named PART_PasswordHost.");
        _host.SetCurrentValue(ContentPresenter.ContentProperty, _editor);
        _clearButton = GetTemplateChild(ClearPartName) as Button;
        if (_clearButton != null) { _clearButton.Click += OnClearClick; }
    }

    private void OnPasswordChanged(object sender, RoutedEventArgs args)
    {
        using (SecureString password = _editor.SecurePassword)
        {
            SetValue(IsEmptyPropertyKey, password.Length == 0);
        }
        PasswordChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnClearClick(object sender, RoutedEventArgs args)
    {
        if (IsEnabled && AllowClear && !IsEmpty) { Clear(); _editor.Focus(); }
        args.Handled = true;
    }

    private void DetachTemplate()
    {
        if (_clearButton != null) { _clearButton.Click -= OnClearClick; _clearButton = null; }
        if (_host != null) { _host.SetCurrentValue(ContentPresenter.ContentProperty, null); _host = null; }
    }

    private static void OnTemplateChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        ((AntPasswordBox)sender).DetachTemplate();
}
