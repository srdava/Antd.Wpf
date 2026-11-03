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
        if (Validation.GetHasError(input) || input.IsOverMaxLength)
        {
            return "Error";
        }

        if (input.Status != ControlStatus.Default)
        {
            return input.Status.ToString();
        }
        return input.IsSearchEnabled && input.IsLoading ? "Loading" : base.GetItemStatusCore();
    }
}
