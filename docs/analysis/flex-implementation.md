# Flex P1 实施映射

来源为官方6.6.8的Flex组件、utils和style源码，URL及SHA256见[flex-manifest](official/styles-6.6.8/flex-manifest.json)。官网Markdown继续复用[既有manifest](official/manifest.json)中的flex条目。以下区分官方行为与本项目契约。

## 官方依据

官方Flex是无包装的flex布局容器，默认横向、不换行，margin/padding为0；不附加排版重置。未设置gap时没有额外间距。显式预设small/medium/large分别使用全局paddingXS/padding/paddingLG；默认token为8/16/24。官方源码还支持换行、分散对齐和CSS flex等能力，这一批没有全部实现。

## 本批公开接口与边界

`AntFlexPanel : Panel`，Orientation默认Horizontal；ColumnGap/RowGap是有限非负DIP，默认0。横向只使用ColumnGap，纵向只使用RowGap。无额外Margin、Padding、字体或颜色资源，直接保留子项官方控件样式。8/16/24间距通过数值传入，不把主题ControlSize隐式变为布局间距。

JustifyContent为Start/Center/End，默认Start；AlignItems为Start/Center/End/Stretch，默认Stretch。Stretch对应跨轴布局槽，不设置子项Width/Height/Alignment，不破坏本地值或绑定；最终渲染仍遵守WPF子项的Min/Max和原生Alignment。需要填满槽的子项应保持原生Stretch；例如AntButton默认VerticalAlignment=Center时不会被强制拉高。

Collapsed不占尺寸和Gap；Hidden保留位置；可见零尺寸项仍参与相邻Gap。RTL由原生FlowDirection镜像，不反转Children集合，也不重写子控件文字方向。子控件保留焦点、Tab、原生AutomationPeer，无额外可聚焦包装。

主轴按自然DesiredSize测量（无限约束），跨轴传递可用空间，不主动Shrink。空/单项/零约束均支持；有限容器不足时从Start溢出，不产生负的居中/末尾偏移；裁剪由宿主ClipToBounds决定。主轴普通文本不会因为面板宽度而自动换行，需要子项显式Width/MaxWidth。Margin计入原生DesiredSize，不由面板再次加算。极大间距求和饱和到double.MaxValue；该边界只作为测量输入健壮性验证，不承诺极大坐标的渲染精度。

P2仍待实现：Wrap、SpaceBetween/Around/Evenly、Grow/Shrink/Basis/AlignSelf；Baseline未定义。这些属性本批不暴露，避免出现设置无行为的API。当前控件不宣称完整CSS Flexbox或数据虚拟化。

## 性能契约与证据

Measure遍历直接孩子一次；Arrange先统计主轴尺寸，再排列一次，均为O(n)。不创建逐项临时集合/包装、不订阅LayoutUpdated/Rendering、不使用定时器或递归扫描。JustifyContent/AlignItems仅AffectsArrange；Gap/Orientation才AffectsMeasure。没有动态生成颜色或主题字典。

`Flex_LayoutVisitsEachChildOnceWithoutAllocatingContainers`用200个测量探针验证首次各Measure/Arrange一次、200个直接视觉节点及Collapsed后的尺寸；测试名称中的containers指额外包装容器，不是GC分配测量。`Flex_AlignmentOnlyChangesAvoidMeasureAndPreserveGapBinding`检查对齐变化不重新测量，并检查Gap绑定更新后实际项间位置。`Flex_ContentAndOrientationChangesRelayoutAndPreserveChildFocus`检查内容宽度和方向改变后位置及焦点。

本批没有新增Flex耗时/分配/GC或完整PF场景，因此时间预算仍待实测；现有PF02/PF05通过不能转移为Flex性能通过。无限约束容器会测量全部直接孩子，不应用于万项数据源。

## 验收映射

| 要求 | 证据 |
| --- | --- |
| 无默认间距/包装/字体改变 | `Flex_DefaultsHaveNoAddedSpacingOrVisualWrappers`；无样式/字体设置的Panel实现 |
| 两方向Gap与主/跨轴对齐 | `Flex_MainAxisAlignmentUsesOnlyRelevantGap`、`Flex_CrossAxisAlignmentControlsSlotWithoutOverwritingChild` |
| Collapsed/Hidden/零尺寸/移除 | `Flex_CollapsedHiddenZeroAndRemovedChildrenHaveDistinctGapBehavior` |
| 无限测量/溢出/MinMax/Margin | `Flex_InfiniteMeasureOverflowAndChildConstraintsAreDeterministic` |
| RTL与保留子项顺序 | `Flex_RtlMirrorsHorizontalOrderWithoutReorderingChildren` |
| 动态内容/方向/焦点/绑定 | `Flex_ContentAndOrientationChangesRelayoutAndPreserveChildFocus`、`Flex_AlignmentOnlyChangesAvoidMeasureAndPreserveGapBinding` |
| 非法值/空面板/极大间距测量 | `Flex_RejectsInvalidGaps`、`Flex_RejectsUnknownEnumsAndHandlesEmptyUnboundedPanel` |
| O(n)遍历/无包装 | `Flex_LayoutVisitsEachChildOnceWithoutAllocatingContainers` |

Gallery提供Flex examples明暗示例，Verify生成flex-matrix-net48/net8两张截图。Consumer使用真实包中的AntFlexPanel验证两个直接子项和28DIP的位置。真实150%/200%DPI、屏幕阅读器和耗时/GC指标仍待验收。
