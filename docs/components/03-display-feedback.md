# 数据展示与反馈组件实现要求

范围：Ant Design 6.6.8 的 Data Display 21 项、Feedback 11 项，共 32 个组件入口。资料检查日期：2026-10-29。官方 Markdown 快照在 [official](../analysis/official/)，来源与 SHA-256 在 [manifest.json](../analysis/official/manifest.json)。以下“官网分析”来自每项官方示例、API 和 FAQ；“WPF 要求”及类型名称是本项目设计，不表示官网已有 WPF 实现。

所有控件共同遵守 [框架设计](../architecture/README.md) 和 [性能要求](../architecture/performance.md)。目标为 `net48` 与 `net8.0-windows`；尺寸采用 DIP，主题采用类型正确的资源，公开属性保持 WPF 绑定与命令语义。首批为 P1，核心 1.0 为 P2，扩展为 P3；推迟交付的能力仍须保留清晰范围，不能默认为已支持。本文验收项为后续实现的门槛，尚未执行。

<a id="avatar"></a>

## avatar — Avatar

- **官网分析**：[官方文档](https://ant.design/components/avatar/) 提供图片、图标、文字头像，圆形/方形、文字自动缩放、响应式尺寸、错误回退及 Avatar.Group；Group 可限制可见数量并通过溢出弹层显示其余项。
- **WPF 契约**：`AntAvatar` 为轻量内容控件，提供 `Source`、`FallbackContent`、`Shape`、`Size`、`Gap`、`AlternativeText` 和 `ImageFailed`；`AntAvatarGroup` 提供 `ItemsSource`、`MaxVisibleCount`、`ItemTemplate`、溢出内容模板。明确 Source→回退图标/文字的优先级，避免重复错误重试。
- **功能要求**：P1 实现裁剪、系统字体、长文字适配、Badge 组合；P2 实现 Group 的重叠、`+N`、RTL 和溢出 Popover。响应式取宿主可用宽度，按 DPI 选择合适图像分辨率；浏览器 `crossOrigin` 不照搬为控件属性。
- **性能/生命周期**：共享有容量上限的图像缓存，按显示尺寸解码；仅对文字/尺寸变化重新测量字体，不逐帧缩放文字；换 Source、卸载时取消旧加载并丢弃过期结果。
- **验收**：①图片 404 后只触发一次失败并显示回退文字；②同为 32 DIP 的一字/六字头像均不越界，200% DPI 不裁切；③10 人 Group、上限 4 时显示 4 项及正确 `+6`，弹层键盘可访问；④重复替换 Source 后旧图片不能覆盖新值。

<a id="badge"></a>

## badge — Badge

- **官网分析**：[官方文档](https://ant.design/components/badge/) 包含计数、溢出阈值、零值开关、红点、独立状态点、偏移、两种大小及 Ribbon；默认溢出计数为 99，状态有 success/processing/default/error/warning。
- **WPF 契约**：`AntBadge` 装饰内容，提供 `Count`、`CountTemplate`、`OverflowCount`、`ShowZero`、`IsDot`、`Status`、`StatusText`、`Offset`；`AntRibbon` 提供文字、颜色与 Start/End 位置。数字与任意内容模式分开判定，数字 0 才适用 ShowZero。
- **功能要求**：P1 实现计数、点状、状态、偏移和禁用动画；P2 实现 Ribbon。`Count>OverflowCount` 显示阈值加号；高对比度下仍有文字或形状区别；点击行为由内容或命令提供，装饰点不抢焦点。
- **性能/生命周期**：数字变化只更新徽标显示，不重新创建宿主内容；Processing 动画在隐藏、卸载、关闭动画时停止，Ribbon 不引入独立窗口。
- **验收**：①0 在 ShowZero=false 时隐藏、true 时显示，99/100 分别显示 99/99+；②切换红点不改变宿主按钮命中和焦点；③Start/End 在 RTL 正确反转；④隐藏 Processing 后无持续动画回调。

<a id="calendar"></a>

## calendar — Calendar

- **官网分析**：[官方文档](https://ant.design/components/calendar/) 包含月/年面板、全尺寸/卡片尺寸、日期内容与整格自定义、周数、有效范围、禁用日期、自定义头部和农历示例。农历是示例中自定义日期内容，不等于内建全球历法支持。
- **WPF 契约**：`AntCalendar` 共享 DatePicker 的日期面板实现，使用 `SelectedDate: DateTime?`、`DisplayDate`、`DisplayMode`、`DisplayDateStart/End`、禁用日期谓词、`DateCellTemplate`、`HeaderTemplate`、`ShowWeekNumbers`；区分 `SelectedDateChanged` 与 `DisplayModeChanged`。
- **功能要求**：P1 基础月视图/单选；P2 年/月切换、紧凑/全尺寸、周数与有效范围。自定义单元格只替换指定内容，保留日期语义与可访问性；日程事件来源由宿主提供；农历/事件跨日条带 P3，通过模板或专门扩展呈现。
- **性能/生命周期**：月面板仅创建当前可见日期格，单格模板不得扫描整年事件；事件预分组按日期查询；切月取消旧异步事件请求，今日更新不逐秒刷新全部单元格。
- **验收**：①2024-02 正确含 29 日、2025-02 不含；②跨年有效范围外不能鼠标或键盘选择；③中文/英文周起始和周数按 CultureInfo 规则；④切换面板不产生虚假的选中更改事件。

<a id="card"></a>

## card — Card

- **官网分析**：[官方文档](https://ant.design/components/card/) 包含 outlined/borderless、普通/小尺寸、标题/Extra/Cover/Actions、Loading、内部卡片、Tabs，以及 Card.Grid 与 Card.Meta（头像、标题、描述）。
- **WPF 契约**：`AntCard` 采用 HeaderedContentControl，增加 `ExtraContent`、`Cover`、`ActionsSource`、`Variant`、`IsLoading` 与各区域模板；Meta 用可组合内容模板，标签复用 AntTabs；Grid 作为卡片布局样式，不另造数据网格。
- **功能要求**：P1 标题、内容、边框、Cover、Extra、Loading；P2 Actions、Meta、内嵌 Tabs 和 Grid。Loading 不销毁已有业务数据；是否保留内容视觉树由显式策略决定，默认不因加载闪烁丢失输入状态。
- **性能/生命周期**：默认浅层 Border/Presenter，悬停优先改变 Brush/Transform，不给大量卡片永久加高成本阴影；Tabs 内容懒加载并有保留/释放选项；布局交给外部容器。
- **验收**：①无标题/无 Extra 时不残留空头部；②Loading 来回切换不丢失已绑定表单值；③Actions 的键盘顺序与显示顺序一致；④窄宽度及 200% DPI 时标题、按钮不重叠。

<a id="carousel"></a>

## carousel — Carousel

- **官网分析**：[官方文档](https://ant.design/components/carousel/) 支持滚动/淡入效果、自动播放、循环、拖动、箭头、四向指示点、自适应高度及点进度，并暴露前后切换回调和 GoTo/Next/Previous。
- **WPF 契约**：`AntCarousel: Selector` 提供 `SelectedIndex`、`IsAutoPlayEnabled`、`AutoPlayInterval`、`IsLooping`、`Transition`、`ShowArrows`、`DotPlacement`、`IsDragEnabled` 与导航命令；切换通知区分开始和完成。
- **功能要求**：P3 一次性实现基础导航和循环，自动播放、淡入/平移与拖动按能力开关交付。悬停/键盘焦点进入时暂停自动播放；用户可关闭自动滚动；动画关闭时仍有完整导航。空集/单项不启动计时，集合删除当前项后修正索引。
- **性能/生命周期**：只保持当前及相邻项的必要视觉内容；转场用 RenderTransform/Opacity；隐藏、窗口最小化、卸载后暂停计时和动画；快速导航明确取消旧动画，不排队无限转场。
- **验收**：①0/1/N 项索引、箭头与循环均正确；②自动播放期间悬停和获得焦点后暂停、离开后按策略恢复；③连续 Next 十次只落在最终目标且通知次序清楚；④卸载后不再切换或持有页面。

<a id="collapse"></a>

## collapse — Collapse

- **官网分析**：[官方文档](https://ant.design/components/collapse/) 支持多面板/Accordion、三种尺寸、边框/透明、图标位置、标题/图标触发、禁用、Extra、懒渲染、强制渲染与关闭销毁。
- **WPF 契约**：`AntCollapse` 管理有稳定 Key 的 `AntCollapseItem`，提供 `IsAccordion`、`ExpandedKeys`、`ExpansionChanged`；Item 提供 `Header`、`ExtraContent`、`IsExpanded`、`TriggerMode`、`IsExpansionEnabled`、`ContentLifetime`。
- **功能要求**：P1 Expander 换肤与单面板；P2 集合式 Accordion、多展开、额外操作区、首开懒建及关闭释放。Extra 中按钮点击不能意外折叠；隐藏箭头与 IconOnly 触发不允许形成无触发入口的配置。
- **性能/生命周期**：未展开内容默认不构建；保留/销毁明确且不重置数据模型；高度动画仅限有限内容，长列表内部保持有限视口；重新模板化解绑旧标题事件。
- **验收**：①Accordion 展开 B 后关闭 A，且 ExpandedKeys 与绑定一致；②禁用面板不能通过 Enter/Space 打开；③Extra 按钮正常执行但不改变展开；④关闭销毁模式下页面可释放，保留模式下编辑值仍在。

<a id="descriptions"></a>

## descriptions — Descriptions

- **官网分析**：[官方文档](https://ant.design/components/descriptions/) 是标签/内容详情布局，支持横向/纵向、边框、三种尺寸、标题/Extra、列数响应式、Item 跨列和填满当前剩余列。
- **WPF 契约**：`AntDescriptions` 使用 ItemsSource、ItemTemplate、`ColumnCount`、`Orientation`、`IsBordered`、`ShowColon`；Item 有 Label、Content、ColumnSpan、FillRemaining。断点只依赖可用宽度，跨列不得使用网页 CSS 字符串。
- **功能要求**：P2 实现详情表与列跨度；P3 响应式列数。长 Label/Content 允许换行，内容可为控件；纵向模式标签位于值上方。非法跨度限制在可布局范围并给出可诊断结果。
- **性能/生命周期**：列数改变时批量重排一次；Item 改值不重建所有条目；大量详情采用分页/折叠，默认不假装为虚拟化表格。
- **验收**：①3 列中 Span=2 与 FillRemaining 正确换行且边框连续；②横纵切换保持绑定值；③窗口跨断点时无溢出和递归测量；④长中文标签与空内容布局稳定。

<a id="empty"></a>

## empty — Empty

- **官网分析**：[官方文档](https://ant.design/components/empty/) 提供默认/简洁插画、自定义图片或内容、可隐藏描述及通过 ConfigProvider 配置统一空状态。
- **WPF 契约**：`AntEmpty` 提供 `Illustration`、`Description`、`ActionsContent` 与对应模板；组件通过主题/配置获取默认资源。内建图示用许可明确的轻量矢量，不能默认下载官网远程插画。
- **功能要求**：P1 支持图示、描述、操作区域及无描述模式。Table/Select/List 复用同一默认空状态；区分空数据、加载中、失败，避免把网络错误当成“暂无数据”。
- **性能/生命周期**：静态矢量可共享，不建立后台任务；无描述时对应布局区域折叠；用户自定义内容不由 Empty 访问网络。
- **验收**：①无 Description 不留文字间距；②全局空图示覆盖与局部覆盖优先级正确；③空表格中的重试命令可由键盘执行；④主题切换图示和描述保持可辨识。

<a id="image"></a>

## image — Image

- **官网分析**：[官方文档](https://ant.design/components/image/) 包含渐进加载、占位/进度、失败回退、独立/分组预览、自定义工具栏、缩放上限/步长、旋转/翻转、拖动、滚轮与预览焦点限制；6.6.0 增加滚轮开关。
- **WPF 契约**：`AntImage` 提供 `Source`、`PreviewSource`、`FallbackSource`、`IsLoading`、`LoadProgress`、`ImageFailed`、`IsPreviewEnabled`；`AntImagePreviewGroup` 管理稳定 Key/CurrentIndex。预览有 Open/Close/Previous/Next/Zoom/Rotate/Flip/Reset 命令与可定制工具区。
- **功能要求**：P3 实现加载及预览；源切换先清理旧请求。预览的 MinScale/MaxScale 必须有效且缩放有锚点，旋转后拖动边界重新计算；按实际 Owner 使用 AntHost，Esc/左右键可操作并关闭恢复焦点。默认不自动下载导出文件，导出由显式命令触发。
- **性能/生命周期**：公共图像加载器有 URI/解码尺寸/DPI 缓存键及字节预算；后台读取/解码后在允许时 Freeze 再切回 UI；限制像素尺寸和失败重试，释放流；仅预读当前相邻图。关闭预览释放全尺寸图、鼠标捕获和订阅；过期加载结果不得覆盖新图。
- **验收**：①A 慢加载切至 B 后只显示 B；②无效图和回退图都失败时终止回退链；③大图缩放/旋转/分组切换后边界与索引正确；④连续预览关闭 100 次不保留图像、窗口和事件；⑤屏幕阅读器可获取替代文本与工具命令名称。

<a id="list"></a>

## list — List（上游废弃）

- **官网分析**：[官方文档](https://ant.design/components/list/) 已明确弃用并计划下个主版本移除，推荐 Listy。旧 List 包含 Header/Footer、横纵 Item、Meta、Actions/Extra、分隔、分页、加载更多、响应式卡片网格及虚拟列表示例。
- **WPF 契约**：不新增一套以废弃名称绑定上游的永久集合内核。P2 提供基于原生 ListBox/ListView 的 `AntList` 兼容外观和 Item/Meta 模板，将数据渲染、分页、加载状态组合到同一个列表方案；新业务优先参考 Listy 要求。
- **功能要求**：保留列表条目的标题/描述/头像/操作插槽、Header/Footer、空态、分隔和大小；Pagination/Spin/加载更多由外部组合。卡片网格走 Grid/Card，与虚拟长列表分开配置；不宣称可以一对一映射全部 React API。
- **性能/生命周期**：绑定数据并启用 Recycling，有限视口；Actions 绑定当前数据而非捕获回收容器旧项；加载更多防重入、可取消，删除项与选择保持一致。
- **验收**：①1 万项纵向列表仅实例化可见区及缓存区容器；②容器回收后操作命令参数仍是当前项；③分页/加载/空态互不重叠；④旧 Meta 模板可组合出等价外观，且文档清晰推荐新方案。

<a id="listy"></a>

## listy — Listy

- **官网分析**：[官方文档](https://ant.design/components/listy/) 自 6.6.0 提供 itemRender、必需 rowKey、有限高度下虚拟滚动、分组、粘性组头、按像素/Item Key/Group Key 定位，并有拖拽排序和无限加载示例。示例使用方式不等于所有能力都有独立内建 API。
- **WPF 契约**：统一列表控件提供 `ItemsSource`、稳定的 `ItemKeyPath`/键选择器、`ItemTemplate`、`GroupDescription`、`GroupHeaderTemplate`、`IsVirtualizing`、`IsGroupHeaderSticky` 与 `ScrollToItem/ScrollToGroup`。同一数据项身份在排序/加载后保持稳定。
- **功能要求**：P2 非分组虚拟列表与按 Key 定位；P3 分组虚拟化/粘性头、变高项和拖拽/增量加载组合。定位支持 Start/Center/End/Nearest 及偏移，重复/缺失 Key 明确诊断；不默认把整个对象序列反射成字符串键。
- **性能/生命周期**：固定行高优先采用原生 VirtualizingStackPanel；分组需实际验证 IsVirtualizingWhenGrouping，粘性头单独轻量绘制。变高行估算与修正不允许对全体项调用 Measure；滚动回调节流并及时解绑。
- **验收**：①1 万项 ScrollToItem 能到目标且不先生成 1 万个容器；②分组后组头吸顶与按 Group Key 定位一致；③变高项动态变化不导致持续跳动或 O(N) 测量；④无限加载失败可重试且快速滚动不会重复请求同一批。

<a id="popover"></a>

## popover — Popover

- **官网分析**：[官方文档](https://ant.design/components/popover/) 是带标题和内容的浮层，支持 hover/focus/click、12 个方向、箭头、自动位移、受控打开和混合触发；公共弹层能力继承 Tooltip 家族。
- **WPF 契约**：`AntPopover` 提供 `PlacementTarget`、`Header`、`Content`、`IsOpen`、`TriggerMode`、`Placement`、`ShowArrow`、打开/关闭延迟与 `OpenChanged`。交互内容使用可获取焦点的 Popup，允许设置外部点击关闭和内容生命期。
- **功能要求**：P2 实现定位与触发；从锚点移动至浮层时不提前关闭；打开状态支持双向绑定，关闭来源可区分外部点击/Esc/命令/Owner 销毁。明确 Tooltip 与可交互 Popover 的焦点差别。
- **性能/生命周期**：锚点布局、窗口移动、滚动与 DPI 改变时合并重定位；只在打开时订阅必要事件，不对全局 LayoutUpdated 永久监听。Popup 桥接资源和 DataContext；Modal 打开时由宿主协调已有独立 HWND。
- **验收**：①屏幕四边与 150% DPI 下浮层在工作区内且箭头指向正确；②锚点→浮层移动不闪烁，Esc 关闭恢复焦点；③切主题与 DataContext 时已打开内容更新；④卸载锚点后 Popup 关闭且事件解除。

<a id="qr-code"></a>

## qr-code — QRCode

- **官网分析**：[官方文档](https://ant.design/components/qr-code/) 支持文本/文本段数组、L/M/Q/H 纠错、boost、图标、颜色、大小、quiet zone、Active/Expired/Loading/Scanned 状态、自定义状态及导出示例；网页 Canvas/SVG 是其输出技术选项。
- **WPF 契约**：`AntQRCode` 接收 `Value`/明确的分段值、`ErrorCorrectionLevel`、`BoostLevel`、`ModuleMargin`、颜色、图标、`Status`、`RefreshCommand`，绘制为 Drawing/位图。二维码编码选择经过许可和 `net48`/`net8.0` 兼容验证的成熟库，包版本在开始实现时固定。
- **功能要求**：P3 提供标准编码与状态层，不实现自定义二维码算法。WPF 以 4 模块 quiet zone 作为可扫描的项目默认值，明确与官网 marginSize=0 默认不同；自定义为 0 必须提示扫描可靠性。空/过长内容报告可处理错误，Expired 刷新由业务提供新值。
- **性能/生命周期**：值/编码参数改变才重编码，尺寸/主题改变只重绘；合并快速更新；缓存矩阵有容量上限。图标覆盖面积受限并建议较高级纠错，输出按整模块对齐；导出和图标读取不阻塞 Dispatcher。
- **验收**：①中文、URL、较长文本可由独立解码器还原；②四种纠错和段数组实现与所选库能力对应，不能静默拼接丢失编码语义；③低对比度/过大图标显示诊断，默认配置在多 DPI 下可扫；④Loading/Expired/Scanned 的覆盖层和刷新命令正确且键盘可用。

<a id="segmented"></a>

## segmented — Segmented

- **官网分析**：[官方文档](https://ant.design/components/segmented/) 是单选分段，支持横纵、Block、Round、三种大小、图标/纯图标、自定义项、动态选项和单项/整体禁用。
- **WPF 契约**：`AntSegmented: Selector` 提供 ItemsSource、ItemTemplate、SelectedItem/SelectedValue、Orientation、Size、Shape、IsStretched；项提供值、内容、图标、IsEnabled、ToolTip。复用 WPF 选择模型与 AutomationPeer，不复刻 HTML radio 的 name 属性。
- **功能要求**：P1 实现基础单选与禁用，P2 加入纵向/图标/圆角外观。方向键/Home/End 跳过禁用项；删除选中项后采用明确的相邻选择或空选择规则，不偷偷选到不可用项。
- **性能/生命周期**：少量选项可普通布局；选中底板动画用 Transform，不逐帧重测每项；快速选择取消旧动画，关闭动画不影响选中结果。
- **验收**：①鼠标、方向键和绑定设置得到相同 SelectedValue；②单个禁用项被跳过；③图标独占项仍有可读名称；④动态删除/重排后选择由稳定值保持。

<a id="statistic"></a>

## statistic — Statistic

- **官网分析**：[官方文档](https://ant.design/components/statistic/) 支持值、标题、精度、分组/小数分隔符、前后缀、Formatter、Loading 和数字动画示例；`Statistic.Countdown` 已废弃，`Statistic.Timer` 自 5.25.0 支持 countdown/countup，倒计时完成时触发 Finish。
- **WPF 契约**：`AntStatistic` 提供 Value、Precision、Culture、ValueTemplate、Prefix/Suffix、IsLoading；`AntStatisticTimer` 接收 `Mode`、`TargetTime/StartTime: DateTimeOffset`、`DisplayFormat` 与 `Finished`。Timer 不用 UI tick 次数累加时间，以绝对目标/起始时间计算当前显示值。
- **功能要求**：P2 数值格式化/单位；P3 Timer 与可关闭的插值动画。倒计时负值钳为 0 且 Finish 只触发一次；计时不自动执行业务操作；大整数或高精度值不得先转 double 再格式化导致丢精度。
- **性能/生命周期**：刷新频率按显示最小时间单位决定，普通秒级显示不使用 60 fps 计时；共享窗口级更新源并在不可见时暂停显示刷新。它是显示计时器：隐藏期间到期，在恢复后的首次计算补发一次Finished；Owner销毁不触发业务完成。需要准时业务调度由宿主负责。时钟读取在测试中可替换，不在静态全局藏不可控状态。
- **验收**：①中文/英文小数和分组正确且 decimal 不丢精度；②窗口卡顿 3 秒后显示直接追上真实时间；③结束事件一次、计数向上不错误触发 Finish；④卸载后 timer 无回调，重新加载无重复订阅。

<a id="table"></a>

## table — Table

- **官网分析**：[官方文档](https://ant.design/components/table/) 包含选择、过滤/搜索/排序（含多列）、分页/Ajax、固定表头/左右列/层叠固定、列隐藏与响应式、展开行/树数据/嵌套表、表头分组和单元格跨行列、编辑、行列拖拽、Summary、虚拟列表及按 Key 定位。功能组合比原生 DataGrid 提供的能力更广。
- **WPF 契约**：P2 的 `AntTable: DataGrid` 保留 ItemsSource、DataGridColumn、Binding、编辑事务和原生 UI Automation。增加稳定 `RowKeyPath`、`FilterDescriptors`、`SortDescriptors`、分页连接及 `QueryChanged`（请求原因和当前状态），而不是同时维护两套 Columns/ItemsSource 模型。列模板、行样式、表头过滤模板和 Summary 模板作为明确定制入口。
- **数据要求**：本地模式使用本控件专属 ICollectionView，并通过 DeferRefresh 合并更改，避免更改共享 DefaultView；远程模式仅发出筛选/排序/分页请求，业务负责 Task/取消/结果，不把 SQL/HTTP 内置进控件。状态包含 PageIndex、PageSize、TotalCount、SelectedKeys；刷新/跨页保留选中的规则显式配置。Key 缺失、重复或源中行被删除时有诊断与一致性处理。
- **核心与扩展能力矩阵**：

| 功能家族 | P2 核心 1.0 | P3 扩展与实现限制 |
| --- | --- | --- |
| 表格外观 | Header/Title/Footer、边框、三种密度、空态、Loading、列宽/最小宽/对齐、省略、Tooltip、动态隐藏 | 响应式列按控件宽度变化；完整自动列宽需避免测量所有行 |
| 筛选/排序 | 每列菜单/自定义过滤区、单/多条件、清除与重置、升降序/无序、多列优先级；本地/远程状态一致 | 树形过滤项与多字段搜索单独验收；关闭过滤层是否提交明确 |
| 分页 | 独立 Pagination 上下/对齐组合；空集、总数缩小、PageSize 改变时修正页码 | 服务端缓存/预读由业务拥有，控件不假装数据虚拟化 |
| 选择 | 单选/复选、全选/反选/禁用行、Shift 范围、受控 Keys、跨页保留规则 | 树表父子关联选择随树表一起交付；全选是当前页还是所有查询结果必须显式 |
| 编辑 | 单元格/行编辑、Enter 提交、Esc 取消、WPF 校验、异步保存结果呈现 | 防止排序/分页绕过未提交或验证失败的编辑；不替业务持久化 |
| 固定区域 | 有限视口、表头固定、左侧冻结列，使用原生 FrozenColumnCount | 右冻结、双侧冻结与层叠固定不是 DataGrid 现成功能；独立原型验证同步滚动、键盘、编辑与回收后再扩展 |
| 展开/树/嵌套 | 简单 RowDetails 按需呈现，可选择首开创建与保留/释放 | 树表用稳定键的可见节点投影；嵌套表限定高度，独立滚动；展开内容不能预建所有子表 |
| 分组/合并 | 普通单层表头，保留列模板 | 多层表头、colSpan/rowSpan 与虚拟化组合需单独布局；可限定为非虚拟小数据模式并公开限制，不可静默关闭虚拟化 |
| 拖拽 | 原生列重排、列宽调整，维护列身份 | 行排序/手柄、跨冻结区列移动独立实现；业务模式决定排序状态下是否允许拖拽，禁止同时给出矛盾顺序 |
| Summary | 以当前页/当前筛选结果/远程汇总三种明确数据范围显示，支持 Footer 模板 | 固定 Summary 与右冻结/合并一起验证；聚合增量或后台计算后提交结果，不逐帧扫描 |
| 虚拟/定位 | 行列虚拟化、Recycling、有限高度/宽度、ScrollIntoView 按 Key；固定行高为基线 | 可变行高、行详情、分组/树、合并的支持组合逐项列兼容矩阵与基准；不承诺任意组合等速 |

- **性能/生命周期**：渲染成本取决于可见行列与缓存区，不能因 1 万行数据生成 1 万容器；Auto 宽度不扫描全数据，属性取值/比较器可复用，不在 Measure/Render 中反射、LINQ 排序或取数据库。筛选输入可防抖，后台计算仅处理不可变快照，提交用请求版本抛弃过期结果。单行字段更新只刷新相应行/单元格；默认不使用每格阴影；主题切换不重设源和选择。清理列事件、CollectionChanged、查询任务与 Popup。
- **验收**：①1 万行×10列达到性能基线，实例化容器数随视口而不是总行数变化；②两个表共享 ItemsSource 时，一个过滤不改变另一个视图；③排序→筛选→分页及远程快速改查询后，最终状态/结果只对应最后请求；④验证失败编辑不能被分页偷偷提交，Esc 可撤销；⑤冻结列滚动、键盘定位和 150% DPI 对齐；⑥回收后勾选/行错误/展开不串到其他 Key；⑦P3 的每种组合有能力矩阵，无法支持的组合显式诊断；⑧Summary 的计算范围、跨页选择与总数变化可预测。

<a id="tag"></a>

## tag — Tag

- **官网分析**：[官方文档](https://ant.design/components/tag/) 包含可关闭、颜色/状态、filled/solid/outlined、图标、禁用、链接、动态增删、可选 Tag、CheckableTagGroup 单/多选与拖拽示例。
- **WPF 契约**：`AntTag` 提供 Content、Icon、Color/Status、Variant、IsClosable、Closing（可取消）与 CloseCommand；`AntCheckableTag` 用 ToggleButton 语义，Group 使用 SelectedValue/SelectedValues 与 SelectionMode。导航以 Command/显式 Navigate 请求表达。
- **功能要求**：P1 普通 Tag/关闭/状态；P2 可选组；P3 拖拽组合。关闭只发请求，集合由绑定源决定删除，避免控件直接修改未知集合；禁用时不触发关闭/选择/导航。
- **性能/生命周期**：大量标签用轻量模板；关闭动画结束后才释放视觉项，数据状态以 Key 为准；按下关闭按钮不得顺带切换选中；拖拽采用 Transform，结束提交一次重排。
- **验收**：①Closing.Cancel=true 时标签保留且无 Closed；②多选组键盘切换与绑定集合一致；③禁用链接标签不导航；④回收或重排后选中仍跟随 Key。

<a id="timeline"></a>

## timeline — Timeline

- **官网分析**：[官方文档](https://ant.design/components/timeline/) 支持纵横、Start/Alternate/End、filled/outlined、反序、逐项标题/正文/颜色/图标/Loading，标题占位和单项位置覆盖。
- **WPF 契约**：`AntTimeline: ItemsControl` 提供 Orientation、Mode、Variant、IsReversed、TitleExtent、ItemTemplate；项含 Header/Content/Icon/Color/IsLoading/Placement。反序通过视图而不是直接反转调用者集合。
- **功能要求**：P2 纵向、交替、反序和自定义节点；P3 横向和复杂标题宽度。终端连线按实际首末项调整，长正文换行不破坏相邻连线；Loading 节点可有完整文本状态。
- **性能/生命周期**：常规短时间线简单布局；大日志使用虚拟列表模板，不能为连接线生成跨整个数据集的巨大 Path。卸载/隐藏 Loading 动画停止，属性更新不重建所有节点。
- **验收**：①奇偶项交替与反序组合正确；②单项/空集没有多余首尾连线；③长标题、200% DPI 连线不穿过文字；④方向切换保留数据次序与操作焦点。

<a id="tooltip"></a>

## tooltip — Tooltip

- **官网分析**：[官方文档](https://ant.design/components/tooltip/) 支持 12 向定位、箭头、自动翻转/位移、彩色、延迟/触发及唯一提示复用；FAQ 明确不会持续监视所有外部布局变化，并提供 forceAlign 重新对齐。
- **WPF 契约**：优先原生 ToolTip 的主题样式，扩展提供 Placement/PlacementTarget、ShowArrow、InitialShowDelay、BetweenShowDelay、ShowDuration、IsOpen、内容模板与 `Reposition`；文本型 Tooltip 默认不接管焦点，交互内容交给 Popover。
- **功能要求**：P1 基础与键盘焦点提示，P2 完整定位、颜色和唯一提示策略。空标题不打开；禁用控件通过明确附加设置允许说明；已有工具提示更新规则清晰；外部布局变化可显式重定位。
- **性能/生命周期**：延迟期结束前不创建复杂内容，唯一 Tooltip 可窗口级复用；只在可见时监听锚点必要事件，合并重定位，禁止全局永久布局扫描；关闭/Owner 销毁后释放内容与计时。
- **验收**：①Hover/键盘 Focus 均可得到文本，空标题无空泡；②四边/DPI 切换正确翻转且不超工作区；③锚点异步移动后显式 Reposition 更新位置；④快速跨过 100 项不产生 100 个遗留 Popup 或 timer。

<a id="tour"></a>

## tour — Tour

- **官网分析**：[官方文档](https://ant.design/components/tour/) 提供模态/非模态引导、多步骤、目标定位/无目标居中、遮罩挖孔、Gap/圆角、禁用目标交互、自动滚入、方向、键盘、自定义动作/指示器及 Finish/Close。
- **WPF 契约**：`AntTour` 属于 AntHost，提供 StepsSource、CurrentIndex、IsOpen、IsModal、DisableTargetInteraction、步骤模板和 Next/Previous/Finish/Close 命令；Step 引用 FrameworkElement 或延迟目标解析器。区分完成与中途关闭通知。
- **功能要求**：P3；目标不存在/已卸载时按配置跳过或居中，不能崩溃或无限等待。自动滚入不破坏用户滚动；目标布局稳定后再定位；模态时合理设置焦点范围，允许目标交互时明确其与背景阻断范围。
- **性能/生命周期**：同一宿主只使用一层遮罩几何与一张引导卡；仅监听当前目标、合并重绘，切步骤解绑旧目标；自动滚动可取消，关闭后解除焦点限制和鼠标拦截；HwndHost 页面提示 airspace 限制。
- **验收**：①滚动后目标框与卡片对齐，125%/200% DPI 不偏移；②目标隐藏/删除时不挂起；③Finish 与 Close 各自只发一次且恢复焦点；④DisableTargetInteraction=true 时目标不可点击，false 时只开放允许范围。

<a id="tree"></a>

## tree — Tree

- **官网分析**：[官方文档](https://ant.design/components/tree/) 包含单/多选、父子关联/独立勾选、展开控制、异步加载、搜索高亮、拖拽、线/图标、目录模式、虚拟滚动与按 Key 展开定位；禁用节点会影响父子状态传播，不能仅把颜色变灰。
- **WPF 契约**：P2 `AntTree : TreeView` 面向小/中规模基础树，Key状态与SelectedItem单选对接。P3另提供 `AntVirtualTree`，以树状态→可见节点扁平投影→虚拟ListBox实现多选/大可见节点；不改变已发布AntTree基类、PART和自动化契约。两控件共享SelectedKeys、ExpandedKeys、CheckedKeys、HalfCheckedKeys的Key语义，明确各自模板和AutomationPeer。
- **功能要求**：P2 单选、展开、图标/线、勾选关联及取消关联、目录单击/双击展开、加载/失败/重试；P3 多选、拖拽和扁平虚拟树。叶子/未加载/加载失败明确区分；未加载子项的“全选”策略需明确，是继承意图还是仅当前已知节点。搜索默认高亮，可选隐藏不匹配并展开祖先；拖拽禁止循环父子关系与业务禁止位置。
- **性能/生命周期**：节点身份、勾选/展开不存容器；Keys 用哈希查找，关联状态增量更新祖先而非每次扫描全树；懒加载每节点防重入/可取消、过期结果丢弃。虚拟化保持有限视口；投影展开仅更新受影响区间。按 Key 定位先按需展开祖先，再定位，不能预生成所有节点。
- **验收**：①父子勾选、半选、禁用隔断与独立模式结果正确；②加载中收起/删除节点后旧结果不重新展开它；③按深层Key定位仅展开所需路径；④P3 AntVirtualTree的10k可见节点基准验证容器数与视口相关；⑤各批回收后展开/勾选不串项；⑥P3拖放节点到自身后代被拒绝且源不变；⑦升级后既有AntTree模板/SelectedItem契约不变。

<a id="alert"></a>

## alert — Alert

- **官网分析**：[官方文档](https://ant.design/components/alert/) 支持 success/info/warning/error、outlined/filled、标题/描述、图标、Banner、可关闭与关闭完成、自定义动作；ErrorBoundary 为 React 组件错误捕获专属封装。
- **WPF 契约**：`AntAlert` 提供 Severity、Variant、Header、Description、Icon、ActionsContent、IsBanner、IsClosable、Closing/Closed；关闭可取消并可绑定 IsOpen。ErrorBoundary 不映射为声称可吞掉任意 Dispatcher 异常的控件。
- **功能要求**：P1 类型、标题/描述、图标、Banner、关闭；P2 自定义动作/关闭动画。Banner 的默认警告语义与普通信息语义明确；长内容自动换行，屏幕阅读器按紧急程度通知。业务异常→错误内容由宿主处理，不暴露堆栈作为默认 UI。
- **性能/生命周期**：静态 Alert 不创建 timer；关闭动画仅作用自身，完成事件一次；隐藏后移出焦点序列；循环 Banner 示例需另组合 Carousel，不塞入基础 Alert。
- **验收**：①四种类型在高对比度下仍可区分；②关闭按钮可键盘操作且 Closing 可阻止关闭；③有无描述正确布局，长中文不裁切；④关闭动画中重复点击仅发一次 Closed。

<a id="drawer"></a>

## drawer — Drawer

- **官网分析**：[官方文档](https://ant.design/components/drawer/) 支持四方向、默认/大/自定义尺寸、可拖动尺寸/MaxSize、标题/Extra/Footer、Loading、遮罩、关闭按钮位置、键盘、焦点管理、关闭销毁、预渲染与多级 Push。
- **WPF 契约**：`AntDrawer` 由 Owner 的 AntHost 管理，提供 IsOpen、Placement、PaneSize、MinPaneSize/MaxPaneSize、IsResizable、Header/Extra/Footer、IsLoading、IsModal、CloseOnEscape/Mask、ContentLifetime 与 Closing/Opened/Closed；拖动通知只表达尺寸，不复刻 DOM mount 节点。
- **功能要求**：P2 四方向/尺寸/遮罩/焦点/表单；P3 拖动调整与嵌套 Push。关闭默认保留数据，视觉内容保留/释放显式配置；未保存表单可拦截关闭；Modal 模式的背景滚动/点击被阻止，非模态不错误抢占整个窗口。
- **性能/生命周期**：进出用 RenderTransform，不逐帧重排整个窗口；调整尺寸按帧合并，控件上限包含可用窗口尺寸；嵌套栈维护焦点和遮罩层级。Owner 关闭取消未完成任务并释放内容；Popup/HwndHost 不能靠 Panel.ZIndex 保证覆盖。
- **验收**：①四方向在小窗口和高 DPI 下不超有效区域；②Esc/遮罩/关闭按钮触发一致关闭策略，表单拦截有效；③嵌套 Drawer 只关闭顶层并恢复正确焦点；④拖动到上限/取消捕获时尺寸稳定；⑤反复开关无残余遮罩、timer 或订阅。

<a id="message"></a>

## message — Message

- **官网分析**：[官方文档](https://ant.design/components/message/) 提供成功/错误/警告/信息/Loading、默认 3 秒消失、duration=0 常驻、同 Key 更新、关闭等待、悬停暂停和最大数量/堆叠；推荐带上下文的调用方式，静态方法已标记弃用。
- **WPF 契约**：每窗口 `AntHost.Messages` 提供 Show/Update/Close/Clear；返回消息句柄（Key、Close、Closed Task），MessageOptions 包含 Content、Severity、Duration、PauseOnHover。后台调用在宿主 Dispatcher 投递，调用者必须能明确选定 Owner。
- **功能要求**：P1 常用消息和 Loading→结果更新；P2 堆叠/容量规则。常驻消息有显式关闭途径；同 Key 更新覆盖旧内容并按文档规则重置剩余时间；达到 MaxCount 时淘汰规则稳定，关闭理由可区分到时/用户/容量/Owner。
- **性能/生命周期**：遵循单窗口3可见/20等待预算；Duration在进入可见态后开始，排队更新不消耗余时；同Key更新保留同一Closed Task并重置其可见计时，已关闭后复用Key创建新代次。淘汰等待项以Evicted完成句柄；窗口级调度到期，Task只结算一次。Owner销毁全部以OwnerClosed完成，点击默认不抢键盘焦点。
- **验收**：①默认到时消失、0 常驻、悬停暂停后余时继续；②同 Key Loading→Success 只占一项且关闭等待一次；③超过容量按规则淘汰；④两个窗口消息互不串窗，关闭 Owner 后所有句柄完成。

<a id="modal"></a>

## modal — Modal

- **官网分析**：[官方文档](https://ant.design/components/modal/) 包含标题/正文/自定义 Footer、信息/确认变体、异步确认、Loading/Skeleton、遮罩/blur/closable、焦点限制与关闭恢复、Esc、宽度/居中、销毁/预渲染和 body 滚动锁定；静态方法存在上下文继承限制。
- **WPF 契约**：`AntModal` 为 AntHost 内的可模板化对话内容，提供 IsOpen、Header/Content/Footer、DialogWidth、IsCentered、IsConfirming、IsLoading、IsClosable、CloseOnEscape/Mask、ContentLifetime。Owner 级 ShowAsync 返回明确结果；确认处理接受 CancellationToken 并返回异步结果，失败保留窗口和错误内容，不能 async void 吞异常。
- **功能要求**：P2；默认按钮有本地化文本，确认/取消/关闭来源明确，确认期间防重入；支持 Footer=null 与自定义按钮。打开移入焦点，Tab/Shift+Tab 不逃逸，关闭恢复原有效元素；多层模态按栈，只允许顶层接收输入；内容销毁与业务 VM 清空分开，不能借销毁偷偷重置绑定数据。
- **性能/生命周期**：无必要不创建独立 Window/Dispatcher；动画只操作遮罩 Opacity/内容 Transform，默认不使用整窗实时 Blur；只创建实际打开的内容。打开时协调原有 Popup 的独立 HWND，不能把 zIndex 当跨 HWND 保证。Owner 关闭取消确认、关闭全部对话并完成等待；HwndHost airspace 另列受限场景。
- **验收**：①输入→提交失败仍打开且错误可读，重试成功返回一次结果；②确认连点不重复执行，Owner 关闭能结束等待；③Tab/Esc/遮罩规则及原焦点恢复正确；④两层 Modal 与 Dropdown 组合不出现浮层穿透；⑤隐藏销毁与保留策略各自保持约定状态，反复开关无内存引用残留。

<a id="notification"></a>

## notification — Notification

- **官网分析**：[官方文档](https://ant.design/components/notification/) 是带标题/描述/图标/操作的通知，默认 4.5 秒、可常驻、六方向、同 Key 更新、可关闭、堆叠、MaxCount、关闭进度和悬停暂停，包含 alert/status 的屏幕阅读语义。
- **WPF 契约**：`AntHost.Notifications` 采用有界窗口级管理，Options 含 Key、Header、Content、Icon、Actions、Placement、Duration、ShowProgress、PauseOnHover、AnnouncementPriority，返回可更新/关闭/等待的句柄。
- **功能要求**：P1 基础通知；P2 方向、堆叠、关闭进度。可交互通知不强制夺焦点，可通过键盘导航抵达；关闭/内容点击/动作按钮独立；默认是应用内通知，不等同 Windows 系统 Toast，也不把通知当后台任务持久存储。
- **性能/生命周期**：遵循单窗口3可见/20等待及Message相同的Key/计时/Closed Task规则；进入可见态开始Duration，淘汰等待项以Evicted完成，Owner关闭以OwnerClosed完成所有句柄。正文受可用高度限制；进度按显示需要更新并共享调度；焦点或悬停暂停自动关闭，关闭后解除库订阅。
- **验收**：①六方向和 RTL/窄窗口排布不出工作区；②同 Key 更新与超容量淘汰一致；③动作按钮不意外触发整个通知的点击关闭；④紧急/普通通知以不同优先级宣布且关闭进度与暂停余时一致。

<a id="popconfirm"></a>

## popconfirm — Popconfirm

- **官网分析**：[官方文档](https://ant.design/components/popconfirm/) 是锚点附近的轻确认，支持标题/说明/图标、确认/取消文字及按钮配置、可隐藏取消、条件触发、12 向/自动位移及异步关闭。
- **WPF 契约**：复用可交互 Popover 的定位与主题桥接，提供 ConfirmCommand/CancelCommand、异步确认入口、IsConfirming、ShowCancel、IsOpen、Confirmed/Cancelled/Failed。禁用确认层与禁用目标动作分开，不能禁用层后直接执行危险动作。
- **功能要求**：P2；确认期间防重入、错误保留内容，成功才关闭；Esc 与外部点击按取消规则处理，不产生 Confirmed；锚点卸载或 Owner 关闭终止等待。轻确认不替代包含复杂表单的 Modal。
- **性能/生命周期**：只在首次打开创建内容，复用定位和订阅生命周期；Task 与锚点拥有同样关闭范围；点击外部是否允许取消执行中的业务需显式约定，不能仅隐藏 UI 就丢弃异常。
- **验收**：①一次确认只触发一次业务命令，失败可见并可重试；②Esc/外部点击只取消不误执行；③滚动锚点和屏幕边缘定位正确；④锚点删除时异步确认结束且无悬浮窗口残留。

<a id="progress"></a>

## progress — Progress

- **官网分析**：[官方文档](https://ant.design/components/progress/) 支持 line/circle/dashboard、成功/异常/普通/活跃、数值文本格式、成功子段、线帽、渐变、分段、尺寸、数值内外位置及仪表盘缺口。
- **WPF 契约**：基础继承 ProgressBar，扩展 `DisplayMode`、`Status`、`SuccessValue`、`ShowValue`、`ValueTemplate`、`StrokeThickness`、`StrokeLineCap`、`GapDegree/Placement`、`Steps`；保留 Minimum/Maximum/Value 及 RangeValue 自动化语义，不额外维护与 Value 冲突的 Percent 状态。
- **功能要求**：P1 线性/状态；P2 环形/仪表盘/成功段；P3 渐变/分段/数值位置。输入范围一致校正，NaN/Infinity 拒绝；零范围和极小尺寸安全；文字自定义不影响辅助功能暴露真实数值。
- **性能/生命周期**：环形优先单/少量 Geometry 绘制，不为每百分比生成元素；值更新只刷新绘制，Active 动画可关闭且不可见暂停；Steps 数量有合理上限，避免无限子元素。
- **验收**：①最小/最大/超界值与显示一致；②0/50/100 的环与缺口方向正确，成功段不超总值；③几百个静态进度控件不启动计时；④高 DPI 线帽/边缘清晰，键盘和屏幕阅读器获得正确范围。

<a id="result"></a>

## result — Result

- **官网分析**：[官方文档](https://ant.design/components/result/) 提供 success/error/info/warning 及 403/404/500 展示，含图标、标题、副标题、操作和可自定义内容。
- **WPF 契约**：`AntResult` 为轻量内容控件，提供 Status、Icon、Header、Description、ActionsContent、DetailContent 与各区域模板。数字状态只表示展示类型，不自动设置网络状态或执行页面跳转。
- **功能要求**：P2；提供许可明确的图示/图标和本地化默认文案；操作按钮由 ICommand 绑定，Details 可展开，长错误说明可读。默认不显示敏感异常堆栈或连接信息。
- **性能/生命周期**：静态状态不启动动画；复杂详情仅展开时构建；错误图示共享矢量，不默认请求网络；状态切换不泄漏旧命令订阅。
- **验收**：①七种状态资源与文字正确；②自定义 Icon/Actions 可以覆盖默认；③长说明/200% DPI 不截断主要动作；④无 Header/Actions 时不留空白区域且焦点顺序合理。

<a id="skeleton"></a>

## skeleton — Skeleton

- **官网分析**：[官方文档](https://ant.design/components/skeleton/) 包含 Title/Paragraph/Avatar、行数/逐行宽度、圆角、Active 动画及独立 Button/Avatar/Input/Image/Node 占位，可包裹真实内容按 Loading 切换。
- **WPF 契约**：`AntSkeleton` 提供 IsLoading、IsAnimated、ShowTitle/Avatar、ParagraphRows、ParagraphWidths、Shape、Content；独立占位通过共用的轻量形状控件/模板表达，任意 Node 用 ContentTemplate。
- **功能要求**：P2；真实内容与骨架的可见性和是否保留状态明确；骨架仅传达加载，不进入键盘顺序或被读作业务文本；Loading=false 时立即显示内容，动画关闭时保留静态占位。
- **性能/生命周期**：多数占位可一个 DrawingVisual/Geometry 绘制，避免每段创建独立 Storyboard；共享动画节拍，离屏/卸载停止；宽度比例改变按可用大小重绘，不订阅全局 LayoutUpdated。
- **验收**：①0/1/N 行和每行宽度分配正确；②Loading 切换不把输入状态错误清空；③关闭动画和高对比度下占位仍可辨；④列表中 100 个占位无 100 个独立永久 timer。

<a id="spin"></a>

## spin — Spin

- **官网分析**：[官方文档](https://ant.design/components/spin/) 支持三种大小、嵌入遮罩、说明、自定义图标、延迟显示、确定/自动进度与全屏模式。
- **WPF 契约**：`AntSpin` 提供 IsSpinning、Delay、Description、IndicatorTemplate、ProgressValue/IsIndeterminate、Size、Content；全窗口遮罩显式请求 Owner AntHost。是否阻止输入为独立属性，不以变灰暗示已阻断。
- **功能要求**：P1 基础/嵌入/延迟；P2 确定进度与宿主遮罩。短请求在 Delay 前完成时不闪现；多次开关用当前请求世代判定；Spin 不负责启动/取消业务任务，取消按钮由业务提供。
- **性能/生命周期**：旋转用 RenderTransform，共用可冻结几何；仅可见且活动时运行动画；延迟调度可取消，卸载后不会弹出；全窗口 Spin 与 Modal 层级由宿主管理。
- **验收**：①50ms 操作、Delay=200ms 时从不出现；②连续 true/false/true 只对应最后状态；③禁用输入策略能实际阻断鼠标和键盘；④隐藏/最小化暂停动画，恢复不丢失绑定进度。

<a id="watermark"></a>

## watermark — Watermark

- **官网分析**：[官方文档](https://ant.design/components/watermark/) 支持多行文字、逐行字体、图像优先、旋转、大小、间隔/偏移、层次，以及传递到 Modal/Drawer；onRemove 观察的是网页 DOM 移除，不是安全防护保证。
- **WPF 契约**：`AntWatermark` 包裹内容或使用附加装饰，提供 Lines/Text、ImageSource、Rotation、TileSize、Gap、Offset、Font、Opacity、InheritToOverlays。使用 IsHitTestVisible=false 的装饰层，宿主负责把水印上下文传给其窗口内弹层。
- **功能要求**：P3；图像失败回退文字或明确空态；多行字体可单独覆盖；不提供声称不可移除的防篡改接口，不把水印作为访问控制/内容防泄漏。截图和导出是否包含由宿主选择渲染路径并测试，不能默认保证。
- **性能/生命周期**：生成一次小尺寸 tile 后以 DrawingBrush/ImageBrush 平铺，不为每个格点创建 TextBlock；仅在文字、图像、DPI、字体、尺寸参数变更时重建；资源缓存有上限，加载图片可取消；不逐帧重绘静态水印。
- **验收**：①多行、图像优先和失败回退正确；②200% DPI 和窗口缩放后平铺无明显缝隙，正文操作不被遮挡；③InheritToOverlays 只影响同一 Owner 的 Modal/Drawer；④截图/打印各自行为有明示文档且不可作为安全保证。

## 范围结论

32 项均有独立官网来源、WPF 契约、功能/阶段、性能与生命周期、具体验收要求。需后续技术原型或依赖选型验证的重点是：DataGrid 复杂固定/合并/树表组合、变高分组虚拟列表、扁平虚拟树、Popup/HWND 与 HwndHost 覆盖限制、图片内存预算及二维码编码依赖的双目标能力。

未做上述 32 项的逐项浏览器交互/像素验证，本文不声称已测得运行时性能。未添加控件实现或测试；所有预算须在实现的基准宿主中测量。
