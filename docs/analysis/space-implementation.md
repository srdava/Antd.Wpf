# Space P1 实施映射

官方基线为Ant Design6.6.8，Markdown复用[原manifest](official/manifest.json)的space条目；组件、Item及style源码URL/hash见[space-manifest](official/styles-6.6.8/space-manifest.json)。本页把官网事实、WPF契约和未完成项分开。

## 官方依据

Space默认horizontal、gap small（8），水平默认align center；vertical未显式设置align时使用浏览器stretch。预设small/medium/large间距引用paddingXS/padding/paddingLG，默认8/16/24；没有额外字体重置。separator是独立呈现元素，相邻元素之间仍应用gap。官方React版为每个child添加包装；WPF按本项目性能契约直接布局孩子。

## 当前接口与WPF映射

`AntSpace : Panel`，默认Gap=8 DIP、Orientation=Horizontal、Wrap=false、Alignment=Auto。RowGap/ColumnGap是可空DIP，null回退到Gap；均拒绝负数、NaN和无穷。显式8/16/24对应官方默认预设，不把主题ControlSize误当布局间距。Panel不添加Margin/Padding或字体/颜色，子控件原样保留。

Alignment为Auto/Start/Center/End。Auto横向居中，纵向跨轴槽拉伸；子项本地Width/Height/Min/Max/Alignment仍按原生WPF规则约束最终绘制，不被内部SetValue覆盖。横向Wrap根据实际可用宽度换行，列Gap和行Gap独立；等于边界时不换行。纵向忽略Wrap。普通子项按主轴无限、跨轴可用尺寸测量，超宽项独占一行并从起始位置溢出；裁剪由宿主ClipToBounds决定。不承诺Flexbox Shrink、Baseline或数据虚拟化。

Collapsed不占Gap或分隔符，Hidden保留空间，可见零尺寸项仍参与间距；集合移除后按剩余可见顺序重新布局。水平行按自然高度从顶端排列，不额外平分容器多余高度。RTL由原生FlowDirection镜像，Children顺序和键盘焦点保留。

SeparatorTemplate是装饰性DataTemplate，每对相邻非Collapsed子项最多一个ContentPresenter，Content绑定Space.DataContext，资源从当前逻辑/视觉父节点继承。子项本身无包装。分隔符前后各有一个Gap；模板中的Margin照常计入DesiredSize，不去重。分隔符不接收鼠标，不进入Tab导航；需要交互的内容应是正式子项。

**明确的WPF边界**：官方CSS允许separator独立换行，本项目把分隔符与相邻项的同一行关联；行首/行尾不显示孤立分隔符（Hidden且零布局槽），宽度增大后恢复。同一组分隔符复用，不随每轮Arrange重建。该差异来自本项目相邻可见项契约，不声称与浏览器所有换行像素一致。

## 生命周期和性能

只维护可复用Line值类型列表及必要分隔符。Measure统计直接子项、同步分隔符数量、测量并分行；Arrange重新分行并遍历各行内不重叠的子项区间，均为O(n)。没有每个子项的额外ContentControl、全局Rendering/LayoutUpdated、计时器、逐帧主题派生或递归视觉扫描。没有分隔符时额外视觉节点为0，有分隔符时最多n−1个presenter。隐藏在换行处的presenter也计入上限。

模板替换立即解除旧Content绑定及逻辑/视觉父引用；可见项减少时移除多余presenter。资源/内容变化正常触发布局，Alignment仅AffectsArrange。200孩子、199分隔符、30次对齐变化验证节点和实例稳定、子项不重新测量。数量/遍历证据不代表堆分配、帧率或耗时预算；Space专用PF耗时/GC预算尚未采集。无限容器会测量所有直接子项，不适用于万项数据源。

## 验收映射

| 要求 | 证据 |
| --- | --- |
| 默认Gap8、零Margin、无子项包装、水平居中 | `Space_DefaultGapAndHorizontalCenterHaveNoWrappers` |
| 两方向显式对齐和原生尺寸 | `Space_ExplicitCrossAlignmentPreservesNativeChildSize` |
| 纵向默认拉伸、忽略Wrap | `Space_VerticalAutoStretchesSlotsAndIgnoresWrap` |
| 换行边界、列/行间距 | `Space_WrapHonorsExactBoundaryAndIndependentRowGap` |
| Collapsed/Hidden/零尺寸/移除/空集合 | `Space_CollapsedHiddenRemovalAndZeroSizeKeepVisibleAdjacency` |
| 分隔符间距、行首隐藏及恢复、导航 | `Space_SeparatorsHaveTwoGapsAndDisappearAtWrapBoundary` |
| 替换/清空模板解绑旧节点 | `Space_SeparatorTemplateReplacementDetachesOldVisualAndLogicalChildren` |
| DataContext/主题/焦点/原控件保留 | `Space_SeparatorContextAndThemeUpdateWithoutReplacingContent` |
| Gap绑定与回退、内容/方向更新 | `Space_GapFallbackBindingAndContentChangesUpdatePositions` |
| RTL换行且集合不反转 | `Space_RtlWrapMirrorsRowsWithoutReorderingChildren` |
| 无限约束/超宽/零高多行/空面板 | `Space_UnboundedOversizedZeroAndEmptyLayoutsRemainDeterministic` |
| 节点/实例有界、仅排列失效 | `Space_StableLayoutReusesBoundedSeparatorsAndAlignmentAvoidsMeasure` |
| 非法参数及Gap0 | `Space_InvalidGapInputsAreRejected`、`Space_UnknownEnumsAreRejectedAndZeroGapsAreAllowed` |

Gallery有Space examples及双目标明暗矩阵；Verify生成space-matrix-net48/net8，真实包Consumer检查两个子项+一个分隔符及第二项X=38。Compact的边框合并/首尾圆角/错误与焦点层级、Addon属于独立P2能力，本批没有假装用Gap0实现。Baseline、完整DPI/读屏、时延和堆根验收仍待完成。
