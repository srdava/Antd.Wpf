# Input Search 实施

本批对应input P2 Search及验收③，采用`AntInput.IsSearchEnabled=True`启用搜索，复用同一个TextBox编辑器和已有AntButton。默认采用官网中性图标按钮；显式设置`SearchButtonContent="Search"`映射主色文本按钮形式。完整Addon组合仍未实现，不把本切片描述为整个Input.Search完成。

2026-11-03追加：`SearchButtonContent`默认值从`"Search"`改为`null`，这是预览API默认外观变化；旧宿主需要文本按钮时显式设置内容。null使用Outlined/Default、Padding0与既有24/32/40最小宽高；Filled/Borderless/Underlined仍为Text。加载时隐藏图标内容模板，仅显示既有Spinner，避免双图标或宽度跳动；清除Loading后恢复图标。非null（含空字符串）属于显式内容，不隐式更改宿主数据。

矢量路径直接适配官方SearchOutlined，保留1024×1024画布的留白与比例，随按钮FontSize/Foreground缩放换色。来源commit、快照SHA256见`official/styles-6.6.8/search-icon-manifest.json`；许可复用该目录`icons-LICENSE.txt`并随NuGet第三方声明打包。每次图标模板实例只创建Viewbox/Canvas/Path，无位图解码、转换器、事件订阅或新增时钟；普通Input未启用Search时不展开图标模板。

## 官方事实与平台设计

复用`official/manifest.json`的Input文档URL/hash。另保存官方6.6.8的Search.tsx和style/search.ts，来源及SHA256在`official/styles-6.6.8/search-manifest.json`。官方通过composition标记及loading守卫Enter，按钮和清除均可触发onSearch；Enter/Button共用input来源，clear来源独立。本项目将原因细分为Enter/Button/Clear，属于WPF接口设计。

官方搜索按钮高度来自controlHeightSM/controlHeight/controlHeightLG，文本enterButton使用primary/solid，Filled/Borderless/Underlined使用text；Filled按钮背景依次为colorFillTertiary/colorFillSecondary/colorFill。WPF复用24/32/40、4/6/8圆角和原有Fill/HoverFill/ActiveFill资源。组合内侧圆角为0，边框重叠1DIP，FlowDirection使用平台镜像，按钮内容和样式可替换。没有改写输入框Padding、字号、外框尺寸或文字绑定。

## API与所有权

| API | 行为 |
| --- | --- |
| IsSearchEnabled | 默认false；开启按钮和搜索键盘行为，关闭时取消活动请求 |
| SearchCommand | ICommand，CanExecute/Execute参数均为InputSearchEventArgs；执行前重新检查当前原因 |
| SearchRequested | 在命令之前发出同一个请求对象，可通过Cancel=True阻止命令分发；可只订阅事件而不设命令 |
| InputSearchEventArgs | Text为触发时快照，Reason为Enter/Button/Clear，CancellationToken关联该次请求 |
| IsLoading | 宿主拥有，不由控件强制写入；Loading期间屏蔽三种触发，隐藏清除入口并显示按钮加载态 |
| CanSearch | 只读，表示Button原因的可执行状态；Enter/Clear另检查各自CanExecute |
| SearchButtonContent / SearchButtonStyle | 自定义按钮内容及Style/ControlTemplate，不替换输入编辑器 |
| CancelSearch / Esc | 取消当前令牌并释放控件拥有的CTS；可重复调用，不改写宿主Loading绑定 |

仅在已加载、启用、搜索模式且非Loading/组合输入/同步分发的状态接受请求。单行且编辑器有键盘焦点时Enter发一次；组合期间及ImeProcessed不触发，AcceptsReturn=True保留原生换行，按钮仍可显式搜索。清除先执行原生Clear并恢复焦点，再以空文本发Clear原因；只读/禁用不能清除，普通Undo或VM更新Text不隐式搜索。按钮保持原生Tab/Invoke语义。

CanExecute的按钮可用性查询使用CancellationToken.None，调用方应只查询Text/Reason等状态并保持短小、无副作用；实际分发才创建带令牌的请求。CanExecuteChanged使用WPF弱事件，替换命令解除旧订阅；通知应遵守与WPF按钮一致的UI线程约定。重模板化解除旧按钮Click，可选搜索按钮缺失时Enter仍可用。

控件最多持有一个CTS。新接受请求会取消旧令牌；退出搜索模式、更换命令、Unloaded、Esc和CancelSearch都会取消。取消前清除字段，允许回调再次取消；取消回调改变启用状态时会再次检查，避免继续分发新请求。事件Cancel或命令抛错时释放请求并恢复同步分发守卫，异常交由宿主处理。宿主将IsLoading从true设回false表示完成，释放当前请求资源。

宿主负责异步任务、失败提示和结果应用，发布结果之前必须检查取消状态及请求版本；取消不保证宿主网络操作立即结束。库没有请求服务器、Task.Run、结果列表或隐式全文搜索。Gallery用短Task.Delay模拟请求，检查令牌和递增版本，旧回调不能覆盖新状态；取消后宿主清除Loading。

## 可访问性与验收

输入仍是原生Edit，子按钮仍提供Invoke。默认状态的搜索Loading由ItemStatus=Loading呈现；显式Status、Validation和超长错误保持原有优先级，恢复空闲后返回调用方状态。修改Loading或搜索模式时通知已有Peer，不强制创建Peer。

| Requirement | Evidence |
| --- | --- |
| 默认图标三尺寸/RTL、主题换色、文本切换与加载恢复、焦点/选区/编辑器保留及UIA名称 | `Input_SearchDefaultIconAndTextSwitchPreserveEditor` |
| 三种原因/快照、替换取消、绑定与Undo不受影响 | `Input_SearchReportsDistinctReasonsAndPreservesBindingUndo` |
| CanExecute、事件取消、同步递归防重入 | `Input_SearchCanExecuteEventCancellationAndReentrancyAreGuarded` |
| Loading屏蔽、Esc令牌、绑定保留、只读/禁用与UIA优先级 | `Input_SearchLoadingEscapeAndDisabledKeepHostBindings` |
| 当前文本和各原因独立命令检查 | `Input_SearchAvailabilityUsesCurrentTextAndSpecificTriggerReason` |
| 组合输入、单行/多行Enter分流 | `Input_SearchCompositionAndMultilineDoNotSubmitEnter`，合成事件，非物理IME验收 |
| 卸载/重载、模式切换、旧命令订阅、旧按钮解绑和可选部件缺失 | `Input_SearchLifecycleAndRetemplateReleaseOldRequestsAndButtons` |
| 三尺寸/组合圆角、RTL、主题、焦点及编辑器保持 | `Input_SearchSizesCornersAndThemePreserveEditor` |
| 非Outlined搜索按钮变体 | `Input_SearchNonOutlinedVariantsUseTextButtons` |
| 自定义按钮模板、内容绑定与原生Invoke | `Input_SearchButtonStyleAndContentBindingsPreserveNativeInvokeAndEditor` |
| 命令失败恢复、取消回调中关闭模式不继续分发 | `Input_SearchCommandFailureAndCancellationCallbacksCannotLeaveDispatchLocked` |
| 普通Input初始折叠按钮不展开子视觉树 | `Input_SearchInitiallyCollapsedDoesNotExpandButtonVisuals` |
| Gallery真实Dispatcher上下文中的取消/再次搜索/关闭清理 | `Gallery_SearchCancellationAndCompletionKeepLatestStatus` |

初始文本按钮切片定向16项通过，默认图标追加3尺寸用例后为19项。最初Gallery合成点击未经过Dispatcher，async回调缺少WPF同步上下文；测试改为从Dispatcher触发并保留取消、Loading恢复及最终文本断言后通过。完整双端、四个真实包消费和截图以`../m0-validation.json`为准。

## 性能与剩余验收

普通Input模板增加一个折叠AntButton节点，首次不展开其子模板；启用搜索后才生成按钮视觉内容，关闭后按WPF规则保留已生成模板。未为普通输入增加持续时钟；加载指示沿用AntButton已有的可见性/卸载/MotionEnabled动画清理。

文本变化只进行O(1)状态及CanExecute查询，参数持有原有字符串引用，不复制全文；命令可用性查询会分配小的参数对象，显式请求创建CTS和事件参数，不宣称零分配。没有全局Rendering/LayoutUpdated订阅、同步网络、全文过滤或新增依赖。PF02/PF05仍按既有代理回归，不等于搜索逐键时间/分配或硬件帧率预算通过。

默认图标形式、完整Addon组合、物理输入/IME、多DPI/高对比度动态切换、读屏实际事件、跨窗口长期泄漏及搜索硬件帧时间/GC仍待验收。

静态性能检查范围为AntInput.Search.cs与InputSearchEventArgs.cs（2个类中1个sealed，另一个保留AntInput扩展契约）。执行文化查找/前后缀/包含、Substring、同步阻塞/ValueTask、临时数组/stackalloc、string.Format、Regex、ContainsKey、LINQ、JSON、HttpClient、集合字符查找、全局事件/Timer及全文复制共15项检查，命中均0；原始计数在`artifacts/performance/input-search-static-checks.json`。复核分支/跨方法没有复合字符串分配，CanExecute查询的小参数分配保留为已知观察，未在无实测依据时引入缓存。

| Severity | Count | Top Issue |
| --- | --- | --- |
| Critical | 0 | 无已确认项 |
| Moderate | 0 | 无已确认项 |
| Info | 1 | CanExecute参数分配，需真实逐键采样评估 |

以上静态检查由AI辅助，可能漏报/误报，不替代运行采样与人工复核。连接角目前按默认4/6/8映射；自定义圆角token的组合裁切尚未验收，可通过整体模板定制。

## 最终验证

默认图标追加批次于2026-11-03 14:25完成`pwsh -NoProfile -File scripts/Verify.ps1`，exit0。Release零警告/错误，net48/net8各259/259、失败0、跳过0；其中Search各19项，新增`Input_SearchDefaultIconAndTextSwitchPreserveEditor`覆盖3尺寸。四个实际包消费宿主通过，14张截图生成，双端明暗矩阵已目视核对；包SHA256为`6D529BDED8EC5A6A56FA9E14856CE8B66BA90EC8960B8E1FA26A7A8FC69117B2`。PF02 Input p95为122.689/121.191ms、混合198.463/81.287ms，PF05为29.761/30.767ms（net48/net8）；精确原始值见`../m0-validation.json`。均为暂定布局代理，net48混合已接近200ms预算；未测独立Search逐帧/GC，不作性能改善声明。

2026-11-03 10:02完整`pwsh -NoProfile -File scripts/Verify.ps1` exit0。Release零警告/零错误，net48/net8各256/256，无失败/跳过；其中Search定向各16项。四个实际nupkg消费宿主net48/net481/net8/net10通过，14张截图重新生成，双端明暗Search/Loading矩阵已目视核对。74份源码hash与报告一致，包SHA256为`FB879C20164E9611D531074564867A8C9B20F7341ECDEC9B793CC755219BF06B`。

PF02 Input布局代理p95为95.52/92.50ms，混合为59.17/65.89ms，PF05为20.38/26.03ms，Divider为31.98/77.90ms（net48/net8），均在暂定代理预算内。net8 Divider较前轮升高，因此按性能契约在同一构建上追加两轮，每轮1/1测试、30个样本，p95为27.29和99.74ms；原始数据及源码hash摘要在`artifacts/performance/input-search-repeat/summary.json`。同构建波动仍明显，没有形成稳定对比基线，未据此宣布性能改善或完整PF门槛通过。
