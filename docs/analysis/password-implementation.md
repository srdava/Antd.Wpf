# Password 掩码输入基础切片

对应组件input的P2 Password前置部分，仍未完成验收⑤的显隐能力。官方来源复用`official/manifest.json`与`official/input.md`的URL/hash；官方Password提供显隐及触发器定制，本批只交付原生掩码输入，不能描述为完整Input.Password。

## 公开接口与所有权

`AntPasswordBox : Control`组合一个原生`PasswordBox`。WPF PasswordBox是密封类型；同一个原生编辑器由控件持有为逻辑孩子，通过必需的`PART_PasswordHost : ContentPresenter`接入模板。模板替换只移动该编辑器，保留密码；缺少该PART且模板生成视觉树时抛出带部件名的诊断。`PART_ClearButton : Button`可省略，旧按钮点击订阅在换模板时解除。

- `SecurePassword`只读CLR属性，返回原生控件提供的SecureString副本。调用方拥有副本并负责Dispose；修改/释放副本不会修改编辑器，控件卸载不会代替宿主释放已取出的副本。
- `PasswordChanged`为CLR事件，仅提供EventArgs.Empty，不附带密码。原生编辑、Clear以及卸载清除会通知；仅主题、尺寸、状态或模板替换不产生密码变更通知。
- `Clear()`清除原生密码；`FocusPassword()`将焦点交给内部原生编辑器。两者及读取密码均要求控件所属Dispatcher线程。
- `IsEmpty`为只读DP，用于占位/清除按钮显示；在PasswordChanged中取得短期SecureString副本读取长度并立即Dispose。没有在Measure/Render读取密码，没有明文Password/Text依赖属性，没有明文转换、日志或长期字符串缓存。
- `Placeholder`、`AllowClear`、`Size`、`Variant`、`Status`支持WPF绑定；`MaxLength`和`PasswordChar`传给原生编辑器。MaxLength负值被拒绝，原生用户输入上限语义不等于程序化字符串截断策略。
- Unloaded清除控件自己的密码，包括临时移出视觉树；重新Loaded为空。此行为是明确的安全/可用性取舍，宿主不能依赖切换页面保留密码。失焦仅保留原生掩码值，没有另一个明文编辑器或显隐状态。

宿主使用示例：

```csharp
using SecureString password = accountPassword.SecurePassword;
// 在宿主认证边界使用副本；不要写入日志、普通绑定属性或长期缓存。
```

## 视觉、键盘和自动化

复用既有Input的24/32/40最小高度、4/6/8圆角、7/11水平内距及0/4/7垂直内距，控件默认Margin0。采用自然字体行框与居中的原生内容宿主，允许放大字体撑高，避免固定高度裁剪。Light/Dark、Outlined/Filled/Borderless/Underlined、Hover/Focus/Disabled、Error/Warning和Validation错误复用既有资源；默认高对比度资源随主题作用域更新，Error加厚边框。完整物理交互/DPI/高对比度矩阵仍待验收。

只有内部PasswordBox进入Tab序列，原生键盘/密码掩码/粘贴继续交由WPF处理。原生PasswordBoxAutomationPeer保留IsPassword=true，Value读取为空字符串；宿主AutomationProperties.Name/LabeledBy向内部编辑器传递，Status/Validation映射ItemStatus。清除按钮复用不抢焦点的既有Clear样式，实际清除后焦点回到原生编辑器；禁用/AllowClear=false下合成点击也不能清除。

## 验收证据

| Requirement | Evidence |
| --- | --- |
| 三尺寸/圆角、主题/RTL、变体、焦点及同一原生编辑器保留 | `Password_SizeThemeAndDecorationsKeepNativeEditor`，3个尺寸用例 |
| 安全副本隔离、清除通知、MaxLength/PasswordChar传递、UIA不读明文和状态 | `Password_ClearAndSecureCopiesDoNotExposeAutomationValue` |
| 模板替换保留密码，旧按钮解绑，可选清除部件缺失仍能Clear | `Password_RetemplateKeepsSecretAndDetachesOldClearButton` |
| 禁用合成清除守卫、卸载擦除及重新加载为空 | `Password_DisabledClearIsGuardedAndUnloadErasesOwnedSecret` |
| 必需模板部件缺失有明确诊断 | `Password_MissingRequiredHostHasDiagnostic` |
| 200控件、5次预热/30次主题切换，节点与编辑器有界、密码状态保留 | `PF02_PasswordThemeSwitchKeepsEditorsAndVisualsBounded` |
| 真实nupkg内BAML、编辑器、安全读取和清除 | `tests/Consumers/Antd.Wpf.Consumer/Program.cs`，四个消费目标由Verify执行 |

初次UIA用例错误预期原生Value抛异常，运行发现返回空字符串；改为明确断言为空并同时保留IsPassword断言。缺失模板用例初次使用空ControlTemplate，WPF未生成视觉树而未调用OnApplyTemplate；改为有Border根但缺失PART的真实坏模板后验证诊断。

## 性能与限制

每实例只持有一个PasswordBox，不创建影子TextBox、计时器、全局订阅或异步任务；主题/布局不复制密码。改变内容时为IsEmpty取一次原生SecureString副本，代价随密码长度增长并立刻释放；逻辑子项枚举还会产生小数组/枚举器。这些是本批静态审查的Info分配项，未宣称零分配。仅审查本批AntPasswordBox，无同步I/O、Task阻塞、正则/LINQ全文遍历或不受控缓存；公开WPF控件保持可派生，不按静态性能建议机械sealed。

PF02.Password独立原始CSV和环境位于`artifacts/performance/password/<target>/`，Verify复制为带时间戳快照并写入`../m0-validation.json`；隐藏窗口的布局/Dispatcher代理不等于真实呈现帧率、GC或堆根验收。样本文件只记录时长、节点数和机器环境，不记录密码。

显隐按钮/模板、前后缀、只读策略、程序化安全初始化、真实IME/物理键盘、屏幕阅读器事件、x86、动态DPI/高对比度和长期堆根仍未完成/未验收。宿主处理密码副本的安全性由宿主负责；此控件没有提供跨进程内存擦除保证。

## 本批验证结果

2026-11-03 14:44，`pwsh -NoProfile -File scripts/Verify.ps1` exit0：Release零警告/错误，net48/net8各267/267、失败0/跳过0；Password独立用例各8项。四个真实包消费目标通过，14张截图生成，两端明暗矩阵已目视核对；78个源码hash与报告一致。包SHA256为`FE7EA7A908FED30D33AADE857ADDD14CA1CA7ABBE2F2310888C124763DC68678`。

200个Password控件的PF02布局代理各有30个样本，视觉节点保持3801且不超过25/实例的总量上限，p95为71.394/107.945ms（net48/net8），处于200ms暂定预算内。所有密码状态/节点断言均在计时段外执行。

同轮既有net48 Input/混合页PF02为235.336/471.955ms，超过200ms预算；保留`../m0-validation.json`对应的失败原始快照，不把Verify的功能exit0解释为性能全部通过。对同一构建以`--no-build`定向复采既有`PerformanceScenarios.PF02_PF05_RecordLayoutLatencyAndBoundedContainers`，1/1通过；每场景30个样本，Input/混合页p95为122.491/112.256ms，PF05为38.126ms。复采CSV、环境和源码hash摘要位于`artifacts/performance/password-followup-net48/`。结果显示波动，尚无稳定对照基线；原超预算记录及硬件/GC门槛继续保留，未据此宣称整体性能改善。
