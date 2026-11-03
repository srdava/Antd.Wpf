using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using Antd.Wpf.Controls;
using Antd.Wpf.Theming;
using Xunit;

namespace Antd.Wpf.Tests;

[Trait("Category", "UIIntegration")]
public sealed class InputSearchTests
{
    [Theory]
    [InlineData(ControlSize.Small, 24, FlowDirection.LeftToRight)]
    [InlineData(ControlSize.Middle, 32, FlowDirection.RightToLeft)]
    [InlineData(ControlSize.Large, 40, FlowDirection.LeftToRight)]
    public void Input_SearchDefaultIconAndTextSwitchPreserveEditor(ControlSize size, double height, FlowDirection direction) => Sta.Run(() =>
    {
        var input = new AntInput { IsSearchEnabled = true, Size = size, FlowDirection = direction, Width = 300, Text = "查询" };
        var scope = new AntThemeScope { Content = input };
        Window window = Sta.Show(scope);
        try
        {
            var button = (AntButton)input.Template.FindName("PART_SearchButton", input);
            object editor = input.Template.FindName("PART_ContentHost", input);
            Assert.Null(input.SearchButtonContent);
            Assert.Equal(ButtonVariant.Outlined, button.Variant);
            Assert.Equal(SemanticColor.Default, button.SemanticColor);
            Assert.InRange(button.ActualWidth, height - 0.8, height + 0.8);
            Assert.InRange(button.ActualHeight, height - 0.8, height + 0.8);
            var icon = Descendants(button).OfType<System.Windows.Shapes.Path>().Single(path => path.IsVisible);
            Assert.True(icon.ActualWidth > 0);
            Assert.Equal(button.Foreground, icon.Fill);
            Assert.Equal("Search", new ButtonAutomationPeer(button).GetName());
            Assert.True(input.Focus());
            input.Select(0, 1);
            scope.Mode = ThemeMode.Dark;
            Sta.Pump();
            Assert.Equal(button.Foreground, icon.Fill);
            input.SearchButtonContent = "搜索";
            Sta.Pump();
            Assert.Equal(ButtonVariant.Solid, button.Variant);
            Assert.Equal(SemanticColor.Primary, button.SemanticColor);
            Assert.Null(button.ContentTemplate);
            input.SearchButtonContent = null;
            input.IsLoading = true;
            Sta.Pump();
            Assert.Null(button.ContentTemplate);
            Assert.True(button.IsLoadingVisible);
            Assert.False(button.IsEnabled);
            Assert.InRange(button.ActualWidth, height - 0.8, height + 0.8);
            input.IsLoading = false;
            Sta.Pump();
            Assert.NotNull(button.ContentTemplate);
            Assert.Equal(1, input.SelectionLength);
            Assert.True(input.IsKeyboardFocused);
            Assert.Same(editor, input.Template.FindName("PART_ContentHost", input));
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Input_SearchInitiallyCollapsedDoesNotExpandButtonVisuals() => Sta.Run(() =>
    {
        var input = new AntInput();
        Window window = Sta.Show(new AntThemeScope { Content = input });
        try
        {
            var button = (Button)input.Template.FindName("PART_SearchButton", input);
            Assert.Equal(Visibility.Collapsed, button.Visibility);
            Assert.Equal(0, VisualTreeHelper.GetChildrenCount(button));
            input.IsSearchEnabled = true;
            Sta.Pump();
            Assert.Equal(Visibility.Visible, button.Visibility);
            Assert.True(VisualTreeHelper.GetChildrenCount(button) > 0);
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(InputVariant.Filled)]
    [InlineData(InputVariant.Borderless)]
    [InlineData(InputVariant.Underlined)]
    public void Input_SearchNonOutlinedVariantsUseTextButtons(InputVariant variant) => Sta.Run(() =>
    {
        var input = new AntInput { IsSearchEnabled = true, Variant = variant };
        Window window = Sta.Show(new AntThemeScope { Content = input });
        try
        {
            var button = (AntButton)input.Template.FindName("PART_SearchButton", input);
            Assert.Equal(ButtonVariant.Text, button.Variant);
            if (variant == InputVariant.Filled)
            {
                Assert.Same(input.FindResource("Antd.Brush.Fill"), button.Background);
            }
            else
            {
                Assert.Equal(0, ((SolidColorBrush)button.Background).Color.A);
            }
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Input_SearchReportsDistinctReasonsAndPreservesBindingUndo() => Sta.Run(() =>
    {
        var source = new TextBox { Text = "中文 query" };
        var requests = new List<InputSearchEventArgs>();
        var events = new List<InputSearchEventArgs>();
        var input = new SearchInput { IsSearchEnabled = true, AllowClear = true, SearchCommand = new ActionCommand(requests.Add) };
        input.SearchRequested += (_, args) => events.Add(args);
        input.SetBinding(TextBox.TextProperty, new Binding(nameof(TextBox.Text))
        {
            Source = source,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        });
        Window window = Sta.Show(new AntThemeScope { Content = input });
        try
        {
            object editor = input.Template.FindName("PART_ContentHost", input);
            input.KeyDown(Key.Enter);
            Click(input, "PART_SearchButton");
            Click(input, "PART_ClearButton");
            Assert.Equal(3, requests.Count);
            Assert.Equal(requests, events);
            Assert.Equal(InputSearchReason.Enter, requests[0].Reason);
            Assert.Equal(InputSearchReason.Button, requests[1].Reason);
            Assert.Equal(InputSearchReason.Clear, requests[2].Reason);
            Assert.Equal("中文 query", requests[0].Text);
            Assert.Equal("中文 query", requests[1].Text);
            Assert.Equal(string.Empty, requests[2].Text);
            Assert.True(requests[0].CancellationToken.IsCancellationRequested);
            Assert.True(requests[1].CancellationToken.IsCancellationRequested);
            Assert.False(requests[2].CancellationToken.IsCancellationRequested);
            Assert.Equal(string.Empty, source.Text);
            Assert.True(input.CanUndo);
            input.Undo();
            Assert.Equal("中文 query", source.Text);
            Assert.True(BindingOperations.IsDataBound(input, TextBox.TextProperty));
            Assert.Same(editor, input.Template.FindName("PART_ContentHost", input));
            Assert.Equal(3, requests.Count);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Input_SearchCanExecuteEventCancellationAndReentrancyAreGuarded() => Sta.Run(() =>
    {
        int calls = 0;
        var input = new SearchInput { IsSearchEnabled = true, Text = "query" };
        var command = new ActionCommand(_ => { calls++; input.KeyDown(Key.Enter); }) { Enabled = false };
        input.SearchCommand = command;
        Window window = Sta.Show(new AntThemeScope { Content = input });
        try
        {
            var button = (Button)input.Template.FindName("PART_SearchButton", input);
            Assert.False(button.IsEnabled);
            input.KeyDown(Key.Enter);
            Click(input, "PART_SearchButton");
            Assert.Equal(0, calls);
            command.Enabled = true;
            command.Notify();
            Sta.Pump();
            Assert.True(button.IsEnabled);
            CancellationToken canceled = default;
            EventHandler<InputSearchEventArgs> cancel = (_, args) => { canceled = args.CancellationToken; args.Cancel = true; };
            input.SearchRequested += cancel;
            Click(input, "PART_SearchButton");
            Assert.Equal(0, calls);
            Assert.True(canceled.IsCancellationRequested);
            Assert.True(input.CanSearch);
            input.SearchRequested -= cancel;
            Click(input, "PART_SearchButton");
            Assert.Equal(1, calls);
            Assert.True(input.CanSearch);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Input_SearchLoadingEscapeAndDisabledKeepHostBindings() => Sta.Run(() =>
    {
        var busy = new CheckBox { IsChecked = false };
        var requests = new List<InputSearchEventArgs>();
        var input = new SearchInput { IsSearchEnabled = true, AllowClear = true, Text = "keep" };
        input.SearchCommand = new ActionCommand(args => { requests.Add(args); busy.IsChecked = true; });
        input.SetBinding(AntInput.IsLoadingProperty, new Binding(nameof(CheckBox.IsChecked)) { Source = busy });
        Window window = Sta.Show(new AntThemeScope { Content = input });
        try
        {
            Click(input, "PART_SearchButton");
            Sta.Pump();
            Assert.True(input.IsLoading);
            Assert.False(input.CanSearch);
            Assert.False(((Button)input.Template.FindName("PART_SearchButton", input)).IsEnabled);
            AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(input);
            Assert.Equal("Loading", peer.GetItemStatus());
            input.Status = ControlStatus.Warning;
            Assert.Equal("Warning", peer.GetItemStatus());
            input.Status = ControlStatus.Default;
            input.MaxLength = 1;
            Assert.Equal("Error", peer.GetItemStatus());
            input.MaxLength = 0;
            Assert.Equal("Loading", peer.GetItemStatus());
            input.KeyDown(Key.Enter);
            Click(input, "PART_SearchButton");
            Click(input, "PART_ClearButton");
            Assert.Single(requests);
            Assert.Equal("keep", input.Text);
            input.KeyDown(Key.Escape);
            Assert.True(requests[0].CancellationToken.IsCancellationRequested);
            Assert.True(input.IsLoading);
            Assert.True(BindingOperations.IsDataBound(input, AntInput.IsLoadingProperty));
            busy.IsChecked = false;
            Sta.Pump();
            Assert.True(input.CanSearch);
            Assert.NotEqual("Loading", peer.GetItemStatus());
            input.IsReadOnly = true;
            Click(input, "PART_ClearButton");
            Assert.Equal("keep", input.Text);
            Click(input, "PART_SearchButton");
            Assert.Equal(2, requests.Count);
            busy.IsChecked = false;
            input.IsEnabled = false;
            input.KeyDown(Key.Enter);
            Click(input, "PART_SearchButton");
            Assert.Equal(2, requests.Count);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Input_SearchAvailabilityUsesCurrentTextAndSpecificTriggerReason() => Sta.Run(() =>
    {
        var requests = new List<InputSearchEventArgs>();
        var input = new SearchInput { IsSearchEnabled = true, Text = "query" };
        input.SearchCommand = new ActionCommand(requests.Add)
        {
            Predicate = request => request.Text.Length > 0 && request.Reason == InputSearchReason.Enter,
        };
        Window window = Sta.Show(new AntThemeScope { Content = input });
        try
        {
            Assert.False(input.CanSearch);
            Click(input, "PART_SearchButton");
            Assert.Empty(requests);
            input.KeyDown(Key.Enter);
            Assert.Single(requests);
            Assert.Equal(InputSearchReason.Enter, requests[0].Reason);
            input.Text = string.Empty;
            input.KeyDown(Key.Enter);
            Assert.Single(requests);
            Assert.False(requests[0].CancellationToken.IsCancellationRequested);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Input_SearchButtonStyleAndContentBindingsPreserveNativeInvokeAndEditor() => Sta.Run(() =>
    {
        var style = (Style)XamlReader.Parse("<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' xmlns:c='https://antd-wpf.dev/xaml' TargetType='{x:Type c:AntButton}'><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type c:AntButton}'><Border x:Name='CustomChrome' Background='{TemplateBinding Background}' Padding='8'><ContentPresenter ContentSource='Content' /></Border></ControlTemplate></Setter.Value></Setter></Style>");
        var source = new TextBox { Text = "查找" };
        int calls = 0;
        var input = new SearchInput { IsSearchEnabled = true, Text = "retained", SearchCommand = new ActionCommand(_ => calls++) };
        input.SetBinding(AntInput.SearchButtonContentProperty, new Binding(nameof(TextBox.Text)) { Source = source });
        Window window = Sta.Show(new AntThemeScope { Content = input });
        try
        {
            Assert.True(input.Focus());
            input.Select(1, 2);
            object editor = input.Template.FindName("PART_ContentHost", input);
            input.SearchButtonStyle = style;
            source.Text = "搜索";
            Sta.Pump();
            var button = (Button)input.Template.FindName("PART_SearchButton", input);
            Assert.IsType<Border>(button.Template.FindName("CustomChrome", button));
            Assert.Equal("搜索", button.Content);
            Assert.True(BindingOperations.IsDataBound(input, AntInput.SearchButtonContentProperty));
            Assert.Same(editor, input.Template.FindName("PART_ContentHost", input));
            Assert.Equal(1, input.SelectionStart);
            Assert.Equal(2, input.SelectionLength);
            Assert.True(input.IsKeyboardFocused);
            var provider = (System.Windows.Automation.Provider.IInvokeProvider)
                UIElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.Invoke);
            provider.Invoke();
            Sta.Pump();
            Assert.Equal(1, calls);
            Assert.Equal("retained", input.Text);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Gallery_SearchCancellationAndCompletionKeepLatestStatus() => Sta.Run(() =>
    {
        var window = new Gallery.MainWindow { ShowInTaskbar = false, ShowActivated = false, Opacity = 0 };
        window.Show();
        Sta.Pump();
        try
        {
            var input = (AntInput)window.FindName("SearchInput");
            void DispatchClick()
            {
                window.Dispatcher.BeginInvoke(new Action(() => Click(input, "PART_SearchButton")));
                Sta.Pump();
            }
            input.Text = "old";
            DispatchClick();
            Assert.True(window.IsSearchBusy);
            input.CancelSearch();
            Sta.WaitUntil(() => !window.IsSearchBusy);
            Sta.Pump();
            Assert.True(input.CanSearch);
            Assert.Equal("Cancelled · 已取消", window.SearchStatus);
            input.Text = "new";
            DispatchClick();
            Assert.True(window.IsSearchBusy);
            Sta.WaitUntil(() => !window.IsSearchBusy);
            Assert.Equal("Button · new", window.SearchStatus);
            DispatchClick();
            window.Close();
            Sta.WaitUntil(() => !window.IsSearchBusy);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Input_SearchCompositionAndMultilineDoNotSubmitEnter() => Sta.Run(() =>
    {
        int calls = 0;
        var input = new SearchInput { SearchCommand = new ActionCommand(_ => calls++) };
        Window window = Sta.Show(new AntThemeScope { Content = input });
        try
        {
            Assert.False(input.KeyDown(Key.Enter));
            Assert.Equal(0, calls);
            input.IsSearchEnabled = true;
            input.Composition(TextCompositionManager.PreviewTextInputStartEvent);
            Assert.False(input.CanSearch);
            Assert.False(input.KeyDown(Key.Enter));
            Assert.Equal(0, calls);
            input.Composition(TextCompositionManager.PreviewTextInputEvent);
            Assert.True(input.KeyDown(Key.Enter));
            Assert.Equal(1, calls);
            input.AcceptsReturn = true;
            Assert.False(input.KeyDown(Key.Enter));
            Assert.Equal(1, calls);
            Click(input, "PART_SearchButton");
            Assert.Equal(2, calls);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Input_SearchLifecycleAndRetemplateReleaseOldRequestsAndButtons() => Sta.Run(() =>
    {
        var requests = new List<InputSearchEventArgs>();
        var command = new ActionCommand(requests.Add);
        var input = new SearchInput { IsSearchEnabled = true, SearchCommand = command, Text = "same editor" };
        var scope = new AntThemeScope { Content = input };
        Window window = Sta.Show(scope);
        try
        {
            Click(input, "PART_SearchButton");
            input.IsSearchEnabled = false;
            Assert.True(requests[0].CancellationToken.IsCancellationRequested);
            input.IsSearchEnabled = true;
            Click(input, "PART_SearchButton");
            scope.Content = null;
            Sta.Pump();
            Assert.True(requests[1].CancellationToken.IsCancellationRequested);
            Assert.False(input.CanSearch);
            scope.Content = input;
            Sta.Pump();
            Click(input, "PART_SearchButton");
            int newCalls = 0;
            var replacementCommand = new ActionCommand(_ => newCalls++);
            input.SearchCommand = replacementCommand;
            Assert.True(requests[2].CancellationToken.IsCancellationRequested);
            int queries = replacementCommand.Queries;
            command.Notify();
            Assert.Equal(queries, replacementCommand.Queries);
            var oldButton = (Button)input.Template.FindName("PART_SearchButton", input);
            ControlTemplate template = input.Template;
            input.Template = null;
            Sta.Pump();
            input.Template = template;
            Sta.Pump();
            oldButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.Equal(0, newCalls);
            Click(input, "PART_SearchButton");
            Assert.Equal(1, newCalls);
            input.Template = (ControlTemplate)XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><ScrollViewer x:Name='PART_ContentHost' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' /></ControlTemplate>");
            Sta.Pump();
            input.KeyDown(Key.Enter);
            Assert.Equal(2, newCalls);
            Assert.Equal("same editor", input.Text);
        }
        finally { window.Close(); }
    });

    [Theory]
    [InlineData(ControlSize.Small, 24, 4, FlowDirection.LeftToRight)]
    [InlineData(ControlSize.Middle, 32, 6, FlowDirection.RightToLeft)]
    [InlineData(ControlSize.Large, 40, 8, FlowDirection.LeftToRight)]
    public void Input_SearchSizesCornersAndThemePreserveEditor(ControlSize size, double height, double radius, FlowDirection direction) => Sta.Run(() =>
    {
        var input = new SearchInput { IsSearchEnabled = true, SearchButtonContent = "Search", Size = size, FlowDirection = direction, Width = 300, Text = "查询" };
        var panel = new StackPanel();
        panel.Children.Add(input);
        var scope = new AntThemeScope { Content = panel };
        Window window = Sta.Show(scope);
        try
        {
            var button = (AntButton)input.Template.FindName("PART_SearchButton", input);
            var chrome = (Border)input.Template.FindName("Chrome", input);
            var buttonChrome = (Border)button.Template.FindName("Chrome", button);
            Assert.InRange(input.ActualHeight, height - 0.8, height + 0.8);
            Assert.InRange(button.ActualHeight, height - 0.8, height + 0.8);
            Assert.Equal(new CornerRadius(radius, 0, 0, radius), chrome.CornerRadius);
            Assert.Equal(new CornerRadius(0, radius, radius, 0), buttonChrome.CornerRadius);
            Assert.Equal(SemanticColor.Primary, button.SemanticColor);
            Assert.True(input.Focus());
            input.Select(0, 1);
            object editor = input.Template.FindName("PART_ContentHost", input);
            scope.Mode = ThemeMode.Dark;
            Sta.Pump();
            Assert.Equal(1, input.SelectionLength);
            Assert.True(input.IsKeyboardFocused);
            Assert.Same(editor, input.Template.FindName("PART_ContentHost", input));
            input.IsSearchEnabled = false;
            Sta.Pump();
            Assert.Equal(Visibility.Collapsed, button.Visibility);
            Assert.Equal(new CornerRadius(radius), chrome.CornerRadius);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Input_SearchCommandFailureAndCancellationCallbacksCannotLeaveDispatchLocked() => Sta.Run(() =>
    {
        InputSearchEventArgs? request = null;
        var input = new SearchInput { IsSearchEnabled = true };
        input.SearchRequested += (_, args) => request = args;
        input.SearchCommand = new ActionCommand(_ => throw new InvalidOperationException("host failure"));
        Window window = Sta.Show(new AntThemeScope { Content = input });
        try
        {
            Assert.Throws<InvalidOperationException>(() => Click(input, "PART_SearchButton"));
            Assert.True(request!.CancellationToken.IsCancellationRequested);
            Assert.True(input.CanSearch);
            int calls = 0;
            input.SearchCommand = new ActionCommand(_ => calls++);
            Click(input, "PART_SearchButton");
            Assert.Equal(1, calls);
            using (request!.CancellationToken.Register(() => input.IsSearchEnabled = false))
            {
                Click(input, "PART_SearchButton");
            }
            Assert.Equal(1, calls);
            Assert.False(input.IsSearchEnabled);
            input.IsSearchEnabled = true;
            Click(input, "PART_SearchButton");
            Assert.Equal(2, calls);
        }
        finally { window.Close(); }
    });

    private static void Click(AntInput input, string name) =>
        ((Button)input.Template.FindName(name, input)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

    private static IEnumerable<DependencyObject> Descendants(DependencyObject element)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(element, i);
            yield return child;
            foreach (DependencyObject descendant in Descendants(child)) { yield return descendant; }
        }
    }

    private sealed class SearchInput : AntInput
    {
        public new bool KeyDown(Key key)
        {
            if (IsEnabled)
            {
                Focus();
            }
            var args = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(this), 0, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
            OnPreviewKeyDown(args);
            return args.Handled;
        }

        public void Composition(RoutedEvent routedEvent) => RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice, new TextComposition(InputManager.Current, this, "中")) { RoutedEvent = routedEvent });
    }

    private sealed class ActionCommand(Action<InputSearchEventArgs> execute) : ICommand
    {
        public bool Enabled { get; set; } = true;
        public int Queries { get; private set; }
        public Func<InputSearchEventArgs, bool>? Predicate { get; set; }
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) { Queries++; return Enabled && (Predicate?.Invoke(Assert.IsType<InputSearchEventArgs>(parameter)) ?? true); }
        public void Execute(object? parameter) => execute(Assert.IsType<InputSearchEventArgs>(parameter));
        public void Notify() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
