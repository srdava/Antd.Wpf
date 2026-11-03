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
public sealed class PasswordPerformanceScenarios
{
    [Fact]
    public void PF02_PasswordThemeSwitchKeepsEditorsAndVisualsBounded() => Sta.Run(() =>
    {
        var panel = new WrapPanel();
        var inputs = new AntPasswordBox[200];
        var editors = new PasswordBox[200];
        for (int i = 0; i < inputs.Length; i++)
        {
            inputs[i] = new AntPasswordBox { Width = 120, AllowClear = i % 2 == 0 };
            panel.Children.Add(inputs[i]);
        }
        var scope = new AntThemeScope { Content = panel, MotionEnabled = false };
        Window window = Sta.Show(scope);
        var rows = new List<string> { "scenario,sample,milliseconds,visualNodes" };
        try
        {
            for (int i = 0; i < inputs.Length; i++)
            {
                editors[i] = (PasswordBox)((ContentPresenter)inputs[i].Template.FindName("PART_PasswordHost", inputs[i])).Content;
                editors[i].Password = "test-only";
            }
            Sta.Pump();
            int nodes = CountVisuals(panel);
            Assert.InRange(nodes, inputs.Length + 1, inputs.Length * 25 + 1);
            for (int i = -5; i < 30; i++)
            {
                var watch = Stopwatch.StartNew();
                scope.Mode = scope.Mode == ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light;
                Sta.Pump();
                watch.Stop();
                if (i >= 0)
                {
                    rows.Add("PF02.password-layout-proxy," + i + "," + watch.Elapsed.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture) + "," + nodes);
                }
                Assert.Equal(nodes, CountVisuals(panel));
                Assert.Single(scope.Resources.MergedDictionaries);
                for (int j = 0; j < inputs.Length; j++)
                {
                    Assert.Same(editors[j], ((ContentPresenter)inputs[j].Template.FindName("PART_PasswordHost", inputs[j])).Content);
                    using var password = inputs[j].SecurePassword;
                    Assert.Equal(9, password.Length);
                }
            }
#if NETFRAMEWORK
            const string target = "net48";
#else
            const string target = "net8.0-windows";
#endif
            string directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts/performance/password", target));
            Directory.CreateDirectory(directory);
            File.WriteAllLines(Path.Combine(directory, "samples.csv"), rows);
            File.WriteAllLines(Path.Combine(directory, "environment.txt"), new[]
            {
                "OS=" + Environment.OSVersion, "Machine=" + Environment.MachineName, "Processors=" + Environment.ProcessorCount,
                "CLR=" + Environment.Version, "RenderTier=" + (RenderCapability.Tier >> 16), "DPI=" + VisualTreeHelper.GetDpi(window).PixelsPerInchX,
                "Method=Release; 200 password controls; 5 warmups + 30 samples; hidden native Window; Dispatcher Background endpoint; state assertions outside timing.",
                "Limit=Layout/Dispatcher proxy only; not frame/input/GC/heap or physical DPI acceptance; no password text written to artifacts.",
            });
        }
        finally { window.Close(); }
    });

    private static int CountVisuals(DependencyObject element)
    {
        int count = 1;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++) { count += CountVisuals(VisualTreeHelper.GetChild(element, i)); }
        return count;
    }
}
