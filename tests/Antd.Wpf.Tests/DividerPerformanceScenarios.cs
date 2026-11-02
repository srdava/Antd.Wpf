using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Antd.Wpf.Controls;
using Antd.Wpf.Theming;
using Xunit;

namespace Antd.Wpf.Tests;

[Trait("Category", "PerformanceObservation")]
public sealed class DividerPerformanceScenarios
{
    [Fact]
    public void PF02_DividerThemeSwitchKeepsVisualTreeBounded() => Sta.Run(() =>
    {
        var panel = new WrapPanel();
        for (int index = 0; index < 200; index++)
        {
            panel.Children.Add(new AntDivider
            {
                Content = index % 2 == 0 ? "Title" : null,
                Width = 120,
                Margin = new Thickness(0),
                Variant = (DividerVariant)(index % 3),
            });
        }

        var scope = new AntThemeScope { Content = panel, MotionEnabled = false };
        Window window = Sta.Show(scope);
        var rows = new List<string> { "scenario,sample,milliseconds,visualNodes" };
        try
        {
            int nodes = CountVisuals(panel);
            Assert.InRange(nodes, 601, 801);
            for (int index = -5; index < 30; index++)
            {
                var watch = Stopwatch.StartNew();
                scope.Mode = scope.Mode == ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light;
                Sta.Pump();
                watch.Stop();
                if (index >= 0)
                {
                    rows.Add("PF02.divider-layout-proxy," + index + "," + watch.Elapsed.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture) + "," + nodes);
                }
            }

            Assert.Equal(nodes, CountVisuals(panel));
            Assert.Single(scope.Resources.MergedDictionaries);
#if NETFRAMEWORK
            const string target = "net48";
#else
            const string target = "net8.0-windows";
#endif
            string directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts/performance/divider", target));
            Directory.CreateDirectory(directory);
            File.WriteAllLines(Path.Combine(directory, "samples.csv"), rows);
            File.WriteAllLines(Path.Combine(directory, "environment.txt"), new[]
            {
                "OS=" + Environment.OSVersion, "Machine=" + Environment.MachineName, "Processors=" + Environment.ProcessorCount,
                "CLR=" + Environment.Version, "RenderTier=" + (RenderCapability.Tier >> 16), "DPI=" + VisualTreeHelper.GetDpi(window).PixelsPerInchX,
                "Method=Release; 200 dividers; 5 warmups + 30 samples; hidden native Window, no hit testing; Dispatcher Background endpoint.",
                "Limit=Layout/Dispatcher proxy only; not GPU/frame/input/heap or physical DPI acceptance.",
            });
        }
        finally { window.Close(); }
    });

    private static int CountVisuals(DependencyObject element)
    {
        int count = 1;
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
        {
            count += CountVisuals(VisualTreeHelper.GetChild(element, index));
        }

        return count;
    }
}
