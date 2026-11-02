# 逐组件实现规格索引

基线：Ant Design 官网6.6.8，资料检查日期2026-10-29。覆盖官网总览**73个核心入口 + 6个外部Pro入口 = 79项**；包含已废弃List和仅类型工具Util，不等于需要创建79个WPF可视控件。

目标：保留.NET Framework4.8+；现代.NET最低8，产品包`net48;net8.0-windows`。目前已实现M0基础切片、AntSwitch、原生AntCheckBox/AntRadioButton单项、AntDivider、支持换行及分散对齐的AntFlexPanel、AntSpace、AntRow/AntCol及Layout/Sider手动折叠，并按官方6.6.8校准视觉token；响应式布局、Compact、组控件和其余规格仍按批次推进。[实施状态](../implementation-status.md)记录实际完成与未验收范围。

先读[共用要求](common.md)，再读本组件；[整体架构](../architecture/README.md)定义公开接口/模块/所有权，[性能契约](../architecture/performance.md)定义复杂度、资源上限与PF01–PF10回归，[dotnet规范](../engineering/dotnet-conventions.md)定义后续项目格式与skills使用。

每项有官方来源、能力分析、拟定WPF契约、功能分批、性能/生命周期及至少3个验收场景。P1/P2/P3针对具体子能力，不代表当前已经交付。核心文档已逐项读取；新上游变更需更新来源manifest再对照规格，不能悄悄改变本项目契约。

## 原始资料与边界

[73项官方来源与SHA-256](../analysis/official/manifest.json)；[Pro资料来源与Git blob](../analysis/official/pro/manifest.json)。REA既有部分首页观察与入口脚本证据见[原计划第2节](../Antd.Wpf-Implementation-Plan.zh-CN.md)。本轮逐组件结论主要来自官方Markdown/API/示例说明，未声称完成每个示例的浏览器交互或WPF性能测量。

## 通用（4）

| 组件 | 实现要求 | 官方参考 |
| --- | --- | --- |
| Button | [button](01-foundation-layout-navigation.md#button) | [官方文档](https://ant.design/components/button/) |
| FloatButton | [float-button](01-foundation-layout-navigation.md#float-button) | [官方文档](https://ant.design/components/float-button/) |
| Icon | [icon](01-foundation-layout-navigation.md#icon) | [官方文档](https://ant.design/components/icon/) |
| Typography | [typography](01-foundation-layout-navigation.md#typography) | [官方文档](https://ant.design/components/typography/) |

## 布局（7）

| 组件 | 实现要求 | 官方参考 |
| --- | --- | --- |
| Divider | [divider](01-foundation-layout-navigation.md#divider) | [官方文档](https://ant.design/components/divider/) |
| Flex | [flex](01-foundation-layout-navigation.md#flex) | [官方文档](https://ant.design/components/flex/) |
| Grid | [grid](01-foundation-layout-navigation.md#grid) | [官方文档](https://ant.design/components/grid/) |
| Layout | [layout](01-foundation-layout-navigation.md#layout) | [官方文档](https://ant.design/components/layout/) |
| Masonry | [masonry](01-foundation-layout-navigation.md#masonry) | [官方文档](https://ant.design/components/masonry/) |
| Space | [space](01-foundation-layout-navigation.md#space) | [官方文档](https://ant.design/components/space/) |
| Splitter | [splitter](01-foundation-layout-navigation.md#splitter) | [官方文档](https://ant.design/components/splitter/) |

## 导航（7）

| 组件 | 实现要求 | 官方参考 |
| --- | --- | --- |
| Anchor | [anchor](01-foundation-layout-navigation.md#anchor) | [官方文档](https://ant.design/components/anchor/) |
| Breadcrumb | [breadcrumb](01-foundation-layout-navigation.md#breadcrumb) | [官方文档](https://ant.design/components/breadcrumb/) |
| Dropdown | [dropdown](01-foundation-layout-navigation.md#dropdown) | [官方文档](https://ant.design/components/dropdown/) |
| Menu | [menu](01-foundation-layout-navigation.md#menu) | [官方文档](https://ant.design/components/menu/) |
| Pagination | [pagination](01-foundation-layout-navigation.md#pagination) | [官方文档](https://ant.design/components/pagination/) |
| Steps | [steps](01-foundation-layout-navigation.md#steps) | [官方文档](https://ant.design/components/steps/) |
| Tabs | [tabs](01-foundation-layout-navigation.md#tabs) | [官方文档](https://ant.design/components/tabs/) |

## 数据录入（18）

| 组件 | 实现要求 | 官方参考 |
| --- | --- | --- |
| AutoComplete | [auto-complete](02-data-entry.md#auto-complete) | [官方文档](https://ant.design/components/auto-complete/) |
| Cascader | [cascader](02-data-entry.md#cascader) | [官方文档](https://ant.design/components/cascader/) |
| Checkbox | [checkbox](02-data-entry.md#checkbox) | [官方文档](https://ant.design/components/checkbox/) |
| ColorPicker | [color-picker](02-data-entry.md#color-picker) | [官方文档](https://ant.design/components/color-picker/) |
| DatePicker | [date-picker](02-data-entry.md#date-picker) | [官方文档](https://ant.design/components/date-picker/) |
| Form | [form](02-data-entry.md#form) | [官方文档](https://ant.design/components/form/) |
| Input | [input](02-data-entry.md#input) | [官方文档](https://ant.design/components/input/) |
| InputNumber | [input-number](02-data-entry.md#input-number) | [官方文档](https://ant.design/components/input-number/) |
| Mentions | [mentions](02-data-entry.md#mentions) | [官方文档](https://ant.design/components/mentions/) |
| Radio | [radio](02-data-entry.md#radio) | [官方文档](https://ant.design/components/radio/) |
| Rate | [rate](02-data-entry.md#rate) | [官方文档](https://ant.design/components/rate/) |
| Select | [select](02-data-entry.md#select) | [官方文档](https://ant.design/components/select/) |
| Slider | [slider](02-data-entry.md#slider) | [官方文档](https://ant.design/components/slider/) |
| Switch | [switch](02-data-entry.md#switch) | [官方文档](https://ant.design/components/switch/) |
| TimePicker | [time-picker](02-data-entry.md#time-picker) | [官方文档](https://ant.design/components/time-picker/) |
| Transfer | [transfer](02-data-entry.md#transfer) | [官方文档](https://ant.design/components/transfer/) |
| TreeSelect | [tree-select](02-data-entry.md#tree-select) | [官方文档](https://ant.design/components/tree-select/) |
| Upload | [upload](02-data-entry.md#upload) | [官方文档](https://ant.design/components/upload/) |

## 数据展示（21）

| 组件 | 实现要求 | 官方参考 |
| --- | --- | --- |
| Avatar | [avatar](03-display-feedback.md#avatar) | [官方文档](https://ant.design/components/avatar/) |
| Badge | [badge](03-display-feedback.md#badge) | [官方文档](https://ant.design/components/badge/) |
| Calendar | [calendar](03-display-feedback.md#calendar) | [官方文档](https://ant.design/components/calendar/) |
| Card | [card](03-display-feedback.md#card) | [官方文档](https://ant.design/components/card/) |
| Carousel | [carousel](03-display-feedback.md#carousel) | [官方文档](https://ant.design/components/carousel/) |
| Collapse | [collapse](03-display-feedback.md#collapse) | [官方文档](https://ant.design/components/collapse/) |
| Descriptions | [descriptions](03-display-feedback.md#descriptions) | [官方文档](https://ant.design/components/descriptions/) |
| Empty | [empty](03-display-feedback.md#empty) | [官方文档](https://ant.design/components/empty/) |
| Image | [image](03-display-feedback.md#image) | [官方文档](https://ant.design/components/image/) |
| List（上游废弃） | [list](03-display-feedback.md#list) | [官方文档](https://ant.design/components/list/) |
| Listy | [listy](03-display-feedback.md#listy) | [官方文档](https://ant.design/components/listy/) |
| Popover | [popover](03-display-feedback.md#popover) | [官方文档](https://ant.design/components/popover/) |
| QRCode | [qr-code](03-display-feedback.md#qr-code) | [官方文档](https://ant.design/components/qr-code/) |
| Segmented | [segmented](03-display-feedback.md#segmented) | [官方文档](https://ant.design/components/segmented/) |
| Statistic | [statistic](03-display-feedback.md#statistic) | [官方文档](https://ant.design/components/statistic/) |
| Table | [table](03-display-feedback.md#table) | [官方文档](https://ant.design/components/table/) |
| Tag | [tag](03-display-feedback.md#tag) | [官方文档](https://ant.design/components/tag/) |
| Timeline | [timeline](03-display-feedback.md#timeline) | [官方文档](https://ant.design/components/timeline/) |
| Tooltip | [tooltip](03-display-feedback.md#tooltip) | [官方文档](https://ant.design/components/tooltip/) |
| Tour | [tour](03-display-feedback.md#tour) | [官方文档](https://ant.design/components/tour/) |
| Tree | [tree](03-display-feedback.md#tree) | [官方文档](https://ant.design/components/tree/) |

## 反馈（11）

| 组件 | 实现要求 | 官方参考 |
| --- | --- | --- |
| Alert | [alert](03-display-feedback.md#alert) | [官方文档](https://ant.design/components/alert/) |
| Drawer | [drawer](03-display-feedback.md#drawer) | [官方文档](https://ant.design/components/drawer/) |
| Message | [message](03-display-feedback.md#message) | [官方文档](https://ant.design/components/message/) |
| Modal | [modal](03-display-feedback.md#modal) | [官方文档](https://ant.design/components/modal/) |
| Notification | [notification](03-display-feedback.md#notification) | [官方文档](https://ant.design/components/notification/) |
| Popconfirm | [popconfirm](03-display-feedback.md#popconfirm) | [官方文档](https://ant.design/components/popconfirm/) |
| Progress | [progress](03-display-feedback.md#progress) | [官方文档](https://ant.design/components/progress/) |
| Result | [result](03-display-feedback.md#result) | [官方文档](https://ant.design/components/result/) |
| Skeleton | [skeleton](03-display-feedback.md#skeleton) | [官方文档](https://ant.design/components/skeleton/) |
| Spin | [spin](03-display-feedback.md#spin) | [官方文档](https://ant.design/components/spin/) |
| Watermark | [watermark](03-display-feedback.md#watermark) | [官方文档](https://ant.design/components/watermark/) |

## 其他（5）

| 组件 | 实现要求 | 官方参考 |
| --- | --- | --- |
| Affix | [affix](01-foundation-layout-navigation.md#affix) | [官方文档](https://ant.design/components/affix/) |
| App | [app](01-foundation-layout-navigation.md#app) | [官方文档](https://ant.design/components/app/) |
| BorderBeam | [border-beam](01-foundation-layout-navigation.md#border-beam) | [官方文档](https://ant.design/components/border-beam/) |
| ConfigProvider | [config-provider](01-foundation-layout-navigation.md#config-provider) | [官方文档](https://ant.design/components/config-provider/) |
| Util | [_util](01-foundation-layout-navigation.md#_util) | [官方文档](https://ant.design/components/_util/) |

## 官网外链 Pro Components（6，独立产品）

| 组件 | 实现要求 | 官方参考 |
| --- | --- | --- |
| ProLayout | [pro-layout](04-pro-extensions.md#pro-layout) | [官方文档](https://procomponents.ant.design/components/layout/) |
| ProForm | [pro-form](04-pro-extensions.md#pro-form) | [官方文档](https://procomponents.ant.design/components/form/) |
| ProTable | [pro-table](04-pro-extensions.md#pro-table) | [官方文档](https://procomponents.ant.design/components/table/) |
| ProDescriptions | [pro-descriptions](04-pro-extensions.md#pro-descriptions) | [官方文档](https://procomponents.ant.design/components/descriptions/) |
| ProList | [pro-list](04-pro-extensions.md#pro-list) | [官方文档](https://procomponents.ant.design/components/list/) |
| EditableProTable | [editable-pro-table](04-pro-extensions.md#editable-pro-table) | [官方文档](https://procomponents.ant.design/components/editable-table/) |

## 开工顺序

先完成M0：双目标包消费 + Button/Input/Popup + 主题与焦点/DPI + 性能采样。随后按依赖推进基础布局/输入 → 导航/反馈 → 复杂选择/弹层 → 数据表/树。AntVirtualTree、复杂Table组合和Pro组合层单独验证，不改变已发布原生派生控件的基类。

提交组件实现时列出slug、本次完成子能力、对应验收场景、两端验证结果及涉及的PF场景。性能预算尚未实测时保持“待测”，不得改为“通过”。

