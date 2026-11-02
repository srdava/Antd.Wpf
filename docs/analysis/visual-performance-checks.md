# 视觉切片的性能证据

日期2026-10-30。范围为本批Button/Input/Select/Switch视觉修正及CheckBox/RadioButton原生单项，不是全部PF门槛的通过声明。

## 已落地的约束

- Checkbox勾与半选块、Radio内点、各控件焦点环使用固定布局及Opacity切换。`Selection_StateChangesDoNotRemeasureControls`在真实STA窗口中，对两种选择控件各执行30次状态切换并逐次排空Dispatcher，断言控件MeasureOverride调用均为0；不是测到“0ms”或没有GPU工作。
- `Selection_OfficialGeometryColorsGapsAndSharedDrawingSurviveThemeChanges`断言两个实例复用同一个冻结Geometry；`Palette_PreservesOfficialAlphaAndSharedFrozenBrushes`断言同作用域两个输入框复用冻结Brush。没有每控件调色板、每状态Brush复制或静态Owner缓存。
- Button零模糊阴影用背景Border；没有DropShadowEffect。Switch、Popup的模糊阴影尚未复刻，不能把省略效果所得时间当作完整上游外观性能。
- 原生CheckBox/RadioButton只新增DefaultStyleKey，不增加事件订阅、计时器、异步工作、组扫描或自定义选择状态仓库。Input仅在ApplyTemplate/Padding变化时清零重复内距，不在Render中修正布局。

## 测量与对照限制

PF02保留200 Input旧场景，新增200 Button/Input/CheckBox/Radio混合场景；每场景5次预热、30次采样。PF05仍为万项虚拟单选。最新双目标p50/p95/max、预算标记、原始CSV和环境路径统一由[验证报告](../m0-validation.json)记录，原始数据按verified时间目录保留。

隐藏窗口现在关闭IsHitTestVisible，避免真实鼠标所在位置使Filled控件意外进入Hover；此前一次net48颜色测试确实观察到hover的0.06而不是default的0.04。测试仍通过原生命令/键处理/UIA接口驱动；这不代表物理鼠标/键盘验收。旧采样存在宿主波动与输入条件差异，不计算新旧改善百分比，不能因为最后一轮变快而宣称历史慢样本已修复。

## C#热点静态检查

使用analyzing-dotnet-performance的critical-only范围，扫描Controls、Theming及Automation；执行记录为`artifacts/performance/visual-static-checks.json`。同步阻塞Task、ValueTask、Substring、临时byte数组、stackalloc、string.Format、无比较器字符串搜索、Regex、ContainsKey重复查找、LINQ、JSON、HttpClient和IndexOfAny/ToCharArray的检测计数均为0。没有可归因的关键API反模式；公开WPF控件允许派生是设计要求，不以sealed替代测量。

该静态检查是非确定性的辅助审查，可能漏报或误报；结论受扫描范围限制，不能证明没有性能问题。未采集ETW/堆分配/GC、GPU呈现、真实输入延迟或完整DPI矩阵。PF01/PF03/PF04/PF06–PF10及硬件发布准入继续待验收。
