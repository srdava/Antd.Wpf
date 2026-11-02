using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using Antd.Wpf.Controls;
using Antd.Wpf.Theming;
using Xunit;

namespace Antd.Wpf.Tests;

[Trait("Category", "UIIntegration")]
public sealed class LayoutTests
{
    [Fact]
    public void Layout_OfficialRegionsKeepHeaderFooterAndScrollingIndependent() => Sta.Run(() =>
    {
        var layout = new AntLayout();
        var header = new AntHeader { Content = "Header" };
        var footer = new AntFooter { Content = "Footer" };
        var sider = new AntSider { Content = "Sidebar", IsCollapsible = true };
        var scroll = new ScrollViewer { Content = new Border { Height = 1200 } };
        var content = new AntContent { Content = scroll };
        layout.Children.Add(header);
        layout.Children.Add(footer);
        layout.Children.Add(sider);
        layout.Children.Add(content);
        Window window = Sta.Show(new AntThemeScope { Content = layout });
        try
        {
            Assert.NotNull(layout.Background);
            Assert.Equal(64, header.ActualHeight);
            Assert.Equal(new Thickness(50, 0, 50, 0), header.Padding);
            Assert.Equal(new Thickness(50, 24, 50, 24), footer.Padding);
            Assert.Equal(70, footer.ActualHeight);
            Assert.Equal(new Thickness(0), content.Padding);
            Assert.Equal(200, sider.ActualWidth);
            Assert.Equal(64, content.TranslatePoint(new Point(), layout).Y);
            Assert.Equal(200, content.TranslatePoint(new Point(), layout).X);
            Assert.Equal(layout.ActualHeight - 134, content.ActualHeight);
            Assert.Equal(layout.ActualWidth - 200, content.ActualWidth);
            Assert.Equal(Color.FromRgb(0, 21, 41), ((SolidColorBrush)header.Background).Color);
            Assert.Equal(Colors.Black, ((SolidColorBrush)header.Foreground).Color);
            Assert.Equal(0.88, header.Foreground.Opacity);
            var trigger = Part(sider, "PART_Trigger");
            Assert.Equal(48, trigger.ActualHeight);
            Assert.Equal(Color.FromRgb(0, 33, 64), ((SolidColorBrush)trigger.Background).Color);
            Assert.Equal(1, trigger.Foreground.Opacity);
            double footerY = footer.TranslatePoint(new Point(), layout).Y;
            scroll.ScrollToVerticalOffset(500);
            Sta.Pump();
            Assert.True(scroll.VerticalOffset > 0);
            Assert.Equal(0, header.TranslatePoint(new Point(), layout).Y);
            Assert.Equal(footerY, footer.TranslatePoint(new Point(), layout).Y);
            Assert.Equal(64, sider.TranslatePoint(new Point(), layout).Y);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Layout_NestingAndLocalSpacingSurviveThemeAndFontChanges() => Sta.Run(() =>
    {
        var outer = new AntLayout();
        var sider = new AntSider();
        var inner = new AntLayout();
        var header = new AntHeader { Content = "Header", Padding = new Thickness(7), Background = Brushes.Orange };
        var footer = new AntFooter { Content = "Footer" };
        inner.Children.Add(header);
        inner.Children.Add(footer);
        inner.Children.Add(new AntContent { Content = new AntInput { Text = "保留" } });
        outer.Children.Add(sider);
        outer.Children.Add(inner);
        var scope = new AntThemeScope { Content = outer };
        Window window = Sta.Show(scope);
        try
        {
            Assert.Equal(outer.ActualHeight, sider.ActualHeight);
            Assert.Equal(200, inner.TranslatePoint(new Point(), outer).X);
            Assert.Equal(0, header.TranslatePoint(new Point(), inner).Y);
            scope.Mode = ThemeMode.Dark;
            footer.FontSize = 28;
            Sta.Pump();
            Assert.Same(Brushes.Orange, header.Background);
            Assert.Equal(new Thickness(7), header.Padding);
            Assert.Equal(92, footer.ActualHeight);
            Assert.Equal(Colors.Black, ((SolidColorBrush)outer.Background).Color);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Sider_WidthBindingAndDynamicResourcesRestoreExpandedBaseValue() => Sta.Run(() =>
    {
        var source = new Border { Tag = 240.0 };
        var sider = new AntSider { IsCollapsible = true, Content = "Body" };
        sider.SetBinding(FrameworkElement.WidthProperty, new Binding("Tag") { Source = source });
        Window window = ShowSider(sider);
        try
        {
            Assert.Equal(240, sider.ActualWidth);
            sider.IsCollapsed = true;
            source.Tag = 300.0;
            Sta.Pump();
            Assert.Equal(80, sider.Width);
            Assert.Equal(80, sider.ActualWidth);
            Assert.Equal(300.0, source.Tag);
            Assert.True(BindingOperations.IsDataBound(sider, FrameworkElement.WidthProperty));
            sider.IsCollapsed = false;
            Sta.Pump();
            Assert.Equal(300, sider.Width);
            Assert.Equal(300, sider.ActualWidth);
            BindingOperations.ClearBinding(sider, FrameworkElement.WidthProperty);
            sider.Resources["ExpandedWidth"] = 220.0;
            sider.SetResourceReference(FrameworkElement.WidthProperty, "ExpandedWidth");
            sider.IsCollapsed = true;
            sider.Resources["ExpandedWidth"] = 260.0;
            Sta.Pump();
            Assert.Equal(80, sider.Width);
            sider.IsCollapsed = false;
            Sta.Pump();
            Assert.Equal(260, sider.Width);
            sider.Width = double.NaN;
            sider.IsCollapsed = true;
            sider.IsCollapsed = false;
            Assert.True(double.IsNaN(sider.Width));
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(Dock.Left, FlowDirection.LeftToRight, true)]
    [InlineData(Dock.Right, FlowDirection.LeftToRight, false)]
    [InlineData(Dock.Left, FlowDirection.RightToLeft, false)]
    [InlineData(Dock.Right, FlowDirection.RightToLeft, true)]
    public void Sider_ZeroWidthHasVisibleHittableKeyboardTrigger(Dock dock, FlowDirection flow, bool physicalLeft) => Sta.Run(() =>
    {
        var input = new AntInput { Text = "Preserve", Width = 140 };
        var sider = new AntSider { Content = input, IsCollapsible = true, CollapsedWidth = 0 };
        DockPanel.SetDock(sider, dock);
        var layout = new AntLayout { FlowDirection = flow };
        layout.Children.Add(sider);
        layout.Children.Add(new AntContent { Background = Brushes.White });
        var host = new Border { Child = layout };
        Window window = Sta.Show(host);
        window.IsHitTestVisible = true;
        try
        {
            Assert.True(input.Focus());
            sider.IsCollapsed = true;
            Sta.Pump();
            var zero = Part(sider, "PART_ZeroTrigger");
            Assert.Equal(0, sider.ActualWidth);
            Assert.True(zero.IsVisible);
            Assert.True(zero.IsKeyboardFocused);
            Assert.Equal(40, zero.ActualWidth);
            Assert.Equal(40, zero.ActualHeight);
            Assert.Equal(dock == Dock.Left ? new CornerRadius(0, 8, 8, 0) : new CornerRadius(8, 0, 0, 8), Assert.IsType<CornerRadius>(zero.Tag));
            Point center = zero.TranslatePoint(new Point(20, 20), host);
            Assert.Equal(physicalLeft ? 20 : layout.ActualWidth - 20, center.X, 5);
            Assert.Equal(84, center.Y, 5);
            Point localCenter = zero.TranslatePoint(new Point(20, 20), layout);
            Assert.True(Inside(layout.InputHitTest(localCenter) as DependencyObject, zero));
            Invoke(zero);
            Sta.Pump();
            Assert.False(sider.IsCollapsed);
            Assert.Equal(200, sider.ActualWidth);
            Assert.True(zero.IsVisible);
            Assert.False(Part(sider, "PART_Trigger").IsVisible);
            Point expandedCenter = zero.TranslatePoint(new Point(20, 20), host);
            Assert.Equal(physicalLeft ? 220 : layout.ActualWidth - 220, expandedCenter.X, 5);
            Assert.True(Inside(layout.InputHitTest(zero.TranslatePoint(new Point(20, 20), layout)) as DependencyObject, zero));
            Assert.Same(input, sider.Content);
            Assert.Equal("Preserve", input.Text);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Sider_InitiallyZeroCollapsedDoesNotMeasureOrReplaceBody() => Sta.Run(() =>
    {
        var body = new Probe();
        var sider = new AntSider { Content = body, IsCollapsible = true, CollapsedWidth = 0, IsCollapsed = true };
        Window window = ShowSider(sider);
        try
        {
            Assert.Equal(0, body.Measures);
            Assert.True(Part(sider, "PART_ZeroTrigger").IsVisible);
            sider.CollapseCommand.Execute(null);
            Sta.Pump();
            Assert.True(body.Measures > 0);
            Assert.Same(body, sider.Content);
            Assert.False(sider.IsZeroWidthCollapsed);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Sider_CommandsBindingsReasonsAndAutomationHaveDistinctTransitions() => Sta.Run(() =>
    {
        var source = new CheckBox { IsChecked = false };
        var sider = new AntSider { IsCollapsible = true };
        sider.SetBinding(AntSider.IsCollapsedProperty, new Binding("IsChecked") { Source = source });
        var reasons = new List<SiderCollapseReason>();
        sider.CollapsedChanged += (_, args) => reasons.Add(args.Reason);
        Window window = ShowSider(sider);
        try
        {
            source.IsChecked = true;
            Sta.Pump();
            Assert.True(sider.IsCollapsed);
            Invoke(Part(sider, "PART_Trigger"));
            Sta.Pump();
            Assert.False(sider.IsCollapsed);
            Assert.False(source.IsChecked);
            var peer = UIElementAutomationPeer.CreatePeerForElement(sider);
            Assert.Equal(AutomationControlType.Pane, peer.GetAutomationControlType());
            var provider = Assert.IsAssignableFrom<IExpandCollapseProvider>(peer.GetPattern(PatternInterface.ExpandCollapse));
            provider.Collapse();
            Sta.Pump();
            Assert.True(source.IsChecked);
            Assert.Equal(ExpandCollapseState.Collapsed, provider.ExpandCollapseState);
            provider.Collapse();
            provider.Expand();
            Sta.Pump();
            Assert.Equal(ExpandCollapseState.Expanded, provider.ExpandCollapseState);
            Assert.Equal(new[] { SiderCollapseReason.Programmatic, SiderCollapseReason.Trigger, SiderCollapseReason.Automation, SiderCollapseReason.Automation }, reasons);
            Assert.True(BindingOperations.IsDataBound(sider, AntSider.IsCollapsedProperty));
            sider.IsEnabled = false;
            Sta.Pump();
            Assert.False(sider.CollapseCommand.CanExecute(null));
            Assert.False(Part(sider, "PART_Trigger").IsEnabled);
            Assert.Equal(0.25, Part(sider, "PART_Trigger").Foreground.Opacity);
            Assert.Throws<ElementNotEnabledException>(() => provider.Collapse());
            sider.CollapseCommand.Execute(null);
            Assert.False(sider.IsCollapsed);
            sider.IsEnabled = true;
            sider.IsCollapsible = false;
            Assert.Null(peer.GetPattern(PatternInterface.ExpandCollapse));
            Assert.Throws<InvalidOperationException>(() => provider.Collapse());
            sider.CollapseCommand.Execute(null);
            Assert.False(sider.IsCollapsed);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Sider_ReentrantEventDoesNotReuseTriggerReason() => Sta.Run(() =>
    {
        var sider = new AntSider { IsCollapsible = true };
        var events = new List<SiderCollapseReason>();
        sider.CollapsedChanged += (_, args) =>
        {
            events.Add(args.Reason);
            if (args.IsCollapsed) { sider.IsCollapsed = false; }
        };
        sider.CollapseCommand.Execute(null);
        Assert.Equal(new[] { SiderCollapseReason.Trigger, SiderCollapseReason.Programmatic }, events);
        Assert.False(sider.IsCollapsed);
        Assert.Equal(200, sider.Width);
    });

    [Fact]
    public void Sider_IndependentThemesAndTwoWindowsPreserveLocalOverrides() => Sta.Run(() =>
    {
        var first = new AntSider { IsCollapsible = true, Content = new AntInput { Text = "First" } };
        var lightStyle = new Style(typeof(AntSider));
        lightStyle.Setters.Add(new Setter(AntThemeScope.ModeProperty, ThemeMode.Light));
        var second = new AntSider { Style = lightStyle, IsCollapsible = true, Content = "Second" };
        var firstScope = new AntThemeScope { Mode = ThemeMode.Light, Content = first };
        var secondScope = new AntThemeScope { Mode = ThemeMode.Dark, Content = second };
        Window one = Sta.Show(firstScope);
        Window two = Sta.Show(secondScope);
        try
        {
            Assert.Equal(ThemeMode.Dark, first.Mode);
            Assert.Equal(Color.FromRgb(0, 21, 41), ((SolidColorBrush)first.Background).Color);
            Assert.Equal(Colors.White, ((SolidColorBrush)second.Background).Color);
            var zero = Part(second, "PART_ZeroTrigger");
            Assert.Equal(new Thickness(0, 1, 1, 1), zero.BorderThickness);
            DockPanel.SetDock(second, Dock.Right);
            Sta.Pump();
            Assert.Equal(new Thickness(1, 1, 0, 1), zero.BorderThickness);
            first.IsCollapsed = true;
            firstScope.Mode = ThemeMode.Dark;
            secondScope.Mode = ThemeMode.Light;
            second.Background = Brushes.Orange;
            second.Mode = ThemeMode.Dark;
            Sta.Pump();
            Assert.True(first.IsCollapsed);
            Assert.False(second.IsCollapsed);
            Assert.Same(Brushes.Orange, second.Background);
            Assert.Equal(new Thickness(0), zero.BorderThickness);
            Assert.Single(first.Resources.MergedDictionaries);
            Assert.Single(second.Resources.MergedDictionaries);
            Assert.Equal("First", ((AntInput)first.Content).Text);
        }
        finally { two.Close(); one.Close(); }
    });

    [Fact]
    public void Sider_RetemplateDetachesOldCommandAndSupportsCustomTriggerTemplate() => Sta.Run(() =>
    {
        var sider = new AntSider { IsCollapsible = true, Content = "Body" };
        Window window = ShowSider(sider);
        try
        {
            var old = Part(sider, "PART_Trigger");
            var oldZero = Part(sider, "PART_ZeroTrigger");
            sider.TriggerTemplate = (DataTemplate)XamlReader.Parse("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><StackPanel><TextBlock Text='Custom' /><TextBlock Text='{Binding IsCollapsed}' /></StackPanel></DataTemplate>");
            Sta.Pump();
            var custom = (ContentPresenter)sider.Template.FindName("CustomTrigger", sider);
            var customRoot = (StackPanel)VisualTreeHelper.GetChild(custom, 0);
            Assert.Equal("Custom", ((TextBlock)customRoot.Children[0]).Text);
            Assert.Equal("False", ((TextBlock)customRoot.Children[1]).Text);
            sider.CollapseCommand.Execute(null);
            sider.CollapsedWidth = 0;
            Sta.Pump();
            var customZero = (ContentPresenter)sider.Template.FindName("CustomZeroTrigger", sider);
            Assert.Equal("True", ((TextBlock)((StackPanel)VisualTreeHelper.GetChild(customZero, 0)).Children[1]).Text);
            sider.CollapseCommand.Execute(null);
            Sta.Pump();
            sider.Template = new ControlTemplate(typeof(AntSider));
            sider.ApplyTemplate();
            Assert.Null(old.Command);
            Assert.Null(oldZero.Command);
            old.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.False(sider.IsCollapsed);
            sider.IsCollapsed = true;
            var peer = new Antd.Wpf.Automation.AntSiderAutomationPeer(sider);
            peer.Expand();
            Assert.False(sider.IsCollapsed);
            Assert.Equal("Body", sider.Content);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Layout_HighContrastResourcesUseSystemColors() => Sta.Run(() =>
    {
        var resources = new ResourceDictionary { Source = new Uri("/Antd.Wpf;component/Themes/Tokens/HighContrast.xaml", UriKind.Relative) };
        Assert.Equal(SystemColors.WindowColor, ((SolidColorBrush)resources["Antd.Brush.LayoutHeader"]).Color);
        Assert.Equal(SystemColors.WindowColor, ((SolidColorBrush)resources["Antd.Brush.LayoutSider"]).Color);
        Assert.Equal(SystemColors.ControlColor, ((SolidColorBrush)resources["Antd.Brush.LayoutTrigger"]).Color);
        Assert.Equal(SystemColors.ControlTextColor, ((SolidColorBrush)resources["Antd.Brush.LayoutTriggerText"]).Color);
    });

    [Fact]
    public void Sider_CollapsedWidthBindingCanMoveFocusedBodyToZeroTrigger() => Sta.Run(() =>
    {
        var source = new Border { Tag = 80.0 };
        var input = new AntInput { Text = "Keep" };
        var sider = new AntSider { Content = input, IsCollapsible = true, IsCollapsed = true };
        sider.SetBinding(AntSider.CollapsedWidthProperty, new Binding("Tag") { Source = source });
        Window window = ShowSider(sider);
        try
        {
            Assert.True(input.Focus());
            source.Tag = 0.0;
            Sta.Pump();
            Assert.Equal(0, sider.ActualWidth);
            Assert.True(Part(sider, "PART_ZeroTrigger").IsKeyboardFocused);
            source.Tag = 80.0;
            Sta.Pump();
            Assert.Equal(80, sider.ActualWidth);
            Assert.True(Part(sider, "PART_Trigger").IsVisible);
            Assert.True(Part(sider, "PART_Trigger").IsKeyboardFocused);
            Assert.Same(input, sider.Content);
            Assert.True(BindingOperations.IsDataBound(sider, AntSider.CollapsedWidthProperty));
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Sider_PaintOverridesDoNotRemeasureLayoutOrBody() => Sta.Run(() =>
    {
        var body = new Probe();
        var sider = new MeasuredSider { Content = body, IsCollapsible = true };
        Window window = ShowSider(sider);
        try
        {
            int measured = sider.Measures;
            int bodyMeasured = body.Measures;
            for (int index = 0; index < 30; index++)
            {
                sider.Background = index % 2 == 0 ? Brushes.Red : Brushes.Blue;
                sider.Foreground = index % 2 == 0 ? Brushes.White : Brushes.Black;
                Sta.Pump();
            }
            Assert.Equal(measured, sider.Measures);
            Assert.Equal(bodyMeasured, body.Measures);
            Assert.Same(body, sider.Content);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Sider_RepeatedCollapseKeepsContentTemplatesAndResourcesBounded() => Sta.Run(() =>
    {
        var input = new AntInput { Text = "Stable" };
        var sider = new AntSider { Content = input, IsCollapsible = true, CollapsedWidth = 0 };
        Window window = ShowSider(sider);
        try
        {
            var trigger = Part(sider, "PART_ZeroTrigger");
            sider.IsCollapsed = true;
            Sta.Pump();
            sider.IsCollapsed = false;
            Sta.Pump();
            int nodes = Count(sider);
            for (int index = 0; index < 30; index++)
            {
                sider.IsCollapsed = !sider.IsCollapsed;
                sider.Mode = index % 2 == 0 ? ThemeMode.Light : ThemeMode.Dark;
                Sta.Pump();
                Assert.Same(input, sider.Content);
                Assert.Same(trigger, Part(sider, "PART_ZeroTrigger"));
                Assert.Single(sider.Resources.MergedDictionaries);
            }
            Assert.Equal(nodes, Count(sider));
            Assert.Equal("Stable", input.Text);
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Sider_InvalidCollapsedWidthsAreRejected(double value) => Sta.Run(() =>
    {
        var sider = new AntSider();
        Assert.Throws<ArgumentException>(() => sider.CollapsedWidth = value);
        Assert.Equal(80, sider.CollapsedWidth);
    });

    private static Window ShowSider(AntSider sider)
    {
        var layout = new AntLayout();
        layout.Children.Add(sider);
        layout.Children.Add(new AntContent());
        return Sta.Show(layout);
    }
    private static Button Part(AntSider sider, string name) => Assert.IsAssignableFrom<Button>(sider.Template.FindName(name, sider));
    private static void Invoke(Button button) => ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();
    private static bool Inside(DependencyObject? element, DependencyObject ancestor)
    {
        while (element != null) { if (element == ancestor) { return true; } element = VisualTreeHelper.GetParent(element); }
        return false;
    }
    private static int Count(DependencyObject element)
    {
        int count = 1;
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++) { count += Count(VisualTreeHelper.GetChild(element, index)); }
        return count;
    }
    private sealed class Probe : FrameworkElement
    {
        public int Measures { get; private set; }
        protected override Size MeasureOverride(Size availableSize) { Measures++; return new Size(20, 10); }
    }
    private sealed class MeasuredSider : AntSider
    {
        public int Measures { get; private set; }
        protected override Size MeasureOverride(Size constraint) { Measures++; return base.MeasureOverride(constraint); }
    }
}
