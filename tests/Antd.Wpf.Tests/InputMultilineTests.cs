using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Antd.Wpf.Controls;
using Antd.Wpf.Theming;
using Xunit;

namespace Antd.Wpf.Tests;

[Trait("Category", "UIIntegration")]
public sealed class InputMultilineTests
{
    [Fact]
    public void Input_MultilineGrowsToMaxLinesThenScrollsToTheLastLine() => Sta.Run(() =>
    {
        AntInput input = CreateInput();
        var panel = new StackPanel();
        panel.Children.Add(input);
        Window window = Sta.Show(new AntThemeScope { Content = panel });
        try
        {
            var viewer = (ScrollViewer)input.Template.FindName("PART_ContentHost", input);
            Assert.Equal(ScrollBarVisibility.Auto, viewer.VerticalScrollBarVisibility);
            Assert.Equal(ScrollBarVisibility.Disabled, viewer.HorizontalScrollBarVisibility);
            Assert.InRange(input.ActualHeight, 53, 55);
            input.Text = "First\r\nSecond\r\nThird";
            Sta.Pump();
            Assert.InRange(input.ActualHeight, 75, 77);
            input.Text = "一\r\n二\r\n三\r\n四\r\n五\r\n六\r\n七";
            Sta.Pump();
            Assert.InRange(input.ActualHeight, 97, 99);
            Assert.Equal(Visibility.Visible, viewer.ComputedVerticalScrollBarVisibility);
            Assert.True(viewer.ExtentHeight > viewer.ViewportHeight);
            input.ScrollToEnd();
            Sta.Pump();
            Assert.True(viewer.VerticalOffset > 0);
            Rect last = input.GetRectFromCharacterIndex(input.Text.Length - 1);
            Assert.False(last.IsEmpty);
            Assert.InRange(last.Top, 0, input.ActualHeight);
            Assert.InRange(last.Bottom, 0, input.ActualHeight);
            input.Text = string.Empty;
            Sta.Pump();
            Assert.InRange(input.ActualHeight, 53, 55);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Input_MultilineLineLimitsSizeAndExplicitHeightUpdateInPlace() => Sta.Run(() =>
    {
        AntInput input = CreateInput();
        var panel = new StackPanel();
        panel.Children.Add(input);
        Window window = Sta.Show(new AntThemeScope { Content = panel });
        try
        {
            var viewer = (ScrollViewer)input.Template.FindName("PART_ContentHost", input);
            input.MinLines = 3;
            Sta.Pump();
            Assert.InRange(input.ActualHeight, 75, 77);
            input.Size = ControlSize.Large;
            Sta.Pump();
            Assert.InRange(input.ActualHeight, 87, 89);
            input.Text = "1\r\n2\r\n3\r\n4\r\n5\r\n6";
            input.MaxLines = 5;
            Sta.Pump();
            Assert.InRange(input.ActualHeight, 135, 137);
            input.Height = 80;
            Sta.Pump();
            Assert.Equal(80, input.ActualHeight);
            Assert.Equal(0, viewer.MinHeight);
            Assert.Equal(double.PositiveInfinity, viewer.MaxHeight);
            input.ClearValue(FrameworkElement.HeightProperty);
            Sta.Pump();
            Assert.InRange(input.ActualHeight, 135, 137);
            input.Padding = new Thickness(8);
            Sta.Pump();
            Assert.InRange(input.ActualHeight, 137, 139);
            Assert.Same(viewer, input.Template.FindName("PART_ContentHost", input));
            double height = input.ActualHeight;
            for (int i = 0; i < 5; i++)
            {
                Sta.Pump();
                Assert.Equal(height, input.ActualHeight);
                Assert.True(input.IsMeasureValid);
            }
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Input_MultilineHorizontalScrollbarPreservesVisibleRows() => Sta.Run(() =>
    {
        AntInput input = CreateInput();
        input.MinLines = 1;
        input.MaxLines = 2;
        input.TextWrapping = TextWrapping.NoWrap;
        input.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        input.Text = "A very long line that extends beyond the visible editor width.\r\nSecond\r\nThird";
        var panel = new StackPanel();
        panel.Children.Add(input);
        Window window = Sta.Show(new AntThemeScope { Content = panel });
        try
        {
            var viewer = (ScrollViewer)input.Template.FindName("PART_ContentHost", input);
            Assert.Equal(Visibility.Visible, viewer.ComputedHorizontalScrollBarVisibility);
            Assert.InRange(viewer.ViewportHeight, 43, 45);
            input.ScrollToEnd();
            Sta.Pump();
            Assert.True(viewer.VerticalOffset > 0);
            Assert.InRange(viewer.ViewportHeight, 43, 45);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Input_MultilineNaturalMetricsAndRetemplatePreserveText() => Sta.Run(() =>
    {
        AntInput input = CreateInput();
        input.Text = "甲\r\n乙\r\n丙\r\n丁\r\n戊";
        TextBlock.SetLineHeight(input, double.NaN);
        var panel = new StackPanel();
        panel.Children.Add(input);
        Window window = Sta.Show(new AntThemeScope { Content = panel });
        try
        {
            var original = (ScrollViewer)input.Template.FindName("PART_ContentHost", input);
            Assert.True(original.ExtentHeight > original.ViewportHeight);
            ControlTemplate template = input.Template;
            input.Template = null;
            Sta.Pump();
            input.Template = template;
            Sta.Pump();
            var replacement = (ScrollViewer)input.Template.FindName("PART_ContentHost", input);
            Assert.NotSame(original, replacement);
            Assert.Equal("甲\r\n乙\r\n丙\r\n丁\r\n戊", input.Text);
            Assert.True(replacement.ExtentHeight > replacement.ViewportHeight);
            input.MinHeight = 100;
            Sta.Pump();
            Assert.True(input.ActualHeight >= 100);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Input_MultilineWrapAndScrollbarOptionsReactWithoutReplacingEditor() => Sta.Run(() =>
    {
        AntInput input = CreateInput();
        input.MinLines = 1;
        input.MaxLines = 10;
        input.Width = 160;
        input.Text = "中文长段落随着可用宽度自动换行。A long paragraph wraps as the available width changes.";
        var panel = new StackPanel();
        panel.Children.Add(input);
        Window window = Sta.Show(new AntThemeScope { Content = panel });
        try
        {
            var viewer = (ScrollViewer)input.Template.FindName("PART_ContentHost", input);
            double narrowHeight = input.ActualHeight;
            Assert.True(input.LineCount > 1);
            string original = input.Text;
            input.Width = 480;
            Sta.Pump();
            Assert.True(input.ActualHeight < narrowHeight);
            Assert.Equal(original, input.Text);
            input.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            input.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            input.TextWrapping = TextWrapping.NoWrap;
            Sta.Pump();
            Assert.Equal(ScrollBarVisibility.Hidden, viewer.VerticalScrollBarVisibility);
            Assert.Equal(ScrollBarVisibility.Auto, viewer.HorizontalScrollBarVisibility);
            Assert.Same(viewer, input.Template.FindName("PART_ContentHost", input));
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Input_MultilineThemeChangesPreserveBindingSelectionAndUndo() => Sta.Run(() =>
    {
        var source = new TextBox { Text = "甲\r\n乙" };
        AntInput input = CreateInput();
        input.SetBinding(TextBox.TextProperty, new Binding(nameof(TextBox.Text))
        {
            Source = source,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        });
        var scope = new AntThemeScope { Content = input };
        Window window = Sta.Show(scope);
        try
        {
            Assert.True(input.Focus());
            input.Select(0, 1);
            input.SelectedText = "新";
            Assert.Equal("新\r\n乙", source.Text);
            Assert.True(input.CanUndo);
            input.Undo();
            Assert.Equal("甲\r\n乙", input.Text);
            input.Redo();
            input.Select(0, 1);
            scope.Mode = ThemeMode.Dark;
            Sta.Pump();
            Assert.Equal("新\r\n乙", input.Text);
            Assert.Equal("新\r\n乙", source.Text);
            Assert.Equal(0, input.SelectionStart);
            Assert.Equal(1, input.SelectionLength);
            Assert.True(input.IsKeyboardFocused);
            Assert.True(BindingOperations.IsDataBound(input, TextBox.TextProperty));
            input.IsReadOnly = true;
            Assert.False(ApplicationCommands.Delete.CanExecute(null, input));
        }
        finally { window.Close(); }
    });

    private static AntInput CreateInput() => new()
    {
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        MinLines = 2,
        MaxLines = 4,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        VerticalContentAlignment = VerticalAlignment.Top,
        VerticalAlignment = VerticalAlignment.Top,
        Width = 240,
        UseLayoutRounding = false,
    };
}
