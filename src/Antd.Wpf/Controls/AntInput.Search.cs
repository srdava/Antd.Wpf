using System;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;

namespace Antd.Wpf.Controls;

[TemplatePart(Name = SearchButtonPartName, Type = typeof(Button))]
public partial class AntInput
{
    private const string SearchButtonPartName = "PART_SearchButton";
    private Button? _searchButton;
    private CancellationTokenSource? _searchCancellation;
    private bool _isSearching;
    private bool _isComposing;

    public AntInput()
    {
        Loaded += OnSearchLoaded;
        Unloaded += OnSearchUnloaded;
        AddHandler(TextCompositionManager.PreviewTextInputStartEvent, new TextCompositionEventHandler(OnCompositionStarted), true);
        AddHandler(TextCompositionManager.PreviewTextInputEvent, new TextCompositionEventHandler(OnCompositionCompleted), true);
    }

    public static readonly DependencyProperty IsSearchEnabledProperty = DependencyProperty.Register(
        nameof(IsSearchEnabled), typeof(bool), typeof(AntInput), new FrameworkPropertyMetadata(false, OnSearchModeChanged));

    public bool IsSearchEnabled
    {
        get => (bool)GetValue(IsSearchEnabledProperty);
        set => SetValue(IsSearchEnabledProperty, value);
    }

    public static readonly DependencyProperty SearchCommandProperty = DependencyProperty.Register(
        nameof(SearchCommand), typeof(ICommand), typeof(AntInput), new FrameworkPropertyMetadata(null, OnSearchCommandChanged));

    public ICommand? SearchCommand
    {
        get => (ICommand?)GetValue(SearchCommandProperty);
        set => SetValue(SearchCommandProperty, value);
    }

    public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(
        nameof(IsLoading), typeof(bool), typeof(AntInput), new FrameworkPropertyMetadata(false, OnSearchLoadingChanged));

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public static readonly DependencyProperty SearchButtonContentProperty = DependencyProperty.Register(
        nameof(SearchButtonContent), typeof(object), typeof(AntInput), new FrameworkPropertyMetadata("Search"));

    public object? SearchButtonContent
    {
        get => GetValue(SearchButtonContentProperty);
        set => SetValue(SearchButtonContentProperty, value);
    }

    public static readonly DependencyProperty SearchButtonStyleProperty = DependencyProperty.Register(
        nameof(SearchButtonStyle), typeof(Style), typeof(AntInput), new FrameworkPropertyMetadata(null));

    public Style? SearchButtonStyle
    {
        get => (Style?)GetValue(SearchButtonStyleProperty);
        set => SetValue(SearchButtonStyleProperty, value);
    }

    private static readonly DependencyPropertyKey CanSearchPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(CanSearch), typeof(bool), typeof(AntInput), new FrameworkPropertyMetadata(false));
    public static readonly DependencyProperty CanSearchProperty = CanSearchPropertyKey.DependencyProperty;
    public bool CanSearch => (bool)GetValue(CanSearchProperty);

    public event EventHandler<InputSearchEventArgs>? SearchRequested;

    private bool CanInvokeSearch => IsSearchEnabled && IsLoaded && IsEnabled && !IsLoading && !_isComposing && !_isSearching;
    private string DefaultItemStatus => IsSearchEnabled && IsLoading ? "Loading" : AutomationProperties.GetItemStatus(this);

    public void CancelSearch()
    {
        VerifyAccess();
        CancellationTokenSource? source = _searchCancellation;
        _searchCancellation = null;
        try { source?.Cancel(); }
        finally { source?.Dispose(); }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (!e.Handled && IsSearchEnabled && e.Key == Key.Escape && _searchCancellation != null)
        {
            CancelSearch();
            e.Handled = true;
        }
        else if (!e.Handled && IsSearchEnabled && IsKeyboardFocused && !AcceptsReturn && !_isComposing && e.Key == Key.Enter)
        {
            if (!e.IsRepeat)
            {
                TrySearch(InputSearchReason.Enter);
            }
            e.Handled = true;
        }
        base.OnPreviewKeyDown(e);
    }

    private void TrySearch(InputSearchReason reason)
    {
        if (!CanInvokeSearch)
        {
            return;
        }

        _isSearching = true;
        CancellationTokenSource? source = null;
        bool accepted = false;
        try
        {
            UpdateSearchAvailability();
            source = new CancellationTokenSource();
            var request = new InputSearchEventArgs(Text, reason, source.Token);
            ICommand? command = SearchCommand;
            if (command != null && !command.CanExecute(request))
            {
                return;
            }
            CancelSearch();
            if (!IsSearchEnabled || !IsEnabled || !IsLoaded || IsLoading || _isComposing)
            {
                return;
            }
            _searchCancellation = source;
            SearchRequested?.Invoke(this, request);
            if (request.Cancel || request.CancellationToken.IsCancellationRequested || !IsSearchEnabled || !IsEnabled)
            {
                return;
            }
            command?.Execute(request);
            accepted = true;
        }
        finally
        {
            try
            {
                if (!accepted && source != null)
                {
                    if (ReferenceEquals(_searchCancellation, source))
                    {
                        CancelSearch();
                    }
                    else
                    {
                        source.Dispose();
                    }
                }
            }
            finally
            {
                _isSearching = false;
                UpdateSearchAvailability();
            }
        }
    }

    private void ApplySearchTemplate()
    {
        _searchButton = GetTemplateChild(SearchButtonPartName) as Button;
        if (_searchButton != null)
        {
            _searchButton.Click += OnSearchClick;
        }
        UpdateSearchAvailability();
    }

    private void DetachSearchButton()
    {
        if (_searchButton != null)
        {
            _searchButton.Click -= OnSearchClick;
            _searchButton = null;
        }
    }

    private void OnSearchClick(object sender, RoutedEventArgs args)
    {
        TrySearch(InputSearchReason.Button);
        args.Handled = true;
    }

    private void UpdateSearchAvailability()
    {
        bool enabled = CanInvokeSearch && (SearchCommand == null ||
            SearchCommand.CanExecute(new InputSearchEventArgs(Text, InputSearchReason.Button, CancellationToken.None)));
        if (enabled != CanSearch)
        {
            SetValue(CanSearchPropertyKey, enabled);
        }
    }

    private void OnSearchLoaded(object sender, RoutedEventArgs args) => UpdateSearchAvailability();
    private void OnSearchUnloaded(object sender, RoutedEventArgs args)
    {
        _isComposing = false;
        CancelSearch();
        UpdateSearchAvailability();
    }

    private void OnCompositionStarted(object sender, TextCompositionEventArgs args)
    {
        _isComposing = IsSearchEnabled;
        UpdateSearchAvailability();
    }

    private void OnCompositionCompleted(object sender, TextCompositionEventArgs args)
    {
        _isComposing = false;
        UpdateSearchAvailability();
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        _isComposing = false;
        UpdateSearchAvailability();
        base.OnLostKeyboardFocus(e);
    }

    private void OnSearchCanExecuteChanged(object? sender, EventArgs args) => UpdateSearchAvailability();

    private static void OnSearchCommandChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var input = (AntInput)sender;
        if (args.OldValue is ICommand oldCommand)
        {
            CanExecuteChangedEventManager.RemoveHandler(oldCommand, input.OnSearchCanExecuteChanged);
        }
        if (args.NewValue is ICommand newCommand)
        {
            CanExecuteChangedEventManager.AddHandler(newCommand, input.OnSearchCanExecuteChanged);
        }
        input.CancelSearch();
        input.UpdateSearchAvailability();
    }

    private static void OnSearchModeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var input = (AntInput)sender;
        bool oldBusy = (bool)args.OldValue && input.IsLoading;
        if (!(bool)args.NewValue)
        {
            input._isComposing = false;
            input.CancelSearch();
        }
        input.RaiseSearchBusyChanged(oldBusy, (bool)args.NewValue && input.IsLoading);
        input.UpdateSearchAvailability();
    }

    private static void OnSearchLoadingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var input = (AntInput)sender;
        if (!(bool)args.NewValue)
        {
            input._searchCancellation?.Dispose();
            input._searchCancellation = null;
        }
        input.RaiseSearchBusyChanged(input.IsSearchEnabled && (bool)args.OldValue, input.IsSearchEnabled && (bool)args.NewValue);
        input.UpdateSearchAvailability();
    }

    private void RaiseSearchBusyChanged(bool oldBusy, bool newBusy)
    {
        AutomationPeer? peer = UIElementAutomationPeer.FromElement(this);
        if (oldBusy != newBusy && peer != null && AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged) &&
            !Validation.GetHasError(this) && !IsOverMaxLength && Status == ControlStatus.Default)
        {
            string fallback = AutomationProperties.GetItemStatus(this);
            peer.RaisePropertyChangedEvent(AutomationElementIdentifiers.ItemStatusProperty,
                oldBusy ? "Loading" : fallback, newBusy ? "Loading" : fallback);
        }
    }
}
