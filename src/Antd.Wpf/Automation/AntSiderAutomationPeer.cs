using System;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Antd.Wpf.Controls;

namespace Antd.Wpf.Automation;

public sealed class AntSiderAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
{
    public AntSiderAutomationPeer(AntSider owner) : base(owner) { }
    private AntSider Sider => (AntSider)Owner;
    protected override string GetClassNameCore() => nameof(AntSider);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;
    public override object? GetPattern(PatternInterface patternInterface) =>
        patternInterface == PatternInterface.ExpandCollapse && Sider.IsCollapsible ? this : base.GetPattern(patternInterface);
    public ExpandCollapseState ExpandCollapseState => Sider.IsCollapsed ? ExpandCollapseState.Collapsed : ExpandCollapseState.Expanded;
    public void Expand() => SetCollapsed(false);
    public void Collapse() => SetCollapsed(true);

    internal void NotifyStateChanged(bool previous, bool current) => RaisePropertyChangedEvent(
        ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
        previous ? ExpandCollapseState.Collapsed : ExpandCollapseState.Expanded,
        current ? ExpandCollapseState.Collapsed : ExpandCollapseState.Expanded);

    private void SetCollapsed(bool value)
    {
        if (!Sider.IsEnabled) { throw new ElementNotEnabledException(); }
        if (!Sider.IsCollapsible) { throw new InvalidOperationException("AntSider is not collapsible."); }
        Sider.ChangeCollapsed(value, SiderCollapseReason.Automation);
    }
}
