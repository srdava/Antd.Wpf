using System.Windows;
using System.Windows.Controls;

namespace Antd.Wpf.Controls;

public class AntRadioButton : RadioButton
{
    static AntRadioButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AntRadioButton), new FrameworkPropertyMetadata(typeof(AntRadioButton)));
    }
}
