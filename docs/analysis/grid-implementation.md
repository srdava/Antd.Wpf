# Grid P1 实施映射

官方基线Ant Design6.6.8；文档复用[既有manifest](official/manifest.json)中的grid条目，Row/Col/style源码与SHA256见[grid-manifest](official/styles-6.6.8/grid-manifest.json)。本批实现基础24列，不代表响应式Grid已完成。

## 官方事实与数值

官方Row使用flex row wrap，默认justify start、align top、gutter0。列网格总数24；正Span使用对应百分比宽度，Span0为display:none；Col默认min-height:1px。横向gutter在Row两侧产生-g/2的margin，并在Col内侧产生g/2的padding；纵向使用row-gap。本批不添加颜色、圆角或字体重置，Gallery蓝块是示例内容，不是栅格自身默认外观。

## 当前WPF接口

- `AntRow : Panel`，HorizontalGutter/VerticalGutter为有限非负DIP，默认0；Wrap默认true。Justify支持Start/Center/End/SpaceBetween/SpaceAround/SpaceEvenly；Align支持Top/Middle/Bottom/Stretch，默认Top。Row的直接孩子必须是AntCol，误放其他类型会在测量时抛出明确InvalidOperationException。
- `AntCol : Decorator`，使用原生Child承载内容，Span/Offset均为0–24的整数，Offset默认0。**WPF明确默认Span24**；官网未设置Span时并不等于显式24，本项目选择完整一行作为确定的基础配置。Col默认MinHeight1 DIP，可本地覆盖。后续响应式值不在本批暴露。
- Span0通过CoerceValue隐藏为Collapsed，保留Visibility的原始Binding/LocalValue，也不修改源VM；原生Collapsed路径完全跳过孩子测量。恢复正Span后回到原来的Visible/Hidden/Collapsed值，子内容对象保持不变。

## gutter、布局与边界

设Row可用内容宽W、横向gutter为g，每个单位为(W+g)/24。列槽从-g/2开始，宽度为Span×单位；Col内部留g/2两侧空间。因此普通自动尺寸内容的第一条左边缘为0、最后右边缘为W，内部内容间距为g。这与官方负Row margin加Col padding的默认几何对应，但内部实现不SetValue任何公开Margin/Padding/Width属性，用户绑定优先级保留。

Offset占用前方网格单位，连同Span超过24时换行；刚好24保持一行。Wrap=false从起始端继续排列，溢出不隐式压缩。单项Offset+Span本身超过24时独占行并允许溢出；ClipToBounds由宿主决定。gutter大于列宽时内容可用宽钳制0，不承诺极端坐标的可用呈现。精确栅格几何以Col默认自动尺寸和零Margin为前提；调用方显式Width/Margin/MinWidth等仍遵循原生WPF约束，可能有意溢出槽。

行高取该行Col自然DesiredSize最大值，VerticalGutter仅在实际行间增加一次。Row被显式拉高时，剩余高度均分给行槽，Align在行槽内部生效；Stretch只扩展Col槽，子项自己的Height/Alignment不被改写。列宽变化会重新测量可换行文本，行高和后续行位置随之更新。

有限宽度按24列测量；无限宽度先测量各Col自然尺寸，以最大DesiredWidth/Span确定单位宽，再进行有限列宽测量，最多两次直接子项测量，整体仍O(n)。Offset只影响位置，不反向增加无限宽度回退基准。空Row在有限宿主中保持给定宽度，高度0；空Col默认高度1；Span0、Collapsed不占行、Offset或gutter，Hidden仍占空间。

RTL使用原生FlowDirection镜像；Children、逻辑阅读顺序和焦点对象不反转。嵌套AntRow通过Col.Child自然获取内容区域宽度，内外gutter分别由各自Row拥有。Col移到其他容器后清理内部gutter，不改调用方Margin/Width绑定。

## 性能与生命周期

各阶段仅顺序扫描直接列，复用Line值类型列表；每个Col只保留一个原生Child，不加隐藏测量控件、独立资源字典或内部包装层。不订阅窗口SizeChanged、LayoutUpdated、Rendering、计时器或全局事件。Justify/Align仅AffectsArrange；Span/Offset/gutter等确需改变尺寸时才重新测量。没有调整稳定的Flex/Space实现或引入通用布局框架。

200列探针验证有限宽度首次每个孩子Measure一次、Row直接视觉节点200、Col恰好一个原始孩子；30次Justify/Align变化后逐一核对200个孩子及Row本身的Measure计数不增加。源循环为O(n)，但没有独立Grid时间/GC/分配采样，不能声称达到完整PF预算。它不提供数据虚拟化，无限容器会测量全部孩子。

## 验收映射

| 要求 | 证据 |
| --- | --- |
| 8+8+8、12+16、24/25边界、nowrap及Offset | `Grid_SpansOffsetsAndWrapUse24Tracks` |
| 负Row半gutter、列半内距和外边缘 | `Grid_GutterUsesOfficialNegativeRowAndColumnHalfPadding` |
| 六种Justify、多项与单项 | `Grid_JustificationDistributesOnlyFreeTracks` |
| 四种Align、自然高度、额外行高与行间距 | `Grid_AlignUsesNaturalLineHeight` |
| Span0、跳过测量、恢复Visibility/Span绑定 | `Col_ZeroSpanCoercesVisibilityWithoutMeasuringOrLosingBindings` |
| 重归属清理gutter及保留本地绑定 | `Col_ReparentingClearsOnlyOwnedGutter` |
| RTL+Offset+gutter | `Grid_RtlMirrorsOffsetAndGutter` |
| 无限宽、零宽、空行/列及Offset溢出 | `Grid_UnboundedAndZeroLayoutsRemainFinite` |
| 改宽/主题/gutter绑定、输入值与焦点 | `Grid_ResizeAndGutterBindingsPreserveContentAndFocus` |
| 文本换行后后续行重新定位 | `Grid_TextWrappingRecomputesLineHeightsAfterResize` |
| 200列节点/测量有界 | `Grid_LinearLayoutKeepsDirectColumnsAndAvoidsRemeasure` |
| 非法Span/Offset/gutter/枚举及非列孩子 | `Col_InvalidTrackCountsAreRejected`、`Grid_InvalidGuttersAreRejected`、`Grid_InvalidParametersAndNonColumnChildrenAreRejected` |

Gallery提供Grid examples双端明暗矩阵，含嵌套示例。Verify新增grid-matrix-net48/net8两张截图；真实包Consumer检查gutter几何和Span0恢复。P2响应式断点/CurrentBreakpoint、Order、Push/Pull、Grow/Basis尚未实现；本批没有注册空属性。真实多DPI/读屏、耗时/GC及发布门槛仍待验收。
