# 官网外部 Pro Components 入口要求

官网组件总览另外链接到以下6个独立产品入口。这里也逐项分析，避免“每个组件”漏掉这些入口；它们作为P3之后的业务组合层，不计入73个核心入口或核心1.0工作量。

来源：官网外链页正文在本次文本检索中为空，改用官方`ant-design/pro-components`仓库对应文档。六份文档由GitHub连接器读取并保存在[Pro资料目录](../analysis/official/pro/)，来源与Git blob标识见[清单](../analysis/official/pro/manifest.json)。Pro与antd独立版本，不能声称这些文档固定在antd6.6.8版本上。以下是文档分析和WPF拟定要求，不是运行时验证。

共同要求见[common.md](common.md)；组合层只依赖核心控件和宿主VM，不反向影响核心包。没有实际业务消费者之前不新增Pro程序集。

<a id="pro-layout"></a>

## pro-layout — ProLayout

官方：[页面](https://procomponents.ant.design/components/layout/)；[官方文档源码](https://github.com/ant-design/pro-components/blob/master/site/zh-CN/components/layout/index.mdx)。

- 官网能力：side/top/mix布局、响应式折叠、菜单/路由数据生成、PageContainer标题与面包屑、页脚/操作区、加载和主题设置。
- WPF契约：拟定组合视图包含Header/Sider/Content/Footer插槽、导航ItemsSource、SelectedKey、IsCollapsed、NavigationCommand、PageTitle与BreadcrumbItems；路由状态由宿主负责，不复制Umi/history。
- 实现要求：复用AntLayout/Menu/Breadcrumb/Spin，支持布局模式和实际窗口宽度断点；权限过滤由宿主提供可见菜单，隐藏菜单不代表权限验证；持久页缓存的生命周期可控。
- 性能/生命周期：只实例化当前页面，缓存数量有界；折叠不重建业务页，切换导航时释放库拥有的旧页资源；不在每帧重新计算整棵菜单。
- 验收：①宽度跨断点后折叠且当前页面/选择保持；②异步菜单返回失败/取消不覆盖新导航；③窗口关闭后页缓存与菜单订阅可回收。SettingDrawer、跨应用列表及页面权限集成均列后续，不内置登录/授权框架。

<a id="pro-form"></a>

## pro-form — ProForm

官方：[页面](https://procomponents.ant.design/components/form/)；[官方文档源码](https://github.com/ant-design/pro-components/blob/master/site/zh-CN/components/form.mdx)。

- 官网能力：基于Form的布局和默认行为、异步onFinish自动Loading、初始化request、字段依赖、值转换、Modal/Drawer/Steps/查询表单及URL同步。
- WPF契约：组合AntFormItem和原生Binding，提交/重置命令、IsSubmitting、初始化状态、可选分步状态；字段类型通过模板选择，不引入另一个全局表单仓库。
- 实现要求：提交前验证，异步成功/失败状态可观察；转换只在明确显示/提交阶段执行；窗口/抽屉复用同一字段契约。URL同步不适用于普通WPF宿主，替换为由宿主决定的状态持久化。
- 性能/生命周期：只更新有依赖的字段，分步懒实例化；初始化/提交取消与请求版本保护；密码/敏感字段默认不写缓存或日志。
- 验收：①无效字段阻止提交且定位首错；②连续点击只提交一次，失败保留用户值并可重试；③重置和后到的初始化结果不覆盖已编辑数据。QueryFilter/LightFilter/StepsForm作为后续组合子项，不作为首期另造校验引擎。

<a id="pro-table"></a>

## pro-table — ProTable

官方：[页面](https://procomponents.ant.design/components/table/)；[官方文档源码](https://github.com/ant-design/pro-components/blob/master/site/zh-CN/components/table/index.mdx)。

- 官网能力：表格、查询表单、request/params、排序筛选分页、工具栏/列设置、刷新、数据格式/值类型及多种分页能力。
- WPF契约：AntTable + 查询区 + Toolbar模板 + Pagination；明确QueryState、IsLoading、Error与Reload/QueryCommand，宿主拥有网络、总数/游标和缓存。
- 实现要求：同一查询快照驱动筛选/排序/页码；列显示/顺序和持久化通过稳定ColumnKey；数值、日期的显示/编辑模板分离，不把服务端数据强制转成字符串。明确手动首次加载、防抖与参数覆盖次序；官网mergeFields的页内连续同值合并列为后续能力，需与虚拟化/排序筛选组合单独验证。
- 性能/生命周期：优先服务器分页；一次查询只触发一次请求；取消和版本校验防旧结果；列设置更新不重建全部数据。普通表、树表、合并/固定列仍遵循核心Table能力矩阵。
- 验收：①改变筛选按约定回第一页并只查询一次；②快速切页旧响应不能覆盖新页；③隐藏/重排列后编辑提交仍映射正确ColumnKey。导出/全量跨页选择与游标分页属于独立后续能力，不能伪装成本地全量数据已加载。

<a id="pro-descriptions"></a>

## pro-descriptions — ProDescriptions

官方：[页面](https://procomponents.ant.design/components/descriptions/)；[官方文档源码](https://github.com/ant-design/pro-components/blob/master/site/zh-CN/components/descriptions.mdx)。

- 官网能力：数据描述布局、columns/valueType、request加载、字段格式和可编辑描述项，与ProTable共享配置语义。
- WPF契约：AntDescriptions + 属性描述项 + 显示/编辑DataTemplate；数据对象、IsLoading、Error、Edit/Save/Cancel命令。
- 实现要求：只读和编辑值分离，字段级验证；Span/响应式布局可用；转换/值枚举由宿主提供；保存失败不得把显示值提前当成成功结果。
- 性能/生命周期：每个字段不创建一套网络/主题服务；数据对象替换仅刷新相关内容，异步加载按版本合并；结束编辑释放库拥有的事件。
- 验收：①空值/枚举/日期格式按Culture正确显示；②单字段保存失败保留编辑并可取消；③改变列数后长标签和Span不重叠。批量编辑与复杂嵌套值映射后续实现。

<a id="pro-list"></a>

## pro-list — ProList

官方：[页面](https://procomponents.ant.design/components/list/)；[官方文档源码](https://github.com/ant-design/pro-components/blob/master/site/zh-CN/components/list.mdx)。

- 官网能力：columns+listSlot、标题/头像/描述/操作、request/搜索/分页、列表/卡片切换、选择/展开/编辑；旧metas配置已废弃。
- WPF契约：虚拟列表 + ItemTemplate/字段插槽 + 工具栏/分页；SelectedKeys、ExpandedKeys、QueryCommand，身份为稳定Key。
- 实现要求：列表与卡片展示同一数据与选择状态；操作区Hover和键盘Focus均可访问；支持批量操作与明确的本页/跨页选择语义，避免新增已废弃metas模型。
- 性能/生命周期：列表保留回收；卡片网格需要单独虚拟化验证，切换布局不得静默生成所有项；展开详情按需创建，数据请求归宿主。
- 验收：①排序/分页后选中实体正确；②列表/卡片切换不丢选择，键盘能访问操作；③1万项列表生成项数与源总量不线性增长。卡片二维虚拟化未通过前只承诺小数据卡片模式。

<a id="editable-pro-table"></a>

## editable-pro-table — EditableProTable

官方：[页面](https://procomponents.ant.design/components/editable-table/)；[官方文档源码](https://github.com/ant-design/pro-components/blob/master/site/zh-CN/components/editable-table.mdx)。

- 官网能力：ProTable上的单/多行编辑、单元格编辑、editableKeys、创建/复制/删除、异步保存/取消、表单联动及新增行位置/父节点。
- WPF契约：核心Table编辑模型 + 稳定RowKey/ColumnKey的编辑集合、行草稿、Save/Cancel/Delete/Add命令；原始值与草稿必须分离。
- 实现要求：单行/多行模式规则明确；新增行Key唯一，取消临时行与取消已提交行区分；保存失败保留草稿、删除需可处理失败；IEditableObject或明确事务模型与VM对接。
- 性能/生命周期：编辑状态不归回收行容器；分页/排序后按RowKey回到同一草稿；批量编辑不能复制全部数据源；Owner结束取消库等待但不得误报服务端事务回滚。
- 验收：①滚出视口/排序后草稿和错误仍属于原实体；②保存失败可修正重试，重复点击不重复保存；③复制新行产生唯一Key，取消新增不删除既有数据。树形新增、跨页多行事务和复杂表单联动为后续，需定义业务一致性而非默认全支持。
