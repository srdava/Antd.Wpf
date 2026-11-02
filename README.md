# Antd.Wpf

原生WPF控件库，保留.NET Framework4.8+，现代.NET最低8，包目标为`net48;net8.0-windows`。

已有可构建的控件库、Gallery、双目标STA行为验证、真实NuGet消费工程和性能采样。当前实现AntThemeScope、AntButton、AntInput、AntSelect、AntSwitch、原生AntCheckBox/AntRadioButton、AntDivider、不换行AntFlexPanel、AntSpace、AntRow/AntCol及Layout/Sider基础切片；79项入口的全量要求尚未完成。视觉数值按Ant Design6.6.8官方样式及实际默认/暗色token核对，见[来源与映射](docs/analysis/visual-token-mapping.md)。

## 运行

在Windows上安装仓库global.json指定的.NET SDK、.NET8桌面运行时与Framework4.8引用程序集。运行Gallery：

```powershell
dotnet run --project samples/Antd.Wpf.Gallery -c Release -f net8.0-windows
```

一键验证（PowerShell7；含双目标构建/测试、打包、四个目标的包消费启动和Gallery渲染）：

```powershell
pwsh -File scripts/Verify.ps1
```

产物位于`artifacts/`。Consumer独立于解决方案，通过PackageReference安装真实包；脚本在pack之后构建它，不以ProjectReference替代包验证。Framework消费目标4.8/4.8.1在本机同一个已安装的4.x运行时执行，不能当作两台不同运行时机器的验证。

各examples窗口支持横纵滚动，缩小窗口后仍能查看完整明暗示例。Layout导航在折叠时使用带提示和无障碍名称的紧凑图标，展开后恢复文字。Matrix截图导出完整内容；交互窗口按实际视口滚动。修复说明见[Gallery预览问题](docs/analysis/gallery-preview-fixes.md)。

## 接入

安装本地生成的`Antd.Wpf.0.1.0-alpha.1.nupkg`后，默认样式自动从程序集加载：

```xml
<antd:AntThemeScope xmlns:antd="https://antd-wpf.dev/xaml" Mode="Light">
  <StackPanel>
    <antd:AntButton Content="保存" Variant="Solid" SemanticColor="Primary" Command="{Binding SaveCommand}" />
    <antd:AntInput Placeholder="请输入" Text="{Binding Name}" AllowClear="True" />
    <antd:AntSelect ItemsSource="{Binding Options}" SelectedItem="{Binding SelectedOption}" />
    <antd:AntSwitch IsChecked="{Binding Enabled}" CheckedContent="开" UncheckedContent="关" />
    <antd:AntCheckBox Content="同意" IsChecked="{Binding Accepted}" />
    <antd:AntRadioButton Content="选项" GroupName="Choice" />
    <antd:AntDivider Content="分组标题" TitlePlacement="Start" />
  </StackPanel>
</antd:AntThemeScope>
```

上面的URI是本库XAML命名空间标识，不会访问网络。请使用AntThemeScope建立颜色资源作用域；单独加载默认控件模板不等于已有完整主题。C#宿主在较新WPF中可使用`using ThemeMode = Antd.Wpf.Theming.ThemeMode;`消除与系统ThemeMode的重名。Gallery中的Visual matrix和Divider examples入口可查看明暗主题及基础布局样例。

| 入口 | 内容 |
| --- | --- |
| [组件索引](docs/components/README.md) | 官网73个核心入口和6个Pro外链入口的逐项实现要求、分批范围与验收 |
| [共用要求](docs/components/common.md) | 状态、绑定、输入、键盘、资源所有权与完成条件 |
| [架构设计](docs/architecture/README.md) | 项目结构、模块接口、依赖方向、主题、弹层、选择、图像与大数据路线 |
| [性能契约](docs/architecture/performance.md) | PF01–PF10场景、复杂度、资源上限、采样与回归门槛 |
| [dotnet项目规范](docs/engineering/dotnet-conventions.md) | 根配置、SDK/运行时区分、项目文件责任和skills路由 |
| [实施计划](docs/Antd.Wpf-Implementation-Plan.zh-CN.md) | REA证据边界、兼容策略、里程碑及估算 |
| [开发入口](AGENTS.md) | 后续代理按任务加载规格和skills的规则 |

发布仍受M0门槛约束：历史PF02/PF05超预算原因尚未确认；真实输入法、完整DPI/键盘/屏幕阅读器与泄漏矩阵待验收。Switch目前即时切换、静态忙碌指示；组控件、模糊阴影与完整状态动画尚未实现，不声称跨浏览器逐像素一致。
