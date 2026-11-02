using System;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Antd.Wpf.Controls;
using Antd.Wpf.Theming;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Antd.Wpf.Tests;

[Trait("Category", "UIIntegration")]
public sealed class M0BehaviorTests
{
    [Fact]
    public void Theme_ChangesPreserveEditorAndLocalOverrides() => Sta.Run(() =>
    {
        var model = new Model { Text = "中文 input" };
        var input = new AntInput();
        input.SetBinding(TextBox.TextProperty, new Binding(nameof(Model.Text)) { Source = model, Mode = BindingMode.TwoWay });
        var local = new AntInput { Foreground = Brushes.Magenta, Size = ControlSize.Large };
        var panel = new StackPanel();
        panel.Children.Add(input);
        panel.Children.Add(local);
        var scope = new AntThemeScope { Content = panel };
        Window window = Sta.Show(scope);
        try
        {
            input.Select(1, 3);
            object templateRoot = input.Template.FindName("PART_ContentHost", input);
            Color light = ((SolidColorBrush)input.Background).Color;
            scope.Mode = ThemeMode.Dark;
            scope.Size = ControlSize.Small;
            Sta.Pump();
            Assert.NotEqual(light, ((SolidColorBrush)input.Background).Color);
            Assert.Same(templateRoot, input.Template.FindName("PART_ContentHost", input));
            Assert.Equal("中文 input", input.Text);
            Assert.Equal(1, input.SelectionStart);
            Assert.Equal(3, input.SelectionLength);
            Assert.NotNull(BindingOperations.GetBindingExpression(input, TextBox.TextProperty));
            Assert.Equal(ControlSize.Small, input.Size);
            Assert.Equal(ControlSize.Large, local.Size);
            Assert.Same(Brushes.Magenta, local.Foreground);
            Assert.IsAssignableFrom<TextBoxAutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(input));
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Theme_NestedScopeOverridesParentAndSurvivesRepeatedChanges() => Sta.Run(() =>
    {
        var input = new AntInput();
        var inner = new AntThemeScope { Mode = ThemeMode.Dark, Content = input };
        var outer = new AntThemeScope { Content = inner };
        Window window = Sta.Show(outer);
        try
        {
            Color expected = ((SolidColorBrush)input.Background).Color;
            for (int index = 0; index < 30; index++)
            {
                outer.Mode = index % 2 == 0 ? ThemeMode.Dark : ThemeMode.Light;
            }

            Sta.Pump();
            Assert.Equal(expected, ((SolidColorBrush)input.Background).Color);
            Assert.Single(outer.Resources.MergedDictionaries);
            Assert.Single(inner.Resources.MergedDictionaries);
            Assert.Throws<ArgumentException>(() => outer.Mode = (ThemeMode)42);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Button_LoadingPreventsCommandAndPreservesEnabledBinding() => Sta.Run(() =>
    {
        var model = new Model();
        var command = new CountingCommand();
        var button = new TestButton { Command = command };
        button.SetBinding(UIElement.IsEnabledProperty, new Binding(nameof(Model.Enabled)) { Source = model });
        button.Invoke();
        Assert.Equal(1, command.Count);
        button.IsLoading = true;
        button.Invoke();
        Assert.False(button.IsEnabled);
        Assert.Equal(1, command.Count);
        button.IsLoading = false;
        Assert.True(button.IsEnabled);
        Assert.NotNull(BindingOperations.GetBindingExpression(button, UIElement.IsEnabledProperty));
        command.Allowed = false;
        button.Invoke();
        Assert.Equal(1, command.Count);
        model.Enabled = false;
        button.IsLoading = true;
        button.IsLoading = false;
        Assert.False(button.IsEnabled);
        Assert.Throws<ArgumentException>(() => button.LoadingDelay = TimeSpan.FromTicks(-1));
    });

    [Fact]
    public void Theme_HighContrastResourcesUseSystemColors() => Sta.Run(() =>
    {
        var input = new AntInput();
        var scope = new AntThemeScope { Content = input };
        Window window = Sta.Show(scope);
        try
        {
            scope.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("/Antd.Wpf;component/Themes/Tokens/HighContrast.xaml", UriKind.Relative),
            });
            Sta.Pump();
            Assert.Equal(SystemColors.WindowColor, ((SolidColorBrush)input.Background).Color);
            Assert.Equal(SystemColors.WindowTextColor, ((SolidColorBrush)input.Foreground).Color);
            input.IsEnabled = false;
            Sta.Pump();
            Assert.Equal(SystemColors.GrayTextColor, ((SolidColorBrush)input.Foreground).Color);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Theme_PaletteReplacementInvalidatesOnceAndPreservesUserResources() => Sta.Run(() =>
    {
        var scope = new AntThemeScope();
        var custom = new ResourceDictionary { ["Antd.Brush.Text"] = Brushes.Purple };
        scope.Resources.MergedDictionaries.Add(custom);
        int changes = 0;
        ((INotifyCollectionChanged)scope.Resources.MergedDictionaries).CollectionChanged += (_, _) => changes++;
        scope.Mode = ThemeMode.Dark;
        Assert.Equal(SystemParameters.HighContrast ? 0 : 1, changes);
        Assert.Same(custom, scope.Resources.MergedDictionaries[1]);
        Assert.Same(Brushes.Purple, scope.FindResource("Antd.Brush.Text"));
        scope.Resources.MergedDictionaries.RemoveAt(0);
        scope.Mode = ThemeMode.Light;
        Assert.Equal(2, scope.Resources.MergedDictionaries.Count);
        Assert.Same(custom, scope.Resources.MergedDictionaries[1]);
    });

    [Theory]
    [InlineData(ControlSize.Small, 12)]
    [InlineData(ControlSize.Middle, 16)]
    [InlineData(ControlSize.Large, 20)]
    public void Button_RoundShapeUsesHalfHeightRadius(ControlSize size, double radius) => Sta.Run(() =>
    {
        var button = new AntButton { Shape = ButtonShape.Round, Size = size, Content = "Create" };
        Window window = Sta.Show(new AntThemeScope { Content = button });
        try
        {
            var chrome = (Border)button.Template.FindName("Chrome", button);
            Assert.Equal(new CornerRadius(radius), chrome.CornerRadius);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Button_CancelledDelayAndUnloadDoNotLeaveSpinner() => Sta.Run(() =>
    {
        var button = new AntButton { LoadingDelay = TimeSpan.FromMilliseconds(20), Content = "Save" };
        var scope = new AntThemeScope { Content = button, MotionEnabled = false };
        Window window = Sta.Show(scope);
        try
        {
            button.IsLoading = true;
            Assert.False(button.IsLoadingVisible);
            button.IsLoading = false;
            button.IsLoading = true;
            Sta.WaitUntil(() => button.IsLoadingVisible);
            scope.Content = null;
            Sta.Pump();
            Assert.False(button.IsLoadingVisible);
            scope.Content = button;
            Sta.WaitUntil(() => button.IsLoadingVisible);
            button.IsLoading = false;
            Assert.False(button.IsLoadingVisible);
            Assert.True(button.IsEnabled);
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(ButtonVariant.Outlined, "#FFFFFF", false)]
    [InlineData(ButtonVariant.Dashed, "#FFFFFF", true)]
    [InlineData(ButtonVariant.Solid, "#000000", false)]
    [InlineData(ButtonVariant.Filled, "#000000", false)]
    [InlineData(ButtonVariant.Text, "#00FFFFFF", false)]
    [InlineData(ButtonVariant.Link, "#00FFFFFF", false)]
    public void Button_VariantsLoadWithNativeContentAndSize(ButtonVariant variant, string background, bool dashed) => Sta.Run(() =>
    {
        var button = new AntButton { Content = "_Save", Variant = variant, Size = ControlSize.Large };
        Window window = Sta.Show(new AntThemeScope { Content = button, VerticalContentAlignment = VerticalAlignment.Top });
        try
        {
            Assert.Equal("_Save", button.Content);
            Assert.Equal(40, button.MinHeight);
            Assert.NotNull(button.Template.FindName("Chrome", button));
            Assert.Equal((Color)ColorConverter.ConvertFromString(background), ((SolidColorBrush)button.Background).Color);
            Assert.Equal(variant == ButtonVariant.Filled ? 0.04 : 1, button.Background.Opacity);
            var dashedBorder = (System.Windows.Shapes.Rectangle)button.Template.FindName("DashedBorder", button);
            Assert.Equal(dashed ? Visibility.Visible : Visibility.Collapsed, dashedBorder.Visibility);
            Assert.IsType<ButtonAutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(button));
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Button_InitialZeroDelayAndDelayBoundsAreEnforced() => Sta.Run(() =>
    {
        var button = new AntButton { IsLoading = true };
        Assert.False(button.IsLoadingVisible);
        Window window = Sta.Show(new AntThemeScope { Content = button, MotionEnabled = false });
        try
        {
            Assert.True(button.IsLoadingVisible);
            Assert.False(button.IsEnabled);
            button.IsLoading = false;
            button.LoadingDelay = TimeSpan.FromDays(1);
            Assert.Equal(TimeSpan.FromDays(1), button.LoadingDelay);
            Assert.Throws<ArgumentException>(() => button.LoadingDelay = TimeSpan.FromDays(1) + TimeSpan.FromTicks(1));
            Assert.False(button.IsLoadingVisible);
            Assert.True(button.IsEnabled);
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(InputVariant.Outlined, "#FFFFFF", 1)]
    [InlineData(InputVariant.Filled, "#000000", 1)]
    [InlineData(InputVariant.Borderless, "#00FFFFFF", 0)]
    [InlineData(InputVariant.Underlined, "#FFFFFF", 0)]
    public void Input_VariantsAndErrorStateUseThemeTokens(InputVariant variant, string background, double topBorder) => Sta.Run(() =>
    {
        var input = new AntInput { Variant = variant };
        Window window = Sta.Show(new AntThemeScope { Content = input });
        try
        {
            Assert.Equal((Color)ColorConverter.ConvertFromString(background), ((SolidColorBrush)input.Background).Color);
            Assert.Equal(variant == InputVariant.Filled ? 0.04 : 1, input.Background.Opacity);
            Assert.Equal(topBorder, input.BorderThickness.Top);
            input.Status = ControlStatus.Error;
            Sta.Pump();
            Assert.Equal(Color.FromRgb(255, 77, 79), ((SolidColorBrush)input.BorderBrush).Color);
            Assert.Equal("Error", UIElementAutomationPeer.CreatePeerForElement(input).GetItemStatus());
            input.Status = ControlStatus.Warning;
            Sta.Pump();
            Assert.Equal(Color.FromRgb(250, 173, 20), ((SolidColorBrush)input.BorderBrush).Color);
            Assert.Equal("Warning", UIElementAutomationPeer.CreatePeerForElement(input).GetItemStatus());
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Input_ClearPreservesBindingUndoAndReadonlyValue() => Sta.Run(() =>
    {
        var model = new Model { Text = "保留 binding" };
        var input = new AntInput { AllowClear = true, ShowCount = true };
        input.SetBinding(TextBox.TextProperty, new Binding(nameof(Model.Text))
        {
            Source = model,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        });
        Window window = Sta.Show(new AntThemeScope { Content = input });
        try
        {
            var clear = (Button)input.Template.FindName("PART_ClearButton", input);
            clear.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.Equal(string.Empty, input.Text);
            Assert.Equal(string.Empty, model.Text);
            Assert.NotNull(BindingOperations.GetBindingExpression(input, TextBox.TextProperty));
            Assert.True(input.CanUndo);
            input.Undo();
            Assert.Equal("保留 binding", input.Text);
            input.IsReadOnly = false;
            input.IsEnabled = false;
            clear.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.Equal("保留 binding", input.Text);
            input.IsEnabled = true;
            input.IsReadOnly = true;
            clear.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.Equal("保留 binding", input.Text);
            input.Prefix = "¥";
            input.Suffix = "元";
            input.Select(1, 2);
            input.Prefix = null!;
            Sta.Pump();
            Assert.Equal(1, input.SelectionStart);
            Assert.Equal(2, input.SelectionLength);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Input_ReapplyingTemplateDetachesOldClearButton() => Sta.Run(() =>
    {
        var input = new AntInput { Text = "original", AllowClear = true };
        Window window = Sta.Show(new AntThemeScope { Content = input });
        try
        {
            var oldClear = (Button)input.Template.FindName("PART_ClearButton", input);
            ControlTemplate oldTemplate = input.Template;
            input.Template = new ControlTemplate(typeof(AntInput));
            input.ApplyTemplate();
            oldClear.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.Equal("original", input.Text);
            input.Template = oldTemplate;
            input.ApplyTemplate();
            var clear = (Button)input.Template.FindName("PART_ClearButton", input);
            clear.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.Equal(string.Empty, input.Text);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Select_OpenVirtualizesAndTracksOwnerTheme() => Sta.Run(() =>
    {
        var select = new AntSelect
        {
            ItemsSource = Enumerable.Range(0, 10000).Select(index => "Option " + index).ToArray(),
            SelectedIndex = 3,
            Width = 240,
            VerticalAlignment = VerticalAlignment.Top,
        };
        var scope = new AntThemeScope { Content = select };
        Window window = Sta.Show(scope);
        try
        {
            select.IsDropDownOpen = true;
            Sta.Pump();
            var popup = (Popup)select.Template.FindName("PART_Popup", select);
            var root = (Border)popup.Child;
            Assert.True(popup.IsOpen);
            int realized = Enumerable.Range(0, select.Items.Count).Count(index => select.ItemContainerGenerator.ContainerFromIndex(index) != null);
            Assert.InRange(realized, 1, 45);
            Color light = ((SolidColorBrush)root.Background).Color;
            scope.Mode = ThemeMode.Dark;
            Sta.Pump();
            Assert.NotEqual(light, ((SolidColorBrush)root.Background).Color);
            Assert.Equal("Option 3", select.SelectedItem);
            Assert.Single(root.Resources.MergedDictionaries);
            scope.Content = null;
            Sta.Pump();
            Assert.False(select.IsDropDownOpen);
            Assert.Empty(root.Resources.MergedDictionaries);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Select_RetemplateOpenPopupReleasesOwner() => Sta.Run(() =>
    {
        var select = new AntSelect { ItemsSource = new[] { "Alpha", "Beta" } };
        Window window = Sta.Show(new AntThemeScope { Content = select });
        try
        {
            select.IsDropDownOpen = true;
            Sta.Pump();
            var popup = (Popup)select.Template.FindName("PART_Popup", select);
            var root = (Border)popup.Child;
            Assert.Single(root.Resources.MergedDictionaries);
            select.Template = new ControlTemplate(typeof(AntSelect));
            select.ApplyTemplate();
            Sta.Pump();
            Assert.False(select.IsDropDownOpen);
            Assert.False(popup.IsOpen);
            Assert.Empty(root.Resources.MergedDictionaries);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Select_InitialOpenBindingSurvivesFirstTemplate() => Sta.Run(() =>
    {
        var model = new Model { IsOpen = true };
        var select = new AntSelect { ItemsSource = new[] { "Alpha", "Beta" } };
        select.SetBinding(ComboBox.IsDropDownOpenProperty, new Binding(nameof(Model.IsOpen))
        {
            Source = model,
            Mode = BindingMode.TwoWay,
        });
        Window window = Sta.Show(new AntThemeScope { Content = select });
        try
        {
            Sta.WaitUntil(() => select.IsDropDownOpen);
            Assert.True(model.IsOpen);
            Assert.True(((Popup)select.Template.FindName("PART_Popup", select)).IsOpen);
            Assert.NotNull(BindingOperations.GetBindingExpression(select, ComboBox.IsDropDownOpenProperty));
        }
        finally { window.Close(); }
    });

    private sealed class TestButton : AntButton
    {
        public void Invoke() => OnClick();
    }

    private sealed class CountingCommand : ICommand
    {
        public int Count { get; private set; }
        public bool Allowed { get; set; } = true;
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => Allowed;
        public void Execute(object? parameter) => Count++;
    }

    private sealed class Model : INotifyPropertyChanged
    {
        private string _text = string.Empty;
        private bool _enabled = true;
        private bool _isOpen;
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Text
        {
            get => _text;
            set { _text = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text))); }
        }

        public bool Enabled
        {
            get => _enabled;
            set { _enabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled))); }
        }

        public bool IsOpen
        {
            get => _isOpen;
            set { _isOpen = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOpen))); }
        }
    }
}
