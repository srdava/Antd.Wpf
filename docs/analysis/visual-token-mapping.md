# Ant Design 6.6.8 视觉 token 与 WPF 映射

核对日期：2026-10-30。此文件记录官方事实、可重复计算结果与本项目的映射决定，不是全部组件已经视觉验收的声明。

## 证据与复现

- 官方源码版本固定为 [ant-design/ant-design 6.6.8](https://github.com/ant-design/ant-design/tree/6.6.8)。已下载样式、主题种子、派生算法和别名源码，URL、字节数、SHA-256 见 [styles manifest](official/styles-6.6.8/manifest.json)。原有组件说明清单 [manifest.json](official/manifest.json) 继续保留，未替换其来源或 hash。
- 在操作系统临时目录安装官方 npm `antd@6.6.8`，使用 `--ignore-scripts`，直接执行其 `theme.getDesignToken` 的 default/dark 算法；没有给产品添加 npm 或 React 依赖。输出见 [resolved-alias-tokens.json](official/styles-6.6.8/resolved-alias-tokens.json)，复现程序见 [resolve-alias-tokens.cjs](official/styles-6.6.8/resolve-alias-tokens.cjs)。复现时将脚本复制到临时安装目录，再执行 `node resolve-alias-tokens.cjs <输出JSON绝对路径>`，使其加载同目录的官方包。npm 元数据、tarball URL、integrity 见 manifest。
- `components/style/index.ts` 返回 404；实际入口是已下载的 `components/style/index.tsx`。manifest 保留失败条目，不把失败下载当作源码证据。
- 官方默认配置为 `wireframe=false`、字号14、边框1、基础圆角6、基础控件高度32，没有组件级 token 覆盖。自定义主题另行验收。

## 通用几何与外间距：官方事实

默认 `fontSize=14`、`lineHeight=22/14`，因此文本行框为22；`fontSizeLG=16`、`lineHeightLG=1.5`，行框为24；`fontSizeSM=12`、`lineHeightSM=20/12`，行框为20。`fontWeightStrong=600`，普通文字400。

高度 SM/默认/LG 为24/32/40，圆角为4/6/8。来源：[seed.ts](https://github.com/ant-design/ant-design/blob/6.6.8/components/theme/themes/seed.ts)、[genControlHeight.ts](https://github.com/ant-design/ant-design/blob/6.6.8/components/theme/themes/shared/genControlHeight.ts)、[genRadius.ts](https://github.com/ant-design/ant-design/blob/6.6.8/components/theme/themes/shared/genRadius.ts)、[genFontSizes.ts](https://github.com/ant-design/ant-design/blob/6.6.8/components/theme/themes/shared/genFontSizes.ts)。

`resetComponent` 的默认 `boxSizing=border-box`、`margin=0`、`padding=0`；组件在自身样式中再覆盖。参见 [style/index.tsx:12](https://github.com/ant-design/ant-design/blob/6.6.8/components/style/index.tsx#L12)。因此不能把 Gallery 卡片的 Margin 12/20/24 作为控件默认外间距。

| 组件/尺寸 | border-box 高度 | 字号/行框 | 内容左右 padding | 内容上下 padding | 圆角 | 说明 |
| --- | ---: | --- | ---: | ---: | ---: | --- |
| Button Small | 24 | 14 / 未显式设置 | 7 | 0 | 4 | 同默认字号 |
| Button Middle | 32 | 14 / 未显式设置 | 15 | 0 | 6 | 图标/文字 gap8 |
| Button Large | 40 | 16 / 未显式设置 | 15 | 0 | 8 | 固定高度内 inline-flex 居中 |
| Input Small | 24 | 14 / 22 | 7 | 0 | 4 | 普通单行输入 |
| Input Middle | 32 | 14 / 22 | 11 | 4 | 6 | 前后缀 gap4 |
| Input Large | 40 | 16 / 24 | 11 | 7 | 8 | 不是字号14 + padding8 |
| Select Small | 24 | 14 / 22 | 7 | 0 | 4 | 原生单选外框 |
| Select Middle | 32 | 14 / 22 | 11 | 4 | 6 | 箭头占位另行计算 |
| Select Large | 40 | 16 / 24 | 11 | 7 | 8 | 与输入基础行框一致 |

Button 的 `prepareComponentToken` 确实算出 `paddingBlock=4`、`paddingBlockSM=0`、`paddingBlockLG=7`，但 **prepareToken 统一设置 `buttonPaddingVertical=0`，最终三种尺寸实际都使用0**。最终组件样式也没有显式赋 `lineHeight`，不能仅凭未使用的 `contentLineHeight` token 声称浏览器实际 computed lineHeight 必然22/24；字体和宿主全局CSS仍有影响。WPF选择22/24作为相应字号的文本行框属于实施映射。依据：[Button token:287](https://github.com/ant-design/ant-design/blob/6.6.8/components/button/style/token.ts#L287)、[Button token:299](https://github.com/ant-design/ant-design/blob/6.6.8/components/button/style/token.ts#L299)、[Button size styles:159](https://github.com/ant-design/ant-design/blob/6.6.8/components/button/style/index.ts#L159)。实施时必须同时读 token 和最终样式，不只抄中间计算。

Input 横向/纵向数值包含对边框的扣除；输入行框仍由字号和 lineHeight 决定。依据：[Input token:106](https://github.com/ant-design/ant-design/blob/6.6.8/components/input/style/token.ts#L106)、[Input final style:43](https://github.com/ant-design/ant-design/blob/6.6.8/components/input/style/index.ts#L43)。Select 外框由 CSS 变量计算相同的单行 padding，依据：[select-input.ts:118](https://github.com/ant-design/ant-design/blob/6.6.8/components/select/style/select-input.ts#L118)。

WPF单行Input的实施映射：外框高度、字号、Padding继续使用上表；`AcceptsReturn=False`时使用原生字体行框（LineHeight=Auto），避免WPF固定行高产生不对称leading，使编辑文字和Placeholder垂直居中。内部ScrollViewer传递VerticalContentAlignment，保留调用方Top/Center/Bottom选择。多行仍保留22/24的行高。此为WPF排版适配，不是修改官方token。

Button 的所有 variant 均保留 `lineWidth=1`，Solid/Filled/Text/Link 通过透明 border 色隐藏，不是把边框宽度清零。参见 [Button variant:20](https://github.com/ant-design/ant-design/blob/6.6.8/components/button/style/variant.ts#L20)。WPF 去掉 BorderThickness 会改变其内容和外部尺寸关系，必须用对应透明边框代替。

Round/Circle 的 CSS 大圆角被浏览器夹到实际边长的一半；WPF `CornerRadius=999` 不保证与浏览器算法一致。默认高度下 Round 应取12/16/20；Switch 轨道取8/11。内容被放大后的自动增高与自定义圆角需另行验证。

## 选项、Checkbox、Radio：官方事实

| 项目 | 默认值/行为 | 来源 |
| --- | --- | --- |
| Select option | 最小高度32、padding `5px 12px`、字号14、行框22、radius4 | [token:266](https://github.com/ant-design/ant-design/blob/6.6.8/components/select/style/token.ts#L266)、[dropdown:16](https://github.com/ant-design/ant-design/blob/6.6.8/components/select/style/dropdown.ts#L16) |
| Select selected option | 文字仍为 colorText，字重600；背景 colorPrimaryBg；附加勾图标才是 Primary | [dropdown:131](https://github.com/ant-design/ant-design/blob/6.6.8/components/select/style/dropdown.ts#L131) |
| Select popup | padding4、radius8、背景 Elevated、boxShadowSecondary，无默认描边 | [dropdown:45](https://github.com/ant-design/ant-design/blob/6.6.8/components/select/style/dropdown.ts#L45) |
| Checkbox | 正方形16、border1、radius4；文字两侧各 padding8，文字14/22；单项无额外外 Margin | [checkbox:74](https://github.com/ant-design/ant-design/blob/6.6.8/components/checkbox/style/index.ts#L74) |
| Checkbox checked | 背景/边框 Primary，白色2宽勾；勾的未旋转框宽 `16/14*5`、高 `16/14*8` | [checkbox:100](https://github.com/ant-design/ant-design/blob/6.6.8/components/checkbox/style/index.ts#L100) |
| Checkbox indeterminate | 中心 **8×8实心方块** Primary；外框仍Container/Border，不是横线，也不是满蓝底 | [checkbox:192](https://github.com/ant-design/ant-design/blob/6.6.8/components/checkbox/style/index.ts#L192) |
| Checkbox group | flex wrap，columnGap8；不是单项自带 Margin8 | [checkbox:37](https://github.com/ant-design/ant-design/blob/6.6.8/components/checkbox/style/index.ts#L37) |
| Radio | 圆形16、border1；默认 wireframe=false 时内点 **6**、白色，选中底色Primary | [radio:659](https://github.com/ant-design/ant-design/blob/6.6.8/components/radio/style/index.ts#L659) |
| Radio wrapper | 默认行末 Margin8，最后一个为0；文字两侧各 padding8；字号14/22 | [radio:231](https://github.com/ant-design/ant-design/blob/6.6.8/components/radio/style/index.ts#L231)、[radio:359](https://github.com/ant-design/ant-design/blob/6.6.8/components/radio/style/index.ts#L359) |
| Checkbox/Radio hover | 未选边框Primary；已选背景PrimaryHover、边框透明 | [checkbox:143](https://github.com/ant-design/ant-design/blob/6.6.8/components/checkbox/style/index.ts#L143)、[radio:315](https://github.com/ant-design/ant-design/blob/6.6.8/components/radio/style/index.ts#L315) |
| Checkbox/Radio disabled | 背景FillTertiary、边框Border、文字及勾/点/半选块TextDisabled | [checkbox:219](https://github.com/ant-design/ant-design/blob/6.6.8/components/checkbox/style/index.ts#L219)、[radio:342](https://github.com/ant-design/ant-design/blob/6.6.8/components/radio/style/index.ts#L342) |

Radio `dotSize=8` 属于 `wireframe=true` 的另一条路径，不应混入默认6.6.8。基础原生单项保留行末 Margin8、RTL镜像；后续 AntRadioGroup 才负责末项0，单项不能仅靠全局遍历来猜自己是不是组末项。

## Switch：官方事实

来源：[switch/style/index.ts:384](https://github.com/ant-design/ant-design/blob/6.6.8/components/switch/style/index.ts#L384)。

| token/样式 | Default | Small |
| --- | ---: | ---: |
| trackHeight | 22 | 16 |
| trackMinWidth | 44 | 28 |
| trackPadding | 2 | 2 |
| handleSize | 18 | 12 |
| innerMinMargin | 9 | 6 |
| innerMaxMargin | 24 | 18 |
| 文字字号 | 12 | 12 |

Switch 没有官方 Large 尺寸；主题 Large 映射 Default 属于本项目决定，不能声称官方有52×28的Large。轨道无边框，拨钮白色，文字也是白色；不能把文字 Foreground 与拨钮颜色绑定为同一状态画刷来改变暗色拨钮。

未选背景为 `colorTextQuaternary`，hover 为 `colorTextTertiary`；选中背景为 Primary/PrimaryHover；disabled/loading 的整体 opacity 为0.65。原样式按状态和内容宽度计算最小宽度，不固定裁切长文本。`handleShadow = 0 2px 4px 0 rgba(0,35,11,0.2)`。活动按压时把手有拉伸，文本有对应偏移，动画时长0.2s。WPF已实现200ms拨钮RenderTransform位移和CSS ease-in-out曲线，受MotionEnabled控制；颜色/标签过渡、按压拉伸、把手阴影及加载旋转仍待实现。详见[Switch动画实施](switch-motion-implementation.md)。

## 色彩和焦点：官方事实

下表是官方 npm 6.6.8 实际执行输出，完整数值在 JSON，不是根据官网截图取样。

| token | Light | Dark |
| --- | --- | --- |
| Text | rgba(0,0,0,.88) | rgba(255,255,255,.85) |
| Secondary/Tertiary/Quaternary | 黑65% / 45% / 25% | 白65% / 45% / 25% |
| Fill / FillSecondary / FillTertiary / FillQuaternary | 黑15% / 6% / 4% / 2% | 白18% / 12% / 8% / 4% |
| DisabledText | 黑25% | 白25% |
| DisabledBg | FillTertiary | FillTertiary |
| Background / Container / Elevated | #f5f5f5 / #ffffff / #ffffff | #000000 / #141414 / #1f1f1f |
| Border | #d9d9d9 | #424242 |
| Primary / Hover / Active | #1677ff / #4096ff / #0958d9 | #1668dc / #3c89e8 / #1554ad |
| PrimaryBg / PrimaryBgHover | #e6f4ff / #bae0ff | **#15325b / #15417e** |
| Error / Hover / Active | #ff4d4f / #ff7875 / #d9363e | #dc4446 / #e86e6b / #ad393a |
| ErrorBg | #fff2f0 | #2c1618 |
| ErrorBgFilledHover / ErrorBgActive | #ffdfdc / #ffccc7 | #441e1f / #5b2526 |
| Link / LinkHover / LinkActive | #1677ff / #69b1ff / #0958d9 | #1668dc / #15417e / #1554ad |
| controlOutline | rgba(5,145,255,.1) | rgba(23,117,249,.31) |
| PrimaryBorder（可见键盘轮廓） | #91caff | #15325b |

既有 Dark `PrimaryFill=#111a2c` 不是本版本 default algorithm 的 PrimaryBg，不能继续用于对齐6.6.8的选中背景。把半透明填充预先混成 `#f5f5f5` 或 `#262626` 也只在某种底色下近似，换成局部主题背景会偏色。

Focus-visible 轮廓来自 [genFocusOutline](https://github.com/ant-design/ant-design/blob/6.6.8/components/style/index.tsx#L70)：宽3、offset1、颜色PrimaryBorder；Button/Switch/Checkbox/Radio使用该路径。Input outlined 的 focus 使用 Primary 边框 + `0 0 0 2px controlOutline`，不是同一个3宽轮廓；错误/警告使用对应 outline。参见 [Input variants:43](https://github.com/ant-design/ant-design/blob/6.6.8/components/input/style/variants.ts#L43)。

Button 默认阴影为 `0 2px 0 controlTmpOutline`（Light黑2%、Dark白4%）；primary 为 `0 2px 0 controlOutline`，danger 为 `0 2px 0 colorErrorOutline`。均为零模糊。来源：[Button token:324](https://github.com/ant-design/ant-design/blob/6.6.8/components/button/style/token.ts#L324)。Select 弹层为三层 `boxShadowSecondary`，原始 CSS 已保留在 JSON；不能把一个任意 DropShadowEffect 参数叫作完全等价。

Button default-color 的 Solid 在官方 variant API 中为中性色实心，Primary-color Solid 才是蓝色。已直接执行官方 Button `prepareComponentToken` 验证：Light 背景黑、文字白，hover黑75%、active黑95%；Dark 背景白95%、文字黑，hover白100%、active白90%。结果作为 `componentTokens` 保存在 JSON。既有未发布预览中的默认 Solid 蓝色应改为该中性色语义，Gallery 的主按钮显式指定 Primary。

Danger 的 Outlined/Dashed 正常/hover/active 文字和边框分别使用 Error/ErrorHover/ErrorActive，不能复用默认中性色按钮的蓝色交互触发器。Danger Filled 的背景正常/ErrorBg、hover/ErrorBgFilledHover、active/ErrorBgActive，Primary Filled 对应 PrimaryBg/PrimaryBgHover/PrimaryBorder。Default Link 的hover使用 LinkHover，Light 为 #69b1ff、Dark 为 #15417e，并非 PrimaryHover。依据：[Button variant:165](https://github.com/ant-design/ant-design/blob/6.6.8/components/button/style/variant.ts#L165)、[variant:192](https://github.com/ant-design/ant-design/blob/6.6.8/components/button/style/variant.ts#L192)。

## WPF 实施决定与性能约束

以下是本项目设计，不是官网事实；最终执行状态以实施进度和测试为准。

- 尺寸、圆角、字体、行框、gap 作为共用类型化资源，避免每个控件重复派生。默认 Margin 与图库布局 Margin 分开。
- RGBA 画刷使用 RGB `Color` + 精确 `Brush.Opacity`，在 Light/Dark 字典中冻结并复用；ARGB八位 alpha 只能近似0.88、0.65等小数。高对比度仍使用系统画刷，属于明确的可访问性特殊路径。
- 尺寸对齐必须测真实 `ActualHeight`/内容偏移，不能只断言 `MinHeight=32`。WPF字体、系统fallback、文本渲染与浏览器不同，所以数字token一致不构成跨平台逐像素一致证明。
- 勾、半选和内点等状态图形可保留固定几何，用 Opacity 切换，避免选中变化导致 Measure/Arrange；不为静态控件注册逐帧回调或 DispatcherTimer。
- 零模糊 Button 阴影可用一次绘制/额外背景 Border 近似实现，性能上优先于每控件 DropShadowEffect；Switch 模糊影与Popup多层影不能静默省略后声称完全一致。
- Theme/DPI变化才重新求资源或布局；颜色/选中不重新创建模板和整套Brush。实际优化须记录旧/新同场景采样，不能用静态节点数代替耗时证据。

### TextBox 原生 Padding 重复风险

额外核对了 [dotnet/wpf TextBoxBase.cs](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/Primitives/TextBoxBase.cs) 当前 main（2026-10-30下载）：第53行注册 Padding 属性变更回调；第1571行把 TextBox.Padding 同步给 PART_ContentHost ScrollViewer；第2033–2047行以 `ScrollViewer.SetValue(e.Property,value)` 提交。

因此自定义模板同时设置 `Chrome.Padding={TemplateBinding Padding}`，又让原生 ScrollViewer 使用宿主 Padding，会重复计算。WPF映射应让正文 Padding 只生效一次，并单独对齐前后缀和占位文字；Framework4.8与net8的实际结果仍必须通过两端尺寸/编辑测试确认，不能把main源码视为net48运行证据。

## 未验证范围

本次完成源码和token取证，没有操作官网所有交互示例，也没有用浏览器截图与WPF做像素差分。完整动画、复杂组、中文字体基线、100/125/150/200%DPI、屏幕阅读器和硬件帧时间仍需独立验收。对齐结论只能覆盖本批实现且有测试/截图的状态。
