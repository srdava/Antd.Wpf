using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Antd.Wpf.Controls;
using Antd.Wpf.Theming;
using ThemeMode = Antd.Wpf.Theming.ThemeMode;

namespace Antd.Wpf.Consumer;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        try
        {
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            var button = new AntButton { Content = "Packaged button", Variant = ButtonVariant.Solid };
            var input = new AntInput { Text = "中文 package", AllowClear = true };
            var search = new AntInput { IsSearchEnabled = true, AllowClear = true, Text = "Package search" };
            var password = new AntPasswordBox { Placeholder = "Packaged password", AllowClear = true };
            InputSearchEventArgs? searchRequest = null;
            int searchCalls = 0;
            search.SearchRequested += (_, args) => { searchRequest = args; searchCalls++; };
            var select = new AntSelect { ItemsSource = new[] { "Alpha", "Beta" }, SelectedIndex = 1 };
            var toggle = new AntSwitch { IsChecked = true, CheckedContent = "开", UncheckedContent = "关" };
            var checkbox = new AntCheckBox { Content = "Check", IsChecked = null };
            var radio = new AntRadioButton { Content = "Radio", IsChecked = true };
            var divider = new AntDivider { Content = "Packaged section", TitlePlacement = DividerTitlePlacement.Start };
            var panel = new StackPanel();
            var multiline = new AntInput
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinLines = 2,
                MaxLines = 3,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalContentAlignment = VerticalAlignment.Top,
            };
            panel.Children.Add(button);
            panel.Children.Add(input);
            panel.Children.Add(search);
            panel.Children.Add(password);
            panel.Children.Add(multiline);
            panel.Children.Add(select);
            panel.Children.Add(toggle);
            panel.Children.Add(checkbox);
            panel.Children.Add(radio);
            panel.Children.Add(divider);
            var flex = new AntFlexPanel { ColumnGap = 8 };
            var flexFirst = new Border { Width = 20, Height = 10 };
            var flexSecond = new Border { Width = 30, Height = 10 };
            flex.Children.Add(flexFirst);
            flex.Children.Add(flexSecond);
            panel.Children.Add(flex);
            var space = new AntSpace
            {
                SeparatorTemplate = (DataTemplate)System.Windows.Markup.XamlReader.Parse(
                    "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Border Width='2' Height='10' /></DataTemplate>"),
            };
            space.Children.Add(new Border { Width = 20, Height = 10 });
            space.Children.Add(new Border { Width = 30, Height = 10 });
            panel.Children.Add(space);
            var gridRow = new AntRow { HorizontalGutter = 16 };
            var gridCol = new AntCol { Span = 12, Child = new Border { Height = 10 } };
            gridRow.Children.Add(gridCol);
            gridRow.Children.Add(new AntCol { Span = 12, Child = new Border { Height = 10 } });
            panel.Children.Add(gridRow);
            var layout = new AntLayout { Height = 150 };
            var packagedHeader = new AntHeader { Content = "Packaged header" };
            var packagedSider = new AntSider { IsCollapsible = true, Content = new AntInput { Text = "Retained" } };
            layout.Children.Add(packagedHeader);
            layout.Children.Add(packagedSider);
            layout.Children.Add(new AntContent { Content = "Content" });
            panel.Children.Add(layout);
            var scope = new AntThemeScope { Content = panel };
            var window = new Window
            {
                Content = scope,
                Width = 400,
                Height = 300,
                ShowInTaskbar = false,
                ShowActivated = false,
                Opacity = 0,
            };
            window.Show();
            Pump();
            Require(button.Template?.FindName("Chrome", button) is Border, "Button BAML unavailable");
            Require(LayoutInformation.GetLayoutSlot(flexSecond).X == 28 && VisualTreeHelper.GetChildrenCount(flex) == 2,
                "Flex packaged layout unavailable");
            flex.Wrap = true;
            flex.Width = 40;
            flex.RowGap = 6;
            Pump();
            Require(LayoutInformation.GetLayoutSlot(flexSecond).Y == 16, "Flex packaged wrapping unavailable");
            flex.Width = 100;
            flex.JustifyContent = FlexJustification.SpaceEvenly;
            Pump();
            Require(LayoutInformation.GetLayoutSlot(flexFirst).X == 14 && LayoutInformation.GetLayoutSlot(flexSecond).X == 56,
                "Flex packaged distributed justification unavailable");
            Require(LayoutInformation.GetLayoutSlot((FrameworkElement)space.Children[1]).X == 38 && VisualTreeHelper.GetChildrenCount(space) == 3,
                "Space packaged separator layout unavailable");
            Require(LayoutInformation.GetLayoutSlot(gridCol).X == -8 &&
                Math.Abs(LayoutInformation.GetLayoutSlot(gridCol).Width - (gridRow.ActualWidth + 16) / 2) < 0.001 &&
                LayoutInformation.GetLayoutSlot((FrameworkElement)gridCol.Child).X == 8, "Grid packaged gutter layout unavailable");
            gridCol.Span = 0;
            Pump();
            Require(gridCol.Visibility == Visibility.Collapsed && gridCol.Child != null, "Grid zero span did not preserve hidden content");
            gridCol.Span = 12;
            Pump();
            Require(gridCol.Visibility == Visibility.Visible, "Grid span restore failed");
            Require(packagedHeader.ActualHeight == 64 && packagedSider.ActualWidth == 200 && packagedSider.Template.FindName("PART_Trigger", packagedSider) is Button,
                "Layout packaged BAML unavailable");
            packagedSider.CollapseCommand.Execute(null);
            Pump();
            Require(packagedSider.ActualWidth == 80, "Sider packaged collapse failed");
            packagedSider.CollapsedWidth = 0;
            Pump();
            Require(packagedSider.ActualWidth == 0 && packagedSider.Template.FindName("PART_ZeroTrigger", packagedSider) is Button { IsVisible: true },
                "Sider packaged zero-width trigger failed");
            var siderProvider = (System.Windows.Automation.Provider.IExpandCollapseProvider)
                System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(packagedSider).GetPattern(System.Windows.Automation.Peers.PatternInterface.ExpandCollapse);
            siderProvider.Expand();
            Pump();
            Require(packagedSider.ActualWidth == 200 && ((AntInput)packagedSider.Content).Text == "Retained", "Sider packaged restore failed");
            Require(input.Template?.FindName("PART_ContentHost", input) is ScrollViewer, "Input BAML unavailable");
            var searchButton = search.Template?.FindName("PART_SearchButton", search) as Button
                ?? throw new InvalidOperationException("Search BAML unavailable");
            searchButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Require(searchCalls == 1 && searchRequest is { Reason: InputSearchReason.Button, Text: "Package search" }, "Packaged search dispatch failed");
            search.IsLoading = true;
            searchButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Require(searchCalls == 1 && !search.CanSearch, "Packaged loading search reentered");
            search.CancelSearch();
            Require(searchRequest!.CancellationToken.IsCancellationRequested, "Packaged search cancellation failed");
            search.IsLoading = false;
            var searchClear = search.Template?.FindName("PART_ClearButton", search) as Button
                ?? throw new InvalidOperationException("Search clear BAML unavailable");
            searchClear.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Require(searchCalls == 2 && searchRequest is { Reason: InputSearchReason.Clear, Text: "" }, "Packaged clear reason failed");
            search.IsSearchEnabled = false;
            Require(searchRequest!.CancellationToken.IsCancellationRequested, "Packaged search mode cleanup failed");
            var passwordHost = password.Template?.FindName("PART_PasswordHost", password) as ContentPresenter
                ?? throw new InvalidOperationException("Password BAML unavailable");
            var passwordEditor = passwordHost.Content as PasswordBox
                ?? throw new InvalidOperationException("Native password editor unavailable");
            passwordEditor.Password = "test-only";
            using (var secret = password.SecurePassword) { Require(secret.Length == 9 && !password.IsEmpty, "Packaged secure password failed"); }
            password.Clear();
            Require(password.IsEmpty, "Packaged password clear failed");
            input.ShowCount = true;
            input.MaxLength = 4;
            Pump();
            var count = input.Template?.FindName("Count", input) as TextBlock
                ?? throw new InvalidOperationException("Input count BAML unavailable");
            Require(new System.Windows.Documents.TextRange(count.ContentStart, count.ContentEnd).Text == "10 / 4" &&
                input.IsOverMaxLength && input.Text == "中文 package", "Packaged count/overflow destroyed text or failed to display");
            Require(System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(input).GetItemStatus() == "Error",
                "Packaged overflow automation status unavailable");
            input.MaxLength = 0;
            Pump();
            Require(!input.IsOverMaxLength && new System.Windows.Documents.TextRange(count.ContentStart, count.ContentEnd).Text == "10",
                "Packaged unlimited count did not restore");
            Require(Math.Abs(multiline.ActualHeight - 54) <= 1, "Packaged multiline minimum rows failed");
            multiline.Text = "一\r\n二\r\n三\r\n四\r\n五";
            Pump();
            var multilineHost = (ScrollViewer)multiline.Template.FindName("PART_ContentHost", multiline);
            Require(Math.Abs(multiline.ActualHeight - 76) <= 1 && multilineHost.ExtentHeight > multilineHost.ViewportHeight,
                "Packaged multiline maximum rows failed");
            multiline.ScrollToEnd();
            Pump();
            Require(multilineHost.VerticalOffset > 0, "Packaged multiline scrolling failed");
            Require(toggle.Template?.FindName("Label", toggle) is ContentPresenter { Content: "开" }, "Switch BAML unavailable");
            var thumbTranslation = (toggle.Template?.FindName("Thumb", toggle) as Border)?.RenderTransform as TranslateTransform
                ?? throw new InvalidOperationException("Switch thumb translation unavailable");
            Require(!thumbTranslation.HasAnimatedProperties && thumbTranslation.X > 0,
                "Switch initial thumb position unavailable");
            toggle.IsChecked = false;
            Require(thumbTranslation.HasAnimatedProperties, "Switch packaged motion unavailable");
            scope.MotionEnabled = false;
            Require(!thumbTranslation.HasAnimatedProperties && thumbTranslation.X == 0, "Switch motion cancellation failed");
            toggle.IsChecked = true;
            Require(!thumbTranslation.HasAnimatedProperties && thumbTranslation.X > 0, "Switch no-motion position unavailable");
            scope.MotionEnabled = true;
            Require(checkbox.Template?.FindName("Indeterminate", checkbox) is System.Windows.Shapes.Rectangle { Opacity: 1 }, "Checkbox BAML unavailable");
            Require(radio.Template?.FindName("Dot", radio) is System.Windows.Shapes.Ellipse { Width: 6, Opacity: 1 }, "Radio BAML unavailable");
            Require(divider.Template?.FindName("PART_Layout", divider) is Decorator { Child: ContentPresenter { Content: "Packaged section" } }, "Divider internal BAML types unavailable");
            Require(System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(divider).GetAutomationControlType() ==
                System.Windows.Automation.Peers.AutomationControlType.Separator, "Divider automation semantics unavailable");
            Color before = ((SolidColorBrush)input.Background).Color;
            scope.Mode = ThemeMode.Dark;
            Pump();
            Require(((SolidColorBrush)input.Background).Color != before, "Theme resource did not update");
            Require(input.Text == "中文 package", "Theme destroyed input");
            select.IsDropDownOpen = true;
            Pump();
            Require(select.Template.FindName("PART_Popup", select) is Popup { IsOpen: true }, "Popup failed");
            Require((string)select.SelectedItem == "Beta", "Selection changed");
            window.Close();
            Pump();
            Require(!select.IsDropDownOpen, "Popup survived owner close");
            Console.WriteLine("PASS: packaged BAML, theme, input, selection, owner cleanup; CLR " + Environment.Version);
            app.Shutdown();
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
