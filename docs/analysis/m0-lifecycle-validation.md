# M0 PF09 生命周期证据

本批只补齐M0页面/单选弹层的生命周期探针，不新增产品控件，不把整个PF09或M0改为完成。使用双目标xUnit2/VSTest，真实隐藏WPF Window/Popup；强制GC仅存在于测试宿主。

## 场景与直接断言

`PF09_HundredPagePopupCyclesReleaseAfterNativeAutomationBaseline`在一个Owner内连续100次挂载/卸载页面，每页包含AntThemeScope、AntInput Search、AntSelect、加载中的AntButton、运动中的AntSwitch及AntPasswordBox。每次都实际打开Select弹层，触发一次搜索并设置测试密码。

- 卸载后Search请求令牌被取消，Select及原生Popup关闭，Popup移除来源作用域字典，Button停止显示Loading，Password的原生SecurePassword长度为0。
- 追踪每页的主题作用域、面板、Input、Select、Button、Switch、Password包装及原生编辑器、Popup根，共900个对象，另加已关闭Owner，合计901个WeakReference。
- 外部刻意保留100个真实ICommand，CanExecuteChanged使用真实事件，并在卸载后发通知。项目对象回收断言执行时命令仍被持有，用于检验弱事件订阅没有被命令反向保活。
- 随后释放外部命令引用，另外断言100个命令均可回收。NoInlining隔离创建/释放栈帧，ApplicationIdle排空、100ms延迟回调窗口和3轮GC构成有界收尾。
- `PF09_ProbeDetectsRetainedControlAndReleasedControl`用明确保留的控件验证探针能看到存活，移除强引用后验证可回收，避免把不工作的WeakReference探针误认为通过。

## 初始失败及堆根调查

首轮net8中前99页回收，最后一页9个对象及Owner仍存活。加入延迟Dispatcher排空后相同10个对象仍存活，没有通过延长GC循环或排除最后一页来消除失败。

对失败测试进程使用本地诊断工具dotnet-dump10.0.750501采集Heap dump，并在独立的诊断步骤中查询gcroot。观察到唯一根来自引用计数为4的COM handle，路径为：

```text
HandleTable (ref counted handle)
  MS.Internal.Automation.ElementProxy
  ListBoxItemAutomationPeer
  List<AutomationPeer> / AutomationPeer[]
  TextBlockAutomationPeer
  ListBoxItemWrapperAutomationPeer
  ComboBoxItem / VirtualizingStackPanel / ItemContainerGenerator
  AntSelect
  页面控件树
```

Owner的路径继续经过页面中的TextBlockAutomationPeer及WindowAutomationPeer。原始证据为`artifacts/lifecycle-retention.dmp`、`artifacts/lifecycle-heap.txt`、`artifacts/lifecycle-roots.txt`。这些是本机测试进程诊断文件，不提交到仓库，也没有收集用户应用进程；临时诊断暂停代码已删除，产品没有增加诊断环境变量或明文日志。

引用计数handle直接证明UIA原生COM保留链；它不足以区分进程内UIAutomationCore缓存与另一个自动化客户端。这里“外部”指托管控件对象树之外的引用，没有定位到某个外部进程，也不把具体客户端或缓存策略记为已证实根因。

## 原生UIA对照与结论边界

最终场景在保存“对照前”结果后，创建并关闭3个仅使用原生WPF ComboBox的隐藏Window/Popup，再排空Dispatcher并GC。net8定向运行中，项目对象从存活10/901变为0/901，命令释放后为0/100；原生对照对象仍为9/9存活。说明外部UIA引用影响本机场景的回收时机，并且保留可以转移至原生控件。该实验没有识别外部客户端进程，也没有证明其缓存上限或长期释放策略。

最终测试仍逐一断言全部901个项目对象和100个命令均已回收，没有忽略最后一页；但该断言明确以“已执行原生自动化对照”作为前提。原生对照对象的存活数量作为观察结果记录，不能解释为无条件Owner回收通过，也不能假设关闭辅助功能就是产品修复。

每目标输出`artifacts/performance/lifecycle/<target>/`下四阶段的`*-references.csv`和环境：before-native-baseline、pages、native-baseline、commands。Verify完整归档这些文件，核对901/100/9条数及项目/命令对照后0存活，在`docs/m0-validation.json`单独写入lifecycle段；不会把回收观察当成毫秒性能采样。

## 验收映射与未关闭项

| Requirement | Evidence |
| --- | --- |
| 100次页面/Popup、搜索取消、资源桥接解绑、加载/密码收尾；带原生UIA对照的完整回收 | `PF09_HundredPagePopupCyclesReleaseAfterNativeAutomationBaseline` |
| 探针同时能发现强引用保留和解除后的回收 | `PF09_ProbeDetectsRetainedControlAndReleasedControl` |
| 双目标原始结果与源码hash归档 | `scripts/Verify.ps1`、`docs/m0-validation.json`的lifecycle/sourceSnapshot段 |

外部UIA引用尚存时的无条件Owner释放、长期进程堆根/原生句柄、真实屏幕阅读器、未实现的Modal/Drawer/AntHost服务不在本批通过范围。PF09完整验收保持开放；真实IME、150%DPI、PF01启动及稳定性能基线仍按M0清单继续。

## 本批最终验证

2026-11-03 15:02，`pwsh -NoProfile -File scripts/Verify.ps1` exit0。Release零警告/错误，net48/net8各269/269、失败0/跳过0；四个真实nupkg消费目标通过，14张既有Gallery截图生成，79个源码hash一致。本批没有修改产品模板，也未新增物理输入/像素差分验收。包SHA256为`39142632BBE7126C714CF11E1743A2B7C2EDCBE5AFE5911AE197000613DF0A2A`。

两端均记录：原生对照前项目存活10/901，对照后0/901，释放命令后0/100；原生对照自身存活9/9。Verify的lifecycle段保留四阶段原始目录，本轮布局代理均在暂定预算内；此前超预算记录仍为历史失败证据，不据单轮通过宣布性能稳定或M0完成。
