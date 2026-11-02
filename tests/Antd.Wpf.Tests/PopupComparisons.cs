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
public sealed class PopupComparisons
{
    [Theory]
    [InlineData("Native")]
    [InlineData("AntUnscoped")]
    [InlineData("AntScoped")]
    public void Select_ComparePopupSetterAndDispatcherLatency(string scenario) => Sta.Run(() =>
    {
        ComboBox select = scenario == "Native" ? new ComboBox() : new AntSelect();
        select.Width = 240;
        select.VerticalAlignment = VerticalAlignment.Top;
        select.MaxDropDownHeight = 256;
        select.ItemsSource = Enumerable.Range(0, 10000).Select(index => "Item " + index).ToArray();
        select.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel)));
        VirtualizingPanel.SetIsVirtualizing(select, true);
        VirtualizingPanel.SetVirtualizationMode(select, VirtualizationMode.Recycling);
        ScrollViewer.SetCanContentScroll(select, true);
        FrameworkElement content = scenario == "AntScoped" ? new AntThemeScope { Content = select } : select;
        Window window = Sta.Show(content);
        var rows = new List<string> { "sample,setterMilliseconds,dispatcherMilliseconds,realizedContainers" };
        try
        {
            for (int index = -5; index < 30; index++)
            {
                var watch = Stopwatch.StartNew();
                select.IsDropDownOpen = true;
                double setter = watch.Elapsed.TotalMilliseconds;
                Sta.Pump();
                watch.Stop();
                int realized = Enumerable.Range(0, 10000).Count(item => select.ItemContainerGenerator.ContainerFromIndex(item) != null);
                Assert.InRange(realized, 1, 45);
                if (index >= 0)
                {
                    rows.Add(index + "," + setter.ToString("F3", CultureInfo.InvariantCulture) + "," +
                        (watch.Elapsed.TotalMilliseconds - setter).ToString("F3", CultureInfo.InvariantCulture) + "," + realized);
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
        string directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts/performance/popup-comparison", target));
        Directory.CreateDirectory(directory);
        File.WriteAllLines(Path.Combine(directory, scenario + ".csv"), rows);
    });
}
