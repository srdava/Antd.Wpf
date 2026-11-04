# Select 单选清除

对应select P1清除、共用绑定/键盘/模板要求及验收⑤的主题与焦点部分；不表示多选、Tags、分组或搜索已经完成。官方能力复用`official/select.md`及manifest URL/hash；CloseCircleFilled矢量的固定commit、SHA256和许可见`official/styles-6.6.8/select-clear-icon-manifest.json`。本批优先交付功能，保留既有M0性能/实机验收缺口。

## 行为与平台映射

- `AllowClear`默认false，控制清除按钮与单选Delete快捷键。已选中、启用且Hover/键盘焦点在控件内时显示清除按钮；未选、禁用或未启用AllowClear时隐藏。
- `ClearSelection()`为Dispatcher线程上的显式清除动作；已禁用或未选中时无操作。它不依赖AllowClear是否显示，先用SetCurrentValue关闭下拉，再将原生SelectedIndex设为−1，最后将焦点交回ComboBox。SelectedItem/SelectedValue/Text及SelectionChanged继续由原生选择机制更新，不重复手工写这些绑定。
- 未按修饰键、非编辑、下拉关闭且Selector自身聚焦时，Delete清除一次。可编辑TextBox的Delete继续交给原生编辑；下拉打开时不抢走列表键盘处理。Clear按钮保留原生Invoke与名称“Clear selection”，不额外插入Tab停靠点。
- ComboBox的IsReadOnly保持WPF原义，只约束编辑文本；它不会把原生可选的下拉变为只读选择器，因此不阻止选择清除。需要阻止交互时使用IsEnabled=false。
- 清除可编辑ComboBox的已选项时，原生Text同步清空并保留Text绑定。没有选项时仅有的自由输入查询文本不属于本切片的清除对象；后续搜索能力另行定义。
- `PART_ClearButton : Button`是可选部件；缺失时API/Delete仍可用。重模板解除旧按钮Click，旧按钮不能清除新模板的选项；原有Popup资源桥接/卸载清理保持执行。

## 视觉和性能

外部Margin/Padding、24/32/40最小高度、圆角和颜色资源继续使用现有Select默认模板。清除按钮在既有箭头槽上方替代箭头显示，不扩大文本布局槽。默认12DIP矢量保留官方1024画布的留白和镂空，20DIP透明命中区域属于WPF交互映射；Tertiary/Secondary动态前景支持明暗和高对比度主题资源。Toggle继续覆盖选择区域，清除按钮位于其上方，Click被标记Handled；原生Invoke验证清除不重开弹层，物理鼠标按下/拖出矩阵尚未完成。

没有新增集合扫描、过滤视图、缓存、异步任务或动画。按钮起始Collapsed，未启用时不展开其图标模板；选项仍使用同一个VirtualizingStackPanel。10,000项场景检查实际实现容器在1–45之间，清除不改ItemsSource、不重开Popup并释放主题桥接；这不是完整滚动帧率/GC验收。

## 验收证据

| Requirement | Evidence |
| --- | --- |
| SelectedItem/SelectedValue/SelectedIndex三种双向绑定、同名不同键、单次清除事件及外部再次赋值 | `Select_ClearPreservesSelectionBindingsAndDuplicateLabels`，3项 |
| AllowClear/禁用/未选边界，Delete与原生编辑分流，默认不展开图标 | `Select_ClearGuardsAndDeleteRespectNativeEditing` |
| 原生Invoke、下拉绑定保留、万项虚拟化、不重开和作用域字典解绑 | `Select_ClearInvokeClosesPopupAndKeepsVirtualization` |
| 三尺寸/RTL/主题、原编辑器保持、清除后占位与箭头恢复 | `Select_ClearAffordanceKeepsSizeThemeAndEditor`，3项 |
| 可编辑已选项的原生Text绑定保留，自由查询文本无操作；API不依赖UI开关 | `Select_ClearEditableSelectionPreservesNativeTextBinding` |
| 旧按钮解绑、缺少可选PART时API有效 | `Select_ClearRetemplateDetachesOldButtonAndAllowsMissingPart` |
| 实际nupkg中的Clear按钮BAML与清除动作 | Consumer四目标及Verify结果 |

测试初版在所有SelectionChanged上断言RemovedItems非空，导致后续外部重新赋值的正常Added事件失败；已改为分别断言清除时移除计数、总通知次数和绑定恢复后的选中对象，没有改变产品事件语义来适配测试。

多选/Tags、搜索、自由文本清除、分组、远程加载、自定义clearIcon API、空结果完整模板及真实输入/DPI/读屏仍待后续；未将这些列为本批完成项。

验证过程：真实包Consumer新增模板检查后，后续Popup检查被可空性分析标记为可能为null；已将该检查改为安全访问，四目标Consumer独立构建零警告/错误。一次完整回归中的既有`CheckBox_ClickSpaceBindingAndIndeterminateUseNativeSemantics`发生Space切换失败，同构建定向1/1通过，原因未定；原失败TRX保存在`artifacts/test-results/select-clear-keyboard-failure-net8.trx`，未据此修改CheckBox实现或删除键盘断言。

最终验证于2026-11-04 11:33完成，`pwsh -NoProfile -File scripts/Verify.ps1` exit0。Release零警告/错误，net48/net8各283/283、失败0/跳过0，其中Select清除各10项；四个真实nupkg消费目标通过，14张截图生成，两端明暗矩阵已目视检查（静态截图未触发Hover/焦点清除态）。82个源码hash及2份脚本hash一致。包SHA256为`922A0925F42B97873A637C2822D19F6C67505B4724DEDD1B8A565FB3A4F9BFA0`。

本轮PF02/PF05布局代理均在暂定预算内；独立进程启动代理配对Input Dispatcher增量p95为587.9496/963.1037ms（net48/net8），超过300ms暂定预算。仅记录既有测量，不宣布性能改善，M0启动/真实输入/DPI验收仍开放。
