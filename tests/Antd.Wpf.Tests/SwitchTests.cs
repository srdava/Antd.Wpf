using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Antd.Wpf.Controls;
using Antd.Wpf.Theming;
using Xunit;

namespace Antd.Wpf.Tests;

[Trait("Category", "UIIntegration")]
public sealed class SwitchTests
{
    [Fact]
    public void Switch_ClickAndSpacePreserveTwoWayBindingAndCommandOrder() => Sta.Run(() =>
    {
        var model = new Model();
        var control = new TestSwitch { IsThreeState = true };
        control.SetBinding(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty,
            new Binding(nameof(Model.Value)) { Source = model, Mode = BindingMode.TwoWay });
        bool? commandValue = null;
        int calls = 0;
        control.Command = new ActionCommand(() => { calls++; commandValue = model.Value; });
        Window window = Sta.Show(new AntThemeScope { Content = control });
        try
        {
            Assert.False(control.IsThreeState);
            Assert.Null(model.Value);
            Assert.False(control.IsChecked);
            var toggle = (IToggleProvider)UIElementAutomationPeer.CreatePeerForElement(control).GetPattern(PatternInterface.Toggle);
            Assert.Equal(System.Windows.Automation.ToggleState.Off, toggle.ToggleState);
            control.Invoke();
            Assert.True(model.Value);
            Assert.True(commandValue);
            Assert.Equal(1, calls);
            control.Space();
            Assert.False(model.Value);
            Assert.False(commandValue);
            Assert.Equal(2, calls);
            Assert.NotNull(BindingOperations.GetBindingExpression(control, System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty));
            model.Value = true;
            Sta.Pump();
            Assert.True(control.IsChecked);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Switch_LoadingAndDisabledBlockToggleAndPreserveEnabledBinding() => Sta.Run(() =>
    {
        var control = new TestSwitch { IsChecked = true };
        var source = new CheckBox { IsChecked = true };
        control.SetBinding(UIElement.IsEnabledProperty, new Binding(nameof(CheckBox.IsChecked)) { Source = source });
        int calls = 0;
        control.Command = new ActionCommand(() => calls++);
        Window window = Sta.Show(new AntThemeScope { Content = control });
        try
        {
            var peer = UIElementAutomationPeer.CreatePeerForElement(control);
            var toggle = (IToggleProvider)peer.GetPattern(PatternInterface.Toggle);
            control.IsLoading = true;
            control.Invoke();
            control.Space();
            Assert.True(control.IsChecked);
            Assert.Equal(0, calls);
            Assert.False(control.IsEnabled);
            Assert.Equal("Loading", peer.GetItemStatus());
            Assert.Equal(System.Windows.Automation.ToggleState.On, toggle.ToggleState);
            Assert.Throws<System.Windows.Automation.ElementNotEnabledException>(() => toggle.Toggle());
            control.IsLoading = false;
            Assert.True(control.IsEnabled);
            Assert.NotEqual("Loading", peer.GetItemStatus());
            Assert.NotNull(BindingOperations.GetBindingExpression(control, UIElement.IsEnabledProperty));
            source.IsChecked = false;
            Sta.Pump();
            control.Invoke();
            Assert.True(control.IsChecked);
            Assert.Equal(0, calls);
            source.IsChecked = true;
            Sta.Pump();
            control.Invoke();
            Assert.False(control.IsChecked);
            Assert.Equal(1, calls);
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(ControlSize.Small, 12, 16)]
    [InlineData(ControlSize.Middle, 18, 22)]
    [InlineData(ControlSize.Large, 18, 22)]
    public void Switch_ContentGeometryThemeAndRtlKeepState(ControlSize size, double thumbSize, double height) => Sta.Run(() =>
    {
        var control = new TestSwitch { CheckedContent = "开", UncheckedContent = "关", IsChecked = null };
        var scope = new AntThemeScope { Content = control, Size = size, MotionEnabled = false };
        Window window = Sta.Show(scope);
        try
        {
            var label = (ContentPresenter)control.Template.FindName("Label", control);
            var thumb = (Border)control.Template.FindName("Thumb", control);
            Assert.Equal("关", label.Content);
            Assert.Equal(HorizontalAlignment.Left, thumb.HorizontalAlignment);
            Assert.Equal(thumbSize, thumb.Width);
            Assert.Equal(thumbSize, thumb.Height);
            Assert.Equal(height, control.MinHeight);
            Assert.Equal(12, control.FontSize);
            Assert.Equal(new Thickness(2), thumb.Margin);
            Assert.Equal(new CornerRadius(size == ControlSize.Small ? 8 : 11), ((Border)control.Template.FindName("Track", control)).CornerRadius);
            control.Invoke();
            Sta.Pump();
            Assert.True(control.IsChecked);
            Assert.Equal("开", label.Content);
            double checkedLeft = control.ActualWidth - thumb.ActualWidth - 2;
            Assert.InRange(thumb.TranslatePoint(new Point(), control).X, checkedLeft - 0.81, checkedLeft + 0.81);
            scope.Mode = ThemeMode.Dark;
            control.FlowDirection = FlowDirection.RightToLeft;
            Sta.Pump();
            Assert.True(control.IsChecked);
            Assert.Equal("开", label.Content);
            Assert.Equal(FlowDirection.RightToLeft, thumb.FlowDirection);
            Assert.Equal(((SolidColorBrush)scope.FindResource("Antd.Brush.Primary")).Color, ((SolidColorBrush)control.Background).Color);
            scope.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("/Antd.Wpf;component/Themes/Tokens/HighContrast.xaml", UriKind.Relative),
            });
            Sta.Pump();
            Assert.Equal(SystemColors.HighlightColor, ((SolidColorBrush)control.Background).Color);
            Assert.Equal(SystemColors.HighlightTextColor, ((SolidColorBrush)control.Foreground).Color);
            control.IsChecked = false;
            Sta.Pump();
            Assert.Equal(SystemColors.WindowTextColor, ((SolidColorBrush)control.Background).Color);
            Assert.Equal(SystemColors.HighlightTextColor, ((SolidColorBrush)control.Foreground).Color);
            control.IsLoading = true;
            Sta.Pump();
            Assert.Equal(Visibility.Visible, ((TextBlock)control.Template.FindName("Busy", control)).Visibility);
        }
        finally { window.Close(); }
    });

    private sealed class TestSwitch : AntSwitch
    {
        public void Invoke() => OnClick();
        public void Space()
        {
            if (IsEnabled) { Assert.True(Focus()); }
            var source = PresentationSource.FromVisual(this);
            OnKeyDown(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Space) { RoutedEvent = Keyboard.KeyDownEvent, Source = this });
            OnKeyUp(new KeyEventArgs(Keyboard.PrimaryDevice, source, 1, Key.Space) { RoutedEvent = Keyboard.KeyUpEvent, Source = this });
        }
    }

    private sealed class ActionCommand(Action action) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => action();
    }

    private sealed class Model : INotifyPropertyChanged
    {
        private bool? _value;
        public event PropertyChangedEventHandler? PropertyChanged;
        public bool? Value
        {
            get => _value;
            set { _value = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value))); }
        }
    }
}
