using System;
using System.Security;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using Antd.Wpf.Controls;
using Antd.Wpf.Theming;
using Xunit;

namespace Antd.Wpf.Tests;

[Trait("Category", "UIIntegration")]
public sealed class PasswordBoxTests
{
    [Theory]
    [InlineData(ControlSize.Small, 24, 4)]
    [InlineData(ControlSize.Middle, 32, 6)]
    [InlineData(ControlSize.Large, 40, 8)]
    public void Password_SizeThemeAndDecorationsKeepNativeEditor(ControlSize size, double height, double radius) => Sta.Run(() =>
    {
        var input = new AntPasswordBox { Size = size, Placeholder = "密码", Width = 300 };
        var scope = new AntThemeScope { Content = input };
        Window window = Sta.Show(scope);
        try
        {
            PasswordBox editor = Editor(input);
            Assert.InRange(input.ActualHeight, height - 0.8, height + 0.8);
            Assert.Equal(new CornerRadius(radius), ((Border)input.Template.FindName("Chrome", input)).CornerRadius);
            Assert.Equal(Visibility.Visible, ((TextBlock)input.Template.FindName("Placeholder", input)).Visibility);
            editor.Password = "test-only";
            Assert.True(input.FocusPassword());
            scope.Mode = ThemeMode.Dark;
            input.AllowClear = true;
            input.FlowDirection = FlowDirection.RightToLeft;
            Sta.Pump();
            Assert.Same(editor, Editor(input));
            Assert.True(editor.IsKeyboardFocused);
            Assert.Equal(input.Foreground, editor.Foreground);
            Assert.Equal(FlowDirection.RightToLeft, editor.FlowDirection);
            using SecureString password = input.SecurePassword;
            Assert.Equal(9, password.Length);
            Assert.Equal(Visibility.Collapsed, ((TextBlock)input.Template.FindName("Placeholder", input)).Visibility);
            foreach (InputVariant variant in Enum.GetValues(typeof(InputVariant)))
            {
                input.Variant = variant;
                Sta.Pump();
                Assert.Same(editor, Editor(input));
                Assert.Equal(variant == InputVariant.Underlined ? new Thickness(0, 0, 0, 1) :
                    variant == InputVariant.Borderless ? new Thickness(0) : new Thickness(1), input.BorderThickness);
            }
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Password_ClearAndSecureCopiesDoNotExposeAutomationValue() => Sta.Run(() =>
    {
        var input = new AntPasswordBox { AllowClear = true, MaxLength = 20, PasswordChar = '*' };
        AutomationProperties.SetName(input, "Account password");
        Window window = Sta.Show(new AntThemeScope { Content = input });
        try
        {
            PasswordBox editor = Editor(input);
            int changes = 0;
            input.PasswordChanged += (sender, _) => { Assert.Same(input, sender); changes++; };
            editor.Password = "test-only";
            Assert.Equal(1, changes);
            Assert.False(input.IsEmpty);
            Assert.Equal(20, editor.MaxLength);
            Assert.Equal('*', editor.PasswordChar);
            using SecureString snapshot = input.SecurePassword;
            snapshot.Clear();
            using (SecureString current = input.SecurePassword) { Assert.Equal(9, current.Length); }
            var peer = new PasswordBoxAutomationPeer(editor);
            Assert.True(peer.IsPassword());
            Assert.Equal("Account password", peer.GetName());
            input.Status = ControlStatus.Error;
            Sta.Pump();
            Assert.Equal("Error", peer.GetItemStatus());
            Assert.Equal(input.FindResource("Antd.Brush.Danger"), input.BorderBrush);
            input.Status = ControlStatus.Default;
            Sta.Pump();
            Assert.Equal(string.Empty, peer.GetItemStatus());
            var value = (IValueProvider)peer.GetPattern(PatternInterface.Value);
            Assert.Equal(string.Empty, value.Value);
            var clear = (Button)input.Template.FindName("PART_ClearButton", input);
            input.AllowClear = false;
            clear.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            using (SecureString current = input.SecurePassword) { Assert.Equal(9, current.Length); }
            Assert.Equal(1, changes);
            input.AllowClear = true;
            clear.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.True(input.IsEmpty);
            Assert.Equal(2, changes);
            Assert.True(editor.IsKeyboardFocused);
            using (SecureString current = input.SecurePassword) { Assert.Equal(0, current.Length); }
            Assert.Equal(Visibility.Collapsed, clear.Visibility);
            Assert.Throws<ArgumentException>(() => input.MaxLength = -1);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Password_RetemplateKeepsSecretAndDetachesOldClearButton() => Sta.Run(() =>
    {
        var input = new AntPasswordBox { AllowClear = true };
        Window window = Sta.Show(new AntThemeScope { Content = input });
        try
        {
            PasswordBox editor = Editor(input);
            editor.Password = "test-only";
            var oldClear = (Button)input.Template.FindName("PART_ClearButton", input);
            input.Template = (ControlTemplate)XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><ContentPresenter x:Name='PART_PasswordHost' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'/></ControlTemplate>");
            Sta.Pump();
            Assert.Same(editor, Editor(input));
            oldClear.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            using (SecureString current = input.SecurePassword) { Assert.Equal(9, current.Length); }
            input.Clear();
            Assert.True(input.IsEmpty);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Password_DisabledClearIsGuardedAndUnloadErasesOwnedSecret() => Sta.Run(() =>
    {
        var input = new AntPasswordBox { AllowClear = true };
        var panel = new StackPanel();
        panel.Children.Add(input);
        Window window = Sta.Show(new AntThemeScope { Content = panel });
        try
        {
            PasswordBox editor = Editor(input);
            editor.Password = "test-only";
            input.IsEnabled = false;
            Sta.Pump();
            Assert.False(editor.IsEnabled);
            ((Button)input.Template.FindName("PART_ClearButton", input)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            using (SecureString current = input.SecurePassword) { Assert.Equal(9, current.Length); }
            panel.Children.Remove(input);
            Sta.Pump();
            using (SecureString current = input.SecurePassword) { Assert.Equal(0, current.Length); }
            panel.Children.Add(input);
            Sta.Pump();
            Assert.True(input.IsEmpty);
            Assert.Same(editor, Editor(input));
        }
        finally { window.Close(); }
    });

    private static PasswordBox Editor(AntPasswordBox input) =>
        Assert.IsType<PasswordBox>(((ContentPresenter)input.Template.FindName("PART_PasswordHost", input)).Content);

    [Fact]
    public void Password_MissingRequiredHostHasDiagnostic() => Sta.Run(() =>
    {
        var input = new AntPasswordBox
        {
            Template = (ControlTemplate)XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Border /></ControlTemplate>"),
        };
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => input.ApplyTemplate());
        Assert.Contains("PART_PasswordHost", error.Message);
    });
}
