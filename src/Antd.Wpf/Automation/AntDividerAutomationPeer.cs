using System.Collections.Generic;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using Antd.Wpf.Controls;

namespace Antd.Wpf.Automation;

public sealed class AntDividerAutomationPeer : FrameworkElementAutomationPeer
{
    public AntDividerAutomationPeer(AntDivider owner) : base(owner) { }
    protected override string GetClassNameCore() => nameof(AntDivider);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Separator;
    protected override string GetNameCore()
    {
        return ((AntDivider)Owner).HasTitle || !string.IsNullOrEmpty(AutomationProperties.GetName(Owner)) || AutomationProperties.GetLabeledBy(Owner) != null
            ? base.GetNameCore() : string.Empty;
    }

    protected override List<AutomationPeer>? GetChildrenCore() => ((AntDivider)Owner).HasTitle ? base.GetChildrenCore() : null;
}
