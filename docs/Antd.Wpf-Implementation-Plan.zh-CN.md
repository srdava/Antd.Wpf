# Ant Design 的 WPF 实现计划

分析日期：2026-10-29。参考站点：https://ant.design/ 。官网本次显示版本：6.6.8。

本轮修订：现代.NET最低提高为8，保留Framework4.8。逐组件详细契约以[79项组件索引](components/README.md)为入口；[整体架构](architecture/README.md)、[性能契约](architecture/performance.md)与[dotnet项目规范](engineering/dotnet-conventions.md)是后续实施基线。本文件保留阶段规划和历史REA证据，详细组件契约优先于下方概览。

实施已开始：M0基础代码和验证结果见[实施状态](implementation-status.md)。下方最初调查、规划与工作量保留为历史依据，不能把已建立骨架等同于所有组件或M0发布门槛已完成。

## 1. 建议结论与项目边界

建设原生 WPF 控件库 `Antd.Wpf`，以 Ant Design 的视觉、组件语义和关键交互为参考，采用同一份 C#/XAML 源码，多目标生成 `net48` 与 `net8.0-windows` 两套程序集，放入同一个 NuGet 包。新运行时宿主通过 NuGet 框架兼容规则选择对应资产，再经实际宿主验证。

优先复用 WPF 的样式、模板、命令、数据绑定、校验、选择模型与虚拟化。新增控件仅补充原生控件缺失的行为。库不依赖 WebView2、React、浏览器、运行时 Node.js，也不要求使用某个 MVVM 框架。

目标分为两层：

- **首个可用版本**：主题、基础输入、导航和常用反馈，可用于构建设置页、登录页与业务表单。
- **核心 1.0**：增加数据表格、树、完整日期时间选择、复杂下拉、弹层和发布质量保障。

官网组件目录本次列出73个核心入口，包含废弃项和配置/工具入口；已逐项形成实现规格。总览另外链接的6个Pro Components也有独立要求，但不计入核心1.0工期。AntV/Charts、Ant Design X等产品站不递归展开；本轮覆盖边界是官网组件总览的79项入口。

最初分析时工作目录为空；现已有规格、架构、规范及M0工程。实际编译、运行和未通过项分别见实施状态；原有静态调查报告不代表当前WPF验证状态。

## 2. REA 调查结果与证据边界

### 已观察

1. `capture_browser_scenario` 使用 Chromium 156 打开首页并等待页面稳定，返回了首页 DOM、可访问性树与内联 CSS 变量。DOM 为 598,066 字节，SHA-256：`7bc51ada8159cbb178decbb95fa21fa7fd3b74dd9466597a5793f1e6aab9819b`。
2. 观察到的样式基线包括主色、悬停/按下颜色、字号、尺寸、圆角、背景、边框和动画时长，详见第 4 节。
3. 可访问性树暴露了 textbox、checkbox、radio、switch、combobox、dialog、progressbar 等语义，说明实现范围应包含键盘与辅助功能，而不只是绘制外观。
4. 从该 DOM 中识别入口资源 `/umi.ea032418.js`，另行下载并用 REA 静态分析。下载文件为 13,310 字节，SHA-256：`98d297f9d13a07383fa351540f0cc7fd9ae44d0b0d38258180958329efd53f2a`。分析成功解析 1 个 JS 文件，未发生解析失败。

### 证据索引

| 材料 | 标识或文件 | 用途和限制 |
| --- | --- | --- |
| REA 完整静态父证据 | `ev_f5a81f792fe984846258ff7bad6983338f05fe9fde485e35838e09b9ae48c942` | 只覆盖下载的入口脚本，不能代表全站异步组件代码 |
| REA 静态摘要证据 | `ev_89cb75535251afe6a51805708cff0aac3742e4026897ab0ba76c9cd160c10ef0` | `authority=shipped-artifact`、`confidence=derived`；语义覆盖 unknown，存在 859 个语义未知项 |
| 可移植静态证据包 | [analysis/rea-evidence-bundle.json](analysis/rea-evidence-bundle.json) | 保存上述两条证据；不包含失败的浏览器调用 |
| 浏览器原始返回 | [analysis/rea-home-partial.json](analysis/rea-home-partial.json) | 完整保留失败返回中的 partial_observation，无成功 Evidence ID |
| 样式与范围摘要 | [analysis/rea-observations.json](analysis/rea-observations.json) | 从浏览器部分观察中提取的变量，不是全组件默认 token 定义 |
| 可访问性快照 | [analysis/home-accessibility.txt](analysis/home-accessibility.txt) | 首页当时状态，不证明点击、键盘和弹层生命周期正确 |
| 静态分析摘要 | [analysis/rea-static-summary.json](analysis/rea-static-summary.json) | 包含覆盖率、限制与原始 Evidence ID |

### 未证实与调查限制

- 浏览器调用完成导航和快照后，报告 `cleanup_incomplete`，内部执行失败为 `unreadable_output`。因此首页材料只能视为部分观察，不能描述成完整成功的浏览器场景。随后只读查询未发现命令行匹配 Playwright/REA/headless 的残留 Chrome 进程；这也不能反向证明所有临时资源已清理。
- 首页同时记录到一次 React #418 错误，可能影响该场景的解释，不能据此推断所有用户均发生错误。
- 未点击逐个组件、未测量真实动画曲线、未做截图逐像素比较，也未分析全部异步 JS 分块。入口脚本的静态结果不用于声称已还原组件内部实现。
- Button、Input、Select、DatePicker、Form、Modal、Table 的功能范围来自官网组件文档；主题层级来自官网主题文档。这些是官方文档信息，与 REA 观察分开记录。
- 以下 WPF 类型选择、API、排期、性能指标均为实施建议，不是从网站发现的 WPF 实现。

## 3. .NET Framework 4.8+ 与 .NET 8+ 兼容策略

| 项目 | 决策 |
| --- | --- |
| 包内 TFM | `net48;net8.0-windows`；不为每个新版 .NET 机械增加相同程序集 |
| Framework 宿主 | 4.8 为最低基线，4.8.1 宿主另做安装和启动验证 |
| 现代 .NET 宿主 | 8为最低基线，正式矩阵验证8/10的Windows WPF应用；9的兼容选择不等于已完成回归，不再支持6/7 |
| 尚未发布的未来运行时 | 仅设计上保持兼容，发布后增加宿主验证，不预先承诺实测支持 |
| 系统平台 | Windows 桌面。运行时兼容不代表自动兼容所有旧 Windows 版本 |
| UI 目标 | 不能以 `netstandard2.0` 代替 WPF 目标；暂不拆纯逻辑公共包 |
| 构建 | Windows 上的 SDK 风格项目、可固定版本的受支持 SDK、`UseWPF=true`；以 M0 真实双目标构建验证工具链 |
| Framework 引用 | 开发机安装 4.8 Targeting Pack，或使用仅参与构建的 `Microsoft.NETFramework.ReferenceAssemblies.net48`，不得把引用程序集当运行时依赖发布 |
| API 基线 | 公共行为取两端交集；日期用 `DateTime?`，时间用 `TimeSpan?`，异步用 `Task`/`CancellationToken` |
| 条件编译 | 仅对确实存在的运行时差异使用；避免形成两套控件实现 |
| 第三方依赖 | 核心优先零外部运行时依赖；每项新增依赖必须核对 net48 与现代 .NET 资产 |

项目核心属性示意，实施时需验证而非直接视为已构建：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net48;net8.0-windows</TargetFrameworks>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

现代 C# 语法与运行时 API 是两回事。可使用编译器支持的语法，但共享代码避免直接依赖 `DateOnly`、`TimeOnly`、`Task.WaitAsync` 等低版本缺少的 API，也不为语法便利添加大量兼容垫片。

.NET8是最低兼容目标；新应用推荐.NET10。按本次查询到的微软周期，.NET8将在2026-11-10结束支持，最低兼容承诺与运行时厂商支持周期分开管理。[微软支持周期](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)

## 4. 视觉与主题基础

首页 CSS 变量观察值与建议资源映射如下。数值是当前默认主题的参考值，局部示例可能覆盖 token。

| 语义 | 官网观察值 | WPF 建议资源 |
| --- | --- | --- |
| 主色 / Hover / Active | `#1677FF` / `#4096FF` / `#0958D9` | `Antd.Color.Primary*` 与对应 Brush |
| 正文 | `rgba(0,0,0,0.88)` | Brush 透明度转换为 ARGB alpha |
| 边框 | `#D9D9D9` | `Antd.Brush.Border` |
| 页面背景 / 容器 | `#F5F5F5` / `#FFFFFF` | `Antd.Brush.Background*` |
| 正文字号 | `14px` | 默认 14 DIP；优先 Segoe UI，中文按系统字体回退 |
| 小 / 中 / 大控件高度 | 24 / 32 / 40 px | 24 / 32 / 40 DIP，允许内容与无障碍需求扩展 |
| 基础圆角 / 边线 | 6 px / 1 px | `CornerRadius(6)` / `Thickness(1)` |
| 间距单位 | 4 px | 4 DIP，常用 8/12/16/24/32 |
| 快 / 中 / 慢动效 | 0.1 / 0.2 / 0.3 秒 | 100 / 200 / 300 ms，允许关闭 |

浏览器 CSS px 与 WPF DIP 都以 1/96 英寸为设计参照，但字体排版、抗锯齿与缩放实现不同，不承诺物理像素完全一致。启用布局舍入与边线对齐，并验证 100%/125%/150%/200% DPI。

主题采用以下最小结构：

1. 种子值：主色、字体、基础圆角、尺寸模式、是否启用动画。
2. 派生/语义值：文本、背景、边框、状态色与交互色；组件资源只覆盖必要差异。
3. WPF `ResourceDictionary`：为颜色、Brush、Thickness、CornerRadius、时长提供正确类型，避免在模板中到处转换。
4. 可变主题属性使用 `DynamicResource`；固定几何与模板结构可用静态资源。主题切换在 Dispatcher 上替换对应字典，不覆盖用户显式设置的本地值。
5. Light、Dark、Compact 为预设。首批提供固定且校准过的 Light/Dark 与尺寸资源；核心 1.0 再完成自定义主色的调色板派生。Dark 的颜色需依据官方主题/源码另行提取和验证，不能直接把 Light 取反。
6. 若移植官方颜色算法，固定上游版本并保留许可；校验蓝色、红色、自定义主色的明暗状态和对比度，不使用随意变亮/变暗代替算法。

图标使用 WPF `Geometry`/`DrawingImage` 及可换色矢量。首批只携带实际使用图标；多 path、双色、描边与 fill-rule 图标需转换验证。保留上游图标许可，不引入运行时 SVG 引擎或整套字体包。

## 5. WPF 实现结构与 API 原则

建议从一个库项目、一个 Gallery 示例项目和测试目录开始：

```text
src/Antd.Wpf/
  Controls/             # 原生派生控件、少量新增控件
  Themes/Generic.xaml   # 默认样式入口，按控件合并字典
  Themes/Tokens/        # Light、Dark、尺寸与语义资源
  Themes/Controls/      # 每个组件的 ControlTemplate/Style
  Icons/                # 校验过的矢量资源
  Properties/           # ThemeInfo、XAML 命名空间映射
samples/Antd.Wpf.Gallery/
tests/                  # 行为测试与 Windows UI 验证宿主
docs/
```

- **样式足够时只写样式**：如 Divider、Typography、基础 Progress；通过显式样式键提供原生控件换肤，不全局污染第三方控件。
- **需要属性或行为时派生原生控件**：例如 AntButton : Button、AntInput : TextBox、AntTable : DataGrid；继续支持原生 Binding、ICommand、DataTemplate 和 AutomationPeer。
- **缺失能力才新增控件**：例如 Pagination、Tag、Rate、InputNumber、Steps、Drawer、MessageHost。库控件优先可重新模板化的 Control，Gallery 页面可以使用 UserControl。
- 不建立强制统一的 `AntControlBase`：Button、TextBox、Selector 等需要保留各自继承链。真正复用的尺寸/状态可用少量附加属性；只在实际重复时提取。
- 模板公开必要的 `PART_*` 和视觉状态。OnApplyTemplate 重新应用时解绑旧事件，关闭/卸载时释放 timer、hook、订阅和鼠标捕获。
- 公开 API 用 .NET 命名：`IsLoading`、`IsChecked`、`SelectedItem`、`ItemsSource`、`Command`、`Placeholder`、`Size`、`Status`；避免逐项照搬 React props 和 DOM className。
- Button 优先提供 `Variant` 与语义色/危险状态的正交配置，旧式 Primary/Default 可作为明确映射的便捷入口，避免两套属性互相覆盖。
- 文本保留 TextBox 的 IME、撤销、选区和剪贴板行为；前后缀放入稳定模板，不因状态切换替换输入控件。
- Password 使用 PasswordBox 路径；不默认提供把明文密码长期保存在可绑定依赖属性中的便利封装。交接密码的生命周期由宿主业务明确处理。

## 6. 组件映射与交付优先级

P0 是基础设施，P1 是首个可用版本，P2 是核心 1.0，P3 是扩展批次。下表覆盖官网当前 73 个核心入口；同一行中的子能力可分批交付。

| 官网组件 | WPF 实现路径 | 批次与明确边界 |
| --- | --- | --- |
| ConfigProvider、App | 主题/语言配置与每窗口 AntHost | P0；负责主题上下文和反馈宿主，无 React Context API |
| Util | 仅按实际需求映射公开工具 | P3；不创建空工具框架 |
| Icon、Typography | Geometry/DrawingImage；TextBlock/FlowDocument 样式 | P1；省略、复制等行为按需求逐项补 |
| Button | 派生 Button，保留 Command/IsDefault/IsCancel | P1；状态、三种尺寸、图标、Loading；渐变/自定义波纹 P3 |
| FloatButton | 浮层按钮组，复用 Button | P3 |
| Divider、Space | Separator；支持 Gap 的轻量布局容器 | P1 |
| Flex、Grid、Layout、Splitter | 原生布局优先；24 列用 AntRow/AntCol；GridSplitter | P1 基本布局；Flex 换行/响应式断点 P2 |
| Masonry | 自定义 Panel | P3；独立评估虚拟化，不承诺普通 Panel 自动支持 |
| Menu、Dropdown、Tabs | Menu/ContextMenu/TabControl 的模板与少量行为 | P1；支持键盘、禁用、选中与命令 |
| Breadcrumb、Pagination、Steps | ItemsControl 或适当 Selector；独立分页状态 | P1 |
| Anchor、Affix | ScrollViewer 定位/Adorner 或滚动容器附加行为 | P3；限定宿主滚动范围 |
| Input | TextBox 模板，清除、前后缀、TextArea | P1；Password/Search P2，OTP P3 |
| Checkbox、Radio、Switch、Slider | CheckBox/RadioButton/ToggleButton/Slider | P1；保留三态、分组、键盘与辅助功能 |
| InputNumber | TextBox + RepeatButton，decimal?、范围、步长、CultureInfo | P2；处理空值、负号、小数中间态与粘贴 |
| Select、AutoComplete | 单选先复用 ComboBox；多选另用可复用的列表选择弹层 | P1 单选；P2 搜索、多选、标签、虚拟列表 |
| DatePicker、Calendar | 原生 DatePicker/Calendar 换肤，进阶面板组合 | P1 单日期；P2 Range/月/年/时间，周/季度/多选 P3 |
| TimePicker | TimeSpan? + 时间列表/编辑器 | P2；不依赖 TimeOnly |
| Form | FormItem 布局 + WPF Validation 与 INotifyDataErrorInfo | P1；不复制另一套表单状态仓库，异步业务规则由 VM 负责 |
| Cascader、TreeSelect | 共享 Popup 定位，分别实现层级/树选择 | P2；多选、大数据与延迟加载需单列验收 |
| Rate、Transfer | Selector/列表组合 | P3 |
| ColorPicker、Mentions | 独立交互控件 | P3 |
| Upload | 文件选择/拖放 + 文件队列 UI、状态与取消事件 | P3；传输由宿主提供，不内置云存储或上传协议 |
| Avatar、Badge、Card、Tag、Empty | 轻量 ContentControl/Decorator/ItemsControl | P1 |
| Collapse、Segmented | Expander；单选 Selector/RadioButton 组合 | P1 |
| Tooltip、Popover、Popconfirm | ToolTip/Popup 与公共定位、关闭、主题桥接 | P1 Tooltip；其余 P2 |
| Statistic、Descriptions、Timeline | 内容控件与 ItemsControl/布局 | P2 |
| Tree | P2 AntTree派生TreeView；P3独立AntVirtualTree采用可见节点扁平投影 | 状态按Key保存，不通过改变已发布AntTree基类来扩展大树 |
| Table | DataGrid 派生 + 表头筛选 + 独立 Pagination | P2；先排序/筛选/选择/编辑/左冻结列；树表、右冻结、合并单元格、拖拽分组 P3 |
| List（官网已废弃）、Listy | 原生 ListBox/ListView，统一虚拟化模板 | P2；以列表能力为契约，避免绑定到已废弃 Web 名称 |
| Image、Carousel | WPF Image、可选预览层；切换容器 | P3；解码失败/缓存/关闭释放需验证 |
| QRCode | 选择兼容两端的成熟编码库，WPF 绘制输出 | P3；不自行编写二维码编码算法 |
| Tour | 遮罩与目标定位，引导状态 | P3 |
| Alert、Progress、Spin | 内容控件/ProgressBar/旋转图标 | P1；动画可关闭 |
| Message、Notification | 窗口级队列与反馈宿主 | P1；计时、关闭、窗口销毁清理；不是系统通知 |
| Modal、Drawer | 同窗口 AntHost 遮罩层和内容呈现 | P2；焦点限制、恢复、异步确认、Owner 范围明确 |
| Result、Skeleton | 内容模板与占位形状 | P2；Skeleton 动画可关闭 |
| Watermark、BorderBeam | 可选 Adorner/绘制与装饰动画 | P3；水印不作为内容安全防护 |

Layout 的响应式断点以宿主控件可用宽度计算，不以主显示器像素宽度计算。24 列组件取名 AntRow/AntCol，避免与 WPF Grid 名称混淆。简单页面仍建议直接使用 Grid/DockPanel。

## 7. 必须提前解决的行为问题

### 弹层、模态与多窗口

- 每个窗口各有 AntHost 与反馈队列，必须显式识别 Owner，避免消息落到其他窗口。
- 小弹出使用 Popup/ToolTip；Modal/Drawer 使用窗口内遮罩。两者不能假设共享同一个 z-index：WPF Popup 是独立 HWND，要在打开 Modal 时关闭或协调已有 Popup。
- 显式桥接弹层的主题、语言、DataContext 和绑定来源，并校验 PlacementTarget、窗口移动、DPI 切换、屏幕边缘重定位。
- 模态激活时把焦点移入、阻止背景交互、限制 Tab 范围；Esc/取消/点击遮罩遵循配置，关闭后恢复到有效的原焦点元素。支持嵌套关闭的正确顺序。
- 异步确认执行期间防重入；成功关闭，失败保留对话框并显示可处理错误；取消及 Owner 关闭应结束等待，禁止 async void 吞异常。
- 对使用 HwndHost/WindowsFormsHost 的宿主说明 airspace 限制；这类页面如需跨 HWND 遮罩，应另验独立 owned Window 方案，不能宣称窗口内层总能覆盖。

### 表格、列表与表单

- 保留 DataGrid 的行/列 UI 虚拟化；外部不要再包破坏有限视口的 ScrollViewer。UI 虚拟化不等于数据虚拟化。
- 排序/筛选用独立 ICollectionView，避免改变共享源的默认视图影响其他控件；远程分页由宿主按请求事件加载，不由控件偷偷查询数据库或网络。
- Pagination 页大小改变或总数缩减时修正页码；明确空集时显示状态和是否触发请求，防止事件回环。
- Table 首批只保证 DataGrid 擅长的平面表。右侧固定列、树表、复杂跨行跨列合并与虚拟化组合需独立技术验证和排期。
- Validation 呈现错误与警告，Label 能正确关联输入；异步校验的取消、过期结果处理留在 VM/业务层，控件显示最终状态。
- 日期解析按 CultureInfo 和显式格式；处理闰日、跨年、范围顺序、空值和禁用日期。选择器不自动引入时区转换语义。

### 可访问性、性能和生命周期

- 延续原生 AutomationPeer；新增交互控件提供相应选择、展开、切换、范围值等 UI Automation 能力，不仅设置 Name。
- 所有操作可用键盘完成；焦点指示清晰，禁用/只读语义分开，高对比度与关闭动画模式有可用回退。
- 大表默认不逐单元格添加 DropShadowEffect；静态矢量可冻结；回收容器不得携带上一数据项的选中/错误状态。
- Timer、窗口事件、静态事件、Dispatcher 回调和 Popup 均要有关闭/卸载路径，避免反复打开页面后的泄漏。

## 8. 实施里程碑与工作量

以下为工程估算，按熟悉 WPF 自定义控件的开发者、常规代码评审与回归计算，单位为开发人日，不是已完成工作。默认 2 名开发，配合兼职设计/QA；团队经验、视觉精度与复杂表格需求会改变排期。

| 里程碑 | 内容 | 工作量 | 退出标准 |
| --- | --- | --- | --- |
| M0 | 双目标构建、NuGet 试包、Gallery、主题原型、Button/Input/Popup 技术切片 | 5–8 | net48与net8宿主从打包资产启动；主题/IME/焦点/150%DPI正常，建立PF采样 |
| M1 | 基础布局、图标、Typography、Button、基础输入、状态和主题资源 | 12–18 | 主要状态齐全，IME/键盘/焦点稳定，Light/Dark 可切换 |
| M2 | 常用导航、容器、FormItem、反馈与单日期/单选 | 15–22 | 表单页端到端完成；窗口关闭后无遗留反馈/弹层；形成首个可用版本 |
| M3 | 多选搜索、InputNumber、日期范围/时间、Modal/Drawer、弹层体系 | 18–28 | 异步确认、取消、错误、焦点恢复和多窗口完成回归 |
| M4 | Table/Tree/List、分页与数据性能；核心 P2 展示和反馈 | 25–40 | 平面表完整业务流、1 万行基准、绑定/资源/主题均通过 |
| M5 | 包安装、跨版本宿主、辅助功能、DPI、文档、许可与发布修复 | 10–15 | 包中资产正确；清洁消费工程通过；发布清单完成 |

首个可用版本 M0–M2 约 **32–48 人日**；核心 1.0 M0–M5 约 **85–131 人日**，2 人开发约 **12–18 个日历周**，含协作和回归余量。P3 全量扩展另预留 **60–120 人日**；高精度全组件/全状态对齐需重新估算，不包含在核心 1.0 承诺中。

依赖顺序：双目标打包 → token/主题 → 基础输入与选择 → 弹层/校验 → 复杂输入与表格 → 兼容和发布。Gallery 随每个里程碑增长，不等控件全部做完才补样例。

## 9. 验证与验收计划

### 兼容与消费验证

- 库的两套目标必须均编译；用实际 `.nupkg` 安装到独立的net48、net481、net8、net10 WPF消费工程验证资产选择、XAML编译、Pack URI和启动。
- Gallery的基础工程建net48/net8两端；较新宿主作为独立消费矩阵验证，不需要让库生成相同的新TFM资产。
- 验证现代 PackageReference 与现有 Framework 项目的接入路径；只在确定要支持 packages.config 时加入其消费样例。
- CI 使用 Windows runner；运行旧版 .NET 的测试宿主要明确安装对应运行时，不靠新运行时自动替代。x64 为全量 UI 基线，x86 至少验证安装/启动与关键行为；ARM64 按用户实际需求追加，不预先声称通过。

### 行为与视觉验收

- 每个组件至少覆盖 Default/Hover/Pressed/Focus/Disabled；输入另覆盖 ReadOnly/Error/Warning，异步行为覆盖 Loading/失败/取消。
- 分页边界、数值解析、日期范围、主题派生等纯逻辑做小而有区分力的测试；WPF 依赖属性、绑定、模板和 Dispatcher 行为使用 STA 宿主。
- UI 自动化重点放在弹层焦点、键盘选择、表单提交和表格编辑；不用大量截图测试替代行为测试。
- Gallery 固定数据、字体环境、尺寸和关闭动画后建立视觉基线，验证 Light/Dark、尺寸、中文/英文、100%/125%/150%/200% DPI；不对浏览器与 WPF 字体抗锯齿作逐像素等同要求。
- 使用屏幕阅读器与 UI Automation 检查新增控件的名称、状态和操作能力；高对比度与动画关闭后仍能辨认选中、错误及焦点。

### 性能门槛

统一以[性能契约PF01–PF10](architecture/performance.md)为准，涵盖首屏、滚动、输入、主题、树、弹层、队列、缓存、生命周期和回归阈值；M0记录机器与采样协议后冻结。目前均为拟定预算，尚未实测。

## 10. 发布和维护策略

- 首期一个核心 NuGet 包，内含两个运行时资产、默认资源与必要图标；不先拆十几个独立组件包。
- Gallery 和接入文档覆盖资源合并顺序、显式原生样式、主题切换、绑定、命令、验证、每窗口 AntHost 与异步确认。
- 公共 API 和资源键从首个预览版起维护变更记录；上游新增组件不自动触发本库 API 变化。
- 锁定设计参考为 6.6.8，后续通过版本化对照表吸收变化，不追随官网每天改变基线。
- 复用/移植 Ant Design 代码或图标时保留 MIT 版权与许可，图标分别核验来源；项目名称、Logo 和网站素材不默认作为可自由冒用的品牌资产。
- 使用官方 MIT 代码并不保证所有官网图片和字体都具有相同许可。默认使用系统字体，文档中标明社区实现及上游关联。

## 11. 建议首个开发任务

先完成 M0 的最小纵向切片：`Antd.Wpf` 双目标库 + Gallery + Light/Dark token + AntButton + AntInput + 一个单选 Popup 示例 + 真实 NuGet 消费验证。

这个切片用于尽早回答：同一份模板是否在两端加载、主题是否跨 Popup 生效、中文 IME 是否正常、DPI 和焦点是否稳定。它通过后，再按 M1/M2 扩展；不要先批量生成空控件类。

## 12. 官方资料与结论来源

以下文档在 2026-10-29 检查；本地部分观察与静态证据见第 2 节。

- [Ant Design 官网](https://ant.design/)：当前版本与设计系统入口。
- [组件总览](https://ant.design/components/overview/)：73 个核心入口及废弃/新增标记。
- [主题定制](https://ant.design/docs/react/customize-theme/)：种子、派生、别名及组件 token，默认/暗色/紧凑算法、嵌套与动态主题。
- [Button](https://ant.design/components/button/)：按钮样式、颜色、尺寸、Loading 和状态范围。
- [Input](https://ant.design/components/input/) 与 [Select](https://ant.design/components/select/)：输入变体、IME/焦点相关边界、多选与虚拟化功能范围。
- [DatePicker](https://ant.design/components/date-picker/)：日期范围、面板、格式与禁用条件。
- [Form](https://ant.design/components/form/)：标签、校验与反馈的对齐范围。
- [Modal](https://ant.design/components/modal/)：异步关闭、内容及配置上下文。
- [Table](https://ant.design/components/table/)：排序、筛选、编辑、固定列、树表与虚拟列表的范围差异。
- [Ant Design 6.6.8 MIT 许可](https://raw.githubusercontent.com/ant-design/ant-design/6.6.8/LICENSE)：复用源代码的许可要求。
- [WPF 控件编写](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/control-authoring-overview)：优先使用样式、内容模型与模板，按需自定义控件。
- [WPF DynamicResource](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/dynamicresource-markup-extension)：运行时资源查找与依赖属性优先级。
- [WPF 控件性能](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/optimizing-performance-controls)：UI 虚拟化、回收及与数据虚拟化的区别。
- [.NET TFM 文档](https://learn.microsoft.com/en-us/dotnet/standard/frameworks)：net48、net481 与现代平台专用目标。
- [.NET 支持周期](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)：最低兼容版本与受支持运行时的区别。

已实现并构建运行M0基础控件，尚未完成全组件或全站逐示例交互验证。复杂表格、Popup/HWND与焦点、字体/DPI、性能及运行时支持周期仍需按实施状态跟踪，不能用局部通过替代完整验收。
