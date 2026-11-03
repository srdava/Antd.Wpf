using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Antd.Wpf.Controls;
using Antd.Wpf.Theming;
using Xunit;

namespace Antd.Wpf.Tests;

[Trait("Category", "LifecycleObservation")]
public sealed class LifecycleScenarios
{
    [Fact]
    public void PF09_HundredPagePopupCyclesReleaseAfterNativeAutomationBaseline() => Sta.Run(() =>
    {
        var references = new List<KeyValuePair<string, WeakReference>>();
        var commands = new List<RequestCommand>();
        ExercisePages(references, commands);
        Assert.Equal(901, references.Count);
        CollectAndDrain();
        WriteReport(references, commands.Count, "before-native-baseline");
        var nativeReferences = new List<KeyValuePair<string, WeakReference>>();
        ExerciseNativePopupBaseline(nativeReferences);
        CollectAndDrain();
        WriteReport(references, commands.Count, "pages");
        WriteReport(nativeReferences, commands.Count, "native-baseline", 3);
        foreach (KeyValuePair<string, WeakReference> reference in references)
        {
            Assert.False(reference.Value.IsAlive, "Retained after unload/owner close: " + reference.Key);
        }
        Assert.Equal(100, commands.Count);
        var commandReferences = new List<KeyValuePair<string, WeakReference>>();
        ReleaseCommands(commands, commandReferences);
        Assert.Equal(100, commandReferences.Count);
        CollectAndDrain();
        WriteReport(commandReferences, 0, "commands");
        foreach (KeyValuePair<string, WeakReference> reference in commandReferences)
        {
            Assert.False(reference.Value.IsAlive, "Retained search command: " + reference.Key);
        }
    });

    [Fact]
    public void PF09_ProbeDetectsRetainedControlAndReleasedControl() => Sta.Run(() =>
    {
        var roots = new List<object>();
        WeakReference reference = CreateRootedControl(roots);
        CollectAndDrain();
        Assert.True(reference.IsAlive);
        roots.Clear();
        CollectAndDrain();
        Assert.False(reference.IsAlive);
    });

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ExercisePages(List<KeyValuePair<string, WeakReference>> references, List<RequestCommand> commands)
    {
        var host = new Border();
        Window window = Sta.Show(host);
        references.Add(new KeyValuePair<string, WeakReference>("Owner", new WeakReference(window)));
        try
        {
            for (int i = 0; i < 100; i++) { CyclePage(host, references, commands, i); }
        }
        finally { host.Child = null; window.Content = null; window.Close(); }
        foreach (RequestCommand command in commands) { command.Notify(); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CyclePage(Border host, List<KeyValuePair<string, WeakReference>> references, List<RequestCommand> commands, int cycle)
    {
        var command = new RequestCommand();
        commands.Add(command);
        var input = new AntInput { IsSearchEnabled = true, Text = "Lifecycle query", SearchCommand = command };
        var select = new AntSelect { ItemsSource = new[] { "Alpha", "Beta", "Gamma" }, SelectedIndex = 1 };
        var button = new AntButton { Content = "Loading", IsLoading = true };
        var toggle = new AntSwitch { IsChecked = false };
        var password = new AntPasswordBox();
        var panel = new StackPanel();
        panel.Children.Add(input);
        panel.Children.Add(select);
        panel.Children.Add(button);
        panel.Children.Add(toggle);
        panel.Children.Add(password);
        var scope = new AntThemeScope { Content = panel, Mode = cycle % 2 == 0 ? ThemeMode.Light : ThemeMode.Dark };
        host.Child = scope;
        Sta.Pump();
        Assert.True(input.IsLoaded);
        Assert.True(button.IsLoadingVisible);
        var searchButton = (Button)input.Template.FindName("PART_SearchButton", input);
        searchButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Assert.NotNull(command.Request);
        CancellationToken cancellation = command.Request!.CancellationToken;
        Assert.False(cancellation.IsCancellationRequested);
        var passwordEditor = (PasswordBox)((ContentPresenter)password.Template.FindName("PART_PasswordHost", password)).Content;
        passwordEditor.Password = "test-only";
        toggle.IsChecked = true;
        select.IsDropDownOpen = true;
        Sta.Pump();
        var popup = (Popup)select.Template.FindName("PART_Popup", select);
        var popupRoot = Assert.IsAssignableFrom<FrameworkElement>(popup.Child);
        Assert.True(popup.IsOpen);
        Assert.Contains(scope.Resources, popupRoot.Resources.MergedDictionaries);
        object[] tracked = { scope, panel, input, select, button, toggle, password, passwordEditor, popupRoot };
        foreach (object target in tracked)
        {
            references.Add(new KeyValuePair<string, WeakReference>(cycle + ":" + target.GetType().Name, new WeakReference(target)));
        }
        host.Child = null;
        Sta.Pump();
        Assert.False(input.IsLoaded);
        Assert.True(cancellation.IsCancellationRequested);
        Assert.False(select.IsDropDownOpen);
        Assert.False(popup.IsOpen);
        Assert.DoesNotContain(scope.Resources, popupRoot.Resources.MergedDictionaries);
        Assert.False(button.IsLoadingVisible);
        using var secret = password.SecurePassword;
        Assert.Equal(0, secret.Length);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ExerciseNativePopupBaseline(List<KeyValuePair<string, WeakReference>> references)
    {
        for (int i = 0; i < 3; i++)
        {
            var select = new ComboBox { ItemsSource = new[] { "Native Alpha", "Native Beta" }, SelectedIndex = 0 };
            Window window = Sta.Show(select);
            try
            {
                select.IsDropDownOpen = true;
                Sta.Pump();
                var popup = (Popup)select.Template.FindName("PART_Popup", select);
                Assert.True(popup.IsOpen);
                references.Add(new KeyValuePair<string, WeakReference>("NativeOwner" + i, new WeakReference(window)));
                references.Add(new KeyValuePair<string, WeakReference>("NativeSelect" + i, new WeakReference(select)));
                references.Add(new KeyValuePair<string, WeakReference>("NativePopup" + i, new WeakReference(popup.Child)));
                select.IsDropDownOpen = false;
                Sta.Pump();
                Assert.False(popup.IsOpen);
            }
            finally { window.Content = null; window.Close(); }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ReleaseCommands(List<RequestCommand> commands, List<KeyValuePair<string, WeakReference>> references)
    {
        for (int i = 0; i < commands.Count; i++) { references.Add(new KeyValuePair<string, WeakReference>("Command" + i, new WeakReference(commands[i]))); }
        commands.Clear();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateRootedControl(List<object> roots)
    {
        var input = new AntInput();
        roots.Add(input);
        return new WeakReference(input);
    }

    private static void CollectAndDrain()
    {
        // Popup/animation teardown can queue work for a later Dispatcher tick.
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle) { Interval = TimeSpan.FromMilliseconds(100) };
        EventHandler tick = (_, _) => frame.Continue = false;
        timer.Tick += tick;
        timer.Start();
        try { Dispatcher.PushFrame(frame); }
        finally { timer.Stop(); timer.Tick -= tick; }
        for (int i = 0; i < 3; i++)
        {
            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => { }));
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        Sta.Pump();
    }

    private static void WriteReport(List<KeyValuePair<string, WeakReference>> references, int retainedCommands, string phase, int cycles = 100)
    {
#if NETFRAMEWORK
        const string target = "net48";
#else
        const string target = "net8.0-windows";
#endif
        string directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts/performance/lifecycle", target));
        Directory.CreateDirectory(directory);
        var rows = new List<string> { "target,alive" };
        foreach (KeyValuePair<string, WeakReference> reference in references) { rows.Add(reference.Key + "," + reference.Value.IsAlive); }
        File.WriteAllLines(Path.Combine(directory, phase + "-references.csv"), rows);
        File.WriteAllLines(Path.Combine(directory, phase + "-environment.txt"), new[]
        {
            "OS=" + Environment.OSVersion, "Machine=" + Environment.MachineName, "CLR=" + Environment.Version,
            "Cycles=" + cycles + "; references=" + references.Count + "; external search commands retained during collection=" + retainedCommands,
            "Method=Real hidden Window and Popup; unload/close; ApplicationIdle drain; test-host-only GC; NoInlining setup boundaries.",
            "Limit=Conditional WeakReference probe after three native UIA popup baselines; before-baseline/native retention reported separately; not full process heap/native handle or hardware acceptance.",
        });
    }

    private sealed class RequestCommand : ICommand
    {
        public InputSearchEventArgs? Request { get; private set; }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => Request = Assert.IsType<InputSearchEventArgs>(parameter);
        public event EventHandler? CanExecuteChanged;
        public void Notify() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
