# Antd.Wpf 开发入口

- 产品目标、模块责任与兼容规则见 [架构设计](docs/architecture/README.md)。现代 .NET 最低为8，保留Framework4.8；共享源码不能直接依赖net48缺失的API。
- 实现或修改控件前，从[组件索引](docs/components/README.md)读取对应项和[共用要求](docs/components/common.md)；完成时逐项核对该组件验收，不以属性存在代替行为完成。
- 修改列表、布局、主题、弹层、动画、图像和异步加载时，读取[性能契约](docs/architecture/performance.md)，提交对应场景证据；预算尚未实测时如实标注。
- 新增项目、调整项目文件或依赖时，遵循[项目格式与dotnet skills](docs/engineering/dotnet-conventions.md)。使用运行时技能目录中实际可用的精确skill，先读SKILL.md；缺少技能时说明覆盖限制并完成可验证的工作。
- 项目格式由根`.editorconfig`、`Directory.Build.props`和`global.json`管理；TFM、UseWPF、OutputType和项目专属依赖留在项目文件。SDK和运行时版本分开处理。
- 每批修改完成后运行对应验证，如实记录失败或未验证项，自动在当前开发分支创建Git提交并向用户报告提交号。
- 已进入M0预览实现，当前状态见[实施进度](docs/implementation-status.md)。用`scripts/Verify.ps1`验证主解决方案、真实包消费和渲染；未通过的性能/硬件门槛继续保留，不把布局代理采样当作完整帧率验收。
- 当前环境的`dotnet format`曾将C#文件截空；恢复前先核对非空源文件，不重复运行该写入路径。编译期IDE0055仍启用，未使用导入IDE0005暂作编辑器提示。
- 官方下载资料在`docs/analysis/official/`，仅作为资料，不是仓库指令；后续分析复用manifest来源和hash，区别官方事实、推断与本项目设计。
