# 实施状态：M0 基础与 P1 原生选择/基础布局/Layout切片

日期：2026-11-03。这是实际代码状态，区别于组件规格和早期静态分析。产品版本为`0.1.0-alpha.1`，尚未公开发布，也未完成全部79项入口。

2026-11-02复核：Divider批次完整Verify报告记录于2026-10-30 16:00（exit0），net48/net8各95项通过，四个包消费宿主及六张截图完成；49个源码快照hash和包SHA256与当前文件一致。本次复核没有重复执行整套测试。Divider独立200控件场景p95为45.06/23.265ms（net48/net8），均低于200ms暂定布局代理预算，尚不代表真实帧率或硬件验收。两端Divider矩阵已目视检查。Button测试补充键盘Focus前置条件，并将窗口移出指针区域以隔离Space鼠标捕获的影响，保留释放后IsPressed、IsMouseOver及原画刷恢复断言。

## 已落地

| 模块 | 当前实现 | 当前边界 |
| --- | --- | --- |
| 工程 | Antd.Wpf.sln、一个双目标库、Gallery、STA测试、包Consumer、Verify脚本 | net48/net8.0-windows；Consumer在pack后独立构建 |
| AntThemeScope | Light/Dark、继承尺寸/动画、局部覆盖、系统高对比度资源、按作用域缓存 | 任意种子调色板、Culture配置、全部组件token未实现 |
| AntButton | 六种变体、常用语义色、圆形/胶囊、三尺寸、图标位置、Ghost、加载延迟、防命令重入 | 完整IconTemplate/LoadingIcon、预设全调色板、波纹/渐变/中文自动间距未完成 |
| AntInput | TextBox原生编辑/绑定/撤销、占位、前后缀、清除、计数、变体/状态、AutomationPeer状态；多行自动高度、MinLines/MaxLines及超限滚动 | Search/Password/OTP及完整计数策略、所有高对比度交互矩阵未完成 |
| AntSelect | ComboBox单选语义、默认模板、虚拟化/回收、Popup主题桥接、Esc/卸载/重模板化清理 | 清除、多选/Tags、分组、搜索、远程加载与完整状态矩阵未完成 |
| AntSwitch | ToggleButton原生命令/TogglePattern、两态绑定、null按关展示、开关内容/尺寸/RTL、加载防重入、忙碌UIA状态；200ms拨钮动画、关闭动画及隐藏/卸载/重模板化清理 | 颜色/标签过渡、按压拉伸、把手阴影和加载旋转待实现；真实输入、高对比度动态矩阵及硬件帧率/GC待验收 |
| AntCheckBox | 原生CheckBox绑定、Command、Space、三态；16DIP框、4圆角、8DIP标签内距、8×8半选方块；原生TogglePattern | Checkbox.Group/SelectedKeys与跨页全选未实现 |
| AntRadioButton | 原生RadioButton/GroupName、命令/互斥与SelectionItemPattern；16DIP圈、6DIP点、8DIP标签内距/行末Margin及RTL | RadioGroup、按钮外观、组值/方向键/Tab契约和末项Margin0待实现；单项不猜测集合首尾 |
| AntDivider | 水平/垂直、实/虚/点线、标题Start/Center/End及RTL、plain、尺寸外距、可选DIP标题Inset、ContentTemplate与Separator自动化；缓存Pen直接绘制 | 默认布局功能已实现；真实150%/200%DPI、动态DPI、屏幕阅读器及与浏览器的逐像素差分待验收 |
| AntFlexPanel | 横纵布局与Wrap、RowGap/ColumnGap、六种主轴对齐、跨轴Start/Center/End/Stretch、换行槽拉伸、原生RTL与Min/Max/绑定；无包装节点 | Grow/Shrink/Basis/AlignSelf、WrapReverse和AlignContent待后续；Baseline、真实DPI/读屏及耗时/GC预算待验收 |
| AntSpace | 默认Gap8、行列Gap覆盖、横纵方向、跨轴对齐/纵向默认拉伸、水平换行、RTL和装饰SeparatorTemplate；子项无包装 | 换行处隐藏孤立分隔符是WPF明确映射；Compact/Addon/Baseline、DPI/读屏/耗时/GC待验收 |
| AntRow/AntCol | 24列、Span/Offset、换行、横纵gutter、六Justify/四Align、RTL、Span0不测量且保留绑定；列重归属清理内部gutter | 默认Span24是明确WPF映射；断点/Order/Push/Pull/Grow/Basis、真实DPI/读屏/耗时/GC待验收 |
| AntLayout/Header/Content/Footer | 原生DockPanel区域与嵌套、Header MinHeight64/水平padding50、Footer padding24/50及字体相对行框；正文显式滚动 | 容器相对固定；不自动插入ScrollViewer，真实DPI/读屏待验收 |
| AntSider | 默认200/80、手动/零宽折叠、Width/IsCollapsed绑定、左右/RTL、独立主题、两态边缘触发器、焦点转移、UIA/原因事件、自定义触发模板与旧命令清理 | 响应式/遮罩/动画、完整物理输入与文化配置、独立耗时/GC/堆根验收待完成 |
| Gallery | 明暗/尺寸切换、加载演示、绑定输入、万项选项、局部主题及可重复PNG渲染 | 展示当前切片，不展示未实现控件 |

2026-11-03本批修复Switch文字偏下，按WPF自然字体行框居中，保留官方轨道和拨钮几何。Input新增多行自动高度、MinLines/MaxLines及超限滚动，行距仍为22/24DIP；绑定、选区、焦点和Undo保留。完整Verify通过：Release零警告/零错误，net48/net8各237/237，零失败/跳过；四个真实NuGet消费宿主、14张截图和70份源码hash一致性检查通过。包SHA256为`DC21240088D4CDA771A164C4833E2CFC9859D540D35CBE4D0E3FA29D95111746`。新增11项用例及限制见[多行输入实施](analysis/input-multiline-implementation.md)和[Switch排版修复](analysis/switch-motion-implementation.md)。PF02/PF05布局代理暂定预算内，未将其转记为硬件帧率/GC验收；最新原始摘要为m0-validation.json，既有历史计数/hash保留为历史记录。

库不引用浏览器或MVVM框架，没有外部运行时NuGet依赖。不是把全部规格批量变成空壳类。

Divider来源、默认数值、WPF边界及验收见[Divider实施说明](analysis/divider-implementation.md)。新增Gallery的Divider examples及双端明暗矩阵；无标题上下24、标题16、小号8，竖向左右8；Split颜色来自官方实际算法（Light rgba5,5,5,.06 / Dark rgba253,253,253,.12），没有沿用近似纯黑/纯白。自定义标题变宽、零宽和窄容器均有实际渲染/布局断言，不以属性存在当作行为完成。

2026-11-02新增Flex P1切片，来源与边界见[Flex实施说明](analysis/flex-implementation.md)。默认Gap/Margin为0，不添加字体/颜色/包装；Gallery展示8/16/24DIP、自适应宽度、两方向及RTL。布局只遍历直接子项，200子项探针验证首次各测量/排列一次；对齐变化不重新测量。这里只证明布局访问/节点数量，不等于时间或GC预算通过。首批不换行、不自动缩小子项，内容不足时从起始端溢出；调用方可用ClipToBounds决定裁剪。

Flex批次完整Verify于2026-11-02 09:20完成，exit0：Release零警告/零错误，net48/net8各121/121、无失败/跳过，四个真实包消费宿主通过，共八张截图。Flex两端明暗矩阵已目视检查；52个源码快照hash及新包SHA256与报告一致。包SHA256为C776FF6CDACD6FEAFB1F543FA8FC38E4EEAD2BFB032804BEB36F93D494D216EA。历史Divider批次数据仅作历史记录，最新结果以m0-validation.json为准。

Space P1来源、布局边界和逐项验收见[实施说明](analysis/space-implementation.md)。只在有模板时建立至多n−1个分隔符，模板替换解除旧绑定及逻辑/视觉父引用；普通子项不额外包装。布局为O(n)，200子项/199分隔符在30次对齐切换中保持节点/实例稳定且不重新测量。尚未采集Space独立时间/GC预算，不以既有PF02/PF05数据代替。

Space批次完整Verify于2026-11-02 09:32完成，exit0：Release零警告/零错误，net48/net8各145/145、无失败/跳过，四个真实包消费宿主通过，共十张截图。Space两端明暗矩阵已目视检查；55个源码快照hash和包hash与报告一致。新包SHA256为2DD543944D980DB86DF786075118B8DDE9F7CC21B83581FFE1115C0DAB2C5F49。此前包hash/计数保留作历史，最新结果仍以m0-validation.json为准。

Grid P1来源、gutter公式及边界见[实施说明](analysis/grid-implementation.md)。通过内部负半gutter槽和列半内距对齐官方默认几何，不改写公开Margin/Width/Visibility绑定；Span0使用原生Visibility强制路径跳过孩子测量。200列探针检查每列一个原始孩子、初次各测量一次，30次对齐变化后逐一核对计数不增加。未采集Grid独立时延/GC，不以既有PF02/PF05数据代替。

Grid批次最终Verify于2026-11-02 10:47完成，exit0：Release零警告/零错误，net48/net8各171/171、无失败/跳过，四个真实包消费宿主通过，共十二张截图。最终运行已包含Offset占满后的换行、Hidden保留空间及Row自身Measure计数断言。59个源码快照hash和包hash与报告一致，Grid双端截图与已目视检查的图片hash一致。新包SHA256为71EFADADF1C58DCF6B710E91900B134BC95C1848F5E04C9DF791C22EF7090455；此前批次hash/计数仅作历史。

Layout P1的官方源码、实际token算法输出和映射边界见[实施说明](analysis/layout-implementation.md)。Sider复用AntThemeScope；默认暗色由元数据/主题样式提供，调用方Style/绑定仍能覆盖。Width使用CoerceValue，零宽Canvas触发器不贡献DesiredSize；先同步只读模板状态再转移焦点，动态CollapsedWidth从80改0及返回80均保留有效入口。普通48高触发器、40×40边缘触发器和8外侧圆角有具体断言；许可文件按README相同的显式None打包模式处理，保留真实包资产与内容hash检查。

Layout批次最终Verify于2026-11-02 14:30完成，exit0：Release零警告/零错误，net48/net8各191/191、无失败/跳过，四个真实包消费宿主通过，共十四张截图。65个源码快照hash与当前文件一致；Layout两端明暗矩阵已完整目视检查，含底部右侧零宽入口，示例页收紧的是外部留白，未改控件默认尺寸。包SHA256为D42B16BBDCAB617758B57B5A22A67C76C65B55BE9A5CF9E3CCDAB69E4F5BC86A；许可文件包内字节与源文件hash一致（869F3B049CD11E491B0E36BC13A5DE06A901AE70CFB72DE9BA0EFD72F4ABD3FE）。此前批次hash/计数仅作历史。

Gallery预览修复见[复现与验收](analysis/gallery-preview-fixes.md)：六个Matrix窗口加入双向滚动及有限展示宽度；导航折叠使用可访问紧凑图标，展开恢复文字，并修正祖先布局裁剪。回归直接加载Gallery编译资源，测试项目新增Gallery引用，产品库依赖方向不变。Matrix截图捕获完整内容，不再以屏幕工作区大小限制导出高度。

预览修复最终Verify于2026-11-02 14:55完成，exit0：Release零警告/零错误，net48/net8各199/199、无失败/跳过，四个真实包消费宿主和十四张截图通过。66个源码快照hash与当前文件一致；Layout双端及完整Visual matrix已目视检查，导航图标/文字和底部示例完整。包SHA256为DB4CD107C73D61670CDF594E85D19FC107DA7FF30EB38971E7D989BC977718D2；许可内容hash校验仍通过。此前包hash/计数仅作历史。

输入文字居中与邮箱示例修复于2026-11-02 15:09完成完整Verify，exit0：Release零警告/零错误，net48/net8各203/203、无失败/跳过，四个真实包消费宿主和十四张截图通过。66个源码快照hash与当前文件一致，双端Gallery输入区域已目视检查。包SHA256为F9500917AF1A6848BACACDF3E232474F0DA574F22AB04D6498F7ABE5A68F7D0E；此前计数/hash仅作历史。三种尺寸单行输入使用原生字体行框居中，保留高度/Padding及Top/Bottom覆盖；Email直接输入完整地址。复现、回归名称及WPF排版边界见[修复说明](analysis/gallery-preview-fixes.md)。既有PF02/PF05布局代理在临时预算内，不代表完整帧率、GC或多DPI验收。

## 视觉来源与本批修正

2026-11-02后续Flex P2切片已实现横纵Wrap、SpaceBetween/Around/Evenly及默认跨轴行槽拉伸，原有不换行默认与枚举值保留。该批net8定向38/38、整套215项通过，Framework当时受Gallery EXE元数据问题阻塞；在后续Switch批次完整Verify中已补齐两端、四个包消费及截图验证。详情见[Flex实施与验收](analysis/flex-implementation.md#本批验证状态)。

Switch拨钮动画批次于2026-11-02 16:11完成完整Verify，exit0。新增200ms ease-in-out位移、快速反向连续性、关闭动画、隐藏/卸载/禁用/重模板化清理，以及标签/尺寸变化端点更新。Release零警告/零错误，net48/net8各226/226，无失败/跳过；四个真实包消费宿主和14张截图通过，67个源码快照hash一致。包SHA256为CDBD0ACF024584B4CF1FF5A6E74D5CE068BFE98DA60CD385107C26C24014AAEE，许可校验通过。先前EXE元数据阻塞在重建后解除，合成Space测试补齐Focus前提。动画帧不触发控件重新Measure，硬件帧率/GC仍待测。来源、时钟所有权与逐项验收见[Switch动画实施](analysis/switch-motion-implementation.md)，最新摘要为m0-validation.json；此前计数/hash仅作历史。

详见[官方6.6.8样式与token映射](analysis/visual-token-mapping.md)：官方源码URL/hash、官方npm默认/暗色算法实际输出均独立保存。Button三尺寸padding为7/15/15水平、纵向均0；Input/Select为7,0 / 11,4 / 11,7，字号14/14/16、圆角4/6/8。真实ActualHeight有双端断言，不只检查MinHeight。WPF文本行框22/24是明确映射；单行Input使用原生字体行框以修复文字偏下，多行仍保留22/24，不声称浏览器与WPF字体栅格完全相同。

修复TextBoxBase把Padding再次传给PART_ContentHost而产生的双重内距；正文、占位与前后缀由外框统一负责Padding，内层ScrollViewer保持0，更改Size/Padding及重模板化后仍检查。尺寸资源使用ComponentResourceKey，可从Generic默认主题定位；颜色资源仍要求AntThemeScope。

Light/Dark的透明度使用精确Brush.Opacity，不预先混成固定灰；Dark选中背景修为#15325b。Switch恢复官方44×22/28×16、18/12拨钮、2DIP内距、字号均12、圆角11/8、未选黑/白25%与禁用/忙碌透明度0.65；Large映射Default。Select取消额外选项Margin，恢复padding5,12和正常文字色/字重600。Button的1DIP透明边框与零模糊阴影保留，焦点环和新选择图形用Opacity切换。

Gallery的Visual matrix可交互查看，Verify渲染artifacts/visual-matrix-net48.png与visual-matrix-net8.png。页面布局Margin不属于控件默认Margin。缺少的Switch/Popup模糊阴影、动画与完整颜色状态组合仍是待完成事项。Button的Solid+Default已纠正为官方中性色：Light黑底白字、Dark白95%底黑字；主按钮示例显式指定SemanticColor=Primary，旧预览中依赖隐式蓝色的使用处需同步修改。

Button现在按六种Variant与Default/Primary/Danger分别映射普通/Hover/Pressed色，不让通用蓝色触发器覆盖危险色；Filled保持基础色文字，仅变化背景；Default Link使用官方LinkHover（不同于PrimaryHover）；Text/Link禁用时保留透明底/透明边框。按住Space驱动真实IsPressed的18组测试检查字体、背景、透明度、边框及释放/禁用恢复，Hover颜色仍需实际指针验收。

Grid批次全量验证曾复现Button测试释放后仍Pressed的间歇失败。WPF ButtonBase.OnKeyUp在真实鼠标左键按住时保留捕获，合成KeyEvent不改变硬件状态；测试帮助器现于KeyUp之后显式结束原生鼠标捕获并同步鼠标，新增IsMouseCaptured=false断言，原有颜色/Pressed/MouseOver/恢复断言保留。这里验证按下配色及捕获结束后的恢复，不代替物理Space释放/鼠标键盘混合交互验收，也没有修改产品按钮配色来迁就测试。

另修复横向WrapPanel同一行大小混用时默认Stretch把24DIP小控件拉到40的问题：默认VerticalAlignment=Center，显式本地Stretch仍可覆盖。没有强制固定Height，长内容/字体和用户布局仍可决定所需高度。

## 验证方式

```powershell
pwsh -File scripts/Verify.ps1
```

脚本先检查空源文件，完整Release构建，再顺序执行两个目标的测试；随后pack、验证资产、以包内容hash隔离Consumer缓存，构建/启动net48、net481、net8、net10宿主，最后渲染两端Gallery。任一步失败即非成功退出。

包Consumer只有PackageReference，没有ProjectReference。它验证包内BAML与Pack URI、主题切换、输入保留、选择和Owner关闭。Framework两个编译目标在本机同一个4.x运行时执行，不能作为两台不同版本系统的实测。

| Requirement | Evidence |
| --- | --- |
| 主题/绑定/局部覆盖 | `Theme_ChangesPreserveEditorAndLocalOverrides`、`Theme_NestedScopeOverridesParentAndSurvivesRepeatedChanges` |
| 单次资源失效/保留用户资源 | `Theme_PaletteReplacementInvalidatesOnceAndPreservesUserResources` |
| 系统高对比度资源 | `Theme_HighContrastResourcesUseSystemColors`；未修改用户系统设置，真实切换仍需人工验收 |
| 按钮命令、延迟和生命周期 | `Button_LoadingPreventsCommandAndPreservesEnabledBinding`、`Button_CancelledDelayAndUnloadDoNotLeaveSpinner`、`Button_InitialZeroDelayAndDelayBoundsAreEnforced` |
| 变体、尺寸与胶囊几何 | `Button_VariantsLoadWithNativeContentAndSize`、`Button_RoundShapeUsesHalfHeightRadius` |
| 输入清除/撤销/只读/禁用 | `Input_ClearPreservesBindingUndoAndReadonlyValue` |
| 输入样式与自动化状态 | `Input_VariantsAndErrorStateUseThemeTokens` |
| 重模板化不残留旧按钮 | `Input_ReapplyingTemplateDetachesOldClearButton` |
| Popup单选/虚拟容器/作用域清理 | `Select_OpenVirtualizesAndTracksOwnerTheme`、`Select_RetemplateOpenPopupReleasesOwner` |
| 初始打开绑定 | `Select_InitialOpenBindingSurvivesFirstTemplate` |
| Switch点击/Space/VM绑定/命令顺序 | `Switch_ClickAndSpacePreserveTwoWayBindingAndCommandOrder`，Space通过原生按键处理器驱动，不等同物理键盘验收 |
| Switch加载/禁用/命令与自动化 | `Switch_LoadingAndDisabledBlockToggleAndPreserveEnabledBinding` |
| Switch内容/null/尺寸/暗色/RTL/高对比度token | `Switch_ContentGeometryThemeAndRtlKeepState`；高对比度字典加载不等同OS动态切换 |
| 官方尺寸/内距/圆角/零外距 | `Controls_UseOfficialSizePaddingRadiusAndZeroOuterMargin`；`Controls_DefaultGeometryResolvesWithoutThemeScope` |
| 混合尺寸横向排列 | `Controls_MixedSizesKeepTheirHeightInHorizontalLayout`，修复前真实高度40、修复后24 |
| Large Select的独立选项字号/行高 | `Select_LargeOwnerKeepsOfficialOptionMetrics`，选项保持14字号、32行高、无额外Margin，不继承外框16字号 |
| Button六变体/三语义色Pressed矩阵 | `Button_PressedPaletteRespectsVariantAndSemanticColor`，18组；`Button_DefaultSolidUsesOfficialNeutralPalette`覆盖明暗中性色 |
| 外部局部样式覆盖 | `Controls_LocalSpacingAndBrushOverridesSurviveThemeChanges` |
| 真实透明度、暗色选中背景与冻结共享画刷 | `Palette_PreservesOfficialAlphaAndSharedFrozenBrushes` |
| Checkbox点击/Space/绑定/半选与禁用UIA | `CheckBox_ClickSpaceBindingAndIndeterminateUseNativeSemantics` |
| Radio互斥、重复点击和跨窗口隔离 | `RadioButton_GroupIsMutuallyExclusiveAndIsolatedByWindow` |
| 选择控件尺寸/间距/状态/共享几何 | `Selection_OfficialGeometryColorsGapsAndSharedDrawingSurviveThemeChanges` |
| 状态变化无需Measure | `Selection_StateChangesDoNotRemeasureControls`；两种控件各30次切换，断言控件Measure次数0 |
| Divider尺寸、标题字体和作用域覆盖 | `Divider_OfficialMarginsAndTitleTypography`、`Divider_DefaultSpacingAndExplicitScopeSizeAreDistinct`、`Divider_FontTokensAndLocalPaddingSurviveThemeAndSizeChanges` |
| Divider两方向三线型实际绘制 | `Divider_RendersEachLineVariantInBothOrientations`，离屏像素检查，不代表384DPI硬件验收 |
| Divider标题位置与边界 | `Divider_TitlePlacementMirrorsInRtl`、`Divider_CustomInsetLongTitlesAndClearingRemainBounded` |
| Divider自定义标题/画刷/绑定/模板生命周期 | `Divider_ContentTemplateFontScalingAndVerticalModePreserveContent`、`Divider_ThemeUpdatesPreserveBindingsAndMutableBrushOwnership`、`Divider_PaintOnlyChangesAvoidMeasureAndOldTemplatesDetach` |
| Divider标题动态尺寸重绘 | `Divider_TitleSizeChangesRepaintRailsWithoutReplacingTemplate`，同一标题对象变宽后旧线段间隙必须刷新 |
| Divider参数和自动化角色 | `Divider_RejectsInvalidParametersAndReportsSeparatorSemantics`、`Divider_DefaultTemplateLoadsFromPackResource` |
| Divider独立性能场景 | `PF02_DividerThemeSwitchKeepsVisualTreeBounded`，200个Divider、5次预热/30样本，视觉节点及字典数不增长 |
| Flex方向/间距/主跨轴对齐 | `Flex_DefaultsHaveNoAddedSpacingOrVisualWrappers`、`Flex_MainAxisAlignmentUsesOnlyRelevantGap`、`Flex_CrossAxisAlignmentControlsSlotWithoutOverwritingChild` |
| Flex可见性/无限约束/子项MinMax与Margin/RTL | `Flex_CollapsedHiddenZeroAndRemovedChildrenHaveDistinctGapBehavior`、`Flex_InfiniteMeasureOverflowAndChildConstraintsAreDeterministic`、`Flex_RtlMirrorsHorizontalOrderWithoutReorderingChildren` |
| Flex动态布局/焦点/绑定/访问数量 | `Flex_ContentAndOrientationChangesRelayoutAndPreserveChildFocus`、`Flex_AlignmentOnlyChangesAvoidMeasureAndPreserveGapBinding`、`Flex_LayoutVisitsEachChildOnceWithoutAllocatingContainers` |
| Flex非法参数/空项 | `Flex_RejectsInvalidGaps`、`Flex_RejectsUnknownEnumsAndHandlesEmptyUnboundedPanel` |
| Space默认间距/两方向对齐 | `Space_DefaultGapAndHorizontalCenterHaveNoWrappers`、`Space_ExplicitCrossAlignmentPreservesNativeChildSize`、`Space_VerticalAutoStretchesSlotsAndIgnoresWrap` |
| Space换行/可见性/RTL/边界 | `Space_WrapHonorsExactBoundaryAndIndependentRowGap`、`Space_CollapsedHiddenRemovalAndZeroSizeKeepVisibleAdjacency`、`Space_RtlWrapMirrorsRowsWithoutReorderingChildren`、`Space_UnboundedOversizedZeroAndEmptyLayoutsRemainDeterministic` |
| Space分隔符与生命周期 | `Space_SeparatorsHaveTwoGapsAndDisappearAtWrapBoundary`、`Space_SeparatorTemplateReplacementDetachesOldVisualAndLogicalChildren`、`Space_SeparatorContextAndThemeUpdateWithoutReplacingContent` |
| Space绑定/更新/节点与测量数量 | `Space_GapFallbackBindingAndContentChangesUpdatePositions`、`Space_StableLayoutReusesBoundedSeparatorsAndAlignmentAvoidsMeasure` |
| Space参数验证 | `Space_InvalidGapInputsAreRejected`、`Space_UnknownEnumsAreRejectedAndZeroGapsAreAllowed` |
| Grid基础布局/官方gutter/对齐 | `Grid_SpansOffsetsAndWrapUse24Tracks`、`Grid_GutterUsesOfficialNegativeRowAndColumnHalfPadding`、`Grid_JustificationDistributesOnlyFreeTracks`、`Grid_AlignUsesNaturalLineHeight` |
| Col隐藏/恢复/重归属/绑定 | `Col_ZeroSpanCoercesVisibilityWithoutMeasuringOrLosingBindings`、`Col_ReparentingClearsOnlyOwnedGutter` |
| Grid RTL/无限约束/动态尺寸/焦点 | `Grid_RtlMirrorsOffsetAndGutter`、`Grid_UnboundedAndZeroLayoutsRemainFinite`、`Grid_ResizeAndGutterBindingsPreserveContentAndFocus`、`Grid_TextWrappingRecomputesLineHeightsAfterResize` |
| Grid数量与输入验证 | `Grid_LinearLayoutKeepsDirectColumnsAndAvoidsRemeasure`、`Col_InvalidTrackCountsAreRejected`、`Grid_InvalidGuttersAreRejected`、`Grid_InvalidParametersAndNonColumnChildrenAreRejected` |
| Layout区域/滚动/嵌套/字体与局部样式 | `Layout_OfficialRegionsKeepHeaderFooterAndScrollingIndependent`、`Layout_NestingAndLocalSpacingSurviveThemeAndFontChanges` |
| Sider宽度/零宽/RTL/焦点 | `Sider_WidthBindingAndDynamicResourcesRestoreExpandedBaseValue`、`Sider_ZeroWidthHasVisibleHittableKeyboardTrigger`、`Sider_InitiallyZeroCollapsedDoesNotMeasureOrReplaceBody`、`Sider_CollapsedWidthBindingCanMoveFocusedBodyToZeroTrigger` |
| Sider命令/绑定/UIA/原因/禁用 | `Sider_CommandsBindingsReasonsAndAutomationHaveDistinctTransitions`、`Sider_ReentrantEventDoesNotReuseTriggerReason` |
| Sider主题/重模板化/高对比度字典 | `Sider_IndependentThemesAndTwoWindowsPreserveLocalOverrides`、`Sider_RetemplateDetachesOldCommandAndSupportsCustomTriggerTemplate`、`Layout_HighContrastResourcesUseSystemColors` |
| Sider节点/纯绘制/非法宽度 | `Sider_PaintOverridesDoNotRemeasureLayoutOrBody`、`Sider_RepeatedCollapseKeepsContentTemplatesAndResourcesBounded`、`Sider_InvalidCollapsedWidthsAreRejected` |
| examples滚动/导航裁剪/主Gallery入口 | `Gallery_ExampleWindowsCanScrollToLastExampleAfterResize`、`Gallery_LayoutNavigationStaysReadableWhenCollapsedOrResized`、`Gallery_MainWindowCanScrollToLastExamplesLink` |
| 两目标及真实包消费 | `scripts/Verify.ps1`、`tests/Consumers/Antd.Wpf.Consumer/Program.cs` |
| 性能原始数据 | `PF02_PF05_RecordLayoutLatencyAndBoundedContainers`，只断言生成数量，时间另行报告，不把测试通过当性能通过 |
| 原生/无作用域/有作用域弹层对照 | `Select_ComparePopupSetterAndDispatcherLatency`，分开记录setter与Dispatcher耗时，三组各5次预热/30次采样 |

测试为xUnit2/VSTest，UIIntegration与PerformanceObservation分类；两目标顺序运行，关闭用finally清理。原始TRX、性能CSV、环境说明、包及PNG位于`artifacts/`；最终结果摘要见本目录生成的`m0-validation.json`。

## 性能与发布门槛

当前机器为Windows、8逻辑处理器、WPF RenderTier2、125%DPI（120DPI）。PF02/PF05各预热5次、采样30次，隐藏真实WPF窗口后以Dispatcher Background排空为计时终点。它们是布局/Dispatcher代理，不是GPU呈现、输入延迟、滚动帧率或完整冷启动。

- 主题切换已从删除/插入两次通知改为单次替换。旧并发场景与新顺序场景不直接计算改善百分比。
- 2026-10-30完整验证的一轮net48 PF02代理p95为228.59ms，超过200ms暂定预算；随后同构建独立复查为53.88ms。摘要保留在artifacts/validation/m0-20261030-090954.json，波动原因未确定，不据此放宽预算或宣布性能稳定。Verify现将每轮原始CSV/环境及Popup对照复制到独立verified时间目录，报告直接引用该快照。
- 万项下拉当前最多观察到9个实现容器；测试上限45来自可见项及两侧缓存预算，不允许线性生成全部项。
- PF02保留200个Input场景，并新增200个Button/Input/CheckBox/Radio混合场景；原始CSV分别记名，不将两种场景互相替代或计算改善比例。静态节点/冻结共享及0次Measure不等同GPU帧率或堆占用上限验收。
- 新增PF02.divider-layout-proxy独立场景，源数据在artifacts/performance/divider；Verify统一归档及报告。Divider颜色/线型切换只重绘，30次变化的控件Measure次数为0；线段间隙随标题实际布局变化更新，不通过每帧订阅扫描。
- 历史PF05反复打开存在约700ms的样本；2026-10-30在相同实现的新进程中未复现，原生/无作用域/有作用域的对照也未复现。最新数值随Verify写入m0-validation.json，历史原始数据保留在artifacts/performance/m0-before-resource-replace。没有证据归因于资源桥接或声称已修复，代码保留shortcut；首次冷展开与真实交互终点还需独立采样。
- PF01/PF03/PF04/PF06–PF10未完成；实际键盘/中文IME、100/150/200%DPI、高对比度动态切换、屏幕阅读器、x86和100次循环堆根泄漏调查待验收。

功能构建和测试通过只证明当前切片。M0完整发布门槛没有全部通过，不能把本预览当成全组件库或发布就绪版本。

## 开发环境问题与规范

本机dotnet format曾把本轮新增C#写成空文件，文件已按本轮实现恢复并重新构建验证；后续Verify先检查非空，不重跑此写入路径。IDE0055编译检查继续启用，IDE0005因SDK的XML文档前提调整为编辑器提示，没有全局关闭编译警告。

按code-testing在临时非版本化目录保存研究/计划/质量复核，结合test-gap-analysis与assertion-quality补充了具体变体颜色、上界、只读/禁用分支和模板生命周期场景。没有做覆盖率百分比或真实突变存活率声明。

## 后续顺序

继续PF05复现与M0手动验收；Switch拨钮动画和文本居中、多行输入行数限制切片已有实现。后续补齐基础布局/输入、Switch剩余状态动画，再做AntHost消息队列与P2弹层/复杂选择。Table和AntVirtualTree遵守既定分批路线，不以换肤代替大数据能力。未冻结基础主题、绑定与Popup的完整发布契约。
