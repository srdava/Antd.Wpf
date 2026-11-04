# M0 PF01 独立进程启动采样

本批新增启动测量能力，不把PF01或M0改为完成。复用Gallery双目标可执行宿主，不增加产品依赖、控件API或另一个应用项目。

## 场景与时间定义

Gallery新增`--measure-startup empty|controls <output.csv>`。正常Gallery与截图路径被隔离到NoInlining方法，避免其类型解析让空宿主提前加载Antd.Wpf。

- empty：相同1000×800窗口与一个可聚焦原生Grid，不创建主题作用域或Ant控件。脚本和测试都检查程序集清单中没有Antd.Wpf。
- controls：单个Light AntThemeScope、关闭动画、10列20行UniformGrid，200个直接子控件，Button/Input/Select/Checkbox/Radio各40个，使用默认尺寸/模板与固定小数据源。输出数量来自实际Children.Count，内部模板视觉节点另记。
- 两者均为隐藏真实WPF Window，Opacity0、不接收指针点击，测试结束主动Shutdown。它们不是屏幕上实际呈现和人工操作的宿主。

父进程在Process.Start之前记录Windows共享Stopwatch/QPC时钟，包含创建子进程、CLR和WPF初始化；子进程分别记录OnStartup入口、Loaded、Loaded后首个CompositionTarget.Rendering回调，以及Input优先级回调中Focus()返回后的检查点。父进程核对QPC频率及所有检查点位于launch/exit之间，Loaded必须不早于OnStartup，Rendering与Input检查点互相不预设先后顺序。

Rendering回调不证明GPU已经呈现；Focus()成功仅说明WPF接受焦点请求，不能证明物理按键、IME或首次可交互延迟。报告因此使用startup-proxy和Input Dispatcher等名称，不据此关闭完整PF01的“首个可交互”预算。

每个目标默认10对、20个独立进程，偶数对empty先启动、奇数对controls先启动。每个样本都是新进程，但不清空系统文件缓存、不重启机器、不控制用户后台负载；应称为进程首启采样，不宣称完整冷缓存启动。统计每模式各检查点、同一对的controls−empty差值的p50/p95/max，保留负差值，不以p95之差代替配对差值的p95。10样本的nearest-rank p95等于最大样本。

## 原始数据与验证

`scripts/Measure-Startup.ps1`要求先有Release Gallery，无隐式构建；可指定net48或net8.0-windows。每轮创建新的输出目录，保存：

- 每进程原始QPC CSV、startup/ready程序集清单、CLR/RenderTier/DPI/进程起始UTC环境文件及stdout/stderr。
- samples.csv：每对/模式的进程ID、父进程起止QPC、相对各检查点时间及焦点返回值。
- paired-deltas.csv：每对的Loaded/Rendering/Input Dispatcher增量。
- summary.json：各分布、300ms暂定预算布尔值、宿主/库/脚本SHA256、CPU/GPU/OS/频率及测量限制。

单子进程15秒超时，子进程检查点10秒超时；失败应非零退出，不落回普通Gallery或忽略损坏文件。脚本核对模式、实际控件/字典数量、进程ID、时间域和程序集隔离；测量前后对比宿主与库hash，避免中途二进制改写。首轮仅补齐协议时已发现超过预算，输出MEASURED和proxyWithinBudget明确区分采样完成与性能通过。

Verify在功能/包/截图验证之后串行运行双端10对采样，原始文件直接放入本轮verified时间目录。报告startup段保存完整摘要及rawDirectory，并增加Verify与Measure-Startup脚本hash；不得把不同构建、不同运行时或本机虚拟显示驱动结果合并成稳定基线。

| Requirement | Evidence |
| --- | --- |
| 独立进程记录检查点、实际0/200控件/0或1作用域字典，空宿主不加载库 | `StartupProbe_RecordsStagesAndIsolatesEmptyAssemblyLoads`，两种模式 |
| 非法参数及时非零退出，不打开普通Gallery | `StartupProbe_InvalidModeExitsWithoutOpeningNormalGallery` |
| 输出失败非零退出，不能假报采样成功 | `StartupProbe_UnwritableOutputExitsWithFailure` |
| 10对交替启动、元数据验证、分布及原始文件保留 | `scripts/Measure-Startup.ps1`的真实执行与Verify的startup段 |

定向net8/net48各4项通过。初次构建IDE0055指出多行对象初始化器的同一行多个属性，已手动按仓库格式拆行；没有运行dotnet format写入路径。对照协议的初版把控件数量写成常量，复核后改为实际Children.Count，避免缩短创建循环仍报告200。

## 已知失败与下一步

初轮net8的10对结果位于`artifacts/performance/startup/net8.0-windows/20261103-152559-998/`：配对Loaded增量p95为2627.2605ms，Input Dispatcher增量p95为3536.2092ms，300ms暂定预算未通过。空宿主也有显著波动，不能直接把所有耗时归因于控件库，更不能删除慢样本或据此放宽预算。该轮保留原始二进制/脚本hash；随后仅修正实际数量记录和日志语义，未据此宣称性能改善。

完整实测基线尚需相同构建的重复轮次、原生200控件对照及适当CPU/ETW采样来区分初始化、布局、焦点/UIA和环境影响；真实首帧、物理输入、冷缓存与参考硬件、GC/分配也仍未验收。报告proxyWithinBudget=false是未关闭的性能门槛，功能Verify exit0不改变它。

最终完整Verify记录于2026-11-03 15:35，exit0：Release零警告/错误，net48/net8各273/273、四个真实包消费目标通过，14张截图生成。两端各完成10对/20个独立进程，配对Input Dispatcher增量p95分别为360.3526/609.3244ms，均超过300ms暂定预算；PF01门槛保持开放。源码和两份脚本的hash随`../m0-validation.json`归档。本批交付的是采样、参数/输出失败处理及自动归档，未作启动性能改善声明。
