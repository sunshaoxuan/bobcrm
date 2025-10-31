# ChangeLog

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
