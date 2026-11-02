using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Antd.Wpf.Controls;
using Antd.Wpf.Gallery;
using Xunit;

namespace Antd.Wpf.Tests;

[Trait("Category", "UIIntegration")]
public sealed class GalleryPreviewTests
{
    [Theory]
    [InlineData(ControlSize.Small)]
    [InlineData(ControlSize.Middle)]
    [InlineData(ControlSize.Large)]
    public void Input_TextAndPlaceholderStayVerticallyCentered(ControlSize size) => Sta.Run(() =>
    {
        var input = new AntInput { Size = size, Width = 300, Height = 64, Placeholder = "Email address", Prefix = "@", Suffix = ".com" };
        Window window = Sta.Show(input);
        try
        {
            var placeholder = (TextBlock)input.Template.FindName("Placeholder", input);
            double placeholderCenter = placeholder.TranslatePoint(new Point(0, placeholder.ActualHeight / 2), input).Y;
            Assert.InRange(placeholderCenter, 31, 33);
            input.Text = "name@example.com";
            Sta.Pump();
            Rect caret = input.GetRectFromCharacterIndex(0);
            Assert.False(caret.IsEmpty);
            Assert.InRange(caret.Top + caret.Height / 2, 31, 33);
            input.VerticalContentAlignment = VerticalAlignment.Bottom;
            Sta.Pump();
            Assert.True(input.GetRectFromCharacterIndex(0).Top > caret.Top + 5);
            input.VerticalContentAlignment = VerticalAlignment.Top;
            Sta.Pump();
            Assert.True(input.GetRectFromCharacterIndex(0).Top < caret.Top - 5);
            input.VerticalContentAlignment = VerticalAlignment.Center;
            input.Height = double.NaN;
            input.Text = "居中文字 / Input";
            Sta.Pump();
            Rect defaultCaret = input.GetRectFromCharacterIndex(0);
            Assert.InRange(defaultCaret.Top + defaultCaret.Height / 2, input.ActualHeight / 2 - 1, input.ActualHeight / 2 + 1);
            input.Text = string.Empty;
            Sta.Pump();
            Assert.True(placeholder.IsVisible);
            Assert.InRange(placeholder.TranslatePoint(new Point(0, placeholder.ActualHeight / 2), input).Y,
                input.ActualHeight / 2 - 1, input.ActualHeight / 2 + 1);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Gallery_EmailAcceptsACompleteAddressWithoutDecoratedFragments() => Sta.Run(() =>
    {
        var window = new MainWindow();
        window.ShowInTaskbar = false;
        window.ShowActivated = false;
        window.Opacity = 0;
        window.Show();
        Sta.Pump();
        try
        {
            AntInput email = Descendants(window).OfType<AntInput>().Single(input => AutomationProperties.GetName(input) == "Email");
            Assert.Null(email.Prefix);
            Assert.Null(email.Suffix);
            Assert.Equal("name@example.com", email.Placeholder);
            email.Text = "alice@example.org";
            Sta.Pump();
            Assert.Equal("alice@example.org", email.Text);
            Assert.Equal(Visibility.Collapsed, ((TextBlock)email.Template.FindName("Placeholder", email)).Visibility);
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData("VisualMatrix.xaml", "开")]
    [InlineData("DividerMatrix.xaml", "Text wraps in the page; divider titles keep one line and use the available width.")]
    [InlineData("FlexMatrix.xaml", "Cancel / 取消")]
    [InlineData("SpaceMatrix.xaml", "Continue / 继续")]
    [InlineData("GridMatrix.xaml", "Native content / 原生内容")]
    [InlineData("LayoutMatrix.xaml", "Trigger extends into the content edge")]
    public void Gallery_ExampleWindowsCanScrollToLastExampleAfterResize(string file, string finalText) => Sta.Run(() =>
    {
        Window window = Open(file, 650, 420);
        try
        {
            if (window.Content is ScrollViewer scroll)
            {
                scroll.ScrollToBottom();
                scroll.ScrollToRightEnd();
                Sta.Pump();
            }
            TextBlock last = Descendants(window).OfType<TextBlock>().Last(text => text.IsVisible && text.Text == finalText);
            AssertInsideWindow(last, window);
            window.Height = 340;
            Sta.Pump();
            if (window.Content is ScrollViewer resizedScroll)
            {
                resizedScroll.ScrollToBottom();
                resizedScroll.ScrollToRightEnd();
            }
            Sta.Pump();
            AssertInsideWindow(last, window);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Gallery_LayoutNavigationStaysReadableWhenCollapsedOrResized() => Sta.Run(() =>
    {
        Window window = Open("LayoutMatrix.xaml", 1200, 700);
        try
        {
            AntSider[] navigationSiders = Descendants(window).OfType<AntSider>()
                .Where(sider => Descendants(sider).OfType<TextBlock>().Any(text => text.Text == "Navigation")).ToArray();
            Assert.Equal(2, navigationSiders.Length);
            foreach (AntSider sider in navigationSiders)
            {
                TextBlock caption = Descendants(sider).OfType<TextBlock>().Single(text => text.Text == "Navigation");
                AntButton action = Descendants(sider).OfType<AntButton>().Single(button => Equals(button.Content, "Overview"));
                AssertUnclippedInsideSider(action, sider);
                sider.IsCollapsed = true;
                window.Width = 650;
                Sta.Pump();
                Assert.Equal(Visibility.Collapsed, caption.Visibility);
                Assert.Null(action.Content);
                Assert.Equal("Overview", action.ToolTip);
                Assert.Equal("Overview", AutomationProperties.GetName(action));
                var icon = Assert.IsAssignableFrom<ContentPresenter>(action.Template.FindName("Icon", action));
                Assert.True(icon.IsVisible && icon.ActualWidth > 0);
                Point top = action.TranslatePoint(new Point(), sider);
                var trigger = (Button)sider.Template.FindName("PART_Trigger", sider);
                Assert.InRange(top.X, 0, sider.ActualWidth - action.ActualWidth);
                Assert.InRange(top.Y + action.ActualHeight, 0, trigger.TranslatePoint(new Point(), sider).Y);
                AssertUnclippedInsideSider(action, sider);
                sider.IsCollapsed = false;
                Sta.Pump();
                Assert.Equal(Visibility.Visible, caption.Visibility);
                Assert.Equal("Overview", action.Content);
                Assert.Equal(200, sider.ActualWidth);
                AssertUnclippedInsideSider(action, sider);
            }
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Gallery_MainWindowCanScrollToLastExamplesLink() => Sta.Run(() =>
    {
        var window = new MainWindow { Width = 800, Height = 600, ShowInTaskbar = false, ShowActivated = false, Opacity = 0, IsHitTestVisible = false };
        window.Show();
        Sta.Pump();
        try
        {
            AntButton last = Descendants(window).OfType<AntButton>().Single(button => Equals(button.Content, "Layout examples"));
            ScrollViewer scroll = Descendants(window).OfType<ScrollViewer>().First(viewer => Descendants(viewer).Contains(last));
            scroll.ScrollToBottom();
            Sta.Pump();
            Assert.True(scroll.VerticalOffset > 0);
            AssertInsideWindow(last, window);
        }
        finally { window.Close(); }
    });

    private static Window Open(string file, double width, double height)
    {
        _ = typeof(MainWindow).Assembly;
        var window = (Window)Application.LoadComponent(new Uri("/Antd.Wpf.Gallery;component/" + file, UriKind.Relative));
        window.Width = width;
        window.Height = height;
        window.ShowInTaskbar = false;
        window.ShowActivated = false;
        window.Opacity = 0;
        window.IsHitTestVisible = false;
        window.Show();
        Sta.Pump();
        return window;
    }

    private static void AssertInsideWindow(FrameworkElement element, Window window)
    {
        Point point = element.TranslatePoint(new Point(), window);
        Assert.InRange(point.X, -1, window.RenderSize.Width - element.ActualWidth + 1);
        Assert.InRange(point.Y, -1, window.RenderSize.Height - element.ActualHeight + 1);
    }

    private static void AssertUnclippedInsideSider(FrameworkElement element, AntSider sider)
    {
        var bounds = new Rect(element.TranslatePoint(new Point(), sider), element.RenderSize);
        DependencyObject? current = element;
        while (current != null && current != sider)
        {
            if (current is FrameworkElement ancestor && LayoutInformation.GetLayoutClip(ancestor) is Geometry clip)
            {
                Rect allowed = ancestor.TransformToAncestor(sider).TransformBounds(clip.Bounds);
                allowed.Inflate(1, 1);
                Assert.True(allowed.Contains(bounds), $"Navigation bounds {bounds} exceed {ancestor.GetType().Name} clip {allowed}.");
            }
            current = VisualTreeHelper.GetParent(current);
        }
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject element)
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(element, index);
            yield return child;
            foreach (DependencyObject descendant in Descendants(child)) { yield return descendant; }
        }
    }
}
