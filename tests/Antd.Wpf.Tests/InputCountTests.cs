using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using Antd.Wpf.Controls;
using Antd.Wpf.Theming;
using Xunit;

namespace Antd.Wpf.Tests;

[Trait("Category", "UIIntegration")]
public sealed class InputCountTests
{
    [Fact]
    public void Input_CountTracksLimitBoundariesWithoutTruncatingBoundText() => Sta.Run(() =>
    {
        var source = new TextBox { Text = "甲乙" };
        var input = new AntInput { ShowCount = true, MaxLength = 3, AllowClear = true };
        input.SetBinding(TextBox.TextProperty, new Binding(nameof(TextBox.Text))
        {
            Source = source,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        });
        Window window = Sta.Show(new AntThemeScope { Content = input });
        try
        {
            var count = (TextBlock)input.Template.FindName("Count", input);
            Assert.Equal("2 / 3", ReadCount(count));
            source.Text = "甲乙丙";
            Sta.Pump();
            Assert.Equal("3 / 3", ReadCount(count));
            Assert.False(input.IsOverMaxLength);
            Assert.Equal(input.FindResource("Antd.Brush.Tertiary"), count.Foreground);
            Assert.Equal(new Thickness(4, 0, 0, 0), count.Margin);
            input.Suffix = "suffix";
            var suffix = (ContentPresenter)input.Template.FindName("Suffix", input);
            Sta.Pump();
            Assert.True(count.TranslatePoint(new Point(count.ActualWidth, 0), input).X <= suffix.TranslatePoint(new Point(), input).X);
            source.Text = "甲乙丙丁";
            Sta.Pump();
            Assert.Equal("4 / 3", ReadCount(count));
            Assert.True(input.IsOverMaxLength);
            Assert.Equal("甲乙丙丁", input.Text);
            Assert.Equal("甲乙丙丁", source.Text);
            Assert.Equal(input.FindResource("Antd.Brush.Danger"), input.Foreground);
            Assert.Equal(input.Foreground, count.Foreground);
            Assert.Equal(ControlStatus.Default, input.Status);
            input.MaxLength = 4;
            Sta.Pump();
            Assert.Equal("4 / 4", ReadCount(count));
            Assert.False(input.IsOverMaxLength);
            Assert.Equal(input.FindResource("Antd.Brush.Text"), input.Foreground);
            input.MaxLength = 0;
            Sta.Pump();
            Assert.Equal("4", ReadCount(count));
            Assert.False(input.IsOverMaxLength);
            Assert.True(input.Focus());
            input.Select(1, 2);
            object editor = input.Template.FindName("PART_ContentHost", input);
            input.ShowCount = false;
            Sta.Pump();
            Assert.Equal(Visibility.Collapsed, count.Visibility);
            Assert.Equal(1, input.SelectionStart);
            Assert.Equal(2, input.SelectionLength);
            Assert.True(input.IsKeyboardFocused);
            Assert.Same(editor, input.Template.FindName("PART_ContentHost", input));
            input.ShowCount = true;
            input.MaxLength = 3;
            ((Button)input.Template.FindName("PART_ClearButton", input)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Sta.Pump();
            Assert.Equal("0 / 3", ReadCount(count));
            Assert.Equal(string.Empty, source.Text);
            input.Undo();
            Sta.Pump();
            Assert.Equal("甲乙丙丁", source.Text);
            Assert.Equal("4 / 3", ReadCount(count));
            Assert.True(BindingOperations.IsDataBound(input, TextBox.TextProperty));
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Input_CountRecreatesBindingsAfterTemplateAndLimitChanges() => Sta.Run(() =>
    {
        var source = new Border { Tag = 2 };
        var input = new AntInput { ShowCount = true, Text = "三字文", IsReadOnly = true };
        input.SetBinding(TextBox.MaxLengthProperty, new Binding(nameof(Border.Tag)) { Source = source });
        Window window = Sta.Show(new AntThemeScope { Content = input });
        try
        {
            Assert.True(input.IsOverMaxLength);
            ControlTemplate template = input.Template;
            input.Template = null;
            Sta.Pump();
            input.Template = template;
            Sta.Pump();
            var count = (TextBlock)input.Template.FindName("Count", input);
            Assert.Equal("3 / 2", ReadCount(count));
            source.Tag = 0;
            Sta.Pump();
            Assert.Equal("3", ReadCount(count));
            Assert.False(input.IsOverMaxLength);
            source.Tag = 2;
            Sta.Pump();
            Assert.Equal("3 / 2", ReadCount(count));
            Assert.True(input.IsOverMaxLength);
            Assert.True(BindingOperations.IsDataBound(input, TextBox.MaxLengthProperty));
            input.ShowCount = false;
            Sta.Pump();
            Assert.Equal(Visibility.Collapsed, count.Visibility);
            Assert.Equal("三字文", input.Text);
            Assert.True(input.IsOverMaxLength);
            Assert.Equal("Error", UIElementAutomationPeer.CreatePeerForElement(input).GetItemStatus());
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Input_CountUsesUtf16AndKeepsErrorSemanticsAcrossThemeAndRtl() => Sta.Run(() =>
    {
        var input = new AntInput { ShowCount = true, MaxLength = 3, Text = "😀e\u0301", FlowDirection = FlowDirection.RightToLeft };
        var scope = new AntThemeScope { Content = input };
        Window window = Sta.Show(scope);
        try
        {
            var count = (TextBlock)input.Template.FindName("Count", input);
            AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(input);
            Assert.Equal("4 / 3", ReadCount(count));
            Assert.Equal(FlowDirection.LeftToRight, count.FlowDirection);
            Assert.Equal("Error", peer.GetItemStatus());
            scope.Mode = ThemeMode.Dark;
            Sta.Pump();
            Assert.Equal(input.FindResource("Antd.Brush.Danger"), count.Foreground);
            Assert.Equal("😀e\u0301", input.Text);
            input.Status = ControlStatus.Warning;
            Assert.Equal("Error", peer.GetItemStatus());
            input.IsEnabled = false;
            Sta.Pump();
            Assert.Equal(input.FindResource("Antd.Brush.DisabledText"), count.Foreground);
            Assert.Equal("Error", peer.GetItemStatus());
            input.IsEnabled = true;
            input.MaxLength = 4;
            Sta.Pump();
            Assert.Equal("Warning", peer.GetItemStatus());
            Assert.Equal(input.FindResource("Antd.Brush.Tertiary"), count.Foreground);
            input.Status = ControlStatus.Default;
            input.MaxLength = 0;
            Sta.Pump();
            Assert.NotEqual("Error", peer.GetItemStatus());
        }
        finally { window.Close(); }
    });

    private static string ReadCount(TextBlock count) => new TextRange(count.ContentStart, count.ContentEnd).Text;
}
