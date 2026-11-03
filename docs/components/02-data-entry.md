# 数据录入：逐组件 WPF 实现要求

参考基线：Ant Design 6.6.8，2026-10-29 下载的官网 Markdown，原始文件位于 `../analysis/official/`，来源与 SHA-256 见该目录 `manifest.json`。本文件实际检查了下列 18 项的示例目录、公开属性、子组件与相关 FAQ；不声称逐个运行过浏览器交互。

“官网能力”是官方资料的摘要；“WPF 契约”和要求是本项目拟定实现，属性名可在 API 评审中调整，但功能、数据语义和验收条件不能静默删除。目标为 `net48` 与 `net8.0-windows`；双方使用相同公开数据语义。共用要求见[架构](../architecture/README.md)与[性能约束](../architecture/performance.md)。P1 为首个可用批次，P2 为核心 1.0，P3 为后续扩展；标为 P3 的能力仍有明确要求，不视为已实现。

所有输入统一保留 `IsEnabled`、只读、焦点、验证状态、尺寸、模板、主题和 UI Automation 语义。文本显示与已提交值分离，Binding 是数据唯一来源；程序更新、用户输入、预览和提交应可区分，不复制 React 的 `defaultValue`/`value` 双轨状态。弹层共用定位、Esc、Owner、主题和关闭清理机制，组件不得各建常驻全局事件钩子。

<a id="auto-complete"></a>

## auto-complete — AutoComplete

官方来源：[AutoComplete](https://ant.design/components/auto-complete/)，[本地快照](../analysis/official/auto-complete.md)。

**官网能力**：可自由输入的建议框，支持分组、自定义选项/输入、过滤、清除、键盘回填和虚拟列表。FAQ 区分文本改变、搜索和选项选中；空选项时即使 `open=true` 也不展示空弹层。

**WPF 契约**：拟用稳定 TextBox 加建议 ListBox；`Text:string` 双向绑定，`ItemsSource`、`ItemTemplate`、`DisplayMemberPath`、`IsDropDownOpen`、`IsLoading`、`AllowClear`、`Backfill`；`SearchRequested(text, requestId)` 与 `SuggestionChosen(item)` 分开，选项可以补全但不能强迫输入值来自选项。

- P1 支持自由文本、过滤/分组模板、可配置首项激活、上下键导航、Enter 选择、Esc 关闭、清除与前后缀；选项为对象时用展示字段，不直接调用任意对象的 ToString 作为唯一契约。
- P2 支持 TextArea 输入模板、外部异步建议和自定义弹层内容；Composition 期间更新编辑文本，组合完成后再搜索，点击建议不伪装成键入搜索。
- 性能/生命周期：有限高度的回收列表；Popup 宽度设置与虚拟化开关独立，避免照搬 Web 中“宽度不匹配就关虚拟化”的限制。搜索可去抖，宿主按 requestId 丢弃旧结果；卸载取消等待并解绑输入事件。
- 边界：默认空集合不弹出；Loading 和外部错误可显式展示状态，不把失败误报为“无匹配”。Backfill 是预览，Esc 恢复原编辑文本。

**验收**：①中文 IME 一次组合不会反复提交半成品查询；②键入 A 后立即键入 B，A 的迟到结果不覆盖 B；③上下键预览后 Esc 恢复，Enter 只提交一次；④10,000 项建议保持容器数随视口而非总项数增长，空集合不显示空壳弹层。

<a id="cascader"></a>

## cascader — Cascader

官方来源：[Cascader](https://ant.design/components/cascader/)，[本地快照](../analysis/official/cascader.md)。

**官网能力**：多列级联选择，支持点击/悬停展开、任意级提交、单/多选、父/子展示策略、字段映射、路径搜索、异步加载和独立 Panel。官方 `loadData` 与 `showSearch` 不组合；响应式标签数量有渲染成本。

**WPF 契约**：`ItemsSource`、稳定键与 `ChildrenPath`/模板；`SelectedPath:IReadOnlyList<object>` 或 `SelectedPaths`，路径记录完整祖先键；`ExpandTrigger`、`ChangeOnSelect`、`SelectionMode`、`CheckedDisplayStrategy`、`SearchText`、`IsDropDownOpen`；`ChildrenRequested(node, cancellationToken)`/对应任务完成由宿主负责。

- P2 支持逐列展开、禁用节点、叶节点提交、清除、键盘左右切列/上下选项、路径显示模板；多选分离内部勾选集合和标签展示集合，选择父节点时的传播规则必须固定。
- P2 搜索仅检索已加载路径；明确不自动遍历网络加载整个树。P3 若需远程全路径搜索，宿主返回完整路径结果，选中时再定位/加载祖先；Panel 与 Popup 复用同一选择模型。
- 性能/生命周期：只创建当前展开路径需要的列，每列按视口虚拟化；稳定节点键建立索引，避免每次键盘移动全树递归。懒加载按节点去重，折叠、换源、卸载取消或忽略旧任务；固定 MaxVisibleTags 默认优于自动重测所有标签。
- 边界：禁用节点及其可选后代策略须区分；重复键/循环父子数据返回可诊断错误；未加载节点不能被错误认定为叶节点。

**验收**：①三级路径选中后绑定保留三个键，展示模板改变不改变值；②勾选全部子节点时父标签策略正确，取消一个子节点不会清空其他选择；③展开同一节点两次只发一次有效加载，迟到结果不污染换源后的树；④搜索已加载数据不触发全树网络请求，Panel 与弹层行为一致。

<a id="checkbox"></a>

## checkbox — Checkbox

官方来源：[Checkbox](https://ant.design/components/checkbox/)，[本地快照](../analysis/official/checkbox.md)。

**官网能力**：单项、多选组、全选和半选显示，组/单项可禁用；Form 绑定的是 checked 语义而不是文本 value。

**WPF 契约**：派生/换肤 CheckBox，保留 `IsChecked:bool?`、`IsThreeState`、Content、Command；组提供 `ItemsSource`、`SelectedKeys`、`ItemTemplate`。`IsIndeterminate` 的视觉展示与 `bool?` 循环语义须明确，默认不让“全选汇总框”由用户循环进入 null。

- P1 支持键盘 Space、标签点击、焦点、禁用和双向状态；Group 支持独立项禁用、布局模板及选择集合通知。
- 全选仅修改当前约定范围内可操作项；过滤/分页时默认限当前可见结果，跨页全选由业务显式指定范围，半选由实际可操作项推导。
- 性能/生命周期：选中键集合使用增量更新；大组由宿主使用虚拟列表而非生成全部 CheckBox。容器回收时重新绑定 IsChecked，不保留旧项状态。
- 边界：禁用不等于只读；空组的全选显示 false。组值以稳定键保存，重复键拒绝或报告，不用显示文本识别项。

**验收**：①鼠标标签点击与 Space 均只改变一次值；②三项一禁用时全选只改可操作两项，半选计算正确；③VM 更新 SelectedKeys 会刷新界面且不触发重复业务命令；④回收滚动后选中项与对应数据一致。

<a id="color-picker"></a>

## color-picker — ColorPicker

官方来源：[ColorPicker](https://ant.design/components/color-picker/)，[本地快照](../analysis/official/color-picker.md)。

**官网能力**：HEX/RGB/HSB、透明度、预设、清除、颜色文本、自定义触发/面板、单色与线性渐变；分别提供连续改变和操作完成通知。FAQ 提醒字符串格式间反复转换可能损失精度。

**WPF 契约**：单色 `SelectedColor:Color?`，`Format`、`IsAlphaEnabled`、`Presets`、`IsDropDownOpen`、`ShowText`；连续 `ColorChanged` 与提交 `ColorCommitted` 分离。P3 渐变采用独立 `GradientStops` 与角度，不把单色属性在运行时变成任意对象。

- P3 首批完成单色、透明棋盘、色相/饱和度/亮度拖拽、HEX/RGB/HSB 文本输入、预设组、清除、禁用、键盘微调和自定义触发模板；随后实现渐变停靠点增删、排序与角度。
- 内部保留足够精度的颜色分量与色相状态，只在公开 WPF Color/显示文本边界量化；灰色切换时不丢失用户上次色相。格式切换只改变表示，不修改颜色。
- 性能/生命周期：色板绘制/缓存，不生成逐像素 UIElement；拖拽连续变化按渲染帧合并，操作完成单独通知；释放鼠标捕获，关闭后停止预览订阅。
- 边界：校验非法 HEX、通道范围、NaN 与透明值；系统 Color 为有限精度，文档说明往返容差；不把渐变误报为单色完全等价。

**验收**：①透明度 0 和 1 均可恢复并显示正确预设；②连续切换三种格式不产生可见颜色漂移；③拖拽100次仅完成事件一次，Esc 按规定恢复未提交预览；④渐变停靠点交叉排序后每个颜色仍对应正确位置。

<a id="date-picker"></a>

## date-picker — DatePicker

官方来源：[DatePicker](https://ant.design/components/date-picker/)，[本地快照](../analysis/official/date-picker.md)。

**官网能力**：日期、周、月、季度、年面板，范围、多选、时间、预设、禁用条件、格式/掩码、确认、空端点和自定义单元格。面板日期 pickerValue 与选中值区分；多选不支持 showTime；min/max 同时限制面板切换范围。

**WPF 契约**：`SelectedDate:DateTime?`、`DisplayDate`、`PickerMode`、`Culture`、`DisplayFormat`、`ParseFormats`、`MinimumDate/MaximumDate`、`IsDateEnabled`、`NeedsConfirmation`、`IsDropDownOpen`；Range 为 `StartDate/EndDate:DateTime?` 与端点空值策略；多选为稳定 `SelectedDates` 集合。分别通知预览范围、面板变化和提交值。

- P1 复用原生 DatePicker/Calendar 的单日期语义并换肤；P2 提供范围、月/年、时间组合、预设、清除、只读输入、前后缀、单元格/页脚模板和面板独立使用。
- P3 补周/季度、多选、掩码与替代历法。周起始日与周编号规则来自 Culture/显式规则，季度保存季度起始日期；不直接解析 Day.js 格式串，提供已说明的 .NET 格式。多格式输入以首格式展示。
- 值采用 DateTime 的日历/墙钟语义，日期选择不隐式转换时区。确认模式保留草稿，取消不写绑定；默认范围自动排序，需保留顺序的业务可选择显式策略。无效失焦默认显示校验错误并保留编辑文本，禁止悄然提交 Today。
- 性能/生命周期：日历只实现当前可见面板；禁用谓词不得每次布局全日期域扫描；键盘移动只检查邻近可见范围。共享时间列和弹层；卸载断开事件和时间相关更新。
- 边界：DateTime 范围、闰日、跨年、只有一个端点、禁用日期与部分时间、Culture 解析失败均有处理；范围 min/max 改变后显示无效状态并等待显式修正，不偷偷改 VM 数据。

**验收**：①2024-02-29 合法，2025-02-29 不提交；②范围先选结束再选开始时排序策略可预期，Esc 不改变原值；③中文/英文文化与不同周起始日显示一致；④面板翻页不触发 SelectedDateChanged，禁用日期不能由键盘绕过；⑤多选+时间组合被明确拒绝/禁用而非部分生效。

<a id="form"></a>

## form — Form

官方来源：[Form](https://ant.design/components/form/)，[本地快照](../analysis/official/form.md)。

**官网能力**：横向/纵向/行内布局、Form.Item 标签/帮助/反馈、字段联动、动态列表、嵌套路径、提交/失败、重置、定位错误、跨表单协作及同步/异步规则。warningOnly 不阻止提交；动态移除字段的数据保留策略可配置。

**WPF 契约**：`AntForm` 提供布局、标签宽度/对齐、RequiredMark、尺寸/只读/禁用上下文、`SubmitCommand` 与错误聚焦；`AntFormItem` 提供 Label、Target、Help、Extra、IsRequired、ValidationStatus、ErrorTemplate。字段值直接 Binding 到 VM，验证接入 `ValidationRule`、`IDataErrorInfo`、`INotifyDataErrorInfo`，不同时维护另一份 name→value 表单仓库。

- P1 实现 Item、标签换行、反馈图标、帮助与错误并存、键盘标签定位、统一布局；Error/Warning/Validating/Success 明确区分，Warning 不阻止宿主允许的提交。
- P2 提供错误摘要与 `FocusFirstError`/`ScrollToField`，动态 ItemsControl 字段、嵌套 VM、添加/删除/移动列表项的 Gallery 示例；复杂控件仅需正常 WPF 值绑定和验证，不要求反射猜测 value 属性。
- 重置、保存、跨字段依赖和跨表单通信由宿主 VM/Command 协调；异步规则必须有取消/版本号，过期结果不能覆盖新文本。Submit 在 pending 验证结束后由宿主作最终许可，失败不丢字段输入；库不隐式触发数据库/网络验证。
- 性能/生命周期：一个字段更改仅更新该字段及已声明依赖；禁止遍历可视树重新收集全表单值。字段注册/错误摘要使用明确生命周期，隐藏与移除分开，移除后解除订阅。
- 边界：数据是否保留由 VM 集合生命周期决定并写入示例，不通过控件卸载自动清空模型；验证状态呈现与业务提交规则分离，错误项未生成/虚拟化时允许宿主先实现该项再聚焦。

**验收**：①Checkbox/Switch 使用 IsChecked 绑定而非 Text，错误能定位对应输入；②动态列表移动后值和错误仍按稳定项键对应，删除后没有残留错误订阅；③前一次异步校验晚到不覆盖后一次，验证失败保持输入；④200字段页面编辑单项不触发200项重校验；⑤重置和隐藏/移除的保留策略与示例一致。

<a id="input"></a>

## input — Input

官方来源：[Input](https://ant.design/components/input/)，[本地快照](../analysis/official/input.md)。

**官网能力**：基础输入、TextArea 自适应、Search、Password、OTP、前后缀、清除、计数、长度、尺寸和变体。官方 FAQ 指出动态改变前后缀/计数结构可能丢失焦点；OTP 区分每次输入和全部填满事件。

**WPF 契约**：`AntInput : TextBox` 保留 Text、Selection、Undo、输入法与 Binding；新增 Placeholder、Prefix/Suffix、AllowClear、ShowCount、Variant、Status、Size；TextArea 以多行配置/模板实现，Search 提供 `SearchCommand`、IsLoading、搜索原因（Enter/Button/Clear）。Password 基于 PasswordBox，OTP 为独立有限长度编辑控件。

- P1 基础输入/多行、前后缀、清除、只读/禁用、三种尺寸、Outlined/Filled/Borderless/Underlined、计数和长度提示；切换装饰时保留同一编辑器、焦点、选区与撤销链。
- P2 Search 支持按钮模板、Enter及清除事件的明确原因、防重入与取消；TextArea MinLines/MaxLines 超出后滚动，不无限撑大父布局。
- P2 Password 支持显隐触发模板和可访问名称。默认不把明文密码存入可绑定依赖属性、日志、Undo 或普通长期字符串；若实现显隐转换，必须单独评审临时明文、剪贴板、失焦和卸载清理，不能直接复制到常驻 TextBox。
- P3 OTP 支持 Length、Mask、分隔符、粘贴分配、Backspace跨格、方向键、完整事件与部分输入事件；不承诺 Windows 原生短信自动填充能力。计数默认明确采用 UTF-16 单元，P3 可替换字素计数策略，不因视觉计数把合法组合字符截成半个。
- 性能/生命周期：字符计数线性于当前文本，避免每次 Measure 复制全文；中文组合期间不执行会重写 Text 的格式化。清除使用保持 Binding 的方式，解模板时解除按钮事件。
- 边界：绑定写入超长文本不会偷偷截断业务数据；显示错误由宿主处理。Search 的 Enter 不能抢走 IME 确认，TextArea 的换行与提交需区分。

**验收**：①中文输入、撤销/重做、选区和粘贴保留；②输入中切换Suffix/ShowCount不丢焦点或选区；③Search Enter/Button/Clear各只发一次相应原因，IME确认不误搜索；④OTP整串粘贴、超长粘贴、跨格删除和填满通知正确；⑤Password显隐/卸载无残留可视明文与日志。

实施记录：`AntPasswordBox`已开始交付原生掩码输入基础，SecurePassword返回调用方负责Dispose的副本，卸载清除；显隐及验收⑤完整矩阵仍未完成，见[Password实施](../analysis/password-implementation.md)。

<a id="input-number"></a>

## input-number — InputNumber

官方来源：[InputNumber](https://ant.design/components/input-number/)，[本地快照](../analysis/official/input-number.md)。

**官网能力**：范围、步长、精度、格式化/解析、键盘/滚轮、Spinner、只读、高精度 stringMode、失焦提交。官方区分显示字符串与真实数值，动态 min/max 不因变更而伪造用户 onChange。

**WPF 契约**：`Value:decimal?`、`Text`只作编辑态、`Minimum/Maximum`、`Increment:decimal`、`Precision`、`Culture`、FormatString/Parser、CommitMode、IsWheelEnabled、IsKeyboardEnabled；`ValueChanged`、`ValueCommitted`、`StepInvoked(direction, source)` 分离。

- P2 首批支持空值、负号/小数点中间态、区域小数分隔、清除、格式化、上下 RepeatButton、方向键、Home/End范围边界、只读/禁用和错误提示；鼠标滚轮默认不改值，只有焦点且显式启用才接受。
- 提交前可保留 `-`、`1.` 等中间字符串；合法提交才更新 Value。规定超范围时默认拒绝并保留错误文本；可显式选择 Clamp，但不得无提示改写绑定。VM设置越界值显示验证状态，不伪造用户 Step 事件。
- decimal 的约28–29位精度与有限范围写入文档。P3 任意精度需求另评审十进制字符串/BigInteger缩放路径及其完整运算规则；不会把官网 stringMode 默默实现为 double。
- 性能/生命周期：解析只在编辑/提交时进行，不在 Measure/Render 中做 Culture 格式化；长按仅有一个可释放重复计时器或原生 RepeatButton；关闭捕获/卸载结束长按。
- 边界：禁止NaN/Infinity、增量≤0、min>max和decimal溢出；解析失败、超精度按已声明策略处理；Parser/Formatter错误可诊断且不损失上一个提交值。

**验收**：①输入`-`再输入`0.10`不会提前变成0或丢小数；②以0.1递增十次准确得到1.0；③不同Culture的分隔符与千位格式可往返；④修改min使原值越界仅显示错误，不发用户变更事件；⑤最大decimal步进不溢出崩溃，禁用/失焦停止长按。

<a id="mentions"></a>

## mentions — Mentions

官方来源：[Mentions](https://ant.design/components/mentions/)，[本地快照](../analysis/official/mentions.md)。

**官网能力**：在多行文本中按一个或多个前缀触发建议，支持异步加载、过滤/合法搜索检测、选择插入分隔符、自适应高度、只读、清除与自定义弹层。

**WPF 契约**：稳定多行 TextBox，`Text`、`Prefixes`、`ItemsSource`、`ItemTemplate`、`Separator`、`IsLoading`、`IsDropDownOpen`；`MentionSearchRequested(prefix, tokenText, caretVersion)`、`MentionSelected(item, replacedSpan)`；存储默认是纯文本，不暗中转换成带隐藏ID的富文本。

- P3 首批识别光标所在词元，按前缀/空白界限触发；上下/Enter/Esc操作；只替换活动 mention 范围，插入规定分隔符并恢复光标；支持多前缀、无匹配与加载状态。
- 支持自定义提取/过滤规则和模板；若业务需保持用户ID/显示名引用，另交由显式富文本方案，不让纯文本控件伪称能无损保持实体关联。
- 性能/生命周期：输入时仅扫描当前候选词元附近，不每次重解析全文；建议列表回收。异步结果需同时匹配文本/光标版本，移动光标或离开词元即取消旧请求；滚动后按光标矩形定位并合并位置更新。
- 边界：IME组合不触发提前替换；邮箱中的@是否触发由明确规则决定；只读不弹出建议，Undo一次可撤销完整mention插入。

**验收**：①多行文本中间插入只替换当前词元，其余文本/选区不变；②请求途中移走光标，旧结果不能在新位置弹出；③IME输入和邮箱文本不违反触发规则；④选择后Undo一次还原完整输入，禁用/只读不触发搜索。

<a id="radio"></a>

## radio — Radio

官方来源：[Radio](https://ant.design/components/radio/)，[本地快照](../analysis/official/radio.md)。

**官网能力**：独立 Radio、Radio.Button 与组，水平/竖向/占满布局、单项/组禁用、大小、Outline/Solid按钮外观及值绑定。

**WPF 契约**：保留 RadioButton 的 IsChecked/GroupName；`AntRadioGroup` 以 ItemsSource、SelectedValue、SelectedValuePath、ItemTemplate、Orientation、OptionStyle、ButtonStyle 表示组。组的互斥作用域由实例隔离，不能由相同业务字段名造成跨窗口互斥。

- P1 支持圆形/按钮样式、选中/禁用/焦点、组值绑定、横竖排列和伸展；方向键在组内移动并选中，跳过禁用项，Tab 按可访问单选组行为进入/离开。
- 组内最多一项选中；用户再次点击当前项不默认清空。允许初始无选中；业务要求必选时由校验负责，不自动选第一项覆盖VM。
- 性能/生命周期：选中项改变只更新旧/新容器；数据源替换后按稳定值匹配；使用原生分组或实例作用域，不用全局静态列表保存所有Radio。
- 边界：重复值报告错误；删除选中项后明确置空并由宿主校验，分清选中状态与数据项的值。

**验收**：①两个窗口中相同组名不会互相取消；②键盘方向键跳过禁用项，Tab焦点合理；③VM更新SelectedValue仅有一个选中，重复点击不清空；④删除当前项后组值和校验一致。

<a id="rate"></a>

## rate — Rate

官方来源：[Rate](https://ant.design/components/rate/)，[本地快照](../analysis/official/rate.md)。

**官网能力**：评分数量、整星/半星、再次点击清除、只读、自定义字符、每项提示、尺寸、键盘与Hover通知。

**WPF 契约**：`Value:double`、`Count:int`、`AllowHalf`、`AllowClear`、`IsReadOnly`、`CharacterTemplate`、`Tooltips`；PreviewValue 与提交Value分离，提供 `ValueChanged`/`PreviewValueChanged`，UI Automation暴露范围值。

- P3 支持鼠标按星/半星选择、键盘方向键与Home/End、清除、只读、模板和每项描述；Hover离开恢复提交值，不写回Binding。
- 约束Count为正且合理上限，Value在0..Count，步长为1或0.5；对程序越界输入报告/约束策略固定，禁止接受NaN/Infinity。
- 性能/生命周期：少量绘制/裁剪几何复用，半星不需要复制整套复杂可视树；提示与事件在卸载后清理，不启用空转动画。
- 边界：自定义字符必须仍有可见焦点和评分描述；只读允许读取但不操作，不用仅禁用鼠标伪装只读。

**验收**：①3.5分半星点击和键盘等效；②Hover预览后离开不改变原Value；③AllowClear开启时再次点相同值归零，关闭时保持；④只读状态无法修改，屏幕阅读器能获知评分和上限。

<a id="select"></a>

## select — Select

官方来源：[Select](https://ant.design/components/select/)，[本地快照](../analysis/official/select.md)。

**官网能力**：单选、多选、自由标签、分组、多字段搜索/排序、远程检索、令牌分割、自定义选项/标签/显示、最大选择/标签数、虚拟滚动。FAQ指出自定义行高可能破坏滚动、label/value过滤不一致可能产生重复标签。

**WPF 契约**：单选优先 ComboBox 选择语义；多选/标签采用共享列表选择模型。`ItemsSource`、`SelectedItem/SelectedValue`、`SelectedKeys`、`SelectionMode(Single/Multiple/Tags)`、`SearchText`、`IsSearchEnabled`、`IsLoading`、`ItemTemplate`、`SelectedItemTemplate`、`TagTemplate`、`MaximumSelectionCount`、`MaxVisibleTags`、`IsDropDownOpen`；SearchRequested 与 SelectionChanged 分离。

- P1 单选、清除、禁用项、键盘、模板、无结果和尺寸/状态。P2 多选、键移除/Backspace、固定MaxVisibleTags摘要、分组、模板、搜索、异步结果、选择上限、隐藏已选项；选项对象与值/显示名严格分开。
- P3 Tags与分隔粘贴：明确按稳定值去重、Trim/大小写规则由配置固定，标签可不存在于ItemsSource；输入标签和已有选项重名时采用已声明的label/value匹配策略；响应式标签上限为可选昂贵能力。
- 性能/生命周期：有限高度虚拟列表与回收容器；自定义行高使用真实测量或明确固定ItemHeight，不能保留过期固定高度。键索引查找选择，标签只实例化可见上限；外部查询去抖且取消过期结果。
- 边界：Popup内按钮和Tag关闭不导致意外重开或丢焦点；搜索过滤视图独立于共享集合默认视图。数据刷新暂时缺失的选中键按明确策略保留显示快照，不偷偷丢失远程已选值；请求错误与Empty分开。

**验收**：①同名不同键选项可正确选择且标签去重按约定；②远程A请求晚到不覆盖B，移除已选标签不重开Popup；③10,000项、非默认ItemTemplate行高滚动定位和键盘选中正确；④达到最大选择数只阻止新增，仍可取消和清除；⑤主题切换不丢已选值/搜索文本/焦点。

<a id="slider"></a>

## slider — Slider

官方来源：[Slider](https://ant.design/components/slider/)，[本地快照](../analysis/official/slider.md)。

**官网能力**：单值、范围、多手柄、反向、竖向、刻度、Tooltip、整段拖动、动态增删手柄及逐手柄禁用；editable与draggableTrack不组合，存在禁用手柄时不允许editable。

**WPF 契约**：单值复用Slider，`Value/Minimum/Maximum:double`、`TickFrequency`、`Ticks`、`IsSnapToTickEnabled`、`Orientation`、`IsDirectionReversed`；范围/多点独立 `Values`，`IsTrackDraggable`、Min/MaxHandleCount、DisabledHandleKeys；`ValueChanged` 与 `ChangeCompleted` 分离。

- P1 单手柄、键盘、刻度/标签、Tooltip格式化、反向/竖向、禁用。P2双手柄范围与整段拖动，规定手柄相撞不交换身份、保持有序且不越界。P3多手柄、增删和逐点禁用，拒绝互斥配置而非部分执行。
- Step须>0；若只允许刻度，合法集合包含端点；不均分范围时端点仍可达。Tooltip可常显/隐藏/仅交互，不影响实际值。
- 性能/生命周期：拖拽由Thumb/Transform或轻量绘制更新，避免每次值改变重建刻度；昂贵业务由完成事件触发，连续事件不跨线程阻塞。卸载/失去捕获取消拖拽并结束监听。
- 边界：min=max显示固定值、禁止NaN/Infinity、零尺寸不除零；触摸/键盘和鼠标遵守相同范围规则。

**验收**：①键盘与鼠标均不能越过min/max，反向与竖向方向一致；②拖动整段保持间距且边界钳制正确；③一次拖拽连续变化多次、完成只通知一次；④动态编辑与整段拖动互斥被明确诊断，禁用手柄无法移动。

<a id="switch"></a>

## switch — Switch

官方来源：[Switch](https://ant.design/components/switch/)，[本地快照](../analysis/official/switch.md)。

**官网能力**：两态即时切换、选中/未选内容、大小、禁用与Loading；checked/value是相同布尔语义。

**WPF 契约**：基于ToggleButton，沿用 `IsChecked:bool?` 但固定两态（`IsThreeState=false`，null按未选处理），另有CheckedContent、UncheckedContent、Size、IsLoading；保留Command与Toggle自动化语义；公开一个状态值，避免Value与IsChecked互相竞争。

- P1 支持点击/Space、图标文本、两种官网尺寸、焦点与状态动画；Loading期间保留上一个已确认值并禁止重复操作，动画关闭仍能区分两态。
- 对异步保存，控件发请求/命令，宿主选择乐观更新或确认后更新并负责失败恢复；控件不自动写配置文件或网络。
- 性能/生命周期：仅拨钮位置与颜色变化，隐藏/卸载不运行loading动画；不为每个Switch建立持续计时器。
- 边界：不引入第三态；Loading与IsEnabled的组合保留可访问的忙碌状态，不把变灰当作唯一反馈。

**验收**：①点击和Space只切换一次，VM值同步；②Loading连点不重复发请求，保存失败可恢复原值；③关闭动画、暗色、高对比度时仍可识别开关；④只读/禁用策略不会修改值。

<a id="time-picker"></a>

## time-picker — TimePicker

官方来源：[TimePicker](https://ant.design/components/time-picker/)，[本地快照](../analysis/official/time-picker.md)。

**官网能力**：时分秒、12小时制、间隔、禁用小时/分钟/秒、Now、确认、滚动改变、悬停预览、范围和自定义页脚；Range可控制起止排序。

**WPF 契约**：`SelectedTime:TimeSpan?`，有效值0≤t<24小时；`StartTime/EndTime`用于范围，`Format`、Culture、Use12Hours、Hour/Minute/SecondStep、DisabledTimeProvider、NeedsConfirmation、ChangeOnScroll、ShowNow、IsDropDownOpen；PreviewTime与已提交值分离。

- P2 首批支持文本解析、三列/按格式隐藏列、清除、只读输入、AM/PM、间隔、禁用、Now和确认/取消；范围同日内顺序，跨午夜业务用显式日期时间范围，不把结束小于开始默认为跨天。
- P3支持滚动即选、Hover预览、自定义单元格/页脚和更精细时间分量约束；关闭弹层恢复未提交草稿。隐藏禁用选项与禁用显示是两种策略。
- 性能/生命周期：只生成实际可见列，秒列最多60个正常项；按上游小时/分钟变化计算下游禁用状态，不每次布局重新查询全部组合。Now默认点击时读取，不启动每秒刷新计时器。
- 边界：Format采用.NET时间格式并明确AM/PM解析，拒绝负值/≥24h和非法步长；时间不承诺时区/夏令时语义，Range与DatePicker组合时由宿主给日期上下文。

**验收**：①12AM=00:00、12PM=12:00且往返正确；②小时改变后禁用分钟/秒及时更新，键盘无法提交禁用时间；③确认模式拖动后Esc保持原值，确认仅提交一次；④跨午夜输入按明确范围策略报错，不静默调换成另一业务含义。

<a id="transfer"></a>

## transfer — Transfer

官方来源：[Transfer](https://ant.design/components/transfer/)，[本地快照](../analysis/official/transfer.md)。

**官网能力**：左右穿梭、单向、搜索、分页、全选、列表标题/页脚、自定义操作和列表模板，并给出Table/Tree组合示例；targetKeys（已移入）与selectedKeys（当前勾选）不同。

**WPF 契约**：`ItemsSource`、ItemKeyPath、`TargetKeys`、`SourceSelectedKeys/TargetSelectedKeys`、`IsOneWay`、SearchText各侧、标题/页脚/ItemTemplate与MoveCommands；`TransferRequested(direction, movedKeys)`/集合变化携带实际移动集合。

- P3 首批两侧虚拟列表、过滤、选择、全选、批量移动、单向移除、禁用项、空态与计数；目标集合保存键顺序，勾选但未移动不改变TargetKeys。
- 后续支持分页以及Table/Tree内容模板，要求复用同一选择/移动契约；不能把自定义视图的勾选直接写成目标成员。远程分页由宿主管理键全集，页面切换不丢已移入值。
- 性能/生命周期：HashSet/键索引判定成员资格，批量移动一次集合事务，避免每项刷新整个左右列表；每侧独立视图与虚拟化，搜索只过滤展示不篡改原集合。
- 边界：禁用项不能被全选/移动，未知TargetKey显示保留/诊断策略明确；移除源数据时由宿主决定是否保留目标键，控件不偷偷删除业务选择。

**验收**：①只勾选不移动时TargetKeys不变，移动后左右集合无重叠/丢项；②禁用项不随全选移动，取消搜索后选择仍对应原键；③跨页批量移动不重复插入且数量正确；④万项穿梭不创建万项可视容器，Table/Tree模板行为一致。

<a id="tree-select"></a>

## tree-select — TreeSelect

官方来源：[TreeSelect](https://ant.design/components/tree-select/)，[本地快照](../analysis/official/tree-select.md)。

**官网能力**：树型单/多选、勾选、严格/关联选择、父子展示策略、搜索、异步加载、展开状态、节点连线/图标、简单父ID数据、数量上限和虚拟列表。官方说明过滤时不触发loadData，选中值须唯一。

**WPF 契约**：共享Tree的节点索引和选择逻辑；`ItemsSource`、ItemKeyPath/ChildrenPath、SelectedKey或SelectedKeys、CheckedKeys、ExpandedKeys、IsCheckable、CheckMode、CheckedDisplayStrategy、SearchText、NodeTemplate、IsDropDownOpen；选中、勾选、展开是三个不同集合。

- P2 支持单选、清除、多选标签、禁用/不可选/禁止勾选区分、祖先展开、键盘层级导航、异步加载、模板及连线/图标。默认勾选展示叶节点，父/全选展示只影响标签，不修改真实勾选结果。
- P3补简单id/pId输入适配、明确的最大选择数和组合规则；严格勾选模式父子独立，关联模式传播只作用可操作已知节点；未加载后代的“全选”语义由宿主明确，不推断全量未知数据。
- 性能/生命周期：外层AntTreeSelect从首版固定为Control组合契约。P2原生TreeView内核只承诺小/中规模；P3大型树固定采用与AntVirtualTree相同的可见节点扁平投影，不靠更换公开基类扩展。避免默认ExpandAll实例化全树；父索引和祖先增量更新用于路径/半选，异步按节点去重并取消旧请求，搜索仅过滤已加载数据，不每键调用loadData。
- 边界：重复键、父子环、移除已选节点与远程暂缺节点有诊断/保留策略；批量修改展开/勾选合并通知。不能套一个Popup就宣称原生TreeView自动具备多选与平面列表级虚拟化性能。

**验收**：①严格与关联勾选规则可区分，标签策略不改变CheckedKeys；②展开、单选、勾选分别绑定且换源后不串项；③搜索不发全树懒加载，迟到结果不污染新树；④P3扁平内核用10,000节点浅/深两种树验证容器规模；⑤所有批次均验证禁用节点不能被父级全选绕过。

<a id="upload"></a>

## upload — Upload

官方来源：[Upload](https://ant.design/components/upload/)，[本地快照](../analysis/official/upload.md)。

**官网能力**：文件选择、拖放、粘贴、目录、多文件、手动上传、beforeUpload筛选/变换、受控文件列表、自定义请求、缩略图/卡片、进度、预览/下载/移除、数量限制、排序/裁剪示例。官网提供默认HTTP上传选项；这是浏览器实现能力，不代表WPF库必须内置云协议。

**WPF 契约**：`Files`为受控可观察文件项集合，项含稳定Id、Name、Size、RelativePath、Status、Progress、Error和可选Thumbnail；`AllowMultiple`、AcceptedExtensions/Filter、MaxCount、AllowDrop/Paste、ListStyle、FileTemplate；Choose/Upload/Cancel/Retry/Preview/Download/Remove以Command/请求事件交给宿主执行。状态为Queued、Validating、Uploading、Succeeded、Failed、Cancelled，不能混用“未上传”和失败。

- P3 首批文件对话框、拖放、队列、手动开始、进度、失败重试、取消、移除拦截、文本/图片卡片；筛选结果明确Accept/Reject/Ignore三种语义。MaxCount=1的新文件替换必须等校验成功，不能先删旧项。
- 后续目录枚举、剪贴板文件/图片、预处理、拖拽排序与裁剪集成；传输、鉴权、分片、云存储和重试幂等由宿主提供。预处理返回的新流/文件必须声明所有权与长度；宿主传入流默认借用，库仅关闭自己创建或显式转移所有权的流/句柄，外部ImageSource由宿主管理；重试不复用已关闭流。
- 性能/生命周期：元数据先读取，内容用流按需处理，不全部ReadAllBytes；遵循性能契约的建议宿主预算（上传并发3、等待100、目录深度32/枚举10,000项），到上限停止接纳并报告部分结果，不静默丢文件。目录默认跳过reparse point/符号链接，验证RelativePath归属选定根目录，支持取消/无权限报告。进度每文件至多10次/秒，缩略图按显示尺寸解码与有界缓存；卸载取消本控件操作并释放库拥有的资源与拖放订阅。
- 边界：扩展名仅为UI筛选，服务端仍需校验实际内容；本地访问失败不崩溃。状态回调必须按文件Id与请求代次匹配，已移除/取消文件的晚到进度不能复活该项；文件选择/预览不未经宿主允许执行文件或自动发送网络请求。

**验收**：①拒绝的新文件不计入队列，MaxCount=1校验失败保留旧项；②大文件以流处理，取消后句柄释放，失败重试重新开流且无重复成功通知；③目录中无权限/链接循环可结束并报告部分失败；④移除上传项后迟到进度不复活，缩略图大量滚动保持缓存预算；⑤拖放/粘贴/手动选择都经过相同筛选与数量约束。

## 覆盖与待决事项

已为18/18个Data Entry入口分别定义来源、WPF契约、功能、性能/生命周期、边界与验收；主要子能力均已分批决策。实现前仍需通过技术切片验证：Password安全显隐、DatePicker进阶历法/掩码、树的深层虚拟化、ColorPicker色彩精度、decimal以外任意精度，以及Upload目录/剪贴板在目标Windows版本的能力。

本轮未编写控件、执行UI测试或测量性能；以上性能要求与验收条件是后续发布门槛，不是已通过的结果。
