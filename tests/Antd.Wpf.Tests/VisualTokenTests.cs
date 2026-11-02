using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Antd.Wpf.Controls;
using Antd.Wpf.Theming;
using Xunit;

namespace Antd.Wpf.Tests;

[Trait("Category", "UIIntegration")]
public sealed class VisualTokenTests
{
    [Fact]
    public void Select_LargeOwnerKeepsOfficialOptionMetrics() => Sta.Run(() =>
    {
        var select = new AntSelect { Size = ControlSize.Large, ItemsSource = new[] { "Selected", "Other" }, SelectedIndex = 0 };
        Window window = Sta.Show(new AntThemeScope { Content = select });
        try
        {
            select.IsDropDownOpen = true;
            Sta.Pump();
            var item = Assert.IsType<ComboBoxItem>(select.ItemContainerGenerator.ContainerFromIndex(0));
            Assert.Equal(14, item.FontSize);
            Assert.Equal(new Thickness(12, 5, 12, 5), item.Padding);
            Assert.Equal(FontWeights.SemiBold, item.FontWeight);
            Assert.Equal(Colors.Black, ((SolidColorBrush)item.Foreground).Color);
            Assert.Equal(0.88, item.Foreground.Opacity);
            Assert.InRange(item.ActualHeight, 31.19, 32.81);
            Assert.Equal(new Thickness(0), ((Border)VisualTreeHelper.GetChild(item, 0)).Margin);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Controls_MixedSizesKeepTheirHeightInHorizontalLayout() => Sta.Run(() =>
    {
        var panel = new WrapPanel { Width = 500 };
        var small = new Control[]
        {
            new AntButton { Content = "Small", Size = ControlSize.Small, Width = 60 },
            new AntInput { Text = "Small", Size = ControlSize.Small, Width = 60 },
            new AntSelect { Placeholder = "Small", Size = ControlSize.Small, Width = 60 },
        };
        foreach (Control control in small) { panel.Children.Add(control); }
        panel.Children.Add(new AntButton { Content = "Large", Size = ControlSize.Large, Width = 60 });
        panel.Children.Add(new AntInput { Text = "Large", Size = ControlSize.Large, Width = 60 });
        panel.Children.Add(new AntSelect { Placeholder = "Large", Size = ControlSize.Large, Width = 60 });
        var checkbox = new AntCheckBox { Content = "Check", Width = 60 };
        panel.Children.Add(checkbox);
        Window window = Sta.Show(new AntThemeScope { Content = panel });
        try
        {
            foreach (Control control in small) { Assert.InRange(control.ActualHeight, 23.19, 24.81); }
            Assert.InRange(checkbox.ActualHeight, 21.19, 22.81);
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(ControlSize.Small, 24, 14, 4, 7, 0, 0)]
    [InlineData(ControlSize.Middle, 32, 14, 6, 15, 0, 4)]
    [InlineData(ControlSize.Large, 40, 16, 8, 15, 0, 7)]
    public void Controls_UseOfficialSizePaddingRadiusAndZeroOuterMargin(
        ControlSize size, double height, double fontSize, double radius, double buttonHorizontal, double buttonVertical, double inputVertical) => Sta.Run(() =>
    {
        var button = new AntButton { Content = "Button" };
        var input = new AntInput { Text = "Input" };
        var select = new AntSelect { ItemsSource = new[] { "Option" }, SelectedIndex = 0 };
        var panel = new StackPanel();
        panel.Children.Add(button);
        panel.Children.Add(input);
        panel.Children.Add(select);
        Window window = Sta.Show(new AntThemeScope { Content = panel, Size = size });
        try
        {
            foreach (Control control in new Control[] { button, input, select })
            {
                Assert.Equal(new Thickness(0), control.Margin);
                Assert.Equal(fontSize, control.FontSize);
                Assert.InRange(control.ActualHeight, height - 0.81, height + 0.81);
            }

            Assert.Equal(new Thickness(buttonHorizontal, buttonVertical, buttonHorizontal, buttonVertical), button.Padding);
            Assert.Equal(new Thickness(size == ControlSize.Small ? 7 : 11, inputVertical, size == ControlSize.Small ? 7 : 11, inputVertical), input.Padding);
            Assert.Equal(new CornerRadius(radius), ((Border)button.Template.FindName("Chrome", button)).CornerRadius);
            Assert.Equal(new CornerRadius(radius), ((Border)input.Template.FindName("Chrome", input)).CornerRadius);
            Assert.Equal(new CornerRadius(radius), ((Border)select.Template.FindName("Chrome", select)).CornerRadius);
            Assert.Equal(new Thickness(0), ((ScrollViewer)input.Template.FindName("PART_ContentHost", input)).Padding);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Controls_DefaultGeometryResolvesWithoutThemeScope() => Sta.Run(() =>
    {
        var button = new AntButton { Size = ControlSize.Small, Content = "Default theme" };
        Window window = Sta.Show(button);
        try
        {
            Assert.Equal(new Thickness(7, 0, 7, 0), button.Padding);
            Assert.Equal(new CornerRadius(4), ((Border)button.Template.FindName("Chrome", button)).CornerRadius);
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(ThemeMode.Light, "#000000", 0.88, 0.04, "#e6f4ff")]
    [InlineData(ThemeMode.Dark, "#ffffff", 0.85, 0.08, "#15325b")]
    public void Palette_PreservesOfficialAlphaAndSharedFrozenBrushes(ThemeMode mode, string textColor, double opacity, double fillOpacity, string selectionColor) => Sta.Run(() =>
    {
        var first = new AntInput();
        var second = new AntInput();
        var panel = new StackPanel();
        panel.Children.Add(first);
        panel.Children.Add(second);
        var scope = new AntThemeScope { Content = panel, Mode = mode };
        Window window = Sta.Show(scope);
        try
        {
            Assert.Equal((Color)ColorConverter.ConvertFromString(textColor), ((SolidColorBrush)first.Foreground).Color);
            Assert.Equal(opacity, first.Foreground.Opacity);
            Assert.Same(first.Foreground, second.Foreground);
            Assert.True(first.Foreground.IsFrozen);
            Assert.Equal(fillOpacity, ((Brush)scope.FindResource("Antd.Brush.DisabledFill")).Opacity);
            Assert.Equal(0.25, ((Brush)scope.FindResource("Antd.Brush.Quaternary")).Opacity);
            Assert.Equal((Color)ColorConverter.ConvertFromString(selectionColor), ((SolidColorBrush)scope.FindResource("Antd.Brush.Selection")).Color);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Controls_LocalSpacingAndBrushOverridesSurviveThemeChanges() => Sta.Run(() =>
    {
        var input = new AntInput { Padding = new Thickness(3), Margin = new Thickness(9), Foreground = Brushes.Purple };
        var scope = new AntThemeScope { Content = input };
        Window window = Sta.Show(scope);
        try
        {
            scope.Mode = ThemeMode.Dark;
            scope.Size = ControlSize.Large;
            Sta.Pump();
            Assert.Equal(new Thickness(3), input.Padding);
            Assert.Equal(new Thickness(9), input.Margin);
            Assert.Same(Brushes.Purple, input.Foreground);
            Assert.Equal(new Thickness(3), ((Border)input.Template.FindName("Chrome", input)).Padding);
            Assert.Equal(new Thickness(0), ((ScrollViewer)input.Template.FindName("PART_ContentHost", input)).Padding);
        }
        finally { window.Close(); }
    });
}
