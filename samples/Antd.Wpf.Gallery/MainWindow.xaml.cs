using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Antd.Wpf.Controls;
using Antd.Wpf.Theming;

namespace Antd.Wpf.Gallery;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly CancellationTokenSource _lifetime = new();
    private bool _isBusy;
    private bool _isFeatureEnabled;
    private string _inputText = "你好，Ant Design";
    private string _saveStatus = "Ready";
    private bool _isSearchBusy;
    private int _searchVersion;
    private string _searchStatus = "Ready · 搜索只在显式触发时执行";
    private string? _selectedOption;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        Closed += OnClosed;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public IReadOnlyList<string> Options { get; } = Enumerable.Range(1, 10000).Select(index => "Option " + index).ToArray();

    public bool IsBusy
    {
        get => _isBusy;
        private set { _isBusy = value; Changed(nameof(IsBusy)); }
    }

    public bool IsFeatureEnabled
    {
        get => _isFeatureEnabled;
        set { _isFeatureEnabled = value; Changed(nameof(IsFeatureEnabled)); }
    }

    public bool IsSearchBusy
    {
        get => _isSearchBusy;
        private set { _isSearchBusy = value; Changed(nameof(IsSearchBusy)); }
    }

    public string SearchStatus
    {
        get => _searchStatus;
        private set { _searchStatus = value; Changed(nameof(SearchStatus)); }
    }

    public string SaveStatus
    {
        get => _saveStatus;
        private set { _saveStatus = value; Changed(nameof(SaveStatus)); }
    }

    public string InputText
    {
        get => _inputText;
        set { _inputText = value; Changed(nameof(InputText)); }
    }

    public string? SelectedOption
    {
        get => _selectedOption;
        set { _selectedOption = value; Changed(nameof(SelectedOption)); }
    }

    private void ToggleTheme(object sender, RoutedEventArgs args) =>
        Theme.Mode = Theme.Mode == ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light;

    private void ShowVisualMatrix(object sender, RoutedEventArgs args)
    {
        var matrix = (Window)Application.LoadComponent(new Uri("/Antd.Wpf.Gallery;component/VisualMatrix.xaml", UriKind.Relative));
        matrix.Owner = this;
        matrix.Show();
    }

    private void ShowDividerMatrix(object sender, RoutedEventArgs args)
    {
        var matrix = (Window)Application.LoadComponent(new Uri("/Antd.Wpf.Gallery;component/DividerMatrix.xaml", UriKind.Relative));
        matrix.Owner = this;
        matrix.Show();
    }

    private void ShowFlexMatrix(object sender, RoutedEventArgs args)
    {
        var matrix = (Window)Application.LoadComponent(new Uri("/Antd.Wpf.Gallery;component/FlexMatrix.xaml", UriKind.Relative));
        matrix.Owner = this;
        matrix.Show();
    }

    private void ShowSpaceMatrix(object sender, RoutedEventArgs args)
    {
        var matrix = (Window)Application.LoadComponent(new Uri("/Antd.Wpf.Gallery;component/SpaceMatrix.xaml", UriKind.Relative));
        matrix.Owner = this;
        matrix.Show();
    }

    private void ShowGridMatrix(object sender, RoutedEventArgs args)
    {
        var matrix = (Window)Application.LoadComponent(new Uri("/Antd.Wpf.Gallery;component/GridMatrix.xaml", UriKind.Relative));
        matrix.Owner = this;
        matrix.Show();
    }

    private void ShowLayoutMatrix(object sender, RoutedEventArgs args)
    {
        var matrix = (Window)Application.LoadComponent(new Uri("/Antd.Wpf.Gallery;component/LayoutMatrix.xaml", UriKind.Relative));
        matrix.Owner = this;
        matrix.Show();
    }

    private void ChangeSize(object sender, RoutedEventArgs args) =>
        Theme.Size = (ControlSize)Enum.Parse(typeof(ControlSize), (string)((AntButton)sender).Tag);

    private async void Save(object sender, RoutedEventArgs args)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        SaveStatus = "Saving…";
        try
        {
            await Task.Delay(800, _lifetime.Token);
            SaveStatus = "Saved · " + DateTime.Now.ToLongTimeString();
        }
        catch (OperationCanceledException)
        {
            SaveStatus = "Cancelled";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnClosed(object? sender, EventArgs args)
    {
        _lifetime.Cancel();
        _lifetime.Dispose();
    }

    private void CancelSearch(object sender, RoutedEventArgs args) => SearchInput.CancelSearch();

    private async void Search(object? sender, InputSearchEventArgs args)
    {
        int version = ++_searchVersion;
        IsSearchBusy = true;
        SearchStatus = "Searching… · " + args.Reason;
        try
        {
            await Task.Delay(650, args.CancellationToken);
            if (version == _searchVersion && !args.CancellationToken.IsCancellationRequested && !_lifetime.IsCancellationRequested)
            {
                SearchStatus = args.Reason + " · " + args.Text;
            }
        }
        catch (OperationCanceledException)
        {
            if (version == _searchVersion && !_lifetime.IsCancellationRequested)
            {
                SearchStatus = "Cancelled · 已取消";
            }
        }
        finally
        {
            if (version == _searchVersion)
            {
                IsSearchBusy = false;
            }
        }
    }

    private void Changed(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
