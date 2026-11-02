using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Antd.Wpf.Controls;

namespace Antd.Wpf.Theming;

public enum ThemeMode { Light, Dark }

public class AntThemeScope : ContentControl
{
    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
        nameof(Mode), typeof(ThemeMode), typeof(AntThemeScope),
        new FrameworkPropertyMetadata(ThemeMode.Light, FrameworkPropertyMetadataOptions.Inherits, OnModeChanged),
        value => Enum.IsDefined(typeof(ThemeMode), value));
    public static readonly DependencyProperty SizeProperty = DependencyProperty.RegisterAttached(
        nameof(Size), typeof(ControlSize), typeof(AntThemeScope),
        new FrameworkPropertyMetadata(ControlSize.Middle, FrameworkPropertyMetadataOptions.Inherits),
        value => Enum.IsDefined(typeof(ControlSize), value));
    public static readonly DependencyProperty MotionEnabledProperty = DependencyProperty.RegisterAttached(
        nameof(MotionEnabled), typeof(bool), typeof(AntThemeScope),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));
    private ResourceDictionary? _lightResources;
    private ResourceDictionary? _darkResources;
    private ResourceDictionary? _activeResources;
    private ResourceDictionary? _highContrastResources;
    private bool _listening;

    private static readonly DependencyPropertyKey IsHighContrastPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "IsHighContrast", typeof(bool), typeof(AntThemeScope),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
    public static readonly DependencyProperty IsHighContrastProperty = IsHighContrastPropertyKey.DependencyProperty;

    public static bool GetIsHighContrast(DependencyObject element) => (bool)element.GetValue(IsHighContrastProperty);

    public AntThemeScope()
    {
        ApplyTheme();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public ThemeMode Mode
    {
        get => (ThemeMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public ControlSize Size
    {
        get => GetSize(this);
        set => SetSize(this, value);
    }

    public bool MotionEnabled
    {
        get => GetMotionEnabled(this);
        set => SetMotionEnabled(this, value);
    }

    public static ControlSize GetSize(DependencyObject element) => (ControlSize)element.GetValue(SizeProperty);
    public static void SetSize(DependencyObject element, ControlSize value) => element.SetValue(SizeProperty, value);
    public static bool GetMotionEnabled(DependencyObject element) => (bool)element.GetValue(MotionEnabledProperty);
    public static void SetMotionEnabled(DependencyObject element, bool value) => element.SetValue(MotionEnabledProperty, value);

    private static void OnModeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is AntThemeScope scope)
        {
            scope.ApplyTheme();
        }
    }

    private void ApplyTheme()
    {
        bool highContrast = SystemParameters.HighContrast;
        SetValue(IsHighContrastPropertyKey, highContrast);
        ResourceDictionary next = highContrast
            ? _highContrastResources ??= LoadResources("HighContrast")
            : Mode == ThemeMode.Dark
                ? _darkResources ??= LoadResources("Dark")
                : _lightResources ??= LoadResources("Light");
        if (ReferenceEquals(next, _activeResources) && Resources.MergedDictionaries.Contains(next))
        {
            return;
        }

        int index = _activeResources == null ? -1 : Resources.MergedDictionaries.IndexOf(_activeResources);
        if (index >= 0)
        {
            Resources.MergedDictionaries[index] = next;
        }
        else
        {
            Resources.MergedDictionaries.Insert(0, next);
        }

        _activeResources = next;
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        if (!_listening)
        {
            SystemParameters.StaticPropertyChanged += OnSystemPropertyChanged;
            _listening = true;
        }

        ApplyTheme();
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        if (_listening)
        {
            SystemParameters.StaticPropertyChanged -= OnSystemPropertyChanged;
            _listening = false;
        }
    }

    private void OnSystemPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(SystemParameters.HighContrast))
        {
            if (Dispatcher.CheckAccess())
            {
                ApplyTheme();
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (IsLoaded)
                    {
                        ApplyTheme();
                    }
                }));
            }
        }
    }

    private static ResourceDictionary LoadResources(string name) => new()
    {
        Source = new Uri($"/Antd.Wpf;component/Themes/Tokens/{name}.xaml", UriKind.Relative),
    };
}
