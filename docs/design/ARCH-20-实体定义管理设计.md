# ARCH-20 实体定义管理设计

## 1. 引言
实体定义管理是 OneCRM 的元数据枢纽，负责：

- 以多语言方式维护实体/字段信息；
- 将主实体与子实体 1:N 关系以内嵌页签呈现；
- 自动注入接口字段（Base/Audit/Version/Organization 等）并同步到发布流程；
- 启用“所见即所得”的 UI/UE 规范，避免旧版“弹窗+表单”式体验。

本文提供从领域模型到前端交互的完整设计，取代旧版 `UI-04-实体定义画面改版说明.md` 中零散的补丁式记录。

## 2. 范围与非目标
### 2.1 范围
- 主实体信息卡片：命名空间生成、业务领域、图标、分类、多语说明；
- 子实体 Tabs：行内增删字段、排序、接口字段占位；
- 多语言输入组件；
- 接口字段同步（Base/Archive/Audit/Version/TimeVersion/Organization）；
- API 与服务层契约；
- 前端渲染与交互状态；
- 测试策略。

### 2.2 非目标
- 动态实体运行时数据管理；
- 模板/布局设计器；
- 权限与发布流程（见 `ARCH-21`）。

## 3. 术语
| 术语 | 说明 |
| --- | --- |
| EntityDefinition | 主实体定义聚合根 |
| SubEntityDefinition | 子实体定义 |
| FieldMetadata | 字段元数据 |
| InterfaceField | 由接口模板注入的保留字段 |
| MultilingualInput | 自定义多语输入控件 |

## 4. 功能概览
### 4.1 主实体信息
1. **命名空间生成**：`BobCrm.Base.{业务领域代码}`，只读展示；领域来自下拉框（CRM/SCM/FA/HR/MFM/Custom）。
2. **实体名校验**：PascalCase，无空格、数字开头。
3. **描述/图标/类别**：图标支持 AntD 图标名或 URL，类别用于左侧导航分组。
4. **多语言显示名**：复用 `MultilingualInput`，提示文案 `LBL_CLICK_TO_EDIT_MULTILINGUAL`。

### 4.2 子实体与字段
- Tabs 代表子实体，支持新增、重命名、删除；
- 字段行内编辑：PropertyName、DisplayName（多语）、类型、长度/精度、必填、默认值、排序；
- 禁止删除接口字段，除非取消对应接口；
- 页面底部自动校验：字段名唯一、必填项完成、接口字段齐全。

### 4.3 接口勾选
| 接口 | 字段 | 说明 |
| --- | --- | --- |
| Base | Id/IsDeleted/DeletedAt/DeletedBy | 聚合必需字段 |
| Archive | Code/Name | 档案信息 |
| Audit | Created*/Updated* | 审计 |
| Version | Version | 乐观锁 |
| TimeVersion | ValidFrom/ValidTo/VersionNo | 时间版本 |
| Organization | OrganizationId | 组织维度（仅保留 ID） |

## 5. 领域模型
### 5.1 聚合关系
```
EntityDefinition
 ├── SubEntityDefinition (List)
 │    └── FieldMetadata (List)
 └── EntityInterface (List)
```

### 5.2 关键约束
- `EntityDefinition.Code` 唯一；
- 子实体 `Code` 在同一主实体内唯一；
- `FieldMetadata.PropertyName` 在其所属实体内唯一；
- 接口字段所有权由 `_interfaceFieldOwnership` 字典管理，避免误删；
- `OrganizationId` 在实体层只存 ID，其他信息由权限体系补齐。

### 5.3 字段软删除机制
为保证数据完整性和可追溯性，`FieldMetadata` 采用软删除模式：

**软删除字段**：
- `IsDeleted`：布尔标志，默认 `false`
- `DeletedAt`：删除时间戳（可空）
- `DeletedBy`：删除操作用户ID（可空）

**删除规则**：
- **Custom 字段**：标记为已删除，保留元数据记录
- **Interface/System 字段**：必须先取消接口或不可删除
- 必填字段软删除时，DDL 生成逻辑自动将其改为可空类型

**查询过滤**：
- 所有字段查询 API 自动添加 `.Where(f => !f.IsDeleted)` 过滤
- DDL 预览、实体编辑页面均只显示未删除字段
- 软删除记录保留在数据库，用于审计和回滚

## 6. 后端与 API
### 6.1 AppDbContext
新增 DbSet：`EntityDefinitions`、`SubEntityDefinitions`、`FieldMetadatas`、`EntityInterfaces` 并在 `OnModelCreating` 中配置 jsonb 转换。

### 6.2 核心端点
| 方法 | 路径 | 说明 |
| --- | --- | --- |
| `GET /api/entity-definitions` | 列表查询 |
| `POST /api/entity-definitions` | 创建实体（含字段、接口） |
| `PUT /api/entity-definitions/{id}` | 更新实体（含接口同步、字段 CRUD） |
| `POST /api/entity-definitions/{id}/publish` | 触发发布（独立文档描述） |

后台使用 `InterfaceFieldMapping.GetFields` 注入保留字段；`OrganizationId` 注入时带上 `IsEntityRef = true`，由权限模块负责引用组织表。

### 6.3 字段更新逻辑（`PUT /api/entity-definitions/{id}`）
字段更新遵循 **Source-based 保护策略**：

**System 字段**（`Source = FieldSource.System`）：
- 只允许更新：`DisplayName`、`SortOrder`
- 禁止修改：数据类型、必填性、长度、精度等核心属性
- 示例：Customer 的 Id、Code、Name 字段

**Interface 字段**（`Source = FieldSource.Interface`）：
- 允许更新：`DisplayName`、`SortOrder`、`DefaultValue`
- 禁止修改：数据类型、必填性（由接口定义决定）
- 示例：CreatedAt、UpdatedAt、OrganizationId

**Custom 字段**（`Source = FieldSource.Custom`）：
- 允许更新：所有属性（除 `Source` 本身）
- 用户自定义字段，拥有完全控制权

**字段新增**：
- 前端生成临时 GUID（仅用于 UI 状态管理）
- 后端检测到不存在的 ID 时，创建新 `FieldMetadata` 对象
- **不使用前端提供的 ID**，让数据库自动生成主键，避免 `DbUpdateConcurrencyException`

**字段删除**：
- Custom 字段：软删除（设置 `IsDeleted = true`、`DeletedAt`、`DeletedBy`）
- Interface/System 字段：抛出异常（必须先取消接口勾选）
- 必填字段软删除时，DDL 生成器自动将其改为 `nullable`

## 7. 前端实现
### 7.1 组件结构
- `EntityDefinitionEdit.razor`：承载主表单、Tabs、字段表格、接口侧边；
- `MultilingualInput.razor`：复用 JS 与 CSS；
- 状态管理：`_selectedInterfaces`、`_interfaceFieldOwnership`、`_model.Fields`。

### 7.2 交互细则
1. **折叠卡片**：主实体信息使用 Card + Collapse，默认展开，可收起节省空间；
2. **表格样式**：`field-editor-table` CSS 提供 hover、分隔、必填图标；
3. **多语提示**：空值时显示 `LBL_CLICK_TO_EDIT_MULTILINGUAL`；
4. **接口提示**：底部 `TXT_INTERFACE_AUTO_FIELDS_TIP` 描述不可直接删除接口字段；
5. **组织接口**：勾选后仅显示 `OrganizationId` 字段，提示"绑定组织树实现分组织管理"；
6. **操作反馈**：使用全局 `ToastService` 提供清晰的操作结果反馈。

### 7.3 全局 Toast 通知系统
为提升用户体验，实体定义编辑页面集成全局 Toast 通知系统：

**ToastService** (`src/BobCrm.App/Services/ToastService.cs`)：
- 提供四种消息类型：`Success()`、`Error()`、`Warning()`、`Info()`
- 顶部居中显示，最多显示 3 条消息
- 3 秒自动隐藏，带有滑动和淡入动画
- 自动队列管理，超过 3 条时移除最旧的消息

**GlobalToast 组件** (`src/BobCrm.App/Components/Shared/GlobalToast.razor`)：
- 在 `MainLayout.razor` 中全局注册
- 使用 `System.Timers.Timer` 实现自动隐藏
- 支持浅色/深色主题

**色彩编码**：
- **成功**：浅绿底（`rgba(240, 249, 235, 0.95)`）+ 深绿字（`#389e0d`）+ 绿色边框（`#52c41a`）
- **错误**：浅红底（`rgba(255, 241, 240, 0.95)`）+ 大红字（`#cf1322`）+ 红色边框（`#ff4d4f`）
- **警告**：浅黄底（`rgba(255, 251, 230, 0.95)`）+ 深黄字（`#d48806`）+ 黄色边框（`#faad14`）
- **信息**：浅蓝底（`rgba(230, 247, 255, 0.95)`）+ 深蓝字（`#0958d9`）+ 蓝色边框（`#1890ff`）

**使用示例**（EntityDefinitionEdit.razor）：
```csharp
@inject BobCrm.App.Services.ToastService ToastService

private async Task HandleSave()
{
    if (!ValidateFields())
    {
        ToastService.Error(I18n.T("MSG_DISPLAY_NAME_REQUIRED"));
        return;
    }

    try
    {
        await EntityDefService.UpdateAsync(Id.Value, request);
        ToastService.Success(I18n.T("MSG_UPDATE_SUCCESS"));
        GoBack();
    }
    catch (Exception ex)
    {
        ToastService.Error($"{I18n.T("MSG_SAVE_FAILED")}: {ex.Message}");
    }
}
```

## 8. 测试策略
| 测试层级 | 重点 |
| --- | --- |
| 单元测试（后端） | 接口字段注入、命名空间生成、接口字段锁定、数据验证 |
| 单元测试（前端 BUnit） | `_interfaceFieldOwnership` 逻辑、折叠/展开状态、字段排序 |
| 集成测试 | `EntityDefinitionEndpoints` CRUD、接口字段序列化 |
| 手工回归 | 多语言输入、接口切换、子实体 Tab 操作、保存/发布流程 |

## 9. 部署与后续
- 上线前运行 `dotnet ef database update` 同步最新结构；
- 若需新增接口类型，需同时扩展 `InterfaceFieldMapping`、前端模板、i18n 文案；
- 后续工作：将功能与权限文档拆分至 `ARCH-21`，在文档索引中按主题索引，避免补丁式记录。
