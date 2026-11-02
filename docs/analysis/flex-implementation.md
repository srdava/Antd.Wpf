# Flex P1 与 P2 换行/分散对齐实施映射

来源为官方6.6.8的Flex组件、utils和style源码，URL及SHA256见[flex-manifest](official/styles-6.6.8/flex-manifest.json)。官网Markdown继续复用[既有manifest](official/manifest.json)中的flex条目。以下区分官方行为与本项目契约。

## 官方依据

官方Flex是无包装的flex布局容器，默认横向、不换行，margin/padding为0；不附加排版重置。未设置gap时没有额外间距。显式预设small/medium/large分别使用全局paddingXS/padding/paddingLG；默认token为8/16/24。官方源码还支持换行、分散对齐和CSS flex等能力，这一批没有全部实现。

## 本批公开接口与边界

`AntFlexPanel : Panel`，Orientation默认Horizontal，Wrap默认false；ColumnGap/RowGap是有限非负DIP，默认0。横向主轴使用ColumnGap、行间使用RowGap；纵向主轴使用RowGap、列间使用ColumnGap。无额外Margin、Padding、字体或颜色资源，直接保留子项官方控件样式。8/16/24间距通过数值传入，不把主题ControlSize隐式变为布局间距。

JustifyContent为Start/Center/End/SpaceBetween/SpaceAround/SpaceEvenly，默认Start。每行独立扣除自然尺寸及Gap，再分配非负剩余空间；Between单项从Start排列，Around/Evenly单项居中，溢出时从Start排列并保留Gap。既有枚举值0–2保留。

AlignItems为Start/Center/End/Stretch，默认Stretch。Stretch对应跨轴布局槽，不设置子项Width/Height/Alignment，不破坏本地值或绑定；最终渲染仍遵守WPF子项的Min/Max和原生Alignment。需要填满槽的子项应保持原生Stretch；例如AntButton默认VerticalAlignment=Center时不会被强制拉高。换行跨轴的剩余空间均分给各行槽，映射CSS默认align-content:normal/stretch。

Wrap=true时主轴累计尺寸加相邻Gap严格大于有限约束才换行，恰好相等仍同行；行首不增加主轴Gap，交叉轴Gap只在行间出现。横向和纵向使用相同算法。无限主轴约束退化为单行，首项超宽或零约束仍保留其自然大小，避免空首行。WrapReverse及自定义AlignContent尚未开放。

Collapsed不占尺寸和Gap；Hidden保留位置；可见零尺寸项仍参与相邻Gap。RTL由原生FlowDirection镜像，不反转Children集合，也不重写子控件文字方向。子控件保留焦点、Tab、原生AutomationPeer，无额外可聚焦包装。

主轴按自然DesiredSize测量（无限约束），跨轴传递可用空间，不主动Shrink。空/单项/零约束均支持；有限容器不足时从Start溢出，不产生负的居中/末尾偏移；裁剪由宿主ClipToBounds决定。主轴普通文本不会因为面板宽度而自动换行，需要子项显式Width/MaxWidth。Margin计入原生DesiredSize，不由面板再次加算。极大间距求和饱和到double.MaxValue；该边界只作为测量输入健壮性验证，不承诺极大坐标的渲染精度。

P2仍待实现Grow/Shrink/Basis/AlignSelf；Baseline未定义。这些属性本批不暴露。当前控件不宣称完整CSS Flexbox或数据虚拟化。

## 性能契约与证据

Measure对每个未Collapsed孩子调用Measure一次，再线性分行；Arrange根据最终主轴约束线性分行、累计交叉轴尺寸并排列，均为O(n)。行记录使用实例拥有的List和readonly struct，复用Clear后的容量；每行只保存索引、数量和尺寸，容量按历史最大行数增长，不持有孩子/Owner引用。不创建逐项包装、不订阅LayoutUpdated/Rendering、不使用定时器或递归扫描。JustifyContent/AlignItems仅AffectsArrange；Gap/Orientation/Wrap才AffectsMeasure。没有动态生成颜色或主题字典。

`Flex_LayoutVisitsEachChildOnceWithoutAllocatingContainers`用200个测量探针验证首次各Measure/Arrange一次、200个直接视觉节点及Collapsed后的尺寸；测试名称中的containers指额外包装容器，不是GC分配测量。`Flex_AlignmentOnlyChangesAvoidMeasureAndPreserveGapBinding`检查对齐变化不重新测量，并检查Gap绑定更新后实际项间位置。`Flex_ContentAndOrientationChangesRelayoutAndPreserveChildFocus`检查内容宽度和方向改变后位置及焦点。

本批没有新增Flex耗时/分配/GC或完整PF场景，因此时间预算仍待实测；现有PF02/PF05通过不能转移为Flex性能通过。无限约束容器会测量全部直接孩子，不应用于万项数据源。

`Flex_WrapKeepsLinearChildVisitsDuringRepeatedAlignment`以200个直接探针、40行和30次分散对齐切换，断言每项只Measure一次、没有包装、末行位置正确。此场景证明访问/节点约束，未计量GC分配。既有PF02/PF05只继续作为共享主题及下拉回归。

analyzing-dotnet-performance静态检查覆盖本文件188行及同目录Space/Row的行缓存模式。执行清单中IndexOf/Substring/StartsWith/EndsWith/Contains、LINQ、方法内List/Dictionary创建、静态Dictionary/FrozenDictionary、CurrentCulture比较器、Regex、params命中均0；实例复用List命中1，未sealed公开类1/1。Panel是公开扩展点，保持可派生；Space和Row同样复用实例List，无需统一替换。严重/中等/信息级待修问题均0。静态检查及次数断言不能替代真实帧率、GC与硬件验收。

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
| 两方向换行/精确边界/双轴Gap | `Flex_WrapUsesBothGapsAndExactBoundaryInEitherDirection` |
| 六种主轴对齐/单项/溢出 | `Flex_DistributedJustificationIncludesGapAndHandlesSingleItem`，横纵两向均覆盖新增三种对齐 |
| 每行独立分布与跨轴槽拉伸 | `Flex_WrappedLinesJustifyIndependentlyAndStretchCrossAxisSlots` |
| 换行Hidden/Collapsed/零尺寸/超宽/无限约束 | `Flex_WrapPreservesHiddenZeroOversizeAndUnboundedBehavior` |
| 换行缩放/内容尺寸变化/RTL/绑定/焦点 | `Flex_WrapResizeRtlAndBindingKeepChildrenAndFocus` |
| 200项换行与30次对齐成本 | `Flex_WrapKeepsLinearChildVisitsDuringRepeatedAlignment` |

Gallery提供Flex examples明暗示例，包括换行和三种分散对齐，Verify生成flex-matrix-net48/net8两张完整内容截图。Consumer使用真实包中的AntFlexPanel验证两个直接子项和28DIP的位置，再改变Wrap/宽度验证第二行位置16以及SpaceEvenly位置14/56。真实150%/200%DPI、屏幕阅读器和耗时/GC指标仍待验收。

## 本批验证状态

2026-11-02，net8定向`FlexTests`为38/38；整套测试拆成普通210项和性能观察/Popup对照5项运行，TRX去重后215项全部通过。命令入口为`dotnet test tests/Antd.Wpf.Tests/Antd.Wpf.Tests.csproj -c Release -f net8.0-windows`，前者筛选`FullyQualifiedName!~PerformanceScenarios&FullyQualifiedName!~PopupComparison`，后者筛选`FullyQualifiedName~PerformanceScenarios|FullyQualifiedName~PopupComparison`。报告分别位于`artifacts/test-results/flex-full-net8.trx`与`flex-observations-net8.trx`。

独立打包至`artifacts/flex-packages/`，以该目录和nuget.org作为来源、隔离缓存并仅构建net8 Consumer，零警告/零错误；真实包消费通过，包含新增换行和分散对齐位置检查。net8明暗Flex截图已重新生成并目视核对，底部示例可滚动到达。

完整Verify当前未通过。net48库可编译，但Gallery EXE的PE CLR目录为0，版本信息为`Synaptics Pointing Device Driver`，测试项目引用它时报CS0009。失败文件SHA256为`FC8BDE6391D73D891ABE4B1B4FA31FC066C27CDBEAC8F4F7CC146ABD879A96E3`。用户确认本机有会处理EXE的工具并正在检查。本批net48测试、Framework包消费及net48截图待该问题解除后重跑；未运行这份异常EXE。`docs/m0-validation.json`保留15:09的上次完整通过记录，不能作为本批通过证明。
