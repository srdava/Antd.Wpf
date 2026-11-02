using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Antd.Wpf.Controls;
using Antd.Wpf.Theming;
using Xunit;

namespace Antd.Wpf.Tests;

[Trait("Category", "PerformanceObservation")]
public sealed class PerformanceScenarios
{
    [Fact]
    public void PF02_PF05_RecordLayoutLatencyAndBoundedContainers() => Sta.Run(() =>
    {
        var panel = new WrapPanel();
        for (int index = 0; index < 200; index++)
        {
            panel.Children.Add(new AntInput { Width = 80, Text = "Input " + index });
        }

        var scope = new AntThemeScope { Content = panel, MotionEnabled = false };
        Window window = Sta.Show(scope);
        var rows = new List<string> { "scenario,sample,milliseconds,realizedContainers" };
        try
        {
            for (int index = -5; index < 30; index++)
            {
                var watch = Stopwatch.StartNew();
                scope.Mode = scope.Mode == ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light;
                Sta.Pump();
                watch.Stop();
                if (index >= 0)
                {
                    rows.Add("PF02.layout-proxy," + index + "," + watch.Elapsed.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture) + ",200");
                }
            }

            var mixed = new WrapPanel();
            for (int index = 0; index < 200; index++)
            {
                FrameworkElement control = (index % 4) switch
                {
                    0 => new AntButton { Content = "Button", Width = 80 },
                    1 => new AntInput { Text = "Input", Width = 80 },
                    2 => new AntCheckBox { Content = "Check", Width = 80, IsChecked = true },
                    _ => new AntRadioButton { Content = "Radio", Width = 80 },
                };
                mixed.Children.Add(control);
            }

            scope.Content = mixed;
            Sta.Pump();
            for (int index = -5; index < 30; index++)
            {
                var watch = Stopwatch.StartNew();
                scope.Mode = scope.Mode == ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light;
                Sta.Pump();
                watch.Stop();
                if (index >= 0)
                {
                    rows.Add("PF02.mixed-layout-proxy," + index + "," + watch.Elapsed.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture) + ",200");
                }
            }

            var select = new AntSelect
            {
                Width = 240,
                VerticalAlignment = VerticalAlignment.Top,
                ItemsSource = Enumerable.Range(0, 10000).Select(index => "Item " + index).ToArray(),
            };
            scope.Content = select;
            Sta.Pump();
            for (int index = -5; index < 30; index++)
            {
                var watch = Stopwatch.StartNew();
                select.IsDropDownOpen = true;
                Sta.Pump();
                watch.Stop();
                int realized = Enumerable.Range(0, 10000).Count(item => select.ItemContainerGenerator.ContainerFromIndex(item) != null);
                Assert.InRange(realized, 1, 45);
                if (index >= 0)
                {
                    rows.Add("PF05.layout-proxy," + index + "," + watch.Elapsed.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture) + "," + realized);
                }

                select.IsDropDownOpen = false;
                Sta.Pump();
            }
        }
        finally { window.Close(); }

#if NETFRAMEWORK
        const string target = "net48";
#else
        const string target = "net8.0-windows";
#endif
        string directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts/performance/m0", target));
        Directory.CreateDirectory(directory);
        File.WriteAllLines(Path.Combine(directory, "samples.csv"), rows);
        File.WriteAllLines(Path.Combine(directory, "environment.txt"), new[]
        {
            "Machine=" + Environment.MachineName,
            "OS=" + Environment.OSVersion,
            "Processors=" + Environment.ProcessorCount,
            "CLR=" + Environment.Version,
            "Target=" + target,
            "RenderTier=" + (System.Windows.Media.RenderCapability.Tier >> 16),
            "DPI=" + System.Windows.Media.VisualTreeHelper.GetDpi(window).PixelsPerInchX,
            "Method=Release; 5 warmups + 30 samples; invisible native Window; endpoint=Dispatcher Background drained.",
            "Limit=Layout/Dispatcher proxy only, not GPU presentation, cold startup, keyboard latency, scrolling or hardware calibration.",
        });
    });
}
