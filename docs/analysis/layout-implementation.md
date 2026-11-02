# Layout / Sider P1 实施映射

官方基线Ant Design6.6.8。文档复用[既有manifest](official/manifest.json)的layout条目；Layout/Sider/style源码、官方算法实际token输出、图标与许可快照的来源/hash见[layout-manifest](official/styles-6.6.8/layout-manifest.json)。脚本只使用既有临时npm环境，不增加产品依赖。

## 官方数值与呈现

官方Layout.prepareComponentToken实测：Header高64、水平padding50，Footer垂直24/水平50；Sider默认200、collapsedWidth80、theme dark、collapsible false；普通触发器48高，零宽触发器40×40、距Sider顶64、外侧圆角8。Header背景及暗Sider为#001529，暗触发器#002140、文字纯白；Light作用域的浅Sider/触发器白色、文字黑88%。布局背景Light #f5f5f5 / Dark #000000。

HeaderColor来自colorText，Light黑88%、Dark白85%，没有替换为推测的固定白色；Gallery为可读演示显式设Header.Foreground=White。区域默认无额外圆角。Zero trigger的Hover白20%遮层、浅色三侧1DIP边框及左右圆角按官方源码映射。WPF额外提供原生键盘焦点环和禁用文字语义。

LeftOutlined/BarsOutlined路径来自官方@ant-design/icons-svg4.6.0，保留原viewBox，以共享冻结Geometry绘制；默认箭头14、Bars20 DIP。图标来源和MIT许可随THIRD-PARTY-NOTICES.txt打入包。默认None.Update虽已产生Pack元数据，但本机首轮实际包仍遗漏；现按仓库README的None.Remove/Include模式明确打包条目，并以包资产与内容hash检查为准。未增加运行时包。

## 区域与布局契约

`AntLayout : DockPanel`，默认LastChildFill=true。`AntHeader/AntContent/AntFooter : ContentControl`保留Content、ContentTemplate、Padding、Width/Height等原生属性。Header默认Dock.Top，Footer默认Dock.Bottom；填充Content或嵌套AntLayout放在最后。Header/Footer先于Sider可横跨整体；先放Sider再嵌套布局则侧栏占满高度。按原生DockPanel顺序组合，不引入重复的区域存储/包装。

Header采用MinHeight64和内容居中，代替网页强制height/line-height64，使放大字体和自定义内容能撑高；Footer默认14字号、22行框，padding后普通单行高度70；字体放大时行框按22/14比例变化。布局不自建ScrollViewer；只有调用方在Content中放入滚动容器时正文滚动，头尾/侧栏保持所在容器位置，避免隐式破坏数据虚拟化。

## Sider 状态与绑定

`AntSider : AntThemeScope`复用现有作用域资源、明暗缓存及Loaded/Unloaded系统主题订阅。Mode默认Dark，由默认元数据/主题样式提供，调用方Style、本地值或Binding仍可覆盖；不在构造函数写不可覆盖的LocalValue。Mode是独立WPF明暗作用域，不声称已覆盖浏览器全局算法与Sider theme的全部交叉组合。

展开Width默认200；IsCollapsed默认false且默认TwoWay。折叠时通过Width的CoerceValue呈现CollapsedWidth，保留原始Width绑定/动态资源及其最新基础值；展开恢复该值，包括原生Auto(NaN)。CollapsedWidth拒绝负数、NaN和无穷；原生MinWidth/MaxWidth仍由调用方拥有，显式限制可能改变最终实际宽度。

IsCollapsible控制用户触发器/CollapseCommand及UIA操作，默认false；程序仍可设置IsCollapsed。CollapseCommand是实例ICommand，执行时用SetCurrentValue保留绑定，受IsEnabled/IsCollapsible约束。CollapsedChanged携带IsCollapsed和Programmatic/Trigger/Automation原因；重入事件在进入外部处理器前清除当前原因，不把下一次程序变更误标为Trigger。响应式原因没有虚构，Breakpoint待P2。

默认侧栏使用Dock.Left；右侧使用原生DockPanel.Dock=Right，并保留原生FlowDirection镜像。没有同时维护第二套Placement属性。

## 零宽触发器、焦点与可访问性

与官网一致，CollapsedWidth=0时展开/折叠两态都使用边缘40×40按钮，不预留底部48。展开时按钮位于Sider外侧边缘，折叠后位于内容边缘；普通触发器仅用于非零CollapsedWidth。按钮相对所在布局容器定位，不固定到顶级Window或全局屏幕；非常矮的容器仍可能让64DIP定位的按钮超出可用区域，未实现响应式边缘修正。

模板使用不贡献DesiredSize的Canvas承载边缘按钮，Sider默认ZIndex1，避免后面的Content覆盖入口；不创建Popup/全局Adorner。零宽折叠的Body为Collapsed，不测量内容，但不释放Content及其绑定。折叠时或CollapsedWidth从80变0时，若焦点在Sider内则移到有效的零宽按钮；从0改回非零且零宽按钮有焦点时移到普通按钮。UsesZeroWidthTrigger和IsZeroWidthCollapsed为真实模板只读状态，先同步触发器可见性再转移焦点，无延迟Dispatcher焦点任务。

UIA为Pane；可折叠时提供ExpandCollapsePattern，状态变更发出属性通知，重复Expand/Collapse不额外产生状态事件。禁用时UIA抛ElementNotEnabledException，不可折叠时不提供该Pattern；已取得的provider也拒绝操作。按钮为原生Button，有Tab/Space/Enter路径、明确Expand/Collapse名称、焦点环和禁用状态。

PART_Trigger与PART_ZeroTrigger是默认模板契约。TriggerTemplate为装饰内容模板，DataContext为Sider自身，可绑定IsCollapsed；null表示默认矢量，不表示隐藏按钮。重新模板化解除旧按钮的库命令绑定并清理缓存；缺少部件不崩溃，仍能由公开状态/命令/UIA控制，但自定义模板需要提供零宽入口才能保留键盘操作，缺失时发Trace诊断。

## 性能与边界

布局复用原生DockPanel/Grid，不递归扫描整个视觉树；折叠即时推挤正文，没有逐帧Width动画、LayoutUpdated/Rendering或额外定时器/窗口监听。默认Body及模板内容按需展开；稳定节点检查先预热展开/零宽两态，再做30次折叠/主题切换，确认Content、按钮实例、节点数和作用域字典数保持稳定。纯Background/Foreground变化30次不重新测量Sider或正文探针。

这些是布局访问、失效和资源有界证据，不是独立Layout耗时、GC、堆根或完整PF验收。响应式折叠、遮罩展开、过渡动画、完整文化/读屏/物理键盘/多DPI验收仍待完成；默认触发器英文名称可由自定义模板覆盖。没有声称全部CSS固定定位、字体栅格和动画一致。

## 验收映射

| 要求 | 证据 |
| --- | --- |
| Header/Footer数值及正文滚动边界 | `Layout_OfficialRegionsKeepHeaderFooterAndScrollingIndependent` |
| 嵌套、局部样式和字体放大 | `Layout_NestingAndLocalSpacingSurviveThemeAndFontChanges` |
| 展开Width绑定/动态资源/Auto恢复 | `Sider_WidthBindingAndDynamicResourcesRestoreExpandedBaseValue` |
| 左右/RTL、零宽两态按钮位置、命中及焦点 | `Sider_ZeroWidthHasVisibleHittableKeyboardTrigger`，4种组合 |
| 初始零宽不测量且保留正文 | `Sider_InitiallyZeroCollapsedDoesNotMeasureOrReplaceBody` |
| 命令、TwoWay、事件原因、UIA和禁用 | `Sider_CommandsBindingsReasonsAndAutomationHaveDistinctTransitions` |
| 重入原因不串行 | `Sider_ReentrantEventDoesNotReuseTriggerReason` |
| 独立主题、用户Style、两窗口和局部覆盖 | `Sider_IndependentThemesAndTwoWindowsPreserveLocalOverrides` |
| 重模板化清理、缺部件降级、自定义内容绑定 | `Sider_RetemplateDetachesOldCommandAndSupportsCustomTriggerTemplate` |
| 高对比度字典 | `Layout_HighContrastResourcesUseSystemColors`，不等同OS实际切换验收 |
| 动态CollapsedWidth绑定及焦点切换 | `Sider_CollapsedWidthBindingCanMoveFocusedBodyToZeroTrigger` |
| 纯画刷变化无Measure | `Sider_PaintOverridesDoNotRemeasureLayoutOrBody` |
| 稳态节点/字典/内容复用 | `Sider_RepeatedCollapseKeepsContentTemplatesAndResourcesBounded` |
| 非法CollapsedWidth | `Sider_InvalidCollapsedWidthsAreRejected` |

Gallery增加Layout examples及双目标明暗矩阵；Verify新增layout-matrix-net48/net8，实际包Consumer检查区域BAML、200/80/0宽度、边缘按钮和UIA展开后的输入保留，同时检查许可资产存在。
