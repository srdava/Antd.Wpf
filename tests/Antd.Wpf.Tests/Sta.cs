using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Xunit;

namespace Antd.Wpf.Tests;

internal static class Sta
{
    public static void Run(Action action)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
            finally
            {
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        })
        {
            IsBackground = true,
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "STA scenario timed out.");
        failure?.Throw();
    }

    public static Window Show(FrameworkElement content)
    {
        var window = new Window
        {
            Content = content,
            Width = 600,
            Height = 400,
            ShowInTaskbar = false,
            ShowActivated = false,
            Opacity = 0,
            IsHitTestVisible = false,
        };
        window.Show();
        Pump();
        return window;
    }

    public static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    public static void WaitUntil(Func<bool> condition)
    {
        var timeout = Stopwatch.StartNew();
        while (!condition() && timeout.Elapsed < TimeSpan.FromSeconds(3))
        {
            Pump();
            Thread.Sleep(5);
        }

        Assert.True(condition(), "Expected state did not arrive within three seconds.");
    }
}
