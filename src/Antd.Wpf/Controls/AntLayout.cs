using System.Windows;
using System.Windows.Controls;

namespace Antd.Wpf.Controls;

/// <summary>A native DockPanel layout. Place the filling content or nested layout last.</summary>
public class AntLayout : DockPanel
{
    static AntLayout() => DefaultStyleKeyProperty.OverrideMetadata(typeof(AntLayout), new FrameworkPropertyMetadata(typeof(AntLayout)));
}

public class AntHeader : ContentControl
{
    static AntHeader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AntHeader), new FrameworkPropertyMetadata(typeof(AntHeader)));
        DockPanel.DockProperty.OverrideMetadata(typeof(AntHeader), new FrameworkPropertyMetadata(Dock.Top));
    }
}

public class AntContent : ContentControl
{
    static AntContent() => DefaultStyleKeyProperty.OverrideMetadata(typeof(AntContent), new FrameworkPropertyMetadata(typeof(AntContent)));
}

public class AntFooter : ContentControl
{
    static AntFooter()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AntFooter), new FrameworkPropertyMetadata(typeof(AntFooter)));
        DockPanel.DockProperty.OverrideMetadata(typeof(AntFooter), new FrameworkPropertyMetadata(Dock.Bottom));
    }
}
