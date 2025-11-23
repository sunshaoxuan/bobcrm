# GUIDE-07 模板默认与设计器更新说明

面向前端/后端协作者，说明最近一轮模板默认生成与设计器行为的更新，便于验证和排查。

---

## 1. 范围与背景
- 实体元数据加载：新增公共端点，解决系统实体字段无法在设计器显示的问题。
- 模板生成：默认模板名称改为 i18n Key，List/Edit 模板默认布局增强。
- 前端设计器与列表：空布局自动补全、按钮补充、多语展示修正。

---

## 2. 变更摘要
| 类别 | 内容 | 位置 |
| --- | --- | --- |
| API | 新增公共端点 `/api/entities/{entityType}/definition`，返回实体字段与接口列表（大小写/复数/`entity_` 前缀自适应）。 | `src/BobCrm.Api/Endpoints/EntityDefinitionEndpoints.cs` |
| 后端 | 默认模板生成：模板名使用 `TEMPLATE_NAME_{USAGE}_{ENTITY}` Key；List DataGrid 自动配置列/搜索/刷新/分页；Edit 模板新增“Back/Cancel”按钮；生成时确保 `GetInitialDefinition` 先填充元数据。 | `src/BobCrm.Api/Services/DefaultTemplateGenerator.cs` 等 |
| 前端 | 设计器：初始化即加载实体与字段（含重试/候选路由），空的 List 模板自动补一个 DataGrid 并提示；头部新增 Cancel/Return；实体树加载失败有提示。 | `src/BobCrm.App/Components/Pages/FormDesigner.razor` 等 |
| 前端 | 模板列表：根据 i18n Key 或模式渲染模板名/实体名，多语显示修正；DataGrid 运行态在无列配置时使用默认/占位列。 | `src/BobCrm.App/Components/Pages/Templates.razor`, `Components/Shared/DataGridRuntime.razor` |

---

## 3. 接口要点（/api/entities/{entityType}/definition）
- **访问**：匿名允许，返回实体定义的投影，避免循环引用。
- **参数规范**：`entityType` 支持小写、`entity_` 前缀、单复数自适应（如 `user` / `users` / `entity_user`）。
- **返回字段**：
  - `EntityRoute`, `EntityName`, `FullTypeName`, `DisplayName`, `Description`
  - `Fields[]`：`PropertyName`, `DisplayName`, `DataType`, `SortOrder`, `IsRequired`, `Length`, `EnumDefinitionId`, `IsMultiSelect`
  - `Interfaces[]`：`InterfaceType`, `IsEnabled`
- **主要用途**：Form Designer、EntityMetadataTree 加载系统/动态实体的字段结构。

---

## 4. 验证要点
- 设计器打开系统实体模板时，左侧“实体结构”能加载字段；无字段时显示警告，而非空白或 500。
- 新建或空的 List 模板自动插入 DataGrid（含搜索、刷新、分页、默认列）。
- 新生成的 Edit 模板包含 Back/Cancel 按钮；List/Detail/ Edit 模板名称显示为多语（使用 i18n Key）。
- 模板列表页切换语言后，模板名称和实体标签随语言更新。
