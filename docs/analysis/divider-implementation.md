# Divider：官方依据与本批 WPF 映射

日期：2026-10-30；组件slug：divider；批次：P1基础布局。版本固定Ant Design6.6.8。

## 官方事实

复用已有[组件说明](official/divider.md)，补充下载固定tag的[style/index.ts](https://github.com/ant-design/ant-design/blob/6.6.8/components/divider/style/index.ts)和[组件实现](https://github.com/ant-design/ant-design/blob/6.6.8/components/divider/index.tsx)。URL与SHA-256独立记录在[divider-manifest.json](official/styles-6.6.8/divider-manifest.json)。

在既有临时官方npm6.6.8环境执行getDesignToken和Divider.prepareComponentToken，得到[divider-tokens.json](official/styles-6.6.8/divider-tokens.json)。可复现脚本为[resolve-divider-tokens.cjs](official/styles-6.6.8/resolve-divider-tokens.cjs)，复制到临时安装antd@6.6.8的目录后用Node执行，输出路径作为首个参数；没有新增产品依赖。

| 项目 | 官方默认值 |
| --- | --- |
| 水平无标题外距 | 上下24；左右0 |
| 水平有标题外距 | 上下16；Small上下8，Middle上下16；Large仍为有标题16/无标题24 |
| 标题字体 | 16、字重500；plain为14、400；继承基础单位行高22/14 |
| 标题内距 | 左右各1em，随有效字号变化 |
| Start/End短线比例 | 0.05；两侧rail在标题占用空间后按5%/95%分配 |
| 竖线 | 高0.9em、左右外距8、上移0.06em；不呈现children标题 |
| 线宽 | 1；solid/dashed/dotted |
| Light colorSplit | **rgba(5,5,5,0.06)**，不是纯黑6% |
| Dark colorSplit | **rgba(253,253,253,0.12)**，不是纯白12% |
| 语义 | role=separator，无自身交互 |

## WPF 实施与边界

- `AntDivider : ContentControl`，保留Content/ContentTemplate及原生绑定。公开Orientation、Variant、TitlePlacement、TitleInset、Size、IsPlain；HasTitle为只读状态，排除null/空字符串及竖向模式。
- Size复用可继承的共用DependencyProperty，但该类型默认Large；未显式设置的作用域保持Divider自己的默认24，作用域显式Size或控件本地Size可覆盖。测试区分了这些路径。
- 默认模板为一个绘制Decorator与一个ContentPresenter；没有每段虚线子元素、Shape组、全局LayoutUpdated、Timer或Effect。短线和长线用缓存Pen直接绘制；DashStyle在类型内冻结复用。
- 数值和颜色跟随官方。横向宽度使用WPF Stretch；外距遵循WPF父容器规则，不模拟CSS margin collapse。默认垂直对齐Center避免横排时被同排较高内容拉伸。
- TitleInset是可空、非负有限DIP值；null保留默认比例。Start/End显式Inset替换对应标题侧的内距，并取消该侧短线；Center忽略Inset。可用宽度不足时先压缩标题内距/Inset，把剩余空间交给标题并裁切溢出，所有线长和Arrange尺寸非负。
- 字号变化同步1em内距、单位行高以及竖线高度/偏移。用户本地Margin/Padding/FontSize/BorderBrush仍按WPF优先级覆盖；竖向标题只隐藏，Content和ContentTemplate保持原值，恢复横向后重新呈现。
- 标题模板内部尺寸可变化；只在线段边界改变时使绘制失效，避免外框RenderSize不变时保留旧间隙。像素回归测试覆盖了同一标题对象变宽的路径。
- 借用调用方的可变Brush时不冻结它；只有Brush本来已冻结时才冻结Pen。颜色/线型只使绘制失效，30次切换的控件Measure次数断言为0。
- AutomationPeer报告Separator，控件本身不进入Tab；竖向不提供被隐藏标题的名称/子树，显式AutomationProperties.Name或LabeledBy仍生效。复杂标题建议显式提供辅助名称；完整屏幕阅读器验收仍待完成。
- 虚线3/3和圆点0/2是WPF笔划映射，浏览器边缘dash分配和字体栅格可能不同，不能视为逐像素一致。测试以384DPI离屏栅格区分三种线型，这不是384DPI显示器验收。

## 验收与性能证据

功能测试位于`tests/Antd.Wpf.Tests/DividerTests.cs`：覆盖三尺寸/有无标题、两方向三线型、LTR/RTL三位置、Inset、长标题和零宽、清空标题、自定义模板、可变画刷、主题/绑定、模板替换与UIA。

`PF02_DividerThemeSwitchKeepsVisualTreeBounded`使用200个Divider、5次预热+30次采样，检查视觉节点不增长及作用域字典数。原始结果写入artifacts/performance/divider，再由Verify归档到独立快照并更新[验证报告](../m0-validation.json)。它是隐藏窗口布局/Dispatcher代理，不是GPU呈现或堆保留验收。

Gallery新增Divider示例窗口及双端PNG，真实NuGet Consumer检查默认BAML内的内部绘制类型和Separator语义。125%当前设备渲染可检查；150%/200%真实显示器、动态DPI切换、屏幕阅读器和长时堆根检查继续待验收，不因离屏位图测试通过而标为完成。
