using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Antd.Wpf.Controls;
using Antd.Wpf.Theming;

namespace Antd.Wpf.Gallery;

internal sealed class StartupProbe
{
    private readonly Application _application;
    private readonly string _mode;
    private readonly string _output;
    private readonly long _startup;
    private readonly string[] _startupAssemblies;
    private readonly DispatcherTimer _timeout = new() { Interval = TimeSpan.FromSeconds(10) };
    private Window? _window;
    private long _loaded;
    private long _rendering;
    private long _inputDispatch;
    private bool _focusAccepted;
    private bool _finishing;
    private int _controlCount;

    private StartupProbe(Application application, string mode, string output, long startup)
    {
        _application = application;
        _mode = mode;
        _output = output;
        _startup = startup;
        _startupAssemblies = AssemblyNames();
    }

    public static void Run(Application application, string[] args, long startup)
    {
        if (args.Length != 3 || (args[1] != "empty" && args[1] != "controls"))
        {
            Console.Error.WriteLine("Usage: --measure-startup empty|controls <output.csv>");
            application.Shutdown(2);
            return;
        }
        try { new StartupProbe(application, args[1], Path.GetFullPath(args[2]), startup).Start(); }
        catch (Exception error) { Console.Error.WriteLine(error); application.Shutdown(1); }
    }

    private void Start()
    {
        _application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        FrameworkElement content;
        UIElement focusTarget;
        if (_mode == "controls") { content = CreateControls(out focusTarget, out _controlCount); }
        else { var empty = new Grid { Focusable = true }; content = empty; focusTarget = empty; }
        _window = new Window
        {
            Content = content,
            Width = 1000,
            Height = 800,
            ShowInTaskbar = false,
            ShowActivated = false,
            Opacity = 0,
            IsHitTestVisible = false,
        };
        _application.MainWindow = _window;
        _window.Loaded += (_, _) =>
        {
            _loaded = Stopwatch.GetTimestamp();
            _window.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                _focusAccepted = focusTarget.Focus();
                _inputDispatch = Stopwatch.GetTimestamp();
                FinishWhenReady();
            }));
        };
        CompositionTarget.Rendering += OnRendering;
        _timeout.Tick += OnTimeout;
        _timeout.Start();
        _window.Show();
    }

    private void OnRendering(object? sender, EventArgs args)
    {
        if (_loaded == 0) { return; }
        _rendering = Stopwatch.GetTimestamp();
        CompositionTarget.Rendering -= OnRendering;
        FinishWhenReady();
    }

    private void FinishWhenReady()
    {
        if (_loaded == 0 || _rendering == 0 || _inputDispatch == 0 || _finishing) { return; }
        _finishing = true;
        _application.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
        {
            try { WriteResult(); Stop(0); }
            catch (Exception error) { Console.Error.WriteLine(error); Stop(1); }
        }));
    }

    private void OnTimeout(object? sender, EventArgs args)
    {
        Console.Error.WriteLine("Startup probe timed out before all checkpoints.");
        Stop(1);
    }

    private void Stop(int exitCode)
    {
        _timeout.Stop();
        _timeout.Tick -= OnTimeout;
        CompositionTarget.Rendering -= OnRendering;
        _window?.Close();
        _application.Shutdown(exitCode);
    }

    private void WriteResult()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_output)!);
        using Process process = Process.GetCurrentProcess();
        File.WriteAllLines(_output, new[]
        {
            "mode,controlCount,processId,qpcFrequency,startupQpc,loadedQpc,renderingQpc,inputDispatchQpc,focusAccepted,visualNodes,scopeDictionaries",
            string.Join(",", _mode, _controlCount.ToString(CultureInfo.InvariantCulture), process.Id.ToString(CultureInfo.InvariantCulture),
                Stopwatch.Frequency.ToString(CultureInfo.InvariantCulture), _startup.ToString(CultureInfo.InvariantCulture),
                _loaded.ToString(CultureInfo.InvariantCulture), _rendering.ToString(CultureInfo.InvariantCulture),
                _inputDispatch.ToString(CultureInfo.InvariantCulture), _focusAccepted,
                CountVisuals(_window!).ToString(CultureInfo.InvariantCulture),
                ((FrameworkElement)_window!.Content).Resources.MergedDictionaries.Count.ToString(CultureInfo.InvariantCulture)),
        });
        var assemblies = new System.Collections.Generic.List<string> { "stage,assembly" };
        foreach (string name in _startupAssemblies) { assemblies.Add("startup," + name); }
        foreach (string name in AssemblyNames()) { assemblies.Add("ready," + name); }
        File.WriteAllLines(Path.ChangeExtension(_output, "assemblies.csv"), assemblies);
        File.WriteAllLines(Path.ChangeExtension(_output, "environment.txt"), new[]
        {
            "OS=" + Environment.OSVersion, "Machine=" + Environment.MachineName, "CLR=" + Environment.Version,
            "ProcessStartUtc=" + process.StartTime.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
            "RenderTier=" + (RenderCapability.Tier >> 16), "DPI=" + VisualTreeHelper.GetDpi(_window!).PixelsPerInchX,
            "Method=Fresh hidden process; Light/default metrics; no motion; first post-Loaded Rendering and Input Dispatcher checkpoints; OS caches not flushed.",
            "Limit=Rendering callback is not GPU presentation; focus result/dispatcher checkpoint is not physical input readiness; no frame/GC/heap acceptance.",
        });
    }

    private static string[] AssemblyNames()
    {
        System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var names = new string[assemblies.Length];
        for (int i = 0; i < assemblies.Length; i++) { names[i] = assemblies[i].GetName().Name!; }
        Array.Sort(names, StringComparer.Ordinal);
        return names;
    }

    private static int CountVisuals(DependencyObject element)
    {
        int count = 1;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++) { count += CountVisuals(VisualTreeHelper.GetChild(element, i)); }
        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static FrameworkElement CreateControls(out UIElement focusTarget, out int controlCount)
    {
        var panel = new UniformGrid { Columns = 10, Rows = 20 };
        focusTarget = panel;
        string[] options = { "Alpha", "Beta", "Gamma" };
        for (int i = 0; i < 200; i++)
        {
            Control control;
            switch (i % 5)
            {
                case 0: control = new AntButton { Content = "Button" }; break;
                case 1: control = new AntInput { Text = "Input" }; focusTarget = control; break;
                case 2: control = new AntSelect { ItemsSource = options, SelectedIndex = 0 }; break;
                case 3: control = new AntCheckBox { Content = "Check" }; break;
                default: control = new AntRadioButton { Content = "Radio", GroupName = "Startup" }; break;
            }
            panel.Children.Add(control);
        }
        controlCount = panel.Children.Count;
        return new AntThemeScope { Content = panel, MotionEnabled = false };
    }
}
