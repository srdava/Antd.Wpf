using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Antd.Wpf.Controls;

namespace Antd.Wpf.Automation;

public sealed class AntInputAutomationPeer : TextBoxAutomationPeer
{
    public AntInputAutomationPeer(AntInput owner) : base(owner)
    {
    }

    protected override string GetItemStatusCore()
    {
        var input = (AntInput)Owner;
        if (Validation.GetHasError(input))
        {
            return "Error";
        }

        return input.Status == ControlStatus.Default ? base.GetItemStatusCore() : input.Status.ToString();
    }
}
