using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Antd.Wpf.Controls;
using Antd.Wpf.Theming;
using Xunit;

namespace Antd.Wpf.Tests;

[Trait("Category", "UIIntegration")]
public sealed class NativeSelectionTests
{
    [Fact]
    public void CheckBox_ClickSpaceBindingAndIndeterminateUseNativeSemantics() => Sta.Run(() =>
    {
        var model = new CheckBox { IsChecked = false };
        var command = new CounterCommand();
        var control = new MeasuredCheckBox { Content = "_Accept", Command = command };
        control.SetBinding(ToggleButton.IsCheckedProperty, new Binding(nameof(CheckBox.IsChecked)) { Source = model, Mode = BindingMode.TwoWay });
        Window window = Sta.Show(new AntThemeScope { Content = control });
        try
        {
            control.Invoke();
            Assert.True(model.IsChecked);
            Assert.Equal(1, command.Count);
            control.Space();
            Assert.False(model.IsChecked);
            Assert.Equal(2, command.Count);
            model.IsChecked = null;
            Sta.Pump();
            Assert.Null(control.IsChecked);
            Assert.Equal(1, ((Rectangle)control.Template.FindName("Indeterminate", control)).Opacity);
            Assert.Equal(0, ((Path)control.Template.FindName("Check", control)).Opacity);
            Assert.Equal(ToggleState.Indeterminate, ((IToggleProvider)UIElementAutomationPeer.CreatePeerForElement(control).GetPattern(PatternInterface.Toggle)).ToggleState);
            control.Invoke();
            Assert.False(model.IsChecked);
            control.IsThreeState = true;
            control.Invoke();
            Assert.True(model.IsChecked);
            control.Invoke();
            Assert.Null(model.IsChecked);
            Assert.NotNull(BindingOperations.GetBindingExpression(control, ToggleButton.IsCheckedProperty));
            control.IsEnabled = false;
            var toggle = (IToggleProvider)UIElementAutomationPeer.CreatePeerForElement(control).GetPattern(PatternInterface.Toggle);
            Assert.Throws<ElementNotEnabledException>(() => toggle.Toggle());
            Assert.Null(model.IsChecked);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void RadioButton_GroupIsMutuallyExclusiveAndIsolatedByWindow() => Sta.Run(() =>
    {
        var first = new TestRadio { Content = "One", GroupName = "choice" };
        var second = new TestRadio { Content = "Two", GroupName = "choice" };
        var other = new TestRadio { Content = "Other window", GroupName = "choice", IsChecked = true };
        var panel = new StackPanel();
        panel.Children.Add(first);
        panel.Children.Add(second);
        Window window = Sta.Show(new AntThemeScope { Content = panel });
        Window otherWindow = Sta.Show(new AntThemeScope { Content = other });
        try
        {
            first.Invoke();
            Assert.True(first.IsChecked);
            Assert.False(second.IsChecked);
            second.Invoke();
            second.Invoke();
            Assert.False(first.IsChecked);
            Assert.True(second.IsChecked);
            Assert.True(other.IsChecked);
            var peer = UIElementAutomationPeer.CreatePeerForElement(first);
            ((ISelectionItemProvider)peer.GetPattern(PatternInterface.SelectionItem)).Select();
            Assert.True(first.IsChecked);
            Assert.False(second.IsChecked);
            Assert.True(other.IsChecked);
            first.IsEnabled = false;
            Assert.Throws<ElementNotEnabledException>(() => ((ISelectionItemProvider)peer.GetPattern(PatternInterface.SelectionItem)).Select());
        }
        finally { otherWindow.Close(); window.Close(); }
    });

    [Fact]
    public void Selection_OfficialGeometryColorsGapsAndSharedDrawingSurviveThemeChanges() => Sta.Run(() =>
    {
        var checkbox = new MeasuredCheckBox { Content = "Check", IsChecked = true };
        var another = new AntCheckBox { Content = "Second" };
        var radio = new TestRadio { Content = "Radio", IsChecked = true };
        var panel = new StackPanel();
        panel.Children.Add(checkbox);
        panel.Children.Add(another);
        panel.Children.Add(radio);
        var scope = new AntThemeScope { Content = panel };
        Window window = Sta.Show(scope);
        try
        {
            var box = (Border)checkbox.Template.FindName("Box", checkbox);
            Assert.Equal(new CornerRadius(4), box.CornerRadius);
            Assert.InRange(box.ActualWidth, 15.2, 16.8);
            Assert.Equal(new Thickness(8, 0, 8, 0), checkbox.Padding);
            Assert.Equal(new Thickness(0), checkbox.Margin);
            Assert.Equal(new Thickness(0, 0, 8, 0), radio.Margin);
            Assert.Equal(new Thickness(8, 0, 8, 0), radio.Padding);
            var check = (Path)checkbox.Template.FindName("Check", checkbox);
            Assert.True(check.Data.IsFrozen);
            Assert.Same(check.Data, ((Path)another.Template.FindName("Check", another)).Data);
            Assert.Equal(6, ((Ellipse)radio.Template.FindName("Dot", radio)).Width);
            Assert.Equal(Color.FromRgb(22, 119, 255), ((SolidColorBrush)checkbox.Background).Color);
            scope.Mode = ThemeMode.Dark;
            Sta.Pump();
            Assert.Equal(Color.FromRgb(22, 104, 220), ((SolidColorBrush)checkbox.Background).Color);
            Assert.Equal(Color.FromRgb(22, 104, 220), ((SolidColorBrush)radio.Background).Color);
            radio.FlowDirection = FlowDirection.RightToLeft;
            Sta.Pump();
            Assert.Equal(new Thickness(8, 0, 0, 0), radio.Margin);
            checkbox.IsEnabled = false;
            Sta.Pump();
            Assert.Equal(0.08, ((SolidColorBrush)checkbox.Background).Opacity);
            Assert.Equal(0.25, ((SolidColorBrush)check.Stroke).Opacity);
            Assert.True(checkbox.IsChecked);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Selection_StateChangesDoNotRemeasureControls() => Sta.Run(() =>
    {
        var control = new MeasuredCheckBox { Content = "Stable layout" };
        var radio = new TestRadio { Content = "Stable radio" };
        var panel = new StackPanel();
        panel.Children.Add(control);
        panel.Children.Add(radio);
        Window window = Sta.Show(new AntThemeScope { Content = panel });
        try
        {
            control.Measures = 0;
            radio.Measures = 0;
            for (int index = 0; index < 30; index++)
            {
                control.IsChecked = index % 3 == 0 ? null : index % 3 == 1;
                radio.IsChecked = index % 2 == 0;
                Sta.Pump();
            }

            Assert.Equal(0, control.Measures);
            Assert.Equal(0, radio.Measures);
            Assert.False(control.IsChecked);
            Assert.False(radio.IsChecked);
        }
        finally { window.Close(); }
    });

    private sealed class MeasuredCheckBox : AntCheckBox
    {
        public int Measures { get; set; }
        public void Invoke() => OnClick();
        public void Space()
        {
            Assert.True(Focus());
            var source = PresentationSource.FromVisual(this);
            OnKeyDown(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Space) { RoutedEvent = Keyboard.KeyDownEvent, Source = this });
            OnKeyUp(new KeyEventArgs(Keyboard.PrimaryDevice, source, 1, Key.Space) { RoutedEvent = Keyboard.KeyUpEvent, Source = this });
        }

        protected override Size MeasureOverride(Size constraint)
        {
            Measures++;
            return base.MeasureOverride(constraint);
        }
    }

    private sealed class TestRadio : AntRadioButton
    {
        public int Measures { get; set; }
        public void Invoke() => OnClick();
        protected override Size MeasureOverride(Size constraint)
        {
            Measures++;
            return base.MeasureOverride(constraint);
        }
    }

    private sealed class CounterCommand : ICommand
    {
        public int Count { get; private set; }
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => Count++;
    }
}
