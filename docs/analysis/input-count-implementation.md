# Input 计数与长度提示

本批对应input P1计数/长度提示，扩展现有AntInput，不增加编辑器或依赖。ShowCount=True且MaxLength>0时展示“当前长度 / 上限”，MaxLength=0保持只显示长度。长度采用UTF-16单元：一个常用emoji为2，组合字母按实际单元计数；自定义字素计数仍属P3。

## 来源与WPF映射

官网Input文档提供showCount/maxLength、独立count.max和自定义strategy。复用`official/manifest.json`中的URL与hash，重新核对input.md的SHA256为`6E80D1CDEBAC626AED542FCE2BA48E6640BCA11787C31F20484E512123F429BA`。官方6.6.8的style/index.ts的show-count-suffix使用colorTextDescription和LTR，out-of-range将文字及计数改为colorError；快照SHA256为`A32DD54CAD4A1E8690C088C0965EDC901399DB50E00EDBDE845079078C535602`，与styles manifest一致。

本项目沿用原生TextBox.MaxLength对用户编辑的约束；程序设置Text或绑定写入超长值时不偷偷截断，也不覆盖原始Status和Validation。新增只读IsOverMaxLength，按MaxLength>0且Text.Length>MaxLength推导。超限时输入文字和计数使用Danger；正常计数用Tertiary（colorTextDescription），与正文/后缀保留4DIP间隔，组合后缀在计数之后。禁用状态优先使用DisabledText。尺寸、Padding、圆角和边框继续沿用既有Input资源。

UI Automation的ItemStatus在超限时为Error，恢复到上限以内后回到调用方Status/原生ItemStatus；已有Validation错误仍优先。派生状态变化通知已存在的AutomationPeer，不强制创建Peer或重建编辑器。局部Foreground等WPF高优先级设置仍按平台规则覆盖模板默认值。

## 验证

| Requirement | Evidence |
| --- | --- |
| 小于/等于/超过上限、无上限、动态上限与原文保留，清除/Undo/焦点/选区/文本绑定 | `Input_CountTracksLimitBoundariesWithoutTruncatingBoundText` |
| 重新应用模板、MaxLength绑定往返、只读与隐藏计数不掩盖错误 | `Input_CountRecreatesBindingsAfterTemplateAndLimitChanges` |
| UTF-16、RTL计数方向、明暗主题、禁用色和辅助技术错误/Warning恢复 | `Input_CountUsesUtf16AndKeepsErrorSemanticsAcrossThemeAndRtl` |

实现前两个回归均失败，旧计数仅显示长度。模板内Run的呈现文本由TextRange检查，不能用TextBlock.Text属性代替可见Inline文本。真实NuGet Consumer验证组合计数、超限原文、UIA和无上限恢复；Gallery主窗口和明暗矩阵展示正常/超限计数。

## 性能与边界

长度读取为string.Length，超限判断O(1)，仅布尔状态改变时写依赖属性。计数采用原生绑定和两个Run，沿用一个TextBlock；没有新视觉包装、全文复制/扫描、布局/渲染订阅或计时器。隐藏计数不改变原生编辑器及验证状态。已有PF02/PF05布局代理由Verify继续回归，不等于本功能的逐键帧率或GC预算通过。

完整双目标、四个消费宿主和截图以`../m0-validation.json`为准。未实现独立的软上限count.max、自定义计数/裁剪策略或TextArea外置计数模板；真实IME、屏幕阅读器、全DPI和硬件帧时间/GC仍待验收。

2026-11-03 09:37完整Verify exit0，Release零警告/零错误，net48/net8各240/240、零失败/跳过，四个真实NuGet宿主和14张截图通过；71份源码hash一致，包SHA256为`B3AB0AEA2155AB4BEC06D1128C6B90D953AC279BA3C0C2D2080F5C13BEFB5D6B`。两端明暗计数矩阵已目视核对。首次Verify在两端测试通过后被Consumer新增代码的CS8602阻止；补上模板空值检查后完整重跑通过。

PF02/PF05初次采样较上一批升高，依性能契约对同一构建再复采两轮，每场景30样本，每目标每轮2/2测试通过。p95毫秒如下，原始CSV和含源码hash的摘要在`artifacts/performance/input-count-repeat/summary.json`；完整验证原始快照由m0-validation.json指向。未将波动解释为已解决的性能问题，也不将这些数据视作稳定基线或硬件验收。

| 场景 | net48 完整 / 复采1 / 复采2 | net8 完整 / 复采1 / 复采2 |
| --- | --- | --- |
| PF02 Input布局代理 | 120.33 / 95.24 / 55.48 | 83.69 / 123.58 / 83.56 |
| PF02 混合控件布局代理 | 92.82 / 47.43 / 36.35 | 64.50 / 78.51 / 56.38 |
| PF05 选择布局代理 | 22.09 / 13.56 / 14.13 | 16.88 / 27.24 / 18.27 |
| PF02 Divider布局代理 | 20.00 / 30.68 / 23.42 | 36.88 / 37.77 / 18.47 |
