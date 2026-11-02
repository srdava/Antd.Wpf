# 通用、布局、导航与全局配置组件实现要求

基线：Ant Design 6.6.8，资料检查日期 2026-10-29。范围为官网目录中的 23 个核心入口。已逐项读取 [官方 Markdown 快照](../analysis/official/manifest.json) 的示例目录、API、子组件及相关说明；本文件没有声称完成各示例的实际交互测试。

“官网事实”仅描述官方资料；“WPF 契约”“首批”“后续”“验收”均为本项目拟定要求。库目标为 `net48;net8.0-windows`，公开行为在两端一致。统一主题、模块关系见 [架构](../architecture/README.md)，统一基准和性能门槛见 [性能设计](../architecture/performance.md)。各组件仍须满足下面单独列出的边界。

组件里的对象键用于稳定身份，不能用当前索引替代；集合更新后保留仍有效的选择、展开和焦点。模板化、资源覆盖、FlowDirection、UI Automation、键盘操作及关闭动画模式均是交付范围。对 HTML、CSS 与 React 专属功能按 WPF 语义改写，不复制同名空属性。

<a id="button"></a>

## button — Button

- **来源与官网事实**：[Button](https://ant.design/components/button/) / [快照](../analysis/official/button.md)。支持 outlined、dashed、solid、filled、text、link 变体，颜色、形状、三种尺寸、图标起止位置、Ghost、危险状态、Loading 及可定制波纹；`type`/`danger` 是较新变体配置的便捷语法。
- **WPF 契约**：`AntButton : Button`；`Variant`、`SemanticColor`、`Shape`、`Size`、`Icon`/`IconTemplate`、`IconPlacement`、`IsGhost`、`IsLoading`、`LoadingDelay`、`LoadingIcon`。保留 `Content`、`Command`、`CommandParameter`、`IsDefault`、`IsCancel`、`Click` 和 `IsEnabled`。
- **首批 P1**：实现六类变体及常用 Primary/Default/Danger 映射、圆角/圆形/胶囊、24/32/40 DIP、前后图标、单图标、Stretch、完整状态和 Loading 防重入。属性冲突只按一个明确映射处理，不保留会互相覆盖的两套状态。加载期间重复按键和鼠标都不能再次执行命令。
- **后续与边界**：预设完整调色板、渐变、自定义波纹 P3。两汉字自动间距为可关闭的呈现选项，不能更改源 Content/无障碍名称。`href/target/htmlType` 不照搬；桌面链接由导航命令/事件处理，表单提交使用 ICommand。命令 CanExecute 仍决定是否可操作。
- **性能与生命周期**：图标用共享冻结矢量；普通按钮不启动常驻计时器。Loading 延迟可取消，重新模板化、卸载和状态结束后停止动画、解绑事件。鼠标态只改变 Brush/RenderTransform，不逐帧触发布局。
- **验收**：①六种变体在明暗主题、三种尺寸下均有清晰的 Focus/Disabled 状态；②Loading 延迟结束前取消不闪烁，连续点击/Enter 只执行一次；③Command CanExecute=false、IsDefault/IsCancel 与键盘 Space/Enter 行为正确；④更换模板和切换主题不丢失 Command、Content 或用户本地样式。

<a id="float-button"></a>

## float-button — FloatButton

- **来源与官网事实**：[FloatButton](https://ant.design/components/float-button/) / [快照](../analysis/official/float-button.md)。包含圆形/方形按钮、Tooltip、Badge、分组菜单、四向展开、受控开关，以及 BackTop 和滚动进度环；官网可拖动示例依赖第三方库，并非内置拖动属性。
- **WPF 契约**：`AntFloatButton` 复用 AntButton；`AntFloatButtonGroup` 提供 ItemsSource、IsOpen、Placement、Trigger、OpenChanged；`AntBackTopButton` 提供 ScrollTarget、VisibilityThreshold、Duration、ShowProgress。宿主锚定到 AntHost 可用区域，Content、Icon、Badge 和 ToolTip 可模板化。
- **首批 P3**：基本按钮、分组、点击/悬停打开、禁用、Tooltip、Badge 和明确 ScrollViewer 目标的 BackTop。分组展开时可用方向键导航，Esc 收起并恢复焦点；触屏始终存在点击路径。
- **后续与边界**：进度环和可选拖动行为在基础菜单完成后交付；拖动是明确的扩展，不引入网页拖动库。BackTop 不隐式查找全应用滚动容器；目标被移除后按钮隐藏。菜单不得覆盖模态遮罩。
- **性能与生命周期**：只订阅当前 ScrollTarget；每帧最多更新一次滚动进度；不可见、未加载或动画关闭时停止视觉动画。换目标和窗口销毁时解除滚动事件与捕获。
- **验收**：①四向展开在屏幕边缘自动调整且子按钮可键盘访问；②阈值前隐藏、超过阈值显示，点击仅滚动指定容器；③禁用状态不响应菜单或命令，Badge 不拦截点击；④反复换 ScrollTarget/关闭窗口无旧目标订阅与活动动画。

<a id="icon"></a>

## icon — Icon

- **来源与官网事实**：[Icon](https://ant.design/components/icon/) / [快照](../analysis/official/icon.md)。官方图标包提供单色、双色、自定义 SVG、旋转、旋转动画及 iconfont 资源方式。
- **WPF 契约**：`AntIcon` 接受图标标识或 `Drawing/Geometry` 资源；提供 Foreground、SecondaryForeground、Rotation、IsSpinning、Stretch，继承 Width/Height。装饰图标不单独进入 Tab 顺序；有独立行为时由按钮承载并设置 AutomationProperties.Name。
- **首批 P1**：提供实际组件所需的矢量图标与统一尺寸/颜色，支持多路径和 fill-rule、双色、静态旋转与可关闭的旋转动画。颜色按主题语义继承，不把颜色硬编码进每个使用点。
- **后续与边界**：全量图标目录可按需生成；iconfont.cn 脚本加载与任意网络 SVG 执行不映射到桌面运行时。用户自定义图标应先转换为受支持的 WPF Drawing，保留来源许可。
- **性能与生命周期**：按资源共享 Geometry/Drawing 并冻结不可变对象；不同颜色只复用几何而不修改共享画刷。旋转使用 RenderTransform，无布局旋转；卸载/不可见时停止动画。
- **验收**：①复杂多路径、双色和偶奇填充在两端/DPI 下形状一致；②改变前景色不污染其他实例；③关闭动画后 Loading 图标仍可辨识；④装饰图标不产生重复读屏名称，图标按钮有可识别名称。

<a id="typography"></a>

## typography — Typography

- **来源与官网事实**：[Typography](https://ant.design/components/typography/) / [快照](../analysis/official/typography.md)。覆盖 Text、Title 1–5、Paragraph、Link，强调/代码/删除线等文本样式，复制、内联编辑、单行/多行省略、展开收起、后缀和操作栏。
- **WPF 契约**：基础以 TextBlock/Run/Hyperlink 样式为主；有交互的 `AntText/AntParagraph` 提供 Text、TextKind、TitleLevel、IsCopyable、CopyText、IsEditable、IsEditing、MaxLength、TextTrimming、MaxLines、IsExpanded，以及 EditStarted/Committed/Canceled、CopyCommand 和操作模板。
- **首批 P1**：排版层级、语义颜色、粗体/斜体/删除线/下划线、代码与键帽样式、Link 命令、单行省略。内容保留可访问文本，视觉截断不能截断绑定源。
- **后续 P2/P3**：复制与内联编辑 P2；多行/中间省略、后缀、展开收起和操作栏 P3。编辑器保留 IME、撤销和选区，Enter 提交/Esc 取消；复制失败需反馈，不假装成功。
- **性能与生命周期**：普通 Text 不创建 FlowDocument 或隐藏 TextBox。多行截断只在内容、字体、DPI 或有效宽度变化时重新测量，合并 Resize 请求；复制反馈 timer 可取消，链接与编辑事件卸载解绑。
- **验收**：①标题级别、语义色、文本装饰可组合且读屏名称完整；②窄宽度省略后扩宽恢复，Text 源值不变；③编辑中中文组合输入不被 Enter 误提交，Esc 恢复原文；④复制使用 CopyText 或完整原文，失败反馈与成功状态可区分。

<a id="divider"></a>

## divider — Divider

- **来源与官网事实**：[Divider](https://ant.design/components/divider/) / [快照](../analysis/official/divider.md)。支持水平/垂直、标题起/中/末、普通标题、间距尺寸，以及实线/虚线/点线。
- **WPF 契约**：`AntDivider` 或显式 Separator 样式；Orientation、Content/ContentTemplate、TitlePlacement、IsPlain、Variant、Size、TitleInset。使用 Start/End 与 FlowDirection 协同。
- **首批 P1**：水平/垂直分隔、无标题和有标题、三种线型及主题颜色；标题过长时优先保留可用内容，不产生负长度线段。分隔线本身不可获得焦点。
- **后续与边界**：用户自定义复杂标题走 ContentTemplate，无需单独文本框架；垂直模式的标题排版作为显式扩展，首批不套用水平标题位置。
- **性能与生命周期**：使用最少 Border/Line 或绘制节点；固定分隔线无 timer、事件订阅和 Effect。1 DIP 边线启用像素对齐，主题更新只换资源。
- **验收**：①三种线型及水平/垂直方向在 125%/150% DPI 清晰；②Start/End 随 RTL 交换；③长标题/零宽容器不异常、不产生负尺寸；④隐藏标题后间距与线段重新占满。

<a id="flex"></a>

## flex — Flex

- **来源与官网事实**：[Flex](https://ant.design/components/flex/) / [快照](../analysis/official/flex.md)。提供主/交叉轴对齐、方向、Gap、换行及 flex 拉伸；官方特别说明 Flex 不像 Space 那样给每项增加包装节点。
- **WPF 契约**：`AntFlexPanel : Panel`；Orientation、Wrap、JustifyContent、AlignItems、RowGap/ColumnGap；子项附加属性 Grow、Shrink、Basis 和 AlignSelf。Basis 采用明确的 Auto/DIP/比例类型，不解析任意 CSS 字符串。
- **首批 P1/P2**：P1 实现不换行、方向、间距、Start/Center/End/Stretch；P2 实现换行、SpaceBetween/Around/Evenly 与 Grow/Shrink 的有限且文档化算法。遵守子项 Min/Max/DesiredSize，Collapsed 项不占 Gap。
- **后续与边界**：Baseline 对齐只有在子项基线可定义时实现；百分比 Basis 在无限约束时按 Auto 处理并记录行为。普通业务 Grid/DockPanel 能满足时继续使用原生布局。
- **性能与生命周期**：单次 Measure/Arrange 对可见直接子项 O(n)，不递归遍历整个视觉树；尺寸变化才失效，不监听全局 LayoutUpdated。它是布局面板，不宣称大量数据虚拟化。
- **验收**：①主轴剩余空间按 Grow 分配，Min/Max 限制后重新分配正确；②换行边缘、Gap、Collapsed、RTL 不出现重复间距；③无限 Measure、零尺寸、单子项和超宽不可缩项结果确定；④改变一项内容不产生无穷布局循环。

<a id="grid"></a>

## grid — Grid

- **来源与官网事实**：[Grid](https://ant.design/components/grid/) / [快照](../analysis/official/grid.md)。采用 24 栅格 Row/Col，支持 gutter、span、offset、order、push/pull、对齐、换行、弹性伸缩及 xs 到 xxxl 断点（xxxl 为 1920px）。
- **WPF 契约**：`AntRow/AntCol` 避免与 WPF Grid 重名；Row 提供 HorizontalGutter、VerticalGutter、Wrap、Justify、Align；Col 提供 Span(0–24)、Offset、Order、Push/Pull 与断点覆盖对象。公开只读 CurrentBreakpoint/BreakpointChanged，替代 React hook。
- **首批 P1**：24 列、Span、Offset、换行与 gutter；Span=0 等同 Collapsed，不测量内容。P2 添加断点覆盖、顺序和推拉、Grow/Basis。断点以所在布局宿主可用 DIP 宽度计算，明确区别于官网浏览器屏幕宽度。
- **后续与边界**：保留官方 xs/sm/md/lg/xl/xxl/xxxl 的命名与阈值参考，但不直接复用 Layout.Sider 自己的断点表；断点不足或无限宽时的回退固定为基础配置。Push/Pull 仅改变布局，逻辑阅读顺序仍按集合，需在文档说明。
- **性能与生命周期**：排序结果缓存到 Order/集合变化，Resize 不反复排序；按行扫描 O(n) 布局，只在跨阈值时重算响应式配置。不会对每个单元格添加独立全局窗口事件。
- **验收**：①8+8+8 填满一行，12+16 在 Wrap 模式换行；②跨每个断点时只应用最近覆盖，两个不同宽度宿主可同时呈现不同列数；③Span=0 后恢复保留绑定；④RTL、Offset、gutter、Order 联合时无溢出或重叠错误。

<a id="layout"></a>

## layout — Layout

- **来源与官网事实**：[Layout](https://ant.design/components/layout/) / [快照](../analysis/official/layout.md)。组合 Header/Content/Footer/Sider；Sider 支持折叠、宽度、折叠为零时的触发器、响应式、左右方向与独立 Light/Dark。固定 Header/Sider 由布局示例演示。
- **WPF 契约**：优先 Grid/DockPanel 配套样式，`AntLayout` 作为区域宿主；Sider 提供 Width、CollapsedWidth、IsCollapsed、IsCollapsible、CollapseCommand、Breakpoint、ThemeMode、TriggerTemplate 和 CollapsedChanged（携带触发原因）。Content 使用用户内容模板。
- **首批 P1**：基础四区域与嵌套，Sider 手动折叠、80 DIP 默认折叠宽、零宽触发器、右侧栏、独立主题；保留布局尺寸绑定。P2 添加响应式折叠和可选遮罩展开。
- **后续与边界**：用户手动折叠与断点折叠的优先级必须明确，避免每次 SizeChanged 覆盖用户选择；固定区域采用容器布局，不将其当顶级 Window 固定。官网 Layout.Sider 的 xs 阈值与 Grid.Col 的 xs 分类定义不同，禁止混用。
- **性能与生命周期**：折叠动效优先裁剪/变换，只有确需推挤内容时才做受控布局动画；有限时间内合并尺寸变化。隐藏区域是否释放内容由显式策略决定；卸载时解绑宿主宽度监听。
- **验收**：①头尾固定时只有 Content 滚动，Sider 不覆盖内容；②CollapsedWidth=0 仍可键盘展开，折叠后焦点移到有效触发器；③两个窗口有独立宽度/折叠状态；④手动/断点切换有可辨认的事件原因且不形成绑定循环。

<a id="masonry"></a>

## masonry — Masonry

- **来源与官网事实**：[Masonry](https://ant.design/components/masonry/) / [快照](../analysis/official/masonry.md)。列数、响应式 gutter、动态高度监测 `fresh`，条目含稳定 key、指定 column、height、data，并可通知布局变化。
- **WPF 契约**：`AntMasonry : ItemsControl` 与专用 Panel；ItemsSource、ItemTemplate、Columns、RowGap/ColumnGap、IsDynamicSizeTrackingEnabled、ItemKeyPath、可选 HeightHint/PreferredColumn、LayoutChanged（key/column）。
- **首批 P3**：有限集合不等高卡片、最短列分配、指定列和高度提示、宿主宽度断点、图像加载后重新排布。默认仅响应实际高度变化，不无条件轮询。
- **后续与边界**：大数据的二维/不等高虚拟化另行原型验证；首版文档明确为非虚拟化布局，不允许 Gallery 用十万项示例暗示已支持。指定列超范围必须明确回退到自动分配，空项或错误高度不得中断布局。
- **性能与生命周期**：缓存高度与分列，尺寸变化只重排受影响段；使用列高度最小堆时 O(n log columns)，固定少列可线性。图像尺寸变化合并到一帧，不逐项反复 InvalidateMeasure；解绑旧条目监听。
- **验收**：①不同高度的条目填入最短列且稳定 key 重排可追踪；②图片异步加载、删除/插入条目后无重叠；③跨列数断点、指定列失效、空集合和零宽安全；④连续高度抖动不会造成无限布局/事件递归。

<a id="space"></a>

## space — Space

- **来源与官网事实**：[Space](https://ant.design/components/space/) / [快照](../analysis/official/space.md)。间距、水平/垂直、对齐、换行、分隔符；Compact 将相邻输入或按钮边框连成组，另有 Addon。
- **WPF 契约**：`AntSpace` 提供 Orientation、Gap/RowGap/ColumnGap、Alignment、Wrap、SeparatorTemplate；`AntCompact` 提供 Size、Orientation、IsBlock；`AntSpaceAddon` 为首尾辅助内容模板。
- **首批 P1**：间距、方向、Start/Center/End、水平换行和可定制分隔符；P2 完成 Compact 边框合并、首尾圆角、焦点/悬停边框提升及 Addon，不能只把 Gap 设为零当作完成。
- **后续与边界**：Baseline 对齐需要明确定义文本基线的子项。相邻项 Collapsed 或集合改变时按可见顺序确定首尾；不同类型控件仍使用其公开资源/附加属性，不侵入其私有模板。
- **性能与生命周期**：不为单纯间距增加多余 ContentControl 包装；分隔符仅存在于相邻可见项之间，数量至多 n−1。Compact 只在集合/可见性变化时更新首尾标记，不在每帧扫描。
- **验收**：①Collapsed 项不留双 Gap 或孤立分隔符；②换行后行列间距正确，RTL 次序合理；③Compact 中键盘焦点和错误边框不被相邻控件遮住；④动态移除首项后新的首项获得正确圆角，大小绑定仍可覆盖。

<a id="splitter"></a>

## splitter — Splitter

- **来源与官网事实**：[Splitter](https://ant.design/components/splitter/) / [快照](../analysis/official/splitter.md)。多 Panel、水平/垂直、受控尺寸、最小/最大、可折叠、lazy 拖动、双击重置、隐藏销毁及 resize 开始/过程/结束事件。
- **WPF 契约**：`AntSplitter` 组合 Grid/GridSplitter；Panel 的 Size 为可空 `SplitterLength`，单位明确为 DIP 或 Percent，MinSize/MaxSize 使用相同单位；空 Size 表示均分剩余空间。百分比按扣除分隔条后的有限可用长度计算，不能把 GridLength.Star 当作固定百分比。另提供只读 ActualSize(DIP)、IsResizable、IsCollapsed、IsCollapsible、ContentLifetime；Orientation、IsDeferredResizeEnabled，以及 ResizeStarted/Resizing/ResizeCompleted、CollapseChanged、ResetCommand。
- **首批 P1/P2**：双面板、两方向、Min/Max、键盘和鼠标拖动、可绑定尺寸 P1；多面板、嵌套、折叠恢复、预览拖动与双击重置 P2。固定尺寸与比例尺寸不可因为一次拖动丢失所有约束。
- **后续与边界**：Size 是期望值，ActualSize 是约束后实际值；先换算百分比并应用 Min/Max，再把剩余空间均分给空 Size 面板。全部指定时差额由末尾可调整面板向前分摊，直到满足约束；仍无法满足则呈现明确溢出而非负尺寸。拖动更新相邻期望值时保留原 DIP/Percent 单位；窗口 Resize 重新计算百分比。隐藏销毁为显式内容生命周期策略，数据由 VM 保留；相邻 Min 总和超过容器时禁止继续缩小，不隐式丢失未保存内容。
- **性能与生命周期**：Deferred 模式拖动时只移动预览线，释放后再布局重内容；实时模式每帧合并一次尺寸更新。取消拖动/失去捕获恢复正确状态；折叠动画关闭后直接提交最终尺寸。
- **验收**：①拖动和键盘调整均不越过 Min/Max，Esc/失去捕获安全退出；②比例布局随宿主 Resize 保持约束；③折叠再展开恢复上次有效尺寸，双击按定义重置；④Deferred 模式拖动期间重内容不持续重排，事件开始/结束各一次。

<a id="anchor"></a>

## anchor — Anchor

- **来源与官网事实**：[Anchor](https://ant.design/components/anchor/) / [快照](../analysis/official/anchor.md)。支持横/竖目录、嵌套条目、滚动容器、偏移、当前锚点和变化通知、可固定；横向模式不支持嵌套，6.4 支持每项 targetOffset。
- **WPF 契约**：`AntAnchor`：ItemsSource、ItemTemplate、Orientation、ScrollTarget、ActiveKey、DefaultOffset、ActivationBounds、IsAffixed、NavigateCommand/ActiveChanged；条目含 Key、Title、Target（元素或可解析标识）、Offset 与 Children。
- **首批 P3**：滚动定位、可选 Affix、当前项高亮、每项偏移覆盖整体偏移、嵌套竖向目录与横向平铺。点击先提供可取消导航事件；定位完成更新 ActiveKey，滚动期间避免来回抢占高亮。
- **后续与边界**：浏览器 hash/history replace 不直接映射；导航历史由宿主决定。虚拟化中尚未实现的目标需要宿主先定位/加载，不通过强制生成全部列表容器解决；找不到目标提供明确失败结果。
- **性能与生命周期**：缓存目标偏移，内容布局变化后增量失效；滚动一帧最多计算一次当前项，可对有序偏移二分查找。换 ScrollTarget、换目录和卸载时解除订阅，不全局 LayoutUpdated 扫描所有元素。
- **验收**：①自定义滚动容器中点击准确对齐，单项 Offset 优先；②手动滚动、末尾短章节和相同位置目标的高亮规则确定；③目标缺失/移除/尚未实现不会抛未处理异常；④横向嵌套输入按文档拒绝或扁平化，键盘导航可达全部有效项。

<a id="breadcrumb"></a>

## breadcrumb — Breadcrumb

- **来源与官网事实**：[Breadcrumb](https://ant.design/components/breadcrumb/) / [快照](../analysis/official/breadcrumb.md)。路径项、图标、参数化显示、统一/逐项分隔符、下拉菜单与自定义呈现；URL 的 path/href 为互斥语义。
- **WPF 契约**：`AntBreadcrumb : ItemsControl`；ItemsSource、ItemTemplate、Separator/SeparatorTemplate；条目 Key、Header、Icon、Command/CommandParameter、MenuItems、SeparatorOverride 和 IsCurrent。`ItemInvoked` 可由宿主导航处理。
- **首批 P1**：路径显示、当前项非导航、图标、分隔符与命令；P2 增加每项菜单、下拉图标与可模板化条目。导航参数由 ViewModel 提供，不将网页路由器移植为控件责任。
- **后续与边界**：自动折叠长路径作为后续扩展且需完整键盘路径；普通文本显示不能把任意输入隐式当外部链接执行。自定义模板仍保留项语义和有效焦点顺序。
- **性能与生命周期**：按路径集合增量更新，不因悬停重新生成整条路径；分隔符无命令与焦点。菜单惰性创建，关闭后按公共弹层生命周期处理。
- **验收**：①最后项标为当前且不触发导航，其他项仅执行对应命令；②逐项分隔符覆盖全局，RTL 顺序/图标间距正确；③禁用菜单项不可触发，Esc 后焦点回到原路径项；④参数/路径动态改变后不存在旧命令、旧菜单或重复分隔符。

<a id="dropdown"></a>

## dropdown — Dropdown

- **来源与官网事实**：[Dropdown](https://ant.design/components/dropdown/) / [快照](../analysis/official/dropdown.md)。支持 hover/click/contextMenu、12 个定位方向、越界调整、箭头、自定义弹层、菜单/级联/选择/额外内容、隐藏销毁；示例包含组合下拉按钮。
- **WPF 契约**：`AntDropdown`：PlacementTarget、IsOpen、Placement、Trigger、IsArrowVisible、IsAutoAdjustEnabled、ItemsSource/PopupContentTemplate、ContentLifetime、OpenChanged（含原因）；`AntDropdownButton` 组合主按钮命令与独立菜单开关。
- **首批 P1/P2**：点击/右键、菜单命令、禁用、Esc/外部点击关闭与边缘调整 P1；悬停延迟、箭头、全部定位、自定义弹层、级联和分离按钮 P2。自定义内容可选择保持打开，不能每次内容点击均关闭。
- **后续与边界**：触摸设备有点击路径；右键菜单锚定指针位置，其余锚定目标。选择和打开状态是不同契约；关闭原因包含选择、Esc、外部点击、Owner 关闭，WPF 不照搬官网部分关闭不触发 onOpenChange 的差异。
- **性能与生命周期**：共享定位/主题桥接，不为每项创建 Window；打开才实现内容，保留/卸载策略显式。只在目标/Owner 位置、DPI、可用屏幕和内容尺寸变化时重定位。
- **验收**：①屏幕四边与多显示器 DPI 下不会将内容放到不可用区域；②按钮主命令和菜单开关不互相误触发；③自定义输入内容可输入中文，Esc 关闭并恢复焦点；④Owner 销毁、禁用或目标被移除时 Popup 关闭且取消延迟任务。

<a id="menu"></a>

## menu — Menu

- **来源与官网事实**：[Menu](https://ant.design/components/menu/) / [快照](../analysis/official/menu.md)。支持 vertical/horizontal/inline、子菜单、分组、分隔符、危险/禁用项、单/多选、选中与展开 key、折叠 Tooltip、溢出菜单、独立子菜单主题和自定义子菜单内容。
- **WPF 契约**：基于 Menu/MenuItem 与 inline 导航容器；Mode、ItemsSource、SelectedKeys、OpenKeys、IsMultiSelect、IsSelectionEnabled、IsInlineCollapsed、Indent、OpenDelay/CloseDelay；条目稳定 Key、Header、Icon、Extra、Children、Command、IsEnabled、IsDanger；SelectionChanged/OpenKeysChanged/ItemInvoked。
- **首批 P1**：竖向/横向/inline、层级、单选、展开、图标、禁用、分组/分隔符、命令及明暗主题；P2 完成多选、手风琴展开、折叠 Tooltip、横向溢出和子菜单主题/模板。选择不自动等于页面导航。
- **后续与边界**：横向菜单的溢出项保持稳定 key 和命令；容器项不参与可选择 key。避免复刻网页双重渲染收集树结构的过程。大型菜单若需搜索/虚拟化，先限定支持层级和容器模式，不能默认全树预创建。
- **性能与生命周期**：子菜单首次打开才生成；key 索引随集合变化更新，选中变化只更新受影响路径。延迟任务可取消，反复进入/离开不积累 timer；卸载后关闭全部子 Popup 并解绑命令/集合事件。
- **验收**：①方向键/Enter/Esc、热键和焦点返回符合原生菜单语义；②禁用/分组/分隔符不被选择，多选取消事件与绑定一致；③折叠再展开保留有效 OpenKeys，集合删除后清理无效 key；④水平溢出后激活隐藏项仍执行同一命令、正确高亮祖先。

<a id="pagination"></a>

## pagination — Pagination

- **来源与官网事实**：[Pagination](https://ant.design/components/pagination/) / [快照](../analysis/official/pagination.md)。当前页/页大小、大小切换、跳转、总量/范围、简单模式、紧凑页项、单页隐藏、响应式和自定义页项；官网默认 pageSize=10、可选 10/20/50/100。
- **WPF 契约**：`AntPagination`：TotalCount(long)、PageIndex（从 1 起）、PageSize（正整数）、PageSizeOptions、ShowSizeChanger、ShowQuickJumper、IsSimple、HideOnSinglePage、ShowLessItems、Size、ItemTemplate、TotalTemplate；只读 PageCount、RangeStart/End；PageChanged 携带最终页号/大小/原因。
- **首批 P1**：前后/首尾/数字/省略号，页大小、快速跳转、总数范围和禁用。TotalCount=0 时 PageIndex=1、PageCount=0、Range=0–0，导航禁用；TotalCount 非负，PageSize 不能为零。默认大小选择显示可按官网 >50 的阈值资源配置，也可由用户明确覆盖。
- **后续 P2**：简单模式、窗口宽度适配、自定义页项和对齐。页大小变化保持首条记录的可见位置并钳制最终页号，此策略写进文档；TotalCount 缩减只产生一次修正通知。控件只发出请求，不加载数据。
- **性能与生命周期**：页数再大也只生成有界页项窗口（首尾、当前附近与省略号），复杂度与可见页项数量相关；使用溢出安全整数运算，不枚举全部页码。
- **验收**：①0、1、恰好整页及十亿条总数的范围和 PageCount 正确；②改变 PageSize/缩减 TotalCount 后页号合法且只通知一次；③跳转输入空值、非数、负数和超上限有确定行为；④绑定回写不引发重复请求或无限事件回环。

<a id="steps"></a>

## steps — Steps

- **来源与官网事实**：[Steps](https://ant.design/components/steps/) / [快照](../analysis/official/steps.md)。当前步从 0 开始，wait/process/finish/error；横竖、标题位置、图标、进度环、dot/inline/navigation/panel 样式、filled/outlined 及 maxCount 折叠省略。
- **WPF 契约**：`AntSteps`：ItemsSource、CurrentIndex、InitialIndex、Orientation、TitlePlacement、DisplayMode、Variant、Size、Percent、MaxVisibleItems、IsResponsive；项 Key、Title、Subtitle、Content、Icon、StatusOverride、IsEnabled；StepInvoked/CurrentChanged。
- **首批 P1**：基本横/竖、四状态、标题/内容/图标、当前步与点击事件；只显示流程进度，不擅自运行下一步业务。P2 加入进度环、dot/navigation/inline/panel、响应式方向和 MaxVisibleItems。
- **后续与边界**：StatusOverride 优先于根据 CurrentIndex 推导状态；被省略范围不作为可激活步骤，需提供可读范围说明。点击是否切换由明确的绑定/命令决定，未完成步骤的业务准入由宿主校验。
- **性能与生命周期**：当前步变化仅更新旧步/新步及其状态区间；大量步骤启用 MaxVisibleItems 后仅实现可见窗口。进度动画在未显示/关闭动画时停止，不每步常驻计时器。
- **验收**：①CurrentIndex=0、末尾正常；负值编程参数拒绝，集合缩减导致越界时钳到末项，空集合显示无当前项且只通知一次；②显式 Error 不被自动 Finish 覆盖，禁用步骤不能激活；③百分比 0/100 与尺寸变化无几何错误；④MaxVisibleItems≥3 时首尾与当前可达，省略项不接收误导航。

<a id="tabs"></a>

## tabs — Tabs

- **来源与官网事实**：[Tabs](https://ant.design/components/tabs/) / [快照](../analysis/official/tabs.md)。line/card/editable-card、图标、尺寸、四边、指示器、额外内容、溢出菜单、添加关闭、惰性呈现、隐藏销毁；拖动重排为示例组合能力。
- **WPF 契约**：`AntTabs : TabControl`：TabStyle、Size、TabPlacement、HeaderTemplate、IndicatorTemplate、ExtraStart/End、IsAddButtonVisible、AddCommand、CloseTabCommand、OverflowTemplate、ContentLifetime；项 Key、Header、Icon、IsClosable、IsEnabled；TabClosing 可取消、TabClosed、SelectionChanged。保留 SelectedItem/SelectedIndex/ContentTemplate。
- **首批 P1**：line/card、图标、禁用、四边与键盘导航、溢出可达。P2 完成 editable-card、添加/关闭、指示器配置、额外内容及内容生命周期。关闭操作先走可取消请求，再由数据源确认移除；不直接删除业务记录。
- **后续与边界**：拖动重排 P3，先限定同一控件内；外部拖出成窗口另行设计。内容默认首次选中才创建；KeepAlive/UnloadOnHide 明确区分 UI 状态保存和 VM 生命周期，不静默清理尚未保存内容。强制预创建为显式选项且说明内存代价。
- **性能与生命周期**：未选中内容不因头部动画被重新实例化；KeepAlive 只暂停库自身拥有的动画/订阅，可发页面激活通知由宿主决定业务行为，不擅自解绑用户VM订阅；用户选择保留的内容不能被隐式 LRU 销毁。大量标签头使用有界溢出，关闭项解除库缓存、容器和事件引用。
- **验收**：①关闭当前项后按规定选择相邻有效项，取消关闭不改变集合；②惰性页在首次选中前不执行视图初始化，KeepAlive 与 UnloadOnHide 重建行为不同且可预测；③Ctrl+Tab/方向键、禁用和溢出菜单均可访问；④反复切换、关闭、主题变化不重复订阅或重复运行页面初始化。

<a id="affix"></a>

## affix — Affix

- **来源与官网事实**：[Affix](https://ant.design/components/affix/) / [快照](../analysis/official/affix.md)。在指定滚动容器中达到顶部/底部偏移后固定，状态变化回调；文档指出容器监听范围与横向滚动定位的限制。
- **WPF 契约**：`AntAffix`/附加行为：Content、ScrollTarget、Placement(Top/Bottom)、Offset、只读 IsAffixed、AffixedChanged。只允许一种主定位边，避免同时 Top/Bottom 的歧义。
- **首批 P3**：在明确 ScrollViewer 的可用视口内固定，保留原始占位，滚动回来恢复。需跟踪横/纵滚动、目标 Resize/DPI，避免悬浮内容脱离本窗口或拦截其他区域。
- **后续与边界**：没有 ScrollTarget 时绑定最近明确宿主，找不到时保持普通布局并给诊断，不全应用搜索。固定效果依靠容器内呈现/Adorner，不反复将同一 UIElement 在两个父级间重挂。
- **性能与生命周期**：仅订阅自己的滚动容器，位置更新一帧合并一次；不逐帧扫描祖先树。停用、换目标和卸载清除 Adorner/占位、解绑事件并恢复内容。
- **验收**：①跨 Top/Bottom 阈值 IsAffixed 只在变化时通知；②横向滚动、缩放与 Resize 后对齐正确且不超出宿主；③小视口不覆盖关键操作，内容可以键盘访问；④移除滚动容器/反复启停后视觉树没有遗留副本或事件引用。

<a id="app"></a>

## app — App

- **来源与官网事实**：[App](https://ant.design/components/app/) / [快照](../analysis/official/app.md)。为 Message、Notification、Modal 提供携带当前配置上下文的统一使用入口；官方要求注意 ConfigProvider 与 App 的顺序，通常无需嵌套多个 App。
- **WPF 契约**：`AntHost : ContentControl` 对应 App 职责；包含普通内容、模态/抽屉层、Message/Notification 区域、反馈默认配置及 Owner 标识。通过面向该实例的 ShowMessage/ShowNotification/ShowDialogAsync 入口或等价命令访问，不依赖全局“当前窗口”。
- **首批 P0/P1**：每窗口独立宿主和反馈队列，绑定主题/语言/尺寸上下文；显示反馈时明确将请求路由到所属实例。P2 支持模态焦点栈、异步结果、Owner 关闭取消和嵌套流程。
- **后续与边界**：嵌套 AntHost 只用于明确的局部隔离，解析最近的有效宿主；宿主缺失时返回明确错误/失败 Task，不静默使用任意活动窗口。Popup 独立 HWND 与 HwndHost airspace 由架构约束处理，不能仅增大 ZIndex。
- **性能与生命周期**：消息数/等待队列受限，按 key 更新避免无限新增；计时队列仅在存在待关闭反馈时运行。宿主销毁取消全部等待、关闭相关 Popup、解除 Owner/Dispatcher 订阅，未完成异步结果有确定完成状态。
- **验收**：①两个窗口的主题、消息、模态与焦点互不串扰；②模态异步失败仍留在原宿主，成功/取消仅完成一次；③Owner 关闭后全部等待完成为取消且无窗口/内容强引用；④嵌套宿主和未找到宿主的行为符合文档，不发生后台线程访问 UI。

<a id="border-beam"></a>

## border-beam — BorderBeam

- **来源与官网事实**：[BorderBeam](https://ant.design/components/border-beam/) / [快照](../analysis/official/border-beam.md)。6.4 起提供沿容器边缘的装饰光束，支持颜色/渐变、count、duration、lineWidth、outset、size；渐变百分比为 0–100，尾部保留透明衰减。
- **WPF 契约**：`AntBorderBeam : Decorator` 或附加 Adorner；BeamBrush/GradientStops、BeamCount、Duration、LineThickness、Outset、BeamLength、CornerRadius、IsActive、IsHoverOnly；不改变被装饰内容的输入和命令接口。
- **首批 P3**：单光束、圆角边界、纯色、渐变、可配时长/长度/线宽/外扩；实现多束相位均匀分布。输入值要求有限非负，Duration>0、BeamCount 正整数且设置合理上限，错误值不产生无限动画。
- **后续与边界**：任意复杂路径边界作为后续扩展；裁剪容器使用 Outset=0 或明确裁剪行为。高对比度/关闭动画时显示静态边框，纯装饰层不参与 hit-test、不作为唯一错误或状态提示。
- **性能与生命周期**：只改变绘制或 RenderTransform，不在每帧重建 Geometry 或 InvalidateMeasure；缓存轮廓，仅在尺寸/圆角变化时更新。卸载、不可见、IsActive=false 时完全停止时钟；多束共用一个进度时基。
- **验收**：①不同长宽比和圆角下沿边界连续，无跳角或裁掉输入内容；②多束 Count、渐变端点、Outset 与 BeamLength 按定义工作；③动画关闭与高对比度下仍有静态边框；④100 个不可见实例没有活跃帧更新，鼠标点击始终到达内容。

<a id="config-provider"></a>

## config-provider — ConfigProvider

- **来源与官网事实**：[ConfigProvider](https://ant.design/components/config-provider/) / [快照](../analysis/official/config-provider.md)。配置语言、RTL、尺寸、禁用、主题、输入变体、空态、弹层目标、虚拟滚动和组件默认值；静态反馈方法的上下文局限在官网 FAQ 中有说明。CSP、class 前缀等属于 Web 专用配置。
- **WPF 契约**：`AntThemeScope/AntConfiguration` 的继承依赖属性与资源字典：ThemeMode/Seed、ControlSize、Language/CultureInfo、FlowDirection、InputVariant、MotionEnabled、EmptyTemplate、PopupPolicy 及必要的组件默认样式。控件本地值优先于最近作用域默认值；资源按 WPF 优先级覆盖。
- **首批 P0**：Light/Dark、尺寸、语言、方向、关闭动画、动态主题和嵌套覆盖；弹层显式继承所属作用域。P1/P2 增加组件 token 覆盖、输入变体、空态模板与受控主题派生。全局禁用采用 WPF IsEnabled 语义，祖先已禁用时子项不能通过局部默认值重新启用。
- **后续与边界**：不移植 CSP nonce、DOM/CSS class 前缀、React hook 或静态配置 singleton。虚拟化作为具体集合控件可验证的能力，不用一个全局开关声称所有布局均能虚拟化；弹层宽度配置也不应意外关闭 WPF 虚拟化。
- **性能与生命周期**：主题按作用域共享一次派生/资源快照，局部覆盖只重算受影响层；避免每个控件每次 Measure 都解析颜色。UI 资源切换在 Dispatcher 上合并提交，无全局静态事件持有窗口；配置已失效的 Popup 不残留旧主题。
- **验收**：①父 Light/子 Dark/单控件显式 Brush 的优先级正确；②主题/尺寸切换不重建输入控件，不丢失 IME、选区和选择；③Popup、Message、Modal 使用 Owner 的语言与主题；④两个窗口配置互不影响，禁用与 FlowDirection 符合 WPF 语义。

<a id="_util"></a>

## _util — Util

- **来源与官网事实**：[Util](https://ant.design/components/_util/) / [快照](../analysis/official/_util.md)。这里的公开能力主要是 TypeScript 工具类型：`GetRef` 取得 ref 类型；`GetProps` 提取组件/Context/属性对象的类型；`GetProp` 提取非空单个属性，还支持 `Return` 提取函数返回类型。不是需要渲染的控件或一组浏览器运行时函数。
- **WPF 契约**：不创建假的 AntUtil 可视控件，也不以反射复刻 TypeScript 类型运算。对应开发体验由公开具体 .NET 类型、可见依赖属性标识、强类型 EventArgs/配置对象、泛型集合及 XML 文档完成；公开子控件的可操作对象需有稳定 .NET 类型。
- **首批 P0/P1**：每个公开控件提供明确的属性类型/默认值/可空性、DependencyProperty 字段与事件参数类型；自定义模板需要的 PART 名称和接口有文档。XAML 命名空间映射、IntelliSense 和编译期发现是工具契约的一部分。
- **后续与边界**：只有确有重复的类型安全需求才增加泛型辅助函数；不提前暴露内部主题对象或容器生成器。模板部件不存在属于合法重新模板化情形，需要安全降级，而非通过通用反射访问内部私有成员。
- **性能与生命周期**：普通属性、模板、选择和渲染路径不使用属性名字符串反射、动态调用或逐帧类型扫描；真正需要反射的设计时工具独立于运行时控件热路径并有缓存/范围限制。
- **验收**：①使用公开子控件/事件参数的消费代码在 net48/net8 两端编译且不依赖 internal 类型；②XAML 错误属性可在编译期定位，可空属性与 XML 文档一致；③自定义模板缺失可选部件不崩溃；④在性能采样中运行时交互不出现通用反射属性访问链。

## 覆盖与开放项

本文件覆盖 23/23 个分配入口，并对官网重要子能力明确分配首批/后续批次。尚需实现前原型确认的项目是 Flex Shrink/Basis 的精确分配、Masonry 大数据虚拟化、复杂多 Popup 层级、Tabs 内容保留成本，以及 BorderBeam 的边界绘制预算；这些设计要求不是已经通过的性能结论。

本次未执行官网逐示例交互、WPF 实现或性能测试；官网新版本和浏览器排版行为仍需在具体组件开发时按快照差异复核。
