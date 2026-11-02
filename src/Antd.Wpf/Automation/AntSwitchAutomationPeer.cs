using System.Windows.Automation.Peers;
using Antd.Wpf.Controls;

namespace Antd.Wpf.Automation;

public sealed class AntSwitchAutomationPeer : ToggleButtonAutomationPeer
{
    public AntSwitchAutomationPeer(AntSwitch owner) : base(owner)
    {
    }

    protected override string GetItemStatusCore() => ((AntSwitch)Owner).IsLoading ? "Loading" : base.GetItemStatusCore();
}
