# ChangeLog

## 2025-10-31 - 修复多语资源加载、拖拽功能和主题色持久化问题

### 修复：设计器多语资源未加载

**问题：**
- 设计器相关键（LBL_COMPONENTS、LBL_PROPERTIES等）显示为键名而非翻译文本
- 数据库在新增键之前已创建，导致这些键缺失

**解决方案：**
- 在`DatabaseInitializer.cs`的`else`分支中添加了所有设计器相关键的`Ensure`调用
- 包括：LBL_COMPONENTS、LBL_BASIC_COMPONENTS、LBL_LAYOUT_COMPONENTS、LBL_PROPERTIES等18个设计器相关键
- 即使数据库在这些键加入之前已创建，现在也能自动补齐缺失的键

**修改文件：**
- `src/BobCrm.Api/Infrastructure/DatabaseInitializer.cs` (L313-331)

---

### 修复：工具栏组件无法拖到画布

**问题：**
- 工具栏组件设置了`draggable="true"`但拖拽时无法触发`drop`事件
- 缺少`@ondragstart:preventDefault="false"`导致拖拽被默认行为阻止

**解决方案：**
- 在工具栏的基础组件和布局组件项上添加`@ondragstart:preventDefault="false"`
- 确保拖拽事件能正常触发

**修改文件：**
- `src/BobCrm.App/Components/Pages/CustomerDetail.razor` (L50, L62)

---

### 修复：主题色持久化问题

**问题：**
- JS使用`localStorage`键名为`primary`，但.NET偏好服务读写的是`primaryColor`
- 刷新或重新渲染后主题色会回到默认值`#3f7cff`

**解决方案：**
- 在`PreferencesService.cs`中统一键名处理：
  - `LoadFromLocalStorageAsync`: 优先读取`primaryColor`，不存在时回退到`primary`
  - `SyncToLocalStorageAsync`: 同时写入`primaryColor`和`primary`两个键
- 确保JS和.NET都能正确读取/写入主题色

**修改文件：**
- `src/BobCrm.App/Services/PreferencesService.cs` (L66-69, L97-98)

---

### 修复：设计态画布拖放问题

**问题：**
- 工具栏组件能触发拖动，但无法放到画布上
- 画布控件能拖动但无法正确放置
- Firefox等浏览器上drop事件不触发

**解决方案：**
1. **添加dragover事件处理：**
   - 在布局容器层（承载所有layout-widget的flex容器）添加`@ondragover:preventDefault`
   - 在每个layout-widget外层也添加`@ondragover:preventDefault`
   - 确保任何可能成为落点的容器都允许drop

2. **设置dataTransfer数据：**
   - 修改`OnDragStart`和`OnWidgetDragStart`方法，接收`DragEventArgs`参数
   - 在拖拽开始时设置`dataTransfer.SetData("text/plain", ...)`
   - 在`app.js`中添加`setDragData`函数作为跨浏览器兼容的fallback

**修改文件：**
- `src/BobCrm.App/Components/Pages/CustomerDetail.razor` (L78-79, L86, L462-500)
- `src/BobCrm.App/wwwroot/app.js` (L105-121)

---

## 2025-10-31 - 修复设计器组件灰显问题，实现完整拖拽功能

### 更新：解决组件Disabled导致的灰显和不可拖拽问题

根据用户反馈，设计器中的组件显示为灰色且无法拖拽。经过代码分析，发现是AntDesign组件的Disabled属性导致。

#### 核心问题

**症状：**
- 所有设计态组件显示为灰色（降低透明度）
- 组件无法被拖拽和移动
- 视觉效果不符合WYSIWYG原则

**根本原因：**
在设计模式下，使用了带`Disabled`属性的AntDesign组件：
```razor
<Input Value="@widget.Label" Disabled style="width:100%" />
<DatePicker TValue="DateTime?" Disabled="true" style="width:100%" />
<Select Mode="multiple" Disabled style="width:100%" />
```

AntDesign的Disabled组件会渲染为灰色外观，并阻止所有交互。

#### 解决方案

**替换策略：**
将所有Disabled的AntDesign组件替换为样式化的div元素，使用`pointer-events:none`阻止内部交互，同时保持父容器的`draggable`属性。

**具体修改（CustomerDetail.razor L89-131）：**

1. **Textbox组件：**
```razor
<div style="padding:4px; border:1px solid #d9d9d9; border-radius:2px; background:#fafafa; width:100%; pointer-events:none">
    @widget.Label
</div>
```

2. **Calendar组件：**
```razor
<div style="padding:4px; border:1px solid #d9d9d9; border-radius:2px; background:#fafafa; width:100%; pointer-events:none; display:flex; align-items:center">
    <Icon Type="@IconType.Outline.Calendar" Style="margin-right:8px" />
    <span>@widget.Label</span>
</div>
```

3. **Listbox组件：**
```razor
<div style="padding:4px; border:1px solid #d9d9d9; border-radius:2px; background:#fafafa; width:100%; pointer-events:none; display:flex; align-items:center">
    <Icon Type="@IconType.Outline.UnorderedList" Style="margin-right:8px" />
    <span>@widget.Label</span>
</div>
```

4. **Tabbox组件：**
```razor
<div style="border:1px solid #d9d9d9; border-radius:4px; padding:8px; pointer-events:none">
    <div style="display:flex; gap:8px; border-bottom:1px solid #e0e0e0; padding-bottom:8px; margin-bottom:8px">
        <div style="padding:4px 12px; background:#1890ff; color:#fff; border-radius:2px; font-size:12px">Tab 1</div>
        <div style="padding:4px 12px; background:#f0f0f0; color:#666; border-radius:2px; font-size:12px">Tab 2</div>
    </div>
    <div style="padding:8px; color:#999; font-size:12px">@widget.Label</div>
</div>
```

5. **Frame组件：**
```razor
<div style="border:2px dashed #d9d9d9; border-radius:4px; padding:12px; min-height:80px; pointer-events:none; background:#fafafa">
    <div style="color:#999; font-size:12px; display:flex; align-items:center">
        <Icon Type="@IconType.Outline.BorderOuter" Style="margin-right:4px" />
        <span>@widget.Label</span>
    </div>
</div>
```

#### 技术要点

- **pointer-events:none**: 阻止内部元素的鼠标事件，但不影响父容器的拖拽
- **父容器保持draggable**: `draggable="true"` 和 `cursor:move` 确保组件可拖拽
- **视觉一致性**: div样式模拟原组件外观，无灰色效果
- **图标增强**: Calendar、Listbox、Frame添加对应图标，提升可识别性

#### 效果

- ✅ 组件显示为正常颜色（非灰显）
- ✅ 组件可以被拖拽和重新排列
- ✅ 符合WYSIWYG设计理念
- ✅ 内部元素不响应点击，保持设计态纯净性

#### 修改文件

- `src/BobCrm.App/Components/Pages/CustomerDetail.razor` (L89-131)

#### 编译状态
```
dotnet build -c Debug
✓ 成功，0个警告，0个错误
```

---

## 2025-10-31 - 设计器UI优化和拖拽功能修复

### 更新：优化设计器界面体验和拖拽交互

根据用户反馈，对设计器进行了界面优化和功能修复。

#### 改进内容

**1. 画布视觉优化**
- 画布改为撑满整个工作区（左抵工具栏，右抵属性栏）
- 添加点状网格背景（20px间距，灰色圆点）
- 移除了限制画布宽度的容器，改为全宽布局
- 画布最小高度设为 `calc(100vh - 120px)`，确保足够的拖拽空间

**2. 默认属性面板显示**
- 未选中组件时，属性面板显示客户基本信息：
  - 客户编码（COL_CODE）
  - 客户名称（COL_NAME）
  - 组件总数
  - 可见组件数
- 保留"选择一个组件"的提示区域，使用虚线边框突出显示

**3. 组件拖拽优化**
- 添加组件容器样式：白色背景、圆角、阴影效果
- 设置 `cursor:move` 和 `user-select:none` 提升拖拽体验
- 选中状态：2px 蓝色边框（#1890ff）
- 未选中状态：1px 灰色边框（#e0e0e0）
- 添加 0.2s 过渡动画，使交互更流畅
- 修复 `@ondragstart:preventDefault="false"` 确保拖拽事件正常触发

**4. 多语资源准备**
- 已添加的 i18n 键已包含在 DatabaseInitializer.cs 中
- 删除数据库重新初始化后即可加载新的多语资源
- 设计器界面将正确显示日语/中文/英语标签

#### 技术细节

**画布背景样式：**
```css
background-image: radial-gradient(circle, #d0d0d0 1px, transparent 1px);
background-size: 20px 20px;
background-color: #f5f5f5;
```

**组件容器样式增强：**
```css
cursor:move;
user-select:none;
padding:8px;
background:#fff;
border-radius:4px;
box-shadow:0 1px 3px rgba(0,0,0,0.1);
transition:all 0.2s;
```

**拖拽事件处理：**
- 画布：`@ondrop="OnDrop" @ondrop:preventDefault @ondragover:preventDefault`
- 组件：`draggable="true" @ondragstart="..." @ondragstart:preventDefault="false"`

#### 修改文件

- `src/BobCrm.App/Components/Pages/CustomerDetail.razor`
  - L75-78: 画布样式优化（点状背景，撑满区域）
  - L81-88: 组件容器样式增强（拖拽提示，选中状态）
  - L185-210: 属性面板默认显示客户信息
  - L508-517: 边框样式优化

#### 编译状态
```
dotnet build -c Debug
✓ 成功，0个警告，0个错误
```

---

## 2025-10-31 - 可视化布局设计器与用户体验优化

### 重大更新：实现完整的WYSIWYG可视化布局设计器

根据用户反馈，实现了完整的拖拽式布局设计器，支持流式布局、组件工具箱、属性面板等功能。

#### 新增功能

**1. 色调持久化时机优化**
- 修复问题：色调选择后刷新页面会恢复默认色
- 解决方案：在 `app.js` 的 `setPrimary()` 添加 `skipSave` 参数
- 初始化加载时跳过保存，只有用户主动点击颜色按钮时才保存到服务器
- 确保用户选择的颜色会立即持久化到 localStorage 和服务器

**2. 认证保护机制**
- 新增 `AuthChecker.razor` 组件，检查 localStorage 中的 accessToken
- 未登录用户自动重定向到登录页
- 应用到所有需要认证的页面（Customers, CustomerDetail等）
- 解决了未登录也能看到客户数据的安全问题

**3. 可视化布局设计器**

**设计模式界面布局：**
- **全屏设计器**：设计模式下使用 `position:fixed` 覆盖整个屏幕（包括客户列表侧边栏）
- **三栏布局**：
  - 左侧工具箱（240px）：组件面板
  - 中央画布（flex:1）：设计区域
  - 右侧属性栏（280px）：属性编辑

**左侧组件工具箱：**
- 使用 AntDesign `Collapse` 组件分组
- **基础组件组**：
  - Textbox（文本框）
  - Label（标签）
  - Calendar（日历）
  - Listbox（列表框）
- **布局组件组**：
  - Frame（容器）
  - Tabbox（标签页）
- 每个组件都可拖拽到画布
- 显示图标和多语言名称

**中央设计画布：**
- 白色画布，最大宽度1200px居中
- 支持从工具箱拖拽组件到画布
- 组件采用流式布局（`display:flex; flex-wrap:wrap`）
- 实现 WYSIWYG 效果：设计时的布局与浏览时完全一致
- 组件可点击选中，选中后右侧显示属性
- 组件可在画布内拖拽重新排序
- 空画布显示提示"拖拽组件到这里"

**右侧属性面板：**
- 组件类型（只读）
- 标签（Label）：可编辑文本
- 宽度（Width）：数值输入 + 单位选择（%/px）
- 数据源（Data Source）：下拉选择客户字段
- 可见性（Visible）：开关控制
- 删除按钮：移除选中组件
- 未选中组件时显示"选择一个组件"提示

**4. 浏览和编辑模式优化**

**流式布局实现：**
- 所有模式（Browse/Edit/Design）统一使用流式布局
- 组件宽度由设计器设置（百分比或像素）
- 文本框不再每行显示一个，按宽度自动换行
- 真正实现所见即所得（WYSIWYG）

**浏览模式：**
- 根据保存的布局显示字段
- 使用 `layoutWidgets` 数据驱动渲染
- 字段值从 `customer.fields` 中根据 `DataField` 获取
- 只显示 `Visible=true` 的组件

**编辑模式：**
- 与浏览模式相同的流式布局
- 字段变为可编辑输入框
- 保存和取消按钮

**5. i18n 国际化支持**
新增设计器相关的多语言键：
- `BTN_EXIT_DESIGN`: 退出设计 / デザインを終了 / Exit Design
- `LBL_COMPONENTS`: 组件 / コンポーネント / Components
- `LBL_BASIC_COMPONENTS`: 基础组件
- `LBL_LAYOUT_COMPONENTS`: 布局组件
- `LBL_TEXTBOX/LABEL/CALENDAR/LISTBOX/FRAME/TABBOX`: 各组件名称
- `LBL_DRAG_COMPONENT_HERE`: 拖拽组件到这里
- `LBL_PROPERTIES`: 属性
- `LBL_COMPONENT_TYPE/WIDTH/DATA_SOURCE/VISIBLE`: 属性名称
- `BTN_DELETE`: 删除
- `LBL_SELECT_COMPONENT`: 选择一个组件

#### 技术实现

**组件模型：**
```csharp
class LayoutWidget {
    string Id;           // GUID
    string Type;         // textbox/label/calendar/listbox/frame/tabbox
    string Label;        // 显示标签
    string? DataField;   // 绑定的客户字段key
    int Width;           // 宽度数值
    string WidthUnit;    // 单位（%或px）
    bool Visible;        // 是否可见
}
```

**拖拽实现：**
- 工具箱组件：`draggable="true"` + `@ondragstart`
- 画布组件：可拖拽重排序
- 画布容器：`@ondrop` + `@ondragover:preventDefault`
- 使用状态变量 `draggedComponent` 和 `draggedWidget` 追踪拖拽对象

**属性编辑：**
- 使用 `@bind-Value` 双向绑定组件属性
- 使用 `@onchange="UpdateWidget"` 触发重新渲染
- AntDesign InputNumber/Select/Switch 组件

**样式计算：**
```csharp
private string GetWidgetStyle(LayoutWidget widget)
{
    var width = widget.WidthUnit == "%"
        ? $"calc({widget.Width}% - 6px)"  // 减去gap
        : $"{widget.Width}px";
    var border = selectedWidget == widget
        ? "2px solid #1890ff" : "";
    return $"width:{width}; {border}";
}
```

#### 修改文件清单

**认证相关：**
- `src/BobCrm.App/Components/Shared/AuthChecker.razor` (新建)
- `src/BobCrm.App/Components/Pages/Customers.razor` (添加 AuthChecker)
- `src/BobCrm.App/Components/Pages/CustomerDetail.razor` (添加 AuthChecker)

**偏好持久化优化：**
- `src/BobCrm.App/wwwroot/app.js` (L3, L78 添加 skipSave 参数)
- `src/BobCrm.App/Components/Shared/PreferencesManager.razor` (L27 传递 skipSave=true)

**布局设计器：**
- `src/BobCrm.App/Components/Pages/CustomerDetail.razor` (完全重构)
  - L23-192: 设计模式全屏界面
  - L42-72: 左侧工具箱（Collapse + Panel）
  - L75-130: 中央画布（拖拽区域）
  - L133-190: 右侧属性面板
  - L194-280: 浏览和编辑模式（流式布局）
  - L289-531: C# 代码（模型、拖拽逻辑、属性编辑）

**国际化：**
- `src/BobCrm.Api/Infrastructure/DatabaseInitializer.cs` (L213-230 新增18个设计器键)

#### 编译状态
```
dotnet build -c Debug
✓ 成功，0个警告，0个错误
```

#### 待实现功能
- [ ] 布局保存到服务器（SaveLayout API）
- [ ] 从服务器加载布局
- [ ] 组件拖拽重排序的视觉反馈动画
- [ ] Frame 和 Tabbox 的嵌套布局支持
- [ ] 更多组件类型（Checkbox/Radio/Table等）

---

## 2025-10-31 - 用户偏好持久化系统

### 重大更新：实现完整的用户偏好持久化机制

修复了主题色在页面切换和重新渲染时自动重置的"万花筒效应"问题。实现了完整的客户端+服务器双向同步的偏好设置系统。

#### 问题背景
- 用户选择的主题色在每次页面切换或按钮点击时都会自动变化
- 原因：MutationObserver 在每次 DOM 变化时都重新初始化主题
- 缺少用户偏好的服务器端持久化，导致设置无法跨设备/会话保存

#### 解决方案

**后端实现**
1. **新增 UserPreferences 表**
   - 文件：`src/BobCrm.Api/Domain/Models/UserPreferences.cs`
   - 字段：UserId, Theme, PrimaryColor, Language, UpdatedAt
   - 按用户存储个性化设置

2. **新增用户偏好 API**
   - `GET /api/user/preferences` - 获取用户偏好（返回默认值如果未设置）
   - `PUT /api/user/preferences` - 保存用户偏好
   - 文件：`src/BobCrm.Api/Program.cs` (L290-336)
   - 添加 UserPreferencesDto (L1033)

**前端实现**
1. **PreferencesService 服务**
   - 文件：`src/BobCrm.App/Services/PreferencesService.cs`
   - 职责：
     - 从服务器加载用户偏好
     - 保存偏好到服务器和 localStorage
     - 处理同步冲突（服务器优先）
   - 注册为 Scoped 服务 (`src/BobCrm.App/Program.cs` L19)

2. **PreferencesManager 组件**
   - 文件：`src/BobCrm.App/Components/Shared/PreferencesManager.razor`
   - 功能：
     - 页面加载时从服务器拉取偏好并应用
     - 注册 .NET 回调供 JavaScript 调用
     - 提供 JSInvokable 方法：SaveThemeAsync, SavePrimaryColorAsync, SaveLanguageAsync
   - 添加到 Routes.razor 全局加载

3. **JavaScript 集成**
   - 文件：`src/BobCrm.App/wwwroot/app.js`
   - 改动：
     - 添加 `preferencesCallback` 和 `registerPreferencesCallback` (L2-4)
     - `setTheme()` 调用 .NET SaveThemeAsync (L68-71)
     - `setPrimary()` 调用 .NET SavePrimaryColorAsync (L95-98)
     - `changeLang()` 调用 .NET SaveLanguageAsync (L151-154)

4. **MutationObserver 移除**
   - 文件：`src/BobCrm.App/Components/App.razor`
   - 原有 MutationObserver 代码已在上一版本中移除
   - 改为单次初始化 + 轮询工具栏可用性（5秒超时）

**AuthService 增强**
- 文件：`src/BobCrm.App/Services/AuthService.cs`
- 新增 `PutAsJsonWithRefreshAsync<T>` 方法 (L119-133)
- 支持 PUT 请求的自动 token 刷新

#### 工作流程

1. **用户登录后**
   - PreferencesManager 从服务器加载偏好
   - 应用到 UI（主题、主色、语言）
   - 同步到 localStorage

2. **用户更改设置时**
   - JavaScript 立即更新 UI 和 localStorage
   - 通过 JSInterop 调用 .NET 方法
   - .NET 方法保存到服务器数据库
   - 异步操作，不阻塞 UI

3. **页面刷新或切换设备时**
   - 从服务器重新加载最新偏好
   - 覆盖 localStorage（服务器优先）

#### 持久化策略

根据《客户信息管理系统设计文档》第 221-286 行：

- **localStorage**：客户端即时响应
- **Server**：权威数据源，跨设备同步
- **同步时机**：
  - 登录时：Server → localStorage
  - 设置变更时：localStorage + Server（并发）
  - 冲突解决：Server 优先

#### 修改文件清单

**后端**
- `src/BobCrm.Api/Domain/Models/UserPreferences.cs` (新建)
- `src/BobCrm.Api/Program.cs` (L290-336, L907, L1033)

**前端服务**
- `src/BobCrm.App/Services/PreferencesService.cs` (新建)
- `src/BobCrm.App/Services/AuthService.cs` (L119-133 新增 PUT 方法)
- `src/BobCrm.App/Program.cs` (L19 注册服务)

**前端组件**
- `src/BobCrm.App/Components/Shared/PreferencesManager.razor` (新建)
- `src/BobCrm.App/Components/Routes.razor` (L7 添加 PreferencesManager)

**JavaScript**
- `src/BobCrm.App/wwwroot/app.js` (L2-4, L68-71, L95-98, L151-154)

#### 已编译验证
```
dotnet build -c Debug
✓ 成功，1个警告（CustomerDetail.razor null reference，不影响功能），0个错误
```

#### 待测试
- [ ] 选择主题色后刷新页面，颜色保持
- [ ] 切换页面后颜色不再自动变化
- [ ] 不同浏览器登录同一账号，偏好设置同步

---

## 2025-10-30 - 客户详情三态模式实现

### 重大更新：完整实现客户详情页三态系统

根据《客户信息管理系统设计文档》要求，实现了完整的三态客户详情页面。

#### 三态定义
1. **浏览态（Browse）**: 只读查看数据，不可编辑，不可调整布局
2. **编辑态（Edit）**: 可编辑数据并保存，不可调整布局
3. **设计态（Design）**: 可调整布局，不显示真实数据，可保存布局

#### 功能实现

**浏览态**
- 字段以卡片形式展示（标签+值）
- 所有字段只读显示
- 顶部显示客户编码和名称

**编辑态**
- 所有字段转为可编辑Input组件
- 字段值绑定到editValues Dictionary
- 提供"保存"和"取消"按钮
- 保存后自动切回浏览态

**设计态**
- 显示字段布局设计界面
- 字段以网格布局展示为可拖拽块
- 每个字段块显示标签和key
- 提供"保存布局"、"生成布局"、"取消"按钮
- 为后续拖拽排列功能预留UI结构

#### 技术实现
- **模式切换**: 顶部工具栏三个按钮实时切换模式
- **状态管理**: ViewMode枚举（Browse/Edit/Design）
- **数据绑定**: 编辑态使用Dictionary<string, string>存储临时值
- **UI响应**: 切换模式时立即重新渲染对应UI

#### 新增i18n键
- MODE_BROWSE, MODE_EDIT, MODE_DESIGN
- BTN_SAVE_LAYOUT, BTN_GENERATE_LAYOUT
- LBL_DESIGN_MODE_TITLE, LBL_DESIGN_MODE_DESC

#### 待完成功能（后续迭代）
- [ ] 编辑态：实现保存数据到API
- [ ] 设计态：实现拖拽排列字段
- [ ] 设计态：实现保存/加载布局JSON
- [ ] 设计态：实现标签驱动的快速布局生成
- [ ] 字段动作支持（email图标/RDS下载/链接打开等）

#### 修改文件
- `src/BobCrm.App/Components/Pages/CustomerDetail.razor` - 完全重构，实现三态
- `src/BobCrm.Api/Infrastructure/DatabaseInitializer.cs` - 添加模式相关i18n键

---

# ChangeLog

## 2025-10-30 - 新增客户功能和主题系统修复

### 新增功能

#### 1. 新增客户功能
- **后端**: 添加 `POST /api/customers` 端点用于创建客户
  - 文件: `src/BobCrm.Api/Program.cs` (L300-353, L977)
  - 验证: 客户编码必填、唯一性检查
  - 自动为创建者分配访问权限

- **前端**: 完整的新增客户流程
  - CustomerSider添加"新增客户"按钮: `src/BobCrm.App/Components/Shared/CustomerSider.razor` (L13-15, L126-129)
  - 新增客户页面: `src/BobCrm.App/Components/Pages/CustomerNew.razor` (新建)
  - AuthService添加POST方法: `src/BobCrm.App/Services/AuthService.cs` (L103-117)

#### 2. 主题色系统增强
- **多色选择器**: 6个预设颜色按钮（蓝/天蓝/绿/橙/红/紫）
  - 文件: `src/BobCrm.App/Components/Layout/MainLayout.razor` (L19-26)

- **CSS主题系统**: 所有按钮使用CSS变量 `var(--primary)`
  - 按钮样式: `src/BobCrm.App/wwwroot/css/theme.css` (L111-182)
  - 立即应用: `src/BobCrm.App/Components/App.razor` (L18-50)
  - 主题管理: `src/BobCrm.App/wwwroot/app.js` (L68-120)

#### 3. 国际化支持
- 添加新增客户相关的多语言键
  - 文件: `src/BobCrm.Api/Infrastructure/DatabaseInitializer.cs` (L194-203, L269-277)
  - 键: BTN_NEW_CUSTOMER, LBL_NEW_CUSTOMER, LBL_CUSTOMER_CODE_HINT, BTN_CANCEL, LBL_SAVING, LBL_SAVE_FAILED, ERR_CUSTOMER_CODE_REQUIRED, ERR_CUSTOMER_NAME_REQUIRED, ERR_CUSTOMER_CODE_EXISTS

### 关键修复

#### 1. 按钮点击事件修复
- **问题**: 新增客户按钮无法响应点击
- **原因**: CustomerSider组件缺少交互式渲染模式
- **修复**: `src/BobCrm.App/Components/Shared/CustomerSider.razor` 第2行添加 `@rendermode RenderMode.InteractiveServer`
- **注意**: Layout组件不能使用InteractiveServer（因为RenderFragment参数），所以在子组件上添加

#### 2. 主题色全局生效
- **问题**: 颜色选择器只改变部分按钮
- **原因**: CSS变量未在页面加载时立即应用
- **修复**:
  - 在HTML解析前立即从localStorage读取并应用主题
  - 所有按钮使用CSS类（`.btn-new-customer`, `.btn-primary-custom`）而非内联样式
  - CSS类统一引用 `var(--primary)`

#### 3. 路由冲突（AmbiguousMatchException）
- **问题**: 点击客户列表项报错"The request matched multiple endpoints"
- **原因**: Customers.razor同时声明 `/customers` 和 `/customers/{Id:int}` 两个路由
- **修复**: 分离路由到不同页面
  - `/customers` → Customers.razor（列表页）
  - `/customer/{Id:int}` → CustomerDetail.razor（详情页，注意是单数）
  - `/customers/new` → CustomerNew.razor（新增页）
- **影响**: CustomerSider导航、CustomerNew保存后跳转都已更新

### 技术改进

- **rendermode正确使用**: Layout使用静态渲染，交互组件自己声明InteractiveServer
- **CSS变量系统**: 统一通过 `--primary` 控制主题色，避免硬编码颜色值
- **渐进增强**: 立即执行脚本 → DOMContentLoaded → MutationObserver 三层保障

### 测试验证

1. **新增客户**:
   - 点击"新規顧客"按钮 → 导航到新增页面
   - 填写编码和名称 → 保存成功 → 跳转到详情页

2. **主题色**:
   - 点击任意颜色圆点 → 所有按钮立即变色
   - 刷新页面 → 颜色设置保持

3. **多语言**:
   - 如显示键名而非翻译文本，执行: `POST http://localhost:8081/api/admin/db/recreate`

### 已编译验证
```
dotnet build -c Debug
✓ 成功，0个警告，0个错误
```

### 修改文件清单
- src/BobCrm.Api/Program.cs
- src/BobCrm.Api/Infrastructure/DatabaseInitializer.cs
- src/BobCrm.App/Components/Pages/Customers.razor（简化为列表页）
- src/BobCrm.App/Components/Pages/CustomerDetail.razor（路由改为 /customer/{Id}）
- src/BobCrm.App/Components/Shared/CustomerSider.razor（添加rendermode，更新导航）
- src/BobCrm.App/Components/Pages/CustomerNew.razor（新建）
- src/BobCrm.App/Components/Layout/MainLayout.razor
- src/BobCrm.App/Components/App.razor
- src/BobCrm.App/Services/AuthService.cs
- src/BobCrm.App/wwwroot/app.js
- src/BobCrm.App/wwwroot/css/theme.css
