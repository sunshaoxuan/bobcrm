# 模板设计器与宿主补齐临时进度表

> 用途：跟踪“系统实体模板化闭环”在控件、设计器与宿主侧的逐步交付。完成的步骤会在 `CHANGELOG.md` 留痕，同时从本文件移除对应条目。

## 当前迭代目标

1. **Widget 覆盖**
   - 梳理组织/用户/角色/客户页面所需控件，并输出 Schema（属性、数据源、事件）。
   - 在 FormDesigner palette 中加入控件，占位 UI + 属性面板。
   - 扩展 `RuntimeWidgetRenderer` 支持新控件。

2. **宿主能力**
   - 实现 `ListTemplateHost`，负责列表 View 模板渲染（字段列、筛选、批量操作）。
   - 拆分 PageLoader，按 `UsageType` 分别加载 Detail/Edit 模板与数据。
   - 针对四个系统实体交付默认模板与宿主改造，保证能够仅靠模板运行。

3. **验证与落地**
   - FormDesigner 中复制/编辑系统默认模板 → 发布 → 菜单/角色授权 → 页面成功渲染。
   - 补充端到端用例或脚本，验证模板绑定 + 权限链路。

## 交付检查清单

| 步骤 | 负责人 | 输出 | 状态 |
| --- | --- | --- | --- |
| 设计器控件 Schema 定义 | TBD | Widget Schema 文档 + 示例 JSON（含通用 DataGrid/DataSet） | ☐ |
| FormDesigner 控件实现 | TBD | 新 palette + 属性面板 + 数据源选择器 | ☐ |
| Runtime Widget 渲染器扩展 | TBD | 新控件渲染 + DataGrid 数据管道 + Fallback 逻辑 | ☐ |
| 数据源/条件执行管道 | TBD | DataSet/QueryDefinition 模型、运行态 API、权限注入策略 | ☐ |
| ListTemplateHost & PageLoader 扩展 | TBD | Detail/Edit/List 宿主 & API 调用 | ☐ |
| 系统实体默认模板 | TBD | 组织/用户/角色/客户 List+Detail+Edit 模板 | ☐ |
| E2E 验证与文档更新 | TBD | 测试记录 + 文档/Changelog 更新 | ☐ |

> 说明：每完成一项，将其状态改为 `☑`，同步 `CHANGELOG.md`，然后从本文件删除该行，以保持“仅包含剩余事项”的特性。
