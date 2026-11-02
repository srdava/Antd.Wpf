using System.Windows;
using System.Windows.Controls;

namespace Antd.Wpf.Controls;

public class AntCheckBox : CheckBox
{
    static AntCheckBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AntCheckBox), new FrameworkPropertyMetadata(typeof(AntCheckBox)));
    }
}
