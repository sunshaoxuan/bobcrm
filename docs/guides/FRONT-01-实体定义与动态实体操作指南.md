# FRONT-01 实体定义与动态实体操作指南

面向产品与开发协作者，介绍如何在 OneCRM 前端界面完成实体定义、发布与动态数据管理。本指南聚焦“如何使用”，而实现细节请参考 `design/ARCH-20-实体定义管理设计.md`。

---

## 1. 适用对象与前提

| 项目 | 说明 |
| --- | --- |
| 角色 | 实体管理员、实现工程师、解决方案顾问 |
| 权限 | 已登录并具备实体管理权限（可参考 `design/ARCH-21`） |
| 环境 | Blazor Server 前端，默认运行在 `https://localhost:5100` |
| 浏览器 | Chromium/Edge 最新版本，开启中文或日文字体支持 |

---

## 2. 系统概览

### 2.1 技术栈与核心组件
- **Blazor Server + Ant Design Blazor**：提供页面渲染与 UI 组件；
- **DTO 模型**（`src/BobCrm.App/Models/`）：`EntityDefinitionDto`, `FieldMetadataDto`, `EntityInterfaceDto`；
- **服务层**（`src/BobCrm.App/Services/`）：  
  `EntityDefinitionService`（实体 CRUD + 发布）  
  `DynamicEntityService`（动态数据操作）  
  `I18nService`（多语言）；
- **页面组件**（`src/BobCrm.App/Components/Pages/`）：  
  `EntityDefinitions.razor`, `EntityDefinitionEdit.razor`, `EntityDefinitionPublish.razor`, `DynamicEntityData.razor`；
- **共享控件**：`MultilingualInput.razor`, `EntityDefinitionsTable.razor`。

### 2.2 典型流程

```
创建实体定义 → 配置字段/接口 → 发布实体 → 管理动态数据
```

---

## 3. 实体定义管理

### 3.1 列表页 `/entity-definitions`
- **筛选标签**：全部 / 系统 / 自定义 / 草稿；
- **操作按钮**：新建、刷新、编辑、发布、删除（系统实体不可删）；
- **状态标识**：
  - Draft：新建未发布
  - Published：已发布
  - Modified：发布后再次调整
- **来源**：
  - System：平台内置（如 Customer）
  - Custom：用户自建

### 3.2 创建 / 编辑
| 字段 | 说明 |
| --- | --- |
| Namespace | `BobCrm.Base.{业务领域代码}`，生成后不可改 |
| EntityName | PascalCase 类名，创建后不可改 |
| DisplayName / Description | 通过 `MultilingualInput` 维护多语文本 |
| StructureType | Single / MasterDetail / MasterDetailGrandchild |
| 分类 / 图标 | 影响左侧导航展示；支持 AntD 图标或 URL |

### 3.3 子实体与字段
- 子实体以 Tabs 呈现，可增删、排序；
- 字段采用行内编辑：属性名、显示名、多语、类型、长度/精度、必填、默认值、排序；
- 属性名唯一且遵循 PascalCase；
- 接口字段标记为“锁定”，需取消接口才能移除。

### 3.4 接口勾选

| 接口 | 自动字段 | 典型用途 |
| --- | --- | --- |
| Base (IEntity) | Id, IsDeleted, DeletedAt, DeletedBy | 聚合基线（必选） |
| Archive (IArchive) | Code, Name | 档案类实体 |
| Audit (IAuditable) | Created*/Updated* | 创建/修改追踪 |
| Version (IVersioned) | Version | 乐观锁 |
| TimeVersion (ITimeVersioned) | ValidFrom, ValidTo, VersionNo | 时间区间版本 |
| Organization (IOrganizational) | OrganizationId | 组织维度，只保留 ID |

提示：勾选后字段会自动注入到字段表中，底部 `TXT_INTERFACE_AUTO_FIELDS_TIP` 会提示“不可单独删除”。

---

## 4. 发布与动态数据

### 4.1 发布流程
1. 在编辑页完成字段配置；
2. 列表页选择实体 → 点击 **发布**；
3. 发布面板提供 “生成代码 / 编译 / 发布” 三步：  
   - 生成代码：用于预览 C# 输出；  
   - 编译：调用 Roslyn 编译校验；  
   - 发布：执行 DDL 并刷新实体元数据；
4. 发布成功后状态变为 *Published*。

### 4.2 动态实体数据 `/dynamic-entity?entityName=...`
- 自动根据实体元数据渲染列；
- 支持查询、分页、新增、编辑、删除；
- 需保证实体已发布并可通过 API 加载；
- 结合 `DynamicEntityService` 完成数据请求。

---

## 5. 多语言与 I18n

### 5.1 文案维护
- 所有 UI 文案通过 `I18nService` 的 `T()` 方法获取；
- 新增实体时应在 `src/BobCrm.Api/Resources/i18n-resources.json` 添加 `ENTITY_*`、`FIELD_*` 键；
- `MultilingualInput` 提供 `{zh, ja, en}` 三语言的输入区域；
- 建议 key 命名：`ENTITY_{ENTITYNAME}`、`FIELD_{ENTITYNAME}_{FIELD}`。

### 5.2 示例
```json
{
  "ENTITY_PRODUCT": "产品",
  "ENTITY_PRODUCT_DESC": "产品信息管理",
  "FIELD_PRODUCT_CODE": "产品编码"
}
```

---

## 6. 故障排查

| 问题 | 排查步骤 |
| --- | --- |
| 列表为空 | 检查后端 API 状态、浏览器控制台、当前账户权限 |
| 发布失败 | 查看编译日志、检查字段类型/长度、确认数据库连接 |
| 动态数据加载失败 | 确认已发布、FullTypeName 正确、后端日志是否加载实体 |
| 多语未生效 | 确认资源文件同步、`I18nService.LoadAsync` 成功、浏览器缓存 |

---

## 7. 最佳实践

1. **命名规范**：实体/字段使用 PascalCase，多语键使用 UPPER_SNAKE_CASE。  
2. **接口选择**：档案类实体勾选 Archive，需审计的实体勾选 Audit，涉及组织隔离勾选 Organization。  
3. **字段设计**：合理设置字符串长度与排序，必填字段配合默认值提升体验。  
4. **发布前检查**：先生成代码、再编译、最后发布；失败时不要跳过任一步骤。  
5. **权限联动**：若勾选组织接口，确保角色/数据范围覆盖对应组织（参考 ARCH-21）。  
6. **版本管理**：修改已发布实体时优先在草稿中编辑，测试通过后再发布，避免频繁回滚。

---

## 8. 术语速查

| 术语 | 释义 |
| --- | --- |
| Dynamic Entity | 用户自定义并动态发布的实体 |
| Interface Field | 由接口模板注入的保留字段 |
| MultilingualInput | 自研多语言输入控件 |
| Publish | 包括代码生成、编译、DDL 更新三步 |

---

## 9. 相关文档
- 设计：[`design/ARCH-20-实体定义管理设计.md`](../design/ARCH-20-实体定义管理设计.md)
- 权限：[`design/ARCH-21-组织与权限体系设计.md`](../design/ARCH-21-组织与权限体系设计.md)
- API：[`reference/API-01-接口文档.md`](../reference/API-01-接口文档.md)
- 测试概览：[`guides/TEST-00-测试综述.md`](TEST-00-测试综述.md)

如需进一步支持，请联系平台研发团队或在 Issue 系统提交需求。
