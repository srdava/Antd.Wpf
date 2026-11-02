# Gallery 示例滚动与导航裁剪修复

用户反馈：部分examples无法滚动预览完整内容，Layout的Navigation折叠/缩放后显示不全。本次只修改Gallery和对应回归验证，不改变通用Sider对任意Content的布局规则。

## 复现与原因

回归测试直接引用Gallery项目并加载真实编译XAML窗口，使用原生Window和布局/滚动命令，不复制一份简化模板。

```powershell
dotnet test tests/Antd.Wpf.Tests/Antd.Wpf.Tests.csproj -c Release -f net8.0-windows --filter FullyQualifiedName~GalleryPreviewTests -v minimal
```

修复前8项中7项失败：六个Matrix窗口末项超出实际Window.RenderSize，主Gallery已有的ScrollViewer检查通过；Layout导航折叠检查失败。进一步的布局裁剪检查复现展开状态按钮底部也被裁掉约6 DIP：按钮范围16,46.4,168,32，祖先StackPanel可用高度不足。原因是矩阵窗口没有滚动容器，以及导航固定文字/留白未适配200/80两态和有限正文高度。

## 修改

- Visual/Divider/Flex/Space/Grid/Layout六个Matrix页增加双向自动滚动。内容宽度绑定ViewportWidth，并保留示例最小宽度（普通页1000、Layout1200 DIP）；窗口更窄时横向滚动，窗口变矮时纵向滚动。最小宽度只属于Gallery展示，不是库控件的MinWidth。
- Layout导航使用独立滚动区域和12 DIP内容留白；折叠时隐藏Navigation标题，Overview显示紧凑图标，并保留ToolTip和AutomationProperties.Name。展开恢复文字。检查按钮完整落在正文中，且没有被任一祖先布局clip裁剪。
- 截图导出在Matrix根为ScrollViewer时渲染完整示例内容，而不是只截当前可视区域；交互窗口仍按实际可用空间滚动。主Gallery的截图及原有滚动方式沿用。
- 既有测试项目新增指向Gallery的ProjectReference，作为实际UI资源的回归入口；产品库没有引用Gallery，也没有增加运行时NuGet依赖或新项目。

## 验证

| Requirement | Evidence |
| --- | --- |
| 六个例页缩小后可到达末项 | `Gallery_ExampleWindowsCanScrollToLastExampleAfterResize`，650×420后再改650×340，滚至底部/右端，断言实际末项在窗口可视范围内 |
| 导航展开、折叠、窗口缩小后不裁剪 | `Gallery_LayoutNavigationStaysReadableWhenCollapsedOrResized`，两侧主题均验证实际按钮/图标尺寸、所有祖先布局clip及恢复后的文字 |
| 原主Gallery入口仍能滚动到末项 | `Gallery_MainWindowCanScrollToLastExamplesLink` |

修复顺序保留了失败证据：单独增加矩阵滚动后六项通过，导航裁剪仍失败；再修复导航后8项全部通过。net48与net8均执行该组回归；完整构建、实际包消费和更新PNG由Verify.ps1统一检查。真实触控、鼠标驱动及完整多DPI人工验收仍属于既有未完成门槛。

## 输入文字居中与邮箱示例

后续反馈确认“Text不居中”指输入框文字。三种尺寸的64 DIP单行输入中，编辑文字原生行框中心偏下约1.24–1.88 DIP；原有固定行高产生不对称leading。单行改用原生字体行框，内部编辑ScrollViewer传递VerticalContentAlignment；外框MinHeight、Padding和多行行高保留。没有增加视觉节点、转换器、布局循环或逐次文本处理。

Email演示原先将`@`放在前缀、`.com`放在后缀，容易形成错误地址；现在完整输入地址，Placeholder为`name@example.com`，不添加地址片段。通用Prefix/Suffix能力及VisualMatrix装饰示例保留。

| Requirement | Evidence |
| --- | --- |
| 三种尺寸的输入文字与占位居中 | `Input_TextAndPlaceholderStayVerticallyCentered`，修复前3项失败；验证64 DIP、默认高度、中英文内容、空值及Top/Bottom覆盖 |
| 邮箱展示完整可编辑地址 | `Gallery_EmailAcceptsACompleteAddressWithoutDecoratedFragments`，修复前失败；验证真实Gallery输入的占位、无地址片段、完整地址编辑及占位消失 |

新增4项定向回归修复前全部失败，修复后全部通过；最终双端及包消费结果见`docs/m0-validation.json`。未声明完整多DPI/物理IME验收或性能提升。
