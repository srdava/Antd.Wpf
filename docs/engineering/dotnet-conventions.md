# 项目格式与 dotnet skills 规范

本文件将已安装的dotnet skills落实为后续开发流程。技能由Codex环境提供，本仓库不复制第三方SKILL.md，不把技能当成NuGet运行时依赖；更换开发机需在该环境确认技能可用。

## 1. 本次落地范围

- 根 `.editorconfig`：C#、XAML、项目XML与JSON的编码、缩进、换行、括号、命名及主要格式诊断。
- 根 `Directory.Build.props`：C#12、Nullable、显式using、确定性、内置分析器、固定分析规则等级、警告作为错误。
- 根 `global.json`：选择已安装的10.0.403 SDK，限定同feature band的最新patch、禁用预览SDK。SDK选择与库最低net8运行时无关。
- 根 `AGENTS.md`：按控件/性能/项目文件变更条件指向规范和skills。

已有产品、Gallery和测试项目，入口为Antd.Wpf.sln；真实包Consumer在pack之后单独构建。根配置已在双目标构建中生效。IDE0005在当前SDK中要求XML文档才能在编译期运行，因此保留编辑器提示；IDE0055及其余编译规则照常生效。

## 2. 项目XML的分工

| 位置 | 应放内容 | 判定依据 |
| --- | --- | --- |
| Directory.Build.props | 跨项目相同的编译器、可空性、分析器默认策略 | 早导入，项目仍可有明确例外 |
| 每个csproj | TargetFramework(s)、UseWPF、OutputType、AssemblyName、IsPackable、专属依赖 | 项目真实身份与不同之处 |
| Directory.Packages.props | 两个及以上项目真正共享的NuGet版本 | 首个真实依赖出现后按需求引入CPM，不先列假设依赖 |
| Directory.Build.targets | 需要最终SDK属性的规则、真实自定义Target | 只有出现实际需求才创建，不添加空文件 |
| global.json | 构建SDK选择 | 不用TargetFramework充当SDK锁定 |

项目使用SDK风格，源文件与XAML依靠SDK默认项；只有要改变默认项时才显式Include/Remove/Update。各项目保留SDK默认独立bin/obj输出，禁止多个项目共享IntermediateOutputPath。根props不根据尚未设置的TargetFramework作属性分支。

建立第二层Directory.Build文件前确认自动导入规则；内层需要显式导入外层才继承，不复制一份根策略。分析器例外精确到规则/范围并解释原因，不批量NoWarn。

## 3. 后续项目身份

产品库结构示意；实际设置以[src/Antd.Wpf/Antd.Wpf.csproj](../../src/Antd.Wpf/Antd.Wpf.csproj)为准：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net48;net8.0-windows</TargetFrameworks>
    <UseWPF>true</UseWPF>
    <AssemblyName>Antd.Wpf</AssemblyName>
    <RootNamespace>Antd.Wpf</RootNamespace>
    <IsPackable>true</IsPackable>
  </PropertyGroup>
</Project>
```

Gallery保留同样两目标、`OutputType=WinExe`、`IsPackable=false`，通过ProjectReference开发。发布验收Consumer改为安装真实nupkg，不能依靠ProjectReference掩盖资源/资产打包错误。

本轮已选择xUnit2.9.3、Microsoft.NET.Test.Sdk17.14.1和VSTest运行模式，两目标的测试由STA线程承载。没有启用MTP，也没有运行覆盖率工具。包Consumer目标为net48/net481/net8/net10，使用包内容hash隔离还原缓存。

Framework引用使用已安装4.8 Targeting Pack，或编译专用ReferenceAssemblies包并设置PrivateAssets，实际创建时确认版本与还原。共享代码使用C#12编译器并不获得现代运行时API；record/init/required等需要兼容类型的特性在引入前核对，不为语法统一大量补垫片。

公开类型PascalCase、接口I前缀、私有字段下划线camelCase；WPF约定的静态DependencyProperty/RoutedEvent字段保留`TextProperty`/`ChangedEvent`等公开命名。事件处理器的async void仅用于不可避免的UI事件入口，内部业务返回Task并观察异常。

## 4. XAML与资源格式

- 控件模板按组件拆分，DefaultStyleKey/ThemeInfo/Generic.xaml作为库资源接入点；Gallery的页面资源不进入库默认字典。
- 模板命名PART与VisualStateGroup以组件规格为契约；绑定使用TemplateBinding或TemplatedParent，避免到处向上寻找视觉树或每帧转换。
- 主题键采用稳定命名和正确资源类型。数据状态不要用颜色值推断；文字/数值格式使用CultureInfo，禁用/只读/错误状态单独表达。
- 默认显式原生样式；只有Ant类型的默认样式自动应用，避免改变宿主/第三方控件外观。
- 根editorconfig不能自动证明XAML语义正确；构建、设计器加载、模板运行与UI行为需要各自验证。

## 5. dotnet skills 路由

项目默认使用`ponytail:ponytail`的full模式。开始编码前读取当前环境中的SKILL.md，核对受影响的调用者、测试、资源与打包入口，采用满足本批验收的最小完整修改，并在交付时说明实际验证及剩余限制。

Ponytail负责修改范围和实现取舍，dotnet skills按下表负责各专项步骤。已明确专项时直接使用对应技能；归属不明确时由`csharp-expert`路由，按任务所需组合技能。

使用前在本次环境的可用skills目录中找到精确技能并读取，不仅写一个技能名字就声称已执行。以下均为本次环境已列出的技能；按实际工作触发，不要求每次改动加载整套。

| 触发工作 | 使用技能 | 必须产出的证据 |
| --- | --- | --- |
| 不清楚.NET专项归属、C#无更窄技能 | `csharp-expert` | 明确真实项目与责任技能，不以路由代替完成 |
| 创建真实.NET项目/选择模板 | `template-discovery`、`template-instantiation`（分别按需要） | 真实TFM、WPF属性、项目引用与最小构建 |
| 多项目重复构建策略/导入层级缺陷 | `directory-build-organization` | 读取所有受影响项目，验证最终导入和值 |
| 具体MSBuild属性/Item/Target问题 | `property-patterns` / `item-management` / `target-authoring` | 原因、修复与最终求值，不把示例复制成无需求配置 |
| 迁移到中央包管理 | `convert-to-cpm` | 保留既有版本语义与还原结果 |
| 创建/恢复测试工程和发现关系 | `scaffold-dotnet-test-project` | 方案发现、生产引用、平台/框架兼容与最小执行 |
| 新增或修改测试 | `code-testing`及相应框架技能 | 可观察行为断言；修复前失败/修复后成功的必要证据 |
| 运行.NET测试或给出精确命令 | `run-tests` | 先辨平台/框架，再给正确命令与实际结果 |
| 审查已有热点C#代码 | `analyzing-dotnet-performance` | 源码范围、真实命中与运行时适用性；未有源码不执行虚假扫描 |
| 真实进程CPU/GC/泄漏/阻塞取证 | `dotnet-trace-collect` | 运行时/机器/权限、场景与原始采样 |
| 使用BenchmarkDotNet | `microbenchmarking` | 固定参数、Release、对照基线；不代替WPF UI测量 |
| 实测构建慢 | `build-perf-baseline`后按证据选`build-perf-diagnostics`等 | 冷/热/增量基线与瓶颈证据 |
| 捕获/诊断MSBuild二进制日志 | `binlog-generation` / `binlog-failure-analysis` | 正确归属的日志与失败根因 |
| 保持行为的C#重构 | `csharp-refactoring` | 调用点、公开接口、异常与验证全部保留 |

WPF任务不能因为有XAML就套用MAUI/WinForms/Blazor技能。`analyzing-dotnet-performance`的现代API建议在net48共享路径必须重新判断，不能机械套用Span、GeneratedRegex、FrozenDictionary等建议。

## 6. 验证流程与完成定义

1. 先读取对应组件规格、影响调用者和项目文件；明确本次变更的行为与目标。
2. 格式变更执行有针对性的格式验证；编译器/分析器规则在真实解决方案建好后通过构建生效。
3. 库两目标均编译；纯逻辑、STA、UI自动化分层运行；具体测试框架与平台在创建工程时由测试skills确认，不能虚构当前存在的测试命令。
4. 影响性能敏感路径时运行对应PF场景，保留机器信息和原始结果；无实现/无适合机器时记录未验证。
5. 发布前验证真实NuGet消费、Pack URI、主题资源、公开API、许可、x64及x86关键场景。

新组件的完成条件为：规格中本批功能完成、状态与键盘/辅助功能可用、绑定不被破坏、生命周期清理明确、两目标通过、必要行为验证及性能场景达标、Gallery与文档同步。公开API示例可编译性与行为正确性分别验证。

参考：[SDK选择](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)、[构建自定义导入](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory)、[代码格式选项](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/code-style-rule-options)、[中央包管理](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)。
