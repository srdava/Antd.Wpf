using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Antd.Wpf.Gallery;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        long startupTimestamp = Stopwatch.GetTimestamp();
        if (e.Args.Length > 0 && e.Args[0] == "--measure-startup")
        {
            StartupProbe.Run(this, e.Args, startupTimestamp);
        }
        else
        {
            StartGallery(e);
        }
        base.OnStartup(e);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void StartGallery(StartupEventArgs e)
    {
        if (e.Args.Length == 2 && (e.Args[0] == "--capture" || e.Args[0] == "--capture-matrix" || e.Args[0] == "--capture-divider" || e.Args[0] == "--capture-flex" || e.Args[0] == "--capture-space" || e.Args[0] == "--capture-grid" || e.Args[0] == "--capture-layout"))
        {
            string matrix = e.Args[0] == "--capture-layout" ? "LayoutMatrix.xaml" : e.Args[0] == "--capture-grid" ? "GridMatrix.xaml" :
                e.Args[0] == "--capture-space" ? "SpaceMatrix.xaml" : e.Args[0] == "--capture-flex" ? "FlexMatrix.xaml" :
                e.Args[0] == "--capture-divider" ? "DividerMatrix.xaml" : "VisualMatrix.xaml";
            Window window = e.Args[0] != "--capture"
                ? (Window)LoadComponent(new Uri("/Antd.Wpf.Gallery;component/" + matrix, UriKind.Relative))
                : new MainWindow();
            window.ShowInTaskbar = false;
            window.ShowActivated = false;
            window.Opacity = 0;
            window.IsHitTestVisible = false;
            MainWindow = window;
            window.Show();
            window.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                try
                {
                    var content = (FrameworkElement)window.Content;
                    if (content is ScrollViewer { Content: FrameworkElement examples }) { content = examples; }
                    DpiScale dpi = VisualTreeHelper.GetDpi(content);
                    var bitmap = new RenderTargetBitmap(
                        (int)Math.Ceiling(content.ActualWidth * dpi.DpiScaleX),
                        (int)Math.Ceiling(content.ActualHeight * dpi.DpiScaleY),
                        dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
                    bitmap.Render(content);
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    string path = Path.GetFullPath(e.Args[1]);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    using (FileStream stream = File.Create(path))
                    {
                        encoder.Save(stream);
                    }

                    window.Close();
                    Shutdown(0);
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine(exception);
                    Shutdown(1);
                }
            }));
        }
        else
        {
            MainWindow = new MainWindow();
            MainWindow.Show();
        }

    }
}
