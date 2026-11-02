using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Antd.Wpf.Controls;
using Antd.Wpf.Theming;
using Xunit;

namespace Antd.Wpf.Tests;

[Trait("Category", "UIIntegration")]
public sealed class ButtonPaletteTests
{
    [Theory]
    [InlineData(ButtonVariant.Outlined, SemanticColor.Default, "#0958d9", 1, "#ffffff", 1)]
    [InlineData(ButtonVariant.Dashed, SemanticColor.Default, "#0958d9", 1, "#ffffff", 1)]
    [InlineData(ButtonVariant.Solid, SemanticColor.Default, "#ffffff", 1, "#000000", 0.95)]
    [InlineData(ButtonVariant.Filled, SemanticColor.Default, "#000000", 0.88, "#000000", 0.15)]
    [InlineData(ButtonVariant.Text, SemanticColor.Default, "#000000", 0.88, "#000000", 0.15)]
    [InlineData(ButtonVariant.Link, SemanticColor.Default, "#0958d9", 1, "#00ffffff", 1)]
    [InlineData(ButtonVariant.Outlined, SemanticColor.Primary, "#0958d9", 1, "#ffffff", 1)]
    [InlineData(ButtonVariant.Dashed, SemanticColor.Primary, "#0958d9", 1, "#ffffff", 1)]
    [InlineData(ButtonVariant.Solid, SemanticColor.Primary, "#ffffff", 1, "#0958d9", 1)]
    [InlineData(ButtonVariant.Filled, SemanticColor.Primary, "#1677ff", 1, "#91caff", 1)]
    [InlineData(ButtonVariant.Text, SemanticColor.Primary, "#0958d9", 1, "#91caff", 1)]
    [InlineData(ButtonVariant.Link, SemanticColor.Primary, "#0958d9", 1, "#00ffffff", 1)]
    [InlineData(ButtonVariant.Outlined, SemanticColor.Danger, "#d9363e", 1, "#ffffff", 1)]
    [InlineData(ButtonVariant.Dashed, SemanticColor.Danger, "#d9363e", 1, "#ffffff", 1)]
    [InlineData(ButtonVariant.Solid, SemanticColor.Danger, "#ffffff", 1, "#d9363e", 1)]
    [InlineData(ButtonVariant.Filled, SemanticColor.Danger, "#ff4d4f", 1, "#ffccc7", 1)]
    [InlineData(ButtonVariant.Text, SemanticColor.Danger, "#d9363e", 1, "#ffccc7", 1)]
    [InlineData(ButtonVariant.Link, SemanticColor.Danger, "#d9363e", 1, "#00ffffff", 1)]
    public void Button_PressedPaletteRespectsVariantAndSemanticColor(ButtonVariant variant, SemanticColor color,
        string foreground, double foregroundOpacity, string background, double backgroundOpacity) => Sta.Run(() =>
    {
        var button = new TestButton { Content = "Action", Variant = variant, SemanticColor = color };
        Window window = Sta.Show(new AntThemeScope { Content = button });
        try
        {
            // Space captures the mouse; keep physical pointer position out of this palette test.
            window.Left = -10000;
            window.Top = -10000;
            Mouse.Synchronize();
            Sta.Pump();
            Assert.False(button.IsMouseOver);
            Brush before = button.Background;
            button.Press();
            Sta.Pump();
            Assert.True(button.IsPressed);
            Assert.Equal((Color)ColorConverter.ConvertFromString(foreground), ((SolidColorBrush)button.Foreground).Color);
            Assert.Equal(foregroundOpacity, button.Foreground.Opacity);
            Assert.Equal((Color)ColorConverter.ConvertFromString(background), ((SolidColorBrush)button.Background).Color);
            Assert.Equal(backgroundOpacity, button.Background.Opacity);
            Assert.Equal(new Thickness(1), button.BorderThickness);
            if (variant is ButtonVariant.Outlined or ButtonVariant.Dashed)
            {
                Assert.Equal(((SolidColorBrush)button.Foreground).Color, ((SolidColorBrush)button.BorderBrush).Color);
            }
            else
            {
                Assert.Equal(0, ((SolidColorBrush)button.BorderBrush).Color.A);
            }

            button.Release();
            Sta.Pump();
            Assert.False(button.IsPressed);
            Assert.False(button.IsMouseCaptured);
            Assert.False(button.IsMouseOver);
            Assert.Same(before, button.Background);
            button.IsEnabled = false;
            Sta.Pump();
            Assert.Equal(0.25, button.Foreground.Opacity);
            if (variant is ButtonVariant.Text or ButtonVariant.Link)
            {
                Assert.Equal(0, ((SolidColorBrush)button.Background).Color.A);
                Assert.Equal(0, ((SolidColorBrush)button.BorderBrush).Color.A);
            }
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(ThemeMode.Light, "#000000", 1, "#ffffff")]
    [InlineData(ThemeMode.Dark, "#ffffff", 0.95, "#000000")]
    public void Button_DefaultSolidUsesOfficialNeutralPalette(ThemeMode mode, string background, double opacity, string foreground) => Sta.Run(() =>
    {
        var button = new AntButton { Content = "Neutral", Variant = ButtonVariant.Solid };
        Window window = Sta.Show(new AntThemeScope { Content = button, Mode = mode });
        try
        {
            Assert.Equal((Color)ColorConverter.ConvertFromString(background), ((SolidColorBrush)button.Background).Color);
            Assert.Equal(opacity, button.Background.Opacity);
            Assert.Equal((Color)ColorConverter.ConvertFromString(foreground), ((SolidColorBrush)button.Foreground).Color);
        }
        finally { window.Close(); }
    });

    private sealed class TestButton : AntButton
    {
        public void Press()
        {
            Assert.True(Focus());
            OnKeyDown(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(this), 0, Key.Space)
            { RoutedEvent = Keyboard.KeyDownEvent, Source = this });
        }
        public void Release()
        {
            OnKeyUp(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(this), 1, Key.Space)
            { RoutedEvent = Keyboard.KeyUpEvent, Source = this });
            // Synthetic keys do not release the physical mouse button; finish native capture explicitly.
            ReleaseMouseCapture();
            Mouse.Synchronize();
        }
    }
}
