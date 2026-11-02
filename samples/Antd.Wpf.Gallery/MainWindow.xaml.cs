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

    private void Changed(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
