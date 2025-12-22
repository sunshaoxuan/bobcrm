# ARCH-27: 模板系统详细设计（v0.7.0）

> 本文档是 `docs/plans/PLAN-01-v0.7.0-菜单导航完善.md` 的补充，详细描述 T4-T7 的技术实施细节

## T4: 表单设计器功能强化 (3-4天)

### 新增控件

#### 4.1 DataGrid 控件
**文件**: `src/BobCrm.App/Models/Widgets/DataGridWidget.cs`

**配置属性**:
- `EntityType`: 数据源实体类型
- `Columns`: 列配置数组
  - `FieldName`: 字段名
  - `Label`: 列标题
  - `Width`: 列宽度
  - `Sortable`: 是否可排序
- `RowActions`: 行操作按钮（编辑、删除、自定义）
- `PageSize`: 分页大小
- `ShowSearch`: 是否显示搜索框

#### 4.2 SubForm 控件
**用途**: 主从表单（一对多关系）

**配置属性**:
- `RelatedEntityType`: 关联实体类型
- `ForeignKeyField`: 外键字段
- `EmbeddedForm`: 嵌入的子表单模板

#### 4.3 Tab 容器控件
**用途**: 多标签页布局

**配置属性**:
- `Tabs`: 标签页数组
  - `Label`: 标签标题
  - `Children`: 子控件列表

#### 4.4 Card 卡片控件
**用途**: 分组展示表单字段

**配置属性**:
- `Title`: 卡片标题
- `Collapsible`: 是否可折叠
- `DefaultExpanded`: 默认展开状态

### 控件注册
更新 `WidgetRegistry.cs`:
```csharp
new WidgetDefinition("datagrid", "LBL_DATAGRID", IconType.Outline.Table, WidgetCategory.Data, () => new DataGridWidget()),
new WidgetDefinition("subform", "LBL_SUBFORM", IconType.Outline.Subnode, WidgetCategory.Data, () => new SubFormWidget()),
new WidgetDefinition("tabcontainer", "LBL_TABCONTAINER", IconType.Outline.Tabs, WidgetCategory.Layout, () => new TabContainerWidget()),
new WidgetDefinition("card", "LBL_CARD", IconType.Outline.Container, WidgetCategory.Layout, () => new CardWidget())
```

---

## T5: 默认模板自动生成 (2-3天)

### 增强 DefaultTemplateGenerator

**列表模板生成** (`BuildListTemplate`):
```csharp
public PageTemplate BuildListTemplate(EntityDefinition entity)
{
    var template = new PageTemplate
    {
        Name = $"{entity.EntityName} - List",
        EntityType = entity.EntityRoute,
        Purpose = "List",
        IsSystemDefault = true,
        Version = 1
    };
    
    // 布局: DataGrid + 顶部工具栏
    var layout = new {
        widgets = new[] {
            new { // 工具栏
                type = "section",
                children = new[] {
                    new { type = "button", label = "新增", action = "create" },
                    new { type = "textbox", label = "搜索", id = "search" }
                }
            },
            new { // 数据表格
                type = "datagrid",
                entityType = entity.EntityRoute,
                columns = BuildDefaultColumns(entity) // 前5-8个字段
            }
        }
    };
    
    template.LayoutJson = JsonSerializer.Serialize(layout);
    return template;
}
```

**详情模板优化** (`BuildDetailTemplate`):
- 使用 Card/Section 分组字段
- 按字段类型智能选择控件
- 添加保存/取消按钮

---

## T6: 模板列表管理系统 (3-4天)

### 后端 API

**TemplateEndpoints.cs**:
```csharp
// GET /api/templates?entityType=customer&purpose=List&templateType=system
app.MapGet("/api/templates", async (
    string? entityType,
    string? purpose,
    string? templateType,
    TemplateService service) => 
{
    var templates = await service.GetTemplatesAsync(entityType, purpose, templateType);
    return Results.Ok(templates);
});

// POST /api/templates/{id}/copy
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

// PUT /api/templates/{id}/apply
app.MapPut("/api/templates/{id}/apply", async (
    int id,
    TemplateBinding bindingService,
    ClaimsPrincipal user) =>
{
    var userId = user.GetUserId();
    await bindingService.SetUserTemplateAsync(userId, templateId: id);
    return Results.NoContent();
});
```

### 前端页面

**TemplateList.razor**:
- 模板卡片网格布局
- 筛选器（实体类型、用途、模板类型）
- 操作按钮：编辑、复制、删除、应用

### 数据库模型变更 (Logical Schema Updates)

| 目标表 | 变更类型 | 逻辑字段 | 抽象类型 | 说明 |
| :--- | :--- | :--- | :--- | :--- |
| **PageTemplates** | 新增 | IsSystemDefault | Boolean | 标识是否为系统内置模板 |
| **PageTemplates** | 新增 | Version | Integer | 模板版本号 |
| **PageTemplates** | 新增 | CreatedBy | GUID | 创建人 ID |
| **TemplateBindings** | 新增 | UserId | GUID | 用户个人绑定引用 |

---

## T7: 菜单模板关联与渲染 (2-3天)

### 统一路由入口

**TemplatePage.razor**:
```csharp
@page "/page/{FunctionCode}"

@code {
    [Parameter] public string FunctionCode { get; set; }
    
    protected override async Task OnInitializedAsync()
    {
        // 1. 查询菜单节点
        var function = await MenuService.GetByCodeAsync(FunctionCode);
        
        // 2. 查询关联模板（优先用户模板）
        var template = await GetTemplateAsync(function.TemplateId);
        
        // 3. 根据模板用途渲染
        if (template.Purpose == "List")
            await RenderListPage(template);
        else if (template.Purpose == "Detail" || template.Purpose == "Edit")
            await RenderDetailPage(template);
    }
}
```

### 模板加载优先级
```csharp
public async Task<PageTemplate?> GetTemplateAsync(int? systemTemplateId)
{
    var userId = _currentUser.GetUserId();
    
    // 1. 优先查询用户个人模板
    var userTemplate = await _db.TemplateBindings
        .Where(b => b.UserId == userId && b.TemplateId == systemTemplateId)
        .Select(b => b.Template)
        .FirstOrDefaultAsync();
        
    if (userTemplate != null)
        return userTemplate;
    
    // 2. 回退到系统默认模板
    return await _db.PageTemplates
        .FirstOrDefaultAsync(t => t.Id == systemTemplateId);
}
```

### DataGrid 运行时渲染
- 从模板配置加载列定义
- 动态查询实体数据
- 渲染行操作按钮

---

## 验收标准总结

**T4**:
- ✅ 新增 4 种页面级控件
- ✅ 在设计器中可拖拽使用
- ✅ 在运行时正确渲染

**T5**:
- ✅ 每个实体自动生成 List/Detail/Edit 模板
- ✅ 列表模板包含 DataGrid
- ✅ 标记为系统默认模板（`IsSystemDefault = true`）

**T6**:
- ✅ 模板列表页面正常显示
- ✅ 可从系统模板复制为用户模板
- ✅ 系统模板不可被普通用户删除或修改
- ✅ 用户可设置个人默认模板

**T7**:
- ✅ 菜单节点关联系统默认模板
- ✅ 用户可设置个人模板覆盖
- ✅ 前端可根据模板渲染 List/Detail/Edit 页面
- ✅ 模板变更立即生效

---

最后更新: 2025-11-20
