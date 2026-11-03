# Switch 拨钮动画实施

后续排版修复：标签取消固定22/16行框，改用自然字体度量和垂直居中。默认轨道、拨钮、Padding、圆角和颜色数值保持原值，放大字体可撑高轨道。`Switch_LabelsUseNaturalFontMetricsAndStayVerticallyCentered`覆盖中英文、Small/默认/Large、明暗、RTL和字号24；`Switch_CustomContentKeepsItsSizeBindingAndVerticalCenter`验证自定义内容尺寸与绑定。最新完整验证以`../m0-validation.json`为准，下方226项为此前动画批次历史记录。

本批实现switch的P1拨钮位移切片，对应组件验收①原生输入/绑定、②Loading约束、③关闭动画/主题/RTL和④禁用状态。颜色与标签过渡、按压拉伸、把手阴影、Loading旋转继续保留为后续能力。

## 官方依据与WPF映射

复用`official/styles-6.6.8/manifest.json`中的URL及SHA256。`components/switch/style/index.ts`的handle样式使用`switchDuration ease-in-out`，时长取`motionDurationMid`；seed的motionBase=0、motionUnit=0.1，由genCommonMapToken算出0.2s。CSS标准ease-in-out的控制点为(0.42,0)、(0.58,1)。

默认模板的拨钮始终占据原生左侧布局槽，通过TranslateTransform.X移动到选中端点。终点按实际轨道宽、拨钮宽及两侧Margin计算，兼容Small、默认、长标签及原生RTL镜像。原有尺寸、Padding、圆角、颜色资源和两态逻辑继续由模板及ToggleButton管理。125%DPI下原生布局舍入可产生一个物理像素内的端点差异，不能把DIP数值与物理像素混为一谈。

动画为200ms的WPF关键帧时钟，使用冻结并复用的KeySpline。IsChecked、绑定源、命令和UIA状态在操作时立即更新，视觉位移随后完成。快速反向操作从当前显示位置启动，新时钟同步seek到起点，避免首个渲染tick前闪到终点。动画完成后移除时钟，终点保存在专属变换的基值中。

## 模板与生命周期

可选模板部件为`Track: Border`、`Thumb: Border`和`ThumbTranslation: TranslateTransform`。后者的X由控件拥有。自定义模板可省略这些部件并自行提供状态展示，原生Toggle行为仍保留。重新应用模板时解除旧SizeChanged订阅及动画完成订阅，移除旧时钟后查找新部件。

Loaded及初始选中直接显示正确位置。MotionEnabled=false、不可见、禁用、Loading或Unloaded均取消当前时钟并落到当前值的终点；恢复可见或重新加载时直接显示当前状态。尺寸/标签变化会重新计算端点，进行中的位移可按新几何继续，普通Resize直接定位。

时钟、轨道及拨钮引用归Switch实例所有。没有持续Timer、全局Rendering/LayoutUpdated订阅，也没有每帧修改Width/Margin。一次状态变化可能触发标签切换的正常布局，后续动画帧只更新RenderTransform。每次有效过渡创建时间线、两个数值关键帧对象及一个时钟，完成/取消后释放引用；不声称零分配。

使用analyzing-dotnet-performance复核本文件的热路径。LINQ、Timer创建、全局Rendering/LayoutUpdated订阅、字符串切片/查找、每次调用创建List/Dictionary、同步阻塞等待命中均0；冻结KeySpline、时钟创建、时钟移除各1处。公开可派生控件1个、sealed类0个，保持现有扩展契约。严重/中等/信息级待修问题均0，硬件帧率和GC预算仍需实测。

## 验收证据

| Requirement | Evidence |
| --- | --- |
| Small/默认和LTR/RTL的有限动画及终点 | `Switch_MotionFinishesAtOfficialEndpointsInBothDirections` |
| 关闭动画、隐藏、卸载与重加载 | `Switch_MotionCancellationCoversScopeVisibilityAndUnload` |
| 快速反向连续性、绑定和最终状态 | `Switch_RapidMotionReversalPreservesBindingAndLatestState`，30次切换/反向并检查当下位置连续 |
| 旧模板时钟清理、可选部件缺失 | `Switch_RetemplateReleasesOldMotionAndAllowsMissingMotionParts` |
| 动画帧不重新测量控件 | `Switch_MotionFramesDoNotRemeasureControl` |
| 初始选中、Resize、尺寸改变、Loading/禁用 | `Switch_InitiallyCheckedResizeAndDisabledMotionKeepExactPosition` |
| 过渡中长标签变宽及两方向几何更新 | `Switch_LabelResizeRetargetsActiveMotionInEitherDirection` |
| 原有命令、双向值和原生自动化 | `Switch_ClickAndSpacePreserveTwoWayBindingAndCommandOrder`、`Switch_LoadingAndDisabledBlockToggleAndPreserveEnabledBinding` |
| 主题、局部关闭动画、原生舍入与RTL | `Switch_ContentGeometryThemeAndRtlKeepState` |

Gallery主窗口提供普通、小号、Loading、禁用及“即时”关闭动画对照。VisualMatrix继续关闭动画以稳定捕获静态明暗尺寸矩阵。Consumer通过实际NuGet包验证初始位置、时钟启动、作用域关闭动画及即时选中位置。

## 最终验证

2026-11-02 16:11完整执行`pwsh -NoProfile -File scripts/Verify.ps1`，exit0。Release构建零警告/零错误，net48和net8各226/226，无失败/跳过，其中新增动画回归各11项。net48、net481、net8和net10四个真实NuGet消费者通过，14张PNG重新生成，双端VisualMatrix已目视核对。67个源码快照hash与当前源码一致，包SHA256为`CDBD0ACF024584B4CF1FF5A6E74D5CE068BFE98DA60CD385107C26C24014AAEE`。摘要见`docs/m0-validation.json`。

本批重建后Gallery EXE的托管元数据已恢复，Framework运行验证完成。首次完整测试还复现了既有Space合成键盘测试未先聚焦的问题；帮助器现在与原生选择测试一致，启用时先Focus，再发送原生KeyDown/KeyUp，原有值、命令次数、禁用/Loading断言均保留。Verify的SDK构建/测试/打包命令同时生成唯一binlog，保存在`artifacts/verify-*.binlog`。

PF02/PF05既有布局代理在暂定预算内，未将其转记为Switch动画帧率。硬件帧时间、GC、物理输入及多DPI完整矩阵仍需独立验收。
