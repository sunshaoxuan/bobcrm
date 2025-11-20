# BobCRM v0.7.0 开发任务 - 模板系统完整闭环

## 项目概述

你正在为 **BobCRM** 项目开发 v0.7.0 版本的**核心功能**：**模板系统完整闭环**。

**技术栈**：
- 后端：.NET 8 (C#), Minimal API, EF Core
- 前端：Blazor Server, Ant Design Blazor
- 数据库：PostgreSQL
- 架构：动态实体系统、模板驱动UI、RBAC权限

## 当前状态

✅ **已完成** (T1-T3):
- 菜单编辑器使用指南完善
- 图标选择器组件
- 菜单导入/导出功能

⏸️ **推迟到 v0.8.0**:
- 菜单实时预览优化
- 菜单编辑器错误处理增强
- 菜单权限验证测试

## 核心任务：模板系统完整闭环

### 什么是"模板系统闭环"？

实现从**设计**到**应用**到**设置**到**显示**的完整链路：

1. **设计**：表单设计器强化，支持所有页面级控件（T4）
2. **应用**：实体发布时自动生成默认模板（T5）
3. **设置**：用户可以从系统模板复制、修改、应用个性化模板（T6）
4. **显示**：菜单导航时，根据关联的模板动态渲染页面（T7）

### 为什么这很重要？

这是 BobCRM 作为 No-Code/Low-Code 平台的核心能力：
- 管理员可以为每个实体设计专业的列表、详情、编辑页面
- 用户可以个性化自己的工作界面
- 菜单导航与模板渲染无缝集成
- 真正实现"无代码配置页面"

---

## 任务清单

### T4: 表单设计器功能强化 (高优先级，3-4天)

#### 目标
增强表单设计器，支持页面级控件，使其满足真实业务页面的需求。

#### 当前问题
- 现有 16 种控件主要是表单字段级别（TextBox, Select 等）
- 缺少页面级控件（如 DataGrid 列表、Tab 标签页、SubForm 主从表单）
- 无法设计复杂的业务页面

#### 核心任务

##### 4.1 新增 DataGrid 控件（最重要）

**用途**：列表页面的核心控件，展示实体数据列表

**创建文件**：`src/BobCrm.App/Models/Widgets/DataGridWidget.cs`

**配置属性**：
```csharp
public class DataGridWidget : BaseWidget
{
    public string EntityType { get; set; } = string.Empty;  // 数据源实体
    public List<DataGridColumn> Columns { get; set; } = new();  // 列配置
    public List<RowAction> RowActions { get; set; } = new();  // 行操作
    public int PageSize { get; set; } = 20;  // 分页大小
    public bool ShowSearch { get; set; } = true;  // 显示搜索框
    public bool ShowPagination { get; set; } = true;  // 显示分页器
}

public class DataGridColumn
{
    public string FieldName { get; set; }  // 字段名
    public string Label { get; set; }  // 列标题
    public int? Width { get; set; }  // 列宽度
    public bool Sortable { get; set; } = true;  // 可排序
}

public class RowAction
{
    public string Label { get; set; }  // 按钮文本
    public string ActionType { get; set; }  // edit, delete, view, custom
    public string Icon { get; set; }  // 图标
}
```

**运行时渲染**：
- 创建 `src/BobCrm.App/Components/Runtime/DataGridRuntime.razor`
- 读取 `EntityType` 和 `Columns` 配置
- 调用实体 API 加载数据
- 渲染 Ant Design `<Table>` 组件

##### 4.2 新增 SubForm 控件

**用途**：主从表单，处理一对多关系（如订单-订单项）

**创建文件**：`src/BobCrm.App/Models/Widgets/SubFormWidget.cs`

**配置属性**：
```csharp
public class SubFormWidget : BaseWidget
{
    public string RelatedEntityType { get; set; }  // 关联实体
    public string ForeignKeyField { get; set; }  // 外键字段
    public int? EmbeddedTemplateId { get; set; }  // 嵌入的子表单模板
}
```

##### 4.3 新增 TabContainer 控件

**用途**：多标签页布局

**创建文件**：`src/BobCrm.App/Models/Widgets/TabContainerWidget.cs`

**配置属性**：
```csharp
public class TabContainerWidget : ContainerWidget
{
    public List<TabItem> Tabs { get; set; } = new();
}

public class TabItem
{
    public string Label { get; set; }  // 标签标题
    public List<BaseWidget> Children { get; set; } = new();  // 子控件
}
```

##### 4.4 新增 Card 卡片控件

**用途**：分组展示表单字段

**创建文件**：`src/BobCrm.App/Models/Widgets/CardWidget.cs`

**配置属性**：
```csharp
public class CardWidget : ContainerWidget
{
    public string Title { get; set; }  // 卡片标题
    public bool Collapsible { get; set; } = false;  // 可折叠
    public bool DefaultExpanded { get; set; } = true;  // 默认展开
}
```

##### 4.5 控件注册

**修改文件**：`src/BobCrm.App/Services/Widgets/WidgetRegistry.cs`

```csharp
// 在静态构造函数中添加
new WidgetDefinition("datagrid", "LBL_DATAGRID", IconType.Outline.Table, WidgetCategory.Data, () => new DataGridWidget()),
new WidgetDefinition("subform", "LBL_SUBFORM", IconType.Outline.Subnode, WidgetCategory.Data, () => new SubFormWidget()),
new WidgetDefinition("tabcontainer", "LBL_TABCONTAINER", IconType.Outline.Tabs, WidgetCategory.Layout, () => new TabContainerWidget()),
new WidgetDefinition("card", "LBL_CARD", IconType.Outline.Container, WidgetCategory.Layout, () => new CardWidget())
```

##### 4.6 运行时渲染支持

**修改文件**：`src/BobCrm.App/Components/Runtime/RuntimeWidgetRenderer.razor`

添加新控件的渲染分支：
```csharp
case "datagrid":
    <DataGridRuntime Widget="@((DataGridWidget)widget)" Context="@Context" />
    break;
case "subform":
    <SubFormRuntime Widget="@((SubFormWidget)widget)" Context="@Context" />
    break;
// ... 其他控件
```

#### 验收标准
- ✅ 4 种新控件可在设计器中拖拽使用
- ✅ 控件配置属性可在属性面板编辑
- ✅ 控件在运行时正确渲染
- ✅ DataGrid 可正常加载和显示数据

---

### T5: 默认模板自动生成 (高优先级，2-3天)

#### 目标
为所有实体自动生成 List（列表）、Detail（详情）、Edit（编辑）三种默认模板。

#### 当前状况
- `DefaultTemplateGenerator.cs` 已存在，但只生成简单的 Detail 模板
- 缺少 List 模板生成
- 生成的模板较简陋，缺少布局优化

#### 核心任务

##### 5.1 增强 DefaultTemplateGenerator

**修改文件**：`src/BobCrm.Api/Services/DefaultTemplateGenerator.cs`

##### 5.2 实现列表模板生成

**新增方法**：`BuildListTemplate(EntityDefinition entity)`

**模板结构**：
```json
{
  "widgets": [
    {
      "type": "section",
      "label": "工具栏",
      "children": [
        {
          "type": "button",
          "label": "新增",
          "action": "create",
          "icon": "plus"
        },
        {
          "type": "textbox",
          "id": "search",
          "placeholder": "搜索..."
        }
      ]
    },
    {
      "type": "datagrid",
      "entityType": "customer",
      "columns": [
        { "fieldName": "name", "label": "名称", "sortable": true },
        { "fieldName": "email", "label": "邮箱", "sortable": true },
        // 自动选择前 5-8 个字段
      ],
      "rowActions": [
        { "label": "查看", "actionType": "view", "icon": "eye" },
        { "label": "编辑", "actionType": "edit", "icon": "edit" },
        { "label": "删除", "actionType": "delete", "icon": "delete" }
      ]
    }
  ]
}
```

##### 5.3 优化详情/编辑模板

**改进点**：
- 使用 Card/Section 分组字段（按字段标签或类型分组）
- 智能选择控件（根据字段类型）
- 添加顶部操作按钮（保存、取消）

##### 5.4 模板生成触发

**修改文件**：`src/BobCrm.Api/Services/EntityDefinitionSynchronizer.cs`

在 `EnsureTemplatesAndBindingsAsync` 方法中：
```csharp
// 生成三种模板
var listTemplate = _defaultTemplateGenerator.BuildListTemplate(entity);
var detailTemplate = _defaultTemplateGenerator.BuildDetailTemplate(entity);
var editTemplate = _defaultTemplateGenerator.BuildEditTemplate(entity);

// 标记为系统默认模板
listTemplate.IsSystemDefault = true;
detailTemplate.IsSystemDefault = true;
editTemplate.IsSystemDefault = true;

// 保存到数据库
await _db.PageTemplates.AddAsync(listTemplate);
await _db.PageTemplates.AddAsync(detailTemplate);
await _db.PageTemplates.AddAsync(editTemplate);
```

##### 5.5 数据库变更

**迁移脚本**：
```sql
ALTER TABLE "PageTemplates" ADD COLUMN "IsSystemDefault" boolean DEFAULT false;
ALTER TABLE "PageTemplates" ADD COLUMN "Version" integer DEFAULT 1;
ALTER TABLE "PageTemplates" ADD COLUMN "CreatedBy" uuid NULL;
```

**执行命令**：
```bash
dotnet ef migrations add AddTemplateSystemFields -p src/BobCrm.Api
dotnet ef database update -p src/BobCrm.Api
```

#### 验收标准
- ✅ 每个实体自动生成 3 种模板
- ✅ 列表模板包含 DataGrid 和工具栏
- ✅ 详情/编辑模板字段合理分组
- ✅ 所有模板标记为 `IsSystemDefault = true`

---

### T6: 模板列表管理系统 (高优先级，3-4天)

#### 目标
实现完整的模板管理界面，支持查看、复制、应用模板，区分系统模板和用户模板。

#### 核心需求

**模板类型**：
1. **系统默认模板**：
   - 自动生成，`IsSystemDefault = true`
   - 仅管理员可修改
   - 不可删除
2. **用户模板**：
   - 用户从系统模板复制创建
   - 可自由修改和删除
   - 只对创建者可见

#### 核心任务

##### 6.1 后端 API

**创建文件**：`src/BobCrm.Api/Endpoints/TemplateEndpoints.cs`

**API 端点**：
```csharp
app.MapGet("/api/templates", async (
    string? entityType,
    string? purpose,
    string? templateType,  // "system" or "user"
    TemplateService service,
    ClaimsPrincipal user) => 
{
    var userId = user.GetUserId();
    var templates = await service.GetTemplatesAsync(entityType, purpose, templateType, userId);
    return Results.Ok(templates);
});

app.MapPost("/api/templates/{id}/copy", async (
    int id,
    CopyTemplateRequest request,
    TemplateService service,
    ClaimsPrincipal user) =>
{
    var userId = user.GetUserId();
    var newTemplate = await service.CopyTemplateAsync(id, request.Name, userId);
    return Results.Ok(newTemplate);
});

app.MapPut("/api/templates/{id}/apply", async (
    int id,
    string? functionCode,  // 可选：应用到特定菜单节点
    TemplateBindingService bindingService,
    ClaimsPrincipal user) =>
{
    var userId = user.GetUserId();
    await bindingService.SetUserTemplateAsync(userId, id, functionCode);
    return Results.NoContent();
});

app.MapDelete("/api/templates/{id}", async (
    int id,
    TemplateService service,
    ClaimsPrincipal user) =>
{
    // 检查是否为系统模板
    var template = await service.GetByIdAsync(id);
    if (template.IsSystemDefault)
        return Results.BadRequest("不能删除系统默认模板");
    
    await service.DeleteAsync(id);
    return Results.NoContent();
});
```

##### 6.2 前端模板列表页

**创建文件**：`src/BobCrm.App/Components/Pages/TemplateList.razor`

**页面路由**：`@page "/templates"`

**页面布局**：
```razor
<PageHeader Title="@I18n.T("MENU_TEMPLATES")" />

<!-- 筛选器 -->
<div class="filters">
    <Select @bind-Value="entityTypeFilter" Placeholder="实体类型">
        <SelectOption Value="">全部</SelectOption>
        @foreach (var entity in entities)
        {
            <SelectOption Value="@entity.EntityRoute">@entity.DisplayName</SelectOption>
        }
    </Select>
    
    <Select @bind-Value="purposeFilter" Placeholder="用途">
        <SelectOption Value="">全部</SelectOption>
        <SelectOption Value="List">列表</SelectOption>
        <SelectOption Value="Detail">详情</SelectOption>
        <SelectOption Value="Edit">编辑</SelectOption>
    </Select>
    
    <Select @bind-Value="templateTypeFilter" Placeholder="类型">
        <SelectOption Value="">全部</SelectOption>
        <SelectOption Value="system">系统模板</SelectOption>
        <SelectOption Value="user">我的模板</SelectOption>
    </Select>
</div>

<!-- 模板卡片网格 -->
<div class="template-grid">
    @foreach (var template in filteredTemplates)
    {
        <div class="template-card">
            <div class="card-header">
                <h3>@template.Name</h3>
                @if (template.IsSystemDefault)
                {
                    <Tag Color="blue">系统</Tag>
                    <Icon Type="@IconType.Outline.Lock" />
                }
                else
                {
                    <Tag Color="green">我的</Tag>
                }
            </div>
            <div class="card-body">
                <p>实体: @template.EntityType</p>
                <p>用途: @template.Purpose</p>
            </div>
            <div class="card-actions">
                <Button Icon="@IconType.Outline.Eye" OnClick="() => ViewTemplate(template)">预览</Button>
                @if (!template.IsSystemDefault || IsAdmin)
                {
                    <Button Icon="@IconType.Outline.Edit" OnClick="() => EditTemplate(template)">编辑</Button>
                }
                <Button Icon="@IconType.Outline.Copy" OnClick="() => CopyTemplate(template)">复制</Button>
                <Button Type="@ButtonType.Primary" OnClick="() => ApplyTemplate(template)">应用</Button>
                @if (!template.IsSystemDefault)
                {
                    <Popconfirm Title="确认删除?" OnConfirm="() => DeleteTemplate(template)">
                        <Button Danger Icon="@IconType.Outline.Delete">删除</Button>
                    </Popconfirm>
                }
            </div>
        </div>
    }
</div>
```

##### 6.3 模板复制对话框

```razor
<Modal @bind-Visible="copyDialogVisible" Title="复制模板">
    <Form>
        <FormItem Label="新模板名称">
            <Input @bind-Value="copyTemplateName" />
        </FormItem>
        <FormItem Label="用途">
            <Select @bind-Value="copyTemplatePurpose">
                <SelectOption Value="@sourceTemplate.Purpose">保持原样</SelectOption>
                <SelectOption Value="List">列表</SelectOption>
                <SelectOption Value="Detail">详情</SelectOption>
                <SelectOption Value="Edit">编辑</SelectOption>
            </Select>
        </FormItem>
    </Form>
</Modal>
```

##### 6.4 数据库变更

**TemplateBinding 表添加 UserId**：
```sql
ALTER TABLE "TemplateBindings" ADD COLUMN "UserId" uuid NULL;
```

#### 验收标准
- ✅ 模板列表页面正常显示
- ✅ 可按实体、用途、类型筛选
- ✅ 可从系统模板复制为用户模板
- ✅ 系统模板显示锁定图标，不显示删除按钮
- ✅ 用户可应用模板

---

### T7: 菜单模板关联与渲染 (高优先级，2-3天)

#### 目标
实现菜单导航与模板渲染的无缝集成，完成"设计-应用-设置-显示"闭环。

#### 核心任务

##### 7.1 统一模板渲染页面

**创建文件**：`src/BobCrm.App/Components/Pages/TemplatePage.razor`

**路由**：`@page "/page/{FunctionCode}"`

**逻辑**：
```csharp
protected override async Task OnInitializedAsync()
{
    // 1. 根据功能码查询菜单节点
    var function = await MenuService.GetByCodeAsync(FunctionCode);
    if (function == null)
    {
        error = "菜单节点不存在";
        return;
    }
    
    // 2. 查询关联的模板（优先用户模板）
    var template = await GetEffectiveTemplateAsync(function.TemplateId);
    if (template == null)
    {
        error = "未找到关联的模板";
        return;
    }
    
    // 3. 根据模板用途渲染不同页面
    templatePurpose = template.Purpose;
    templateId = template.Id;
    entityType = template.EntityType;
    
    // 4. 加载模板布局
    layoutJson = template.LayoutJson;
}

private async Task<PageTemplate?> GetEffectiveTemplateAsync(int? systemTemplateId)
{
    if (systemTemplateId == null)
        return null;
    
    var userId = _currentUser.GetUserId();
    
    // 优先查询用户个人模板绑定
    var userBinding = await _db.TemplateBindings
        .Where(b => b.UserId == userId && b.TemplateId == systemTemplateId)
        .Include(b => b.UserTemplate)
        .FirstOrDefaultAsync();
    
    if (userBinding?.UserTemplate != null)
        return userBinding.UserTemplate;
    
    // 回退到系统默认模板
    return await _db.PageTemplates
        .FirstOrDefaultAsync(t => t.Id == systemTemplateId);
}
```

##### 7.2 DataGrid 运行时渲染

**创建文件**：`src/BobCrm.App/Components/Runtime/DataGridRuntime.razor`

**功能**：
- 读取 `DataGridWidget` 配置
- 调用实体 API 加载数据
- 渲染 Ant Design `<Table>` 组件
- 处理行操作（查看、编辑、删除）

```csharp
<Table TItem="Dictionary<string, object?>"
       DataSource="@data"
       Loading="@loading"
       RemoteDataSource>
    <PropertyColumn Property="@(item => item[col.FieldName])" 
                    Title="@col.Label"
                    Width="@col.Width"
                    Sortable="@col.Sortable"
                    foreach var col in Widget.Columns />
    
    <ActionColumn Title="操作">
        @foreach (var action in Widget.RowActions)
        {
            <Button Icon="@action.Icon" OnClick="() => HandleRowAction(action, context)">
                @action.Label
            </Button>
        }
    </ActionColumn>
</Table>
```

##### 7.3 模板上下文传递

**扩展 RuntimeContext**：
```csharp
public class RuntimeContext
{
    public int TemplateId { get; set; }
    public string TemplatePurpose { get; set; }  // List, Detail, Edit
    public string EntityType { get; set; }
    public int? EntityId { get; set; }  // Detail/Edit 模式
    public Dictionary<string, object?> EntityData { get; set; }  // 实体数据
}
```

##### 7.4 用户模板绑定

**新增模型**：`src/BobCrm.Api/Base/Models/UserTemplatePreference.cs`

```csharp
public class UserTemplatePreference
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string? FunctionCode { get; set; }  // 可选：绑定到特定菜单
    public int SystemTemplateId { get; set; }  // 系统默认模板
    public int UserTemplateId { get; set; }  // 用户个人模板
}
```

#### 验收标准
- ✅ 菜单导航可跳转到模板渲染页
- ✅ List 模板正确渲染 DataGrid
- ✅ Detail/Edit 模板正确渲染表单
- ✅ 用户模板优先于系统默认模板
- ✅ 模板变更立即生效

---

## 开发规范

参考 `CLAUDE.md` 中的项目规范：

- **命名**：PascalCase（文件名、类名），camelCase（变量）
- **多语言**：所有用户可见文本使用 `I18n.T("KEY")`
- **Git 提交**：`feat(<scope>): <subject>`

## 验收总结

完成 T4-T7 后，应实现：

1. ✅ **表单设计器**支持所有页面级控件
2. ✅ **实体发布**时自动生成 List/Detail/Edit 模板
3. ✅ **模板列表页**可查看、复制、应用模板
4. ✅ **菜单导航**时根据模板渲染页面
5. ✅ **用户可以**个性化自己的页面显示

**闭环完成**：设计 → 应用 → 设置 → 显示 ✨

---

## 参考文档

- `docs/planning/PLAN-01-v0.7.0-菜单导航完善.md` - 总体计划
- `docs/planning/PLAN-01-APPENDIX-模板系统详细设计.md` - 详细技术规格
- `CLAUDE.md` - 项目开发规范
- `docs/design/ARCH-22-标准实体模板化与权限联动设计.md` - 模板系统架构

---

**开始开发吧！这是 BobCRM 最核心的功能！** 🚀
