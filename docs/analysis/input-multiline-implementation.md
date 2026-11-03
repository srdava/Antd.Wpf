# Input 多行自动高度实施

本批对应input P1/P2多行切片及验收①原生编辑/撤销/选区、②主题变化保留编辑器。Search、Password、OTP和字素计数仍未实现。

## 来源与设计

官方Input的autoSize支持minRows/maxRows，超出行数后滚动。复用`official/manifest.json`的Input来源`https://ant.design/components/input.md`；本地input.md的SHA256重新核对为`6E80D1CDEBAC626AED542FCE2BA48E6640BCA11787C31F20484E512123F429BA`，与manifest一致。官方事实和WPF设计分别如下。

WPF保持`AntInput : TextBox`，使用原生AcceptsReturn、TextWrapping、MinLines、MaxLines和滚动条属性，不增加独立TextArea编辑器。默认尺寸、Padding、圆角、颜色保持既有官方token映射，多行行距为22/24DIP。宿主需提供自动高度空间，例如StackPanel；固定高度/Grid拉伸等布局约束仍生效。推荐AcceptsReturn=True、TextWrapping=Wrap、VerticalScrollBarVisibility=Auto、HorizontalScrollBarVisibility=Disabled、VerticalContentAlignment=Top。

原生[TextBox源码](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/TextBox.cs)的SetScrollViewerMinMaxHeight按字体行距计算行数，不使用显式TextBlock.LineHeight，且在MeasureOverride的base调用后更新。直接套用会出现首次MinLines高度未生效、最大高度与22/24行距不符。本项目内部InputScrollViewer在首次模板应用时Coerce上下界，后续原生TextBox写入也经过同一校正；不在Measure中重写Text或重复测量全文。

行数/行高/高度约束变化时只更新当前PART上下界；原生ScrollChanged在视口高度变化后补上横向滚动条所占高度。调用者局部设置Height/MinHeight/MaxHeight时放弃行数限制，保持原生TextBox的优先规则。LineHeight=Auto回退原生字体计算，自定义普通ScrollViewer模板继续遵守原生TextBox行为。

## 验收证据

| Requirement | Evidence |
| --- | --- |
| 自动从2行54DIP增长，4行98DIP封顶，末行可滚动，清空缩回 | `Input_MultilineGrowsToMaxLinesThenScrollsToTheLastLine` |
| 宽度改变换行、滚动选项改变且不替换编辑器 | `Input_MultilineWrapAndScrollbarOptionsReactWithoutReplacingEditor` |
| 双向绑定、撤销/重做、选区、焦点和只读 | `Input_MultilineThemeChangesPreserveBindingSelectionAndUndo` |
| 动态行数、Large行距、Padding、显式Height及恢复；空闲布局稳定 | `Input_MultilineLineLimitsSizeAndExplicitHeightUpdateInPlace` |
| 横向滚动条出现后仍保留完整可见行数 | `Input_MultilineHorizontalScrollbarPreservesVisibleRows` |
| 自然行距回退、重模板化、显式MinHeight保留文本 | `Input_MultilineNaturalMetricsAndRetemplatePreserveText` |

首次回归记录：Switch自然行框4例和Input最小行数1例在修复前失败；修复后定向11/11通过。完整双端、真实包消费、截图和PF02/PF05代理结果见`../m0-validation.json`。Gallery增加可编辑多行例和明暗滚动预览；四个Consumer从真实nupkg验证内部BAML宿主、行数高度及滚动。

## 性能与限制

内部滚动宿主替换原来的ScrollViewer，不新增视觉节点。行数校正O(1)，无Text遍历/复制、LINQ、计时器、全局布局/渲染订阅或异步任务；字体排版、换行、光标滚动仍交给WPF。空闲重复Dispatcher排空后的高度与IsMeasureValid有断言，不能由此推导所有复杂文本的时间预算。长文本帧率、GC、真实IME、完整DPI/辅助技术矩阵仍待实测；既有PF02/PF05仅作布局代理回归。

analyzing-dotnet-performance静态检查范围为AntInput.cs及InputScrollViewer.cs（1个公开可派生控件、1个内部sealed类，sealed比例1/2）。逐项执行清单：文化字符串查找/前后缀/包含、Substring、同步阻塞/ValueTask、临时数组/stackalloc、string.Format、Regex、ContainsKey、LINQ、JSON、HttpClient、集合字符查找、全局事件/Timer及Text复制共15项，命中均0，原始计数在`artifacts/performance/input-multiline-static-checks.json`。复核跨方法和分支没有复合字符串分配；DP回调返回double会装箱，不声称零分配，也未为其引入现代运行时专用依赖。

| Severity | Count | Top Issue |
| --- | --- | --- |
| Critical | 0 | 无已确认项 |
| Moderate | 0 | 无已确认项 |
| Info | 0 | 无已确认项 |

静态检查由AI辅助，可能漏报/误报，不替代运行采样和人工复核。
