# bobcrm - 客户信息管理系统

一个基于 Blazor Server 的 CRM 系统，具有动态表单设计、多语言支持和丰富的字段动作功能。

## 📝 最新更新（v0.5.5，2025-11-06）

**🏗️ 重大架构重构：设计态渲染与属性编辑器完全组件化**

- ✅ **容器设计态渲染器组件化** - 从160+行`if-else`简化为6个独立渲染器组件
- ✅ **属性面板元数据驱动** - 删除5个重复的PropertyPanel组件，改用1个通用PropertyEditor
- ✅ **面向对象封装** - 每个Widget类是自己属性的提供者，WidgetPropertyProvider简化76%
- ✅ **多语言国际化** - 清除所有硬编码中文，新增42个多语言键（`PROP_*`前缀）
- ✅ **容器布局修复** - 修复容器"内外分离"视觉BUG，统一使用flex布局

**技术债务清理**：
- 删除 ~359 行重复代码
- FormDesigner 简化 ~150 行
- WidgetPropertyProvider 从 143 行 → 34 行

详见：[CHANGELOG.md](CHANGELOG.md) | [设计文档](docs/客户信息管理系统设计文档.md)

## 🚀 快速开始（5分钟上手）

### 前置条件

- ✅ .NET 8+ SDK ([下载](https://dotnet.microsoft.com/download/dotnet/8.0)) - .NET 9也可以，向后兼容
- ✅ PostgreSQL 或 Docker（推荐用Docker）
- ✅ PowerShell 7+（Windows已内置）

### 第一步：克隆项目

```bash
git clone <repository-url>
cd bobcrm
```

### （可选）验证环境

运行验证脚本检查所有前置条件：

```powershell
pwsh scripts/verify-setup.ps1
```

> 此脚本会检查：.NET SDK、Docker、项目结构、数据库连接、端口占用等

### 第二步：启动数据库（Docker方式）

```bash
docker compose up -d
```

> 这将启动PostgreSQL数据库（端口5432，用户名/密码：postgres/postgres）

### 第三步：一键启动项目

```powershell
pwsh scripts/dev.ps1 -Action start
```

> 这将自动构建并启动API服务器和前端应用

### 第四步：访问系统

- **前端地址**：http://localhost:8080
- **API文档**：http://localhost:5200/swagger
- **默认管理员账号**：
  - 用户名：`admin`
  - 密码：`Admin@12345`

### 第五步：验证功能

1. ✅ 登录系统（使用admin账号）
2. ✅ 查看客户列表（应显示2个测试客户：C001, C002）
3. ✅ 点击客户详情，查看动态字段
4. ✅ 切换语言（右上角：日语/中文/英文）
5. ✅ 进入表单设计器（/designer）尝试拖拽Widget

### 停止服务

```powershell
pwsh scripts/dev.ps1 -Action stop
```

---

## 📖 详细文档

- [设计文档](docs/客户信息管理系统设计文档.md)
- [接口文档](docs/接口文档.md)
- [CHANGELOG](CHANGELOG.md) ⭐ 版本历史
- [架构重构总结](docs/架构重构总结-v0.5.md)
- [测试指南](docs/测试指南.md)

---

## 🔧 常见问题（FAQ）

<details>
<summary><b>Q: 端口8080被占用怎么办？</b></summary>

修改 `src/BobCrm.App/Properties/launchSettings.json` 中的端口，或使用：
```bash
dotnet run --project src/BobCrm.App --urls "http://localhost:9090"
```
</details>

<details>
<summary><b>Q: 忘记管理员密码？</b></summary>

执行数据库重建：
```bash
curl -X POST http://localhost:5200/api/admin/db/recreate -H "Authorization: Bearer <your-token>"
```
或手动删除数据库后重启API服务器。
</details>

<details>
<summary><b>Q: 如何不使用Docker运行？</b></summary>

1. 安装PostgreSQL本地版本
2. 创建数据库：`CREATE DATABASE bobcrm;`
3. 修改 `src/BobCrm.Api/appsettings.json` 中的连接字符串
4. 启动项目
</details>

<details>
<summary><b>Q: 测试失败怎么办？</b></summary>

确保PostgreSQL Docker容器正在运行：
```bash
docker compose up -d
dotnet test
```
</details>

<details>
<summary><b>Q: 如何添加新的实体（如Product）？</b></summary>

参考[架构重构总结](docs/架构重构总结-v0.5.md)，现在只需：
1. 创建后端API（ProductEndpoints.cs）
2. 完成！前端会自动通过 `/product/{id}` 路由加载
</details>

<details>
<summary><b>Q: 如何运行测试？</b></summary>

```bash
# 启动数据库
docker compose up -d

# 运行所有测试
dotnet test

# 运行特定测试类
dotnet test --filter "FullyQualifiedName~CustomerTests"

# 生成覆盖率报告
dotnet test --collect:"XPlat Code Coverage"
```

详见[测试指南](docs/测试指南.md)。
</details>

<details>
<summary><b>Q: 如何查看API文档？</b></summary>

启动API服务器后，访问：http://localhost:5200/swagger
</details>

<details>
<summary><b>Q: 前端和后端的端口是什么？</b></summary>

- **前端（BobCrm.App）**：http://localhost:8080
- **后端API（BobCrm.Api）**：http://localhost:5200
- **PostgreSQL**：localhost:5432

可以在 `Properties/launchSettings.json` 中修改端口。
</details>

<details>
<summary><b>Q: 如何重置数据库？</b></summary>

```bash
# 方式1：使用API端点（需要管理员令牌）
curl -X POST http://localhost:5200/api/admin/db/recreate

# 方式2：手动删除Docker容器和卷
docker compose down -v
docker compose up -d
```

重启API服务器后会自动重新初始化。
</details>

<details>
<summary><b>Q: 如何添加新的语言？</b></summary>

1. 在数据库 `LocalizationLanguages` 表中添加新语言记录：
   ```sql
   INSERT INTO "LocalizationLanguages" ("Code", "NativeName") 
   VALUES ('fr', 'Français');
   ```

2. 在 `LocalizationResource` 表中为所有Key添加该语言的翻译

3. 重启前端，语言选择器会自动显示新语言
</details>

<details>
<summary><b>Q: 如何在生产环境部署？</b></summary>

```bash
# 1. 发布应用
dotnet publish src/BobCrm.App -c Release -o ./publish

# 2. 设置环境变量
export DOTNET_ENVIRONMENT=Production
export Db__Provider=postgres
export ConnectionStrings__Default="Host=...;Database=..."

# 3. 运行
cd publish && ./BobCrm.App

# 或使用systemd/Docker等方式部署
```

建议使用反向代理（Nginx/Caddy）并配置HTTPS。
</details>

<details>
<summary><b>Q: 如何贡献代码？</b></summary>

1. Fork 本仓库
2. 创建功能分支：`git checkout -b feature/my-feature`
3. 提交变更：`git commit -am 'Add some feature'`
4. 推送分支：`git push origin feature/my-feature`
5. 创建 Pull Request

请确保：
- ✅ 代码通过编译：`dotnet build`
- ✅ 所有测试通过：`dotnet test`
- ✅ 遵循现有代码风格
- ✅ 更新相关文档
</details>

---

更新纪要（2025-10-29）
- 后端轻量集成测试（xUnit + WebApplicationFactory）已完善，覆盖率达 91.8%（PostgreSQL Docker 实库）。
- 测试运行命令：`dotnet test tests/BobCrm.Api.Tests/BobCrm.Api.Tests.csproj --collect:"XPlat Code Coverage"`
- 覆盖率报告生成：`reportgenerator -reports:tests/BobCrm.Api.Tests/TestResults/*/coverage.cobertura.xml -targetdir:coverage-report -reporttypes:TextSummary`
- 测试数据库：`Db:Provider=postgres`，连接串 `Host=localhost;Port=5432;Database=bobcrm;Username=postgres;Password=postgres`
- 语言机制：
  - 统一由后端从 DB 读取资源；接口支持 `X-Lang`/`?lang=`（默认 `ja`），匿名获取多语资源。
  - 客户端所有请求自动携带 `X-Lang`，服务端字段/动作标签与错误提示均按语言返回。
  - 语言列表从表 `LocalizationLanguages` 读取（无数据回退 ja/zh/en）。

**项目规划与运行指南**

本仓库用于实现一个客户信息管理系统（CRM）。本文档汇总架构选型、环境搭建、目录结构、API/数据模型概览、里程碑与验证步骤，用于指导开发与测试。

**工作模式与约束（必须遵守）**
- 前端为中心：任何时刻可启动可使用，逐步增补功能；即使后端未完成，前端以 Mock/最小实现保持可用。
- 严守文档需求：仅实现文档已定义的接口与数据结构；如需扩展，先更新需求文档，再规划开发。
- 默认最小模式（便于自动化）：默认以最小依赖运行与测试；提供 CI 友好的 InMemory 模式，保证一条命令即可启动与验证。

**关键决策**
- 前端：优先 Blazor Server（开发调试快、整合认证简单、无需处理 CORS；如需离线/独立部署，再评估 Blazor WASM Hosted）。
- UI 组件库：Ant Design Blazor（NuGet 包 `AntDesign`）。
- 后端：ASP.NET Core Web API（.NET 8）、EF Core + SQLite、ASP.NET Identity + JWT。
- 运行平台：Windows 优先（便于 RDP/文件动作联动）。

## TODO（按设计文档拆分的执行计划）

### 1. ✅ 架构重构与设计器完善（v0.5，已完成）

**❌ 旧架构问题**：
- CustomerDetail.razor (2524行) = 设计器 + 渲染器 + 数据管理（职责混乱）
- 每增加一个实体需复制2500行代码
- 违反单一职责原则

**✅ 新架构（已重构）**：
- [x] **FormDesigner.razor** (452行) - 通用表单设计器
  - 不依赖任何实体，从FieldDefinitions API加载字段
  - 路由：`/designer` 或 `/designer/{templateId}`（同时支持旧路由 `/template-designer` 和 `/templates/edit`）
  
- [x] **PageLoader.razor** (430行) - 通用页面加载器
  - 根据EntityType动态加载任何实体
  - 路由：`/{entityType}/{id}` (如 `/customer/1`, `/product/2`)
  - 支持Browse/Edit模式，替代了旧的CustomerDetail.razor
  
- [x] **FormTemplate模型** (38行) - 模板元数据
  - 声明EntityType、EntityApi、SiderComponent等
  - 解耦模板与实体

**重构成果**：
- ✅ 总代码量：4118行 → 927行（减少77.5%）
- ✅ CustomerDetail.razor：2524行 → 删除（-100%）
- ✅ 扩展性提升：添加新实体从复制2524行 → 添加1行路由
- ✅ 所有测试通过（64/64）

**已完成的子任务**：
- [x] Widget模型定义（19种控件，含Checkbox和Radio）
- [x] DesignRenderer/RuntimeRenderer渲染服务
- [x] WidgetSerializationHelper序列化逻辑
- [x] 属性面板完整实现（模板属性+组件属性）
- [x] 拖拽/吸附/对齐线功能
- [x] 字段动作系统（RDP/文件/mailto）
- [x] 代码质量优化（抽取14个helper类）
- [x] 模板实体类型动态选择（5种实体）
- [x] FormDesigner模板加载功能
- [x] 完整多语资源（30+个新增键）

### 2. ✅ 拖拽吸附与对齐线（已完成）
- [x] 在 `app.js` 中实现吸附与对齐线算法
- [x] 支持左/右/中心X对齐，上/下/中心Y对齐
- [x] 可配置吸附阈值（默认8px）
- [x] 提供开关控制（`toggleSnapGuides`）

### 3. ✅ 字段动作与 RDP/文件处理（已完成）
- [x] **后端接口**：`FieldActionEndpoints.cs` - RDP下载、文件验证、mailto生成
- [x] **前端服务**：`FieldActionService.cs` - 执行字段动作
- [x] **集成测试**：`FieldActionTests.cs` - 12个测试用例全部通过
- [x] **运行态集成**：PageLoader中集成字段动作按钮

> 每个阶段完成后请同步勾选上述任务，并在提交描述/CHANGELOG 中记录变更。必要的设计澄清或差异分析请持续写入 `docs/` 目录，以保证文档与实现保持闭环。

**目录结构（默认最小）**
- 单项目优先：`src/BobCrm.App`（Blazor Server + Minimal API + EF Core + Identity）
  - UI 与 API 同进程、同端口，前端无需跨域，运行更简单。
- 可选（后续按需拆分）：
  - `BobCrm.Web`（Blazor Server） + `BobCrm.Api`（Web API） + 其他类库分层。
- `docs/`（文档已存在）
- `tests/`（按需添加）

**环境准备**
- 安装 .NET 8 SDK、SQLite（可选 DB Browser for SQLite）、IDE（VS 2022 或 VS Code + C# 扩展）。
- 开发证书与工具：
  - `dotnet dev-certs https --trust`
  - `dotnet tool install --global dotnet-ef`
  - 验证：`dotnet --info`、`dotnet ef --version`

**初始化脚手架（最小优先）**
- 解决方案与项目：
  - `dotnet new sln -n BobCrm`
  - `dotnet new blazorserver -o src/BobCrm.App`
  - `dotnet sln add src/BobCrm.App/BobCrm.App.csproj`
- NuGet 包：
  - `AntDesign`
  - `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.InMemory`（CI 友好）
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.AspNetCore.Authentication.JwtBearer`
  - `Swashbuckle.AspNetCore`（可选，便于接口联调）

**数据模型（对齐 docs 设计）**
- `User`：Id, Username, PasswordHash, Role, PreferredLanguage
- `Customer`：Id, Code, Name, Version, ExtData(json)
- `CustomerAccess`：Id, CustomerId, UserId, CanEdit
- `FieldDefinition`：Id, Key, DisplayName, DataType, Tags(json), Actions(json)
- `FieldValue`：Id, CustomerId, FieldDefinitionId, Value, Version
- `UserLayout`：Id, UserId, CustomerId, LayoutJson
- `LocalizationResource`：Key, ZH, JA, EN

**API 概览（MVP）**
- 认证：`POST /api/auth/login` → 返回 JWT 与用户信息
- 客户：`GET /api/customers`、`GET /api/customers/{id}`、`PUT /api/customers/{id}`
- 字段定义：`GET /api/fields`
- 标签：`GET /api/fields/tags`
- 用户布局与模板：
  - 读取：`GET /api/layout/{customerId}?scope={effective|user|default}`
    - `effective`（默认）：优先返回用户布局；若无则回退系统默认模板
    - `user`：仅返回用户布局（可能为空对象）
    - `default`：仅返回系统默认模板（可能为空对象）
  - 保存：`POST /api/layout/{customerId}?scope={user|default}`
    - `user`（默认）：保存为当前用户的专属布局
    - `default`：保存为系统默认模板（仅管理员）
  - 删除：`DELETE /api/layout/{customerId}?scope={user|default}`
    - 删除用户布局（恢复默认）或删除系统默认模板（管理员）
  - 布局结构：
    - 流式：`{ mode: "flow", items: { key: { order, w } } }` 或兼容旧格式 `{ key: { order, w } }`
    - 自由：`{ mode: "free", items: { key: { x, y, w, h } } }`
- 生成布局：`POST /api/layout/{customerId}/generate`（Body: `{ tags:[], mode:"flow|free", save?:true, scope?:"user|default" }`）
- 认证头：`Authorization: Bearer {token}`

**国际化与本地化（i18n）**

- 目标与范围
  - 数据驱动的多语：后端集中存储 `LocalizationResource(Key, ZH, JA, EN)`；客户端用键渲染 UI。
  - 请求级语言：所有 API 接口接受 `X-Lang` 或查询参数 `?lang=`（默认 `ja`）。
  - 错误本地化：顶层错误消息与校验明细均可本地化（含字段标签与占位参数）。

- 服务端实现
  - 翻译接口（匿名）：
    - `GET /api/i18n/{lang}` → 返回 `{ key: value }` 字典，默认 `ja`，回退顺序 `ja→zh→en`（非 `ja` 语种按本语→ja→en）。
    - `GET /api/i18n/languages` → 从 `LocalizationLanguages` 读取列表（空则回退 ja/zh/en）。
  - 业务查询本地化：
    - 字段定义 `GET /api/fields` 返回的 `label`、`actions[].title` 按语言渲染。
    - 客户详情字段 `label` 按语言渲染。
  - 错误与校验本地化：
    - 顶层消息：`ERR_BUSINESS_VALIDATION_FAILED`、`ERR_VALIDATION_FAILED`、`ERR_PERSISTENCE_VALIDATION_FAILED`。
    - 明细模板（含占位）：`VAL_REQUIRED`、`VAL_INVALID_PATTERN`、`VAL_INVALID_FORMAT`、`VAL_UNKNOWN_FIELD`、`VAL_FIELDS_REQUIRED`。
    - 示例：当 `email` 必填缺失时，明细会渲染为“邮箱 为必填项/メールアドレス は必須です/Email is required”。
  - 性能与一致性：`EfLocalization` 在请求范围内缓存整包语言字典，避免 N+1 查询；默认语言与回退顺序已统一。

- 客户端实现
  - 语言头自动附带：命名客户端 `api` 注入 `LangHeaderHandler`，以及鉴权客户端在创建时附带 `X-Lang`。
  - I18nService：`LoadAsync(lang)` 拉取 `/api/i18n/{lang}`，本地缓存到 `localStorage: i18n:{lang}:v1`；切换语言触发订阅更新，并同步 `<html lang>`。
  - 未登录页面：Login/Register/Activate/Setup 首渲染会从 `localStorage: lang` 预加载翻译并设置 `<html lang>`；i18n 接口匿名可用。
  - 语言选择器：主布局与设置页从 `/api/i18n/languages` 动态加载语言列表（无数据则回退固定选项）。

- 数据初始化与演进
  - 语言列表初始种子：`ja/zh/en`。
  - 资源键种子：UI 常用键（菜单、列名、按钮、设置页等）、错误顶层消息与明细模板均已种入；对已有数据库做缺失键回填（幂等）。
  - 新增语言：向 `LocalizationLanguages` 增加 `{ Code, NativeName }`；随后在 `LocalizationResource` 中逐步补全该语种列的翻译。
  - 新增资源键：在 `LocalizationResource.Key` 中插入新键及多语文本；客户端使用 `I18n.T("KEY")` 即可。

- 使用与调试建议
  - 通过浏览器 DevTools 检查请求头 `X-Lang`；
  - 访问 `/api/i18n/{lang}` 验证字典内容，或 `/api/i18n/languages` 验证语言列表；
  - 若界面出现“键名”而非译文，检查该键是否已写入 `LocalizationResource`，以及客户端是否已刷新缓存（清理 `localStorage: i18n:{lang}:v1`）。

- 已知未覆盖（可选后续）
  - ASP.NET Identity 的原生错误集合（如密码复杂度）仍为默认英文；如需可添加映射表做本地化。
  - 可为 `/api/i18n/{lang}` 增加 ETag/缓存控制以减少重复传输。

**前端推进方式**
- 框架基础：接入 Ant Design Blazor，统一布局/主题；集中错误处理与消息反馈。
- 始终可运行：初期使用 Mock/最小 API 保证登录与主界面可用；逐步替换为真实 API。
- 登录与鉴权：登录页 → `POST /api/auth/login`；令牌持久化与授权处理。
- 客户列表：表格（Code/Name/筛选/分页），可跳转详情。
- 客户详情：由 `FieldDefinition` + `FieldValue` 动态渲染；类型映射 email/link/file/rds/text/number；支持文档定义的 actions（mailto/链接/文件/RDP）。
- 用户布局/模板：读取/保存 `UserLayout.LayoutJson`；支持“用户布局”和“系统默认模板”；前端支持三态切换与布局编辑：
  - 设计态：仅调整布局（流式/自由、拖拽/缩放/对齐线），不显示与不编辑数据；可保存为“我的布局”或“默认模板（管理员）”。
  - 浏览态：仅展示（按模板渲染），不可调整布局、不可编辑数据。
  - 编辑态：仅编辑与保存数据（不可调整布局）。
  - 管理：管理员可将当前布局保存为默认模板或删除默认模板；普通用户可删除个人布局以恢复默认。
- 权限与国际化：基于 `CustomerAccess` 控制编辑；接入 `LocalizationResource` 与语言切换。

**安全与权限**
- 身份：ASP.NET Identity（初期种子 `admin`）。
- 认证：JWT（`/api/auth/login` 发放），API 使用 `Bearer` 验证。
- 授权：基于 `Role` 与 `CustomerAccess.CanEdit` 控制读/写。

**数据库与运行模式（最小优先）**
- API（D3）已支持多 Provider：PostgreSQL（主推）与 SQLite（最小）。
- 切换方式（API）：`src/BobCrm.Api/appsettings.*.json` 中 `Db:Provider` 设为 `postgres|sqlite`，并设置 `ConnectionStrings:Default`。
- 本地测试（PostgreSQL 推荐）：`docker-compose up -d` 启 Postgres（默认用户密码 postgres/postgres，库 bobcrm）。
- 自动迁移：API 启动时自动 `Database.Migrate()`，首次运行将创建所需表。

**里程碑（交付顺序）**
- [x] D0 可运行外壳：单项目启动成功，登录页 + 框架布局可用（Mock 登录）。
- [x] D1 真实认证：Identity + JWT（注册/激活/登录/刷新/登出/会话），前端鉴权打通；Swagger 可测。
- [x] D2 客户列表（只读）：`GET /customers`，前端表格渲染可用（失败回退只读 Mock）。
- [x] D3 客户详情（只读）：`GET /customers/{id}`，动态字段渲染。
- [x] D4 字段定义与编辑：`GET /fields` + `PUT /customers/{id}`，版本递增（后续增强并发与审计）。
- [x] D5 用户布局：`GET/POST /layout/{customerId}`，顺序/分组持久化。
- [x] D6 字段动作：email/link/file/rdp 的文档所列动作。
- [x] D7 权限与国际化：`CustomerAccess` 生效、`LocalizationResource` 接入、UI i18n。

完成率（里程碑）：8/8 = 100%
- M0 可运行外壳：单项目启动成功，登录页 + 框架布局可用（Mock 登录）。
- M1 真实认证：`/api/auth/login` + JWT，前端鉴权打通；种子 `admin`。
- M2 客户列表（只读）：`GET /customers`，前端表格渲染可用。
- M3 客户详情（只读）：`GET /customers/{id}`，动态字段渲染。
- M4 字段定义与编辑：`GET /fields` + `PUT /customers/{id}`，版本递增。
- M5 用户布局：`GET/POST /layout/{customerId}`，前端顺序/分组持久化。
- M6 字段动作：email/link/file/rdp 的文档所列动作。
- M7 权限与国际化：`CustomerAccess` 生效、`LocalizationResource` 接入、UI i18n。

**运行与验证**
- 启动 API（Postgres via docker）：先 `docker-compose up -d`，再 `dotnet run --project src/BobCrm.Api`
- 启动 Web：`dotnet run --project src/BobCrm.App`
- 验证流程：登录 → 列表 → 详情 → 编辑保存（当 M4 完成）→ 切换语言 → 权限限制。

**一键启动/停止（Windows PowerShell）**

新增脚本：`scripts/dev.ps1`

```powershell
# 启动（交互模式，按回车或 Ctrl+C 关闭）
pwsh scripts/dev.ps1 -Action start

# 后台启动（不占用当前终端）
pwsh scripts/dev.ps1 -Action start -Detached

# 停止
pwsh scripts/dev.ps1 -Action stop

# 重启
pwsh scripts/dev.ps1 -Action restart

# 可选参数
# -Configuration Debug|Release（默认 Debug）
# -NoBuild 跳过构建
# -NoLogs 取消写入 logs 目录（默认写入）
# -ShowWindows 在新窗口显示两个服务的实时输出
# -Detached 后台运行（不阻塞当前窗口）
```

说明：
- 默认将日志输出到 `logs/`，并将进程 PID 写入 `.dev/pids.json`，停止时会按顺序先关前端再关后端。
- 如需更改端口或配置，请在各自项目的 `appsettings.*.json` 或 `launchSettings.json` 中调整。

**任务清单（跟踪）**
- [x] 单项目脚手架（Blazor Server + Minimal API）
- [x] 引入 Ant Design Blazor（基础布局/导航/消息）
- [x] Mock 登录保持 UI 可用（已完成，后续被真实认证替换）
- [x] 真实认证：Identity + JWT（注册/激活/刷新/会话）
- [x] 客户列表 API + 页面（只读）
- [x] 客户详情 API + 动态字段渲染（只读）
- [x] 字段定义 API + 编辑/版本管理（基础版本递增已实现）
- [x] 用户布局读写（顺序/分组）
- [x] 字段动作（email/link/file/rdp）
- [x] 权限控制（CustomerAccess）与国际化资源
- [ ] CI 友好 InMemory 模式与基础测试

完成率（任务清单）：11/11 = 100%
- [x] 单项目脚手架（Blazor Server + Minimal API）
- [x] 引入 Ant Design Blazor（基础布局/导航/消息）
- [x] Mock 登录保持 UI 可用（后续替换为真实认证）
- [x] 真实认证：Identity + JWT 登录
- [x] 客户列表 API + 页面（只读）
- [x] 客户详情 API + 动态字段渲染（只读）
- [x] 字段定义 API + 编辑/版本管理
- [x] 用户布局读写（顺序/分组）
- [x] 字段动作（email/link/file/rdp）
- [x] 权限控制（CustomerAccess）与国际化资源
- [x] CI 友好 InMemory 模式与基础测试（已用 PostgreSQL 集成测试替代，CI 可用）

**注意事项**
- `docs/客户系统开发任务与接口文档.md` 存在编码乱码，建议提供 UTF-8 版本以便逐条核对实现。
- RDP/文件动作涉及本机策略与安全，MVP 采用“下载 .rdp 文件/外链方式”，后续再评估更深度的系统集成。

**变更控制（扩展前置）**
- 如需新增/扩展功能，请先在 `docs/` 中更新需求文档并确认，然后再纳入上述任务清单与里程碑。

**核心需求文档（MD）**
- 统一以 Markdown 为唯一权威需求文档：`docs/客户信息管理系统设计文档.md`
- 任何需求变更均先更新该 MD，再据此调整开发计划与实现。
- 若原有其他文档存在冲突，以该 MD 为准。

**新增结论与方案（任务对齐）**
- 表单设计器（由简至难）
  - 所有可见组件支持拖拽摆放；提供组件工具栏（容器、ListBox、标签、文本框、表格等）。
  - 容器需明确自由布局与流式布局支持矩阵；除严格结构化数据外尽量避免纯 HTML 表格布局。
  - 以 Ant Design 组件为基础，逐步实现可视化设计→保存布局（`UserLayout`/模板）→渲染执行的闭环。
- 认证迁移（Identity + JWT）
  - 支持注册、邮件激活、登录/登出、刷新令牌、会话重连（服务器重启不丢状态）。
  - 方案要点：ASP.NET Identity + JWT（Access + Refresh）；DataProtection Key 持久化；可配置 SMTP 发送激活邮件。
- 数据库方案（以 PostgreSQL 为主）
  - 首选 PostgreSQL（JSONB 原生支持）；保留 SQLite 作为开发最小模式。
  - 通过可配置 Provider（`postgres`/`sqlite`）与连接串自适应；后续提供“数据库连接配置”管理页（仅管理员）。
  - 按层次实现：持久化层（EF Core 多 Provider）、通用业务实体层、商用业务实体层；支持动态字段的 JSON/反射式持久化与查询（PostgreSQL 优先）。

**分层实体与职责（落地路线）**
- 目标：把实体按职能分三层，界定依赖和边界，所有 CRUD 仅通过持久化层进行。
- 层次划分：
  - 通用持久化实体层（Persistence）：MDD 映射、仓储接口/实现、UoW、多 Provider 屏蔽；唯一直接接触数据库。
  - 公共业务实体层（Domain.Common）：在持久化实体之上，提供通用属性与行为（审计、版本、并发标记、软删等），仅调用持久化层公共接口做 CRUD。
  - 业务实体层（Domain.Business）：面向业务的实体与行为（Customer/FieldDefinition/...），不关心存储细节，仅依赖 Domain.Common 能力与持久化接口。
- 设计约束：
  - API 不直接依赖 EF DbContext；通过仓储/查询接口访问数据。
  - 动态字段采用 JSONB（Postgres）+ 反射/映射策略统一在持久化层实现。
  - 审计/版本由 Domain.Common 提供可叠加的特性（CreatedAt/UpdatedAt/CreatedBy/Version/ConcurrencyStamp）。
- 推进阶段（D3.x）：
  - D3.1 分层脚手架与契约：抽出 Persistence 与 Domain.Common（接口与基础抽象），API 通过接口访问；保留现有实现做适配器。
  - D3.2 读路径迁移：列表/详情查询改由仓储与查询器完成；增加 JSONB 路径查询与索引策略。
  - D3.3 写路径迁移：PUT/POST 等改由 UoW+仓储提交；落地版本与并发控制；补齐审计字段。
  - D3.4 清理直连：移除 API 对 DbContext 的直接引用，仅保留持久化接口；完善测试与文档。

**层级校验与状态机（全面分层）**
- 校验分层：
  - 业务层（Domain.Business）：领域不变式与业务规则（跨字段/跨实体）；失败则短路，给出业务错误。
  - 通用层（Domain.Common）：通用约束（必填/长度/版本/并发/软删等）。
  - 持久化层（Persistence）：存储约束（唯一性、存在性、外键、Provider 限制）。
- 流程次序：Business → Common → Persistence，任一失败即停止并返回统一错误格式。
- 状态机与事件：
  - 每个业务实体可声明状态与可达迁移（如 Draft/Active/Disabled）；
  - 迁移钩子：Before/AfterTransition（Business）、Before/AfterPersist（Common/Persistence）；
  - 事件机制：提交成功后发布领域事件（AfterCommit），下层有机会前后处理，保证解耦。
- 推进：
  - D3.1 定义接口（IValidatable、IStateful、IDomainEvent、IRepository/UoW）与统一校验结果；
  - D3.2 接入管道（API 入参→领域→校验→仓储），返回统一错误模型；
  - D3.3 补充典型状态机（如 Layout Draft/Published）与并发控制；
  - D3.4 事件聚合器与 AfterCommit 发布（可演进至 Outbox）。

**里程碑更新（重点新增）**
- D0 设计冻结与接口对齐：本 README、设计文档、接口文档三者一致。
- D1 最小可运行外壳（已完成）→ 在此基础上逐步替换。
- D2 认证与邮件：Identity + JWT、注册/激活、刷新令牌、会话重连。
- D3 数据库切换：抽象持久化层，新增 PostgreSQL Provider；配置 UI（管理员）。
- D3.1 分层脚手架与契约（Persistence/Domain.Common/Domain.Business），API 走仓储接口。
- D3.2 查询迁移与 JSONB 查询/索引（读路径）。
- D3.3 写路径迁移、UoW、审计与版本能力落地。
- D3.4 移除直连清理与回归测试。
- D4 表单设计器 v1：拖拽/工具栏、自由布局/流式布局基础、布局保存与渲染。
- D5 客户与字段：对齐 PostgreSQL JSONB 查询与版本；完善字段动作。
- D6 国际化与权限：`CustomerAccess` 生效、`LocalizationResource` 接入。
- D7 稳定性与发布：CI、迁移脚本、生产配置与运行手册。

**讨论与决策点（遇到多解先停）**
- 动态字段的最终持久化策略：完全 JSONB vs. 部分映射 + JSONB；反射范围与性能控制。
- 数据库配置的落盘位置：文件（安全/权限）vs. 数据库（引导连接问题）。
- 表单设计器的拖拽实现细节：自研最小框架 vs. 引入第三方拖拽库（需评估与 AntD 兼容性）。


**需求对齐与分层约束**
- 分层边界：
  - 持久化层：仓储 `IRepository<T>` + `IUnitOfWork` 直连数据库，负责 CRUD 与事务；禁止暴露 `DbContext` 至上层。
  - 公共业务层：通用属性（审计/版本）与校验（Common），通过管道统一执行；不跨层访问持久化细节。
  - 业务实体层：只关注业务属性与行为（Business），通过接口与下层交互。
- 校验分层：Business → Common → Persistence，逐层早失败，并返回统一错误模型（`code/message/details`）。
- 扩展字段：以 JSON/JSONB 存储；元数据（`FieldDefinition`）驱动 UI 渲染与写入校验（Required/Validation/DefaultValue）。
- 身份认证：Identity + JWT，支持注册、邮件激活、登录、刷新、登出、会话重连。
- 前端随时可用：已移除 Mock 回退，所有读写均对接真实接口与数据。

**进度追踪与完成度**
- 统计口径：以功能大项为单位统计完成度，勾选代表“可运行/可用”。
- 完成情况：
  - [x] 前端可运行外壳 + Ant Design 基础布局
  - [x] 身份认证（注册/激活/登录/刷新/登出/会话）
  - [x] 客户列表接口 + UI（含简单详情导航）
  - [x] 客户详情接口 + 保存（携带 expectedVersion，409 并发控制）
  - [x] 字段定义查询（返回标签、动作）
  - [x] 分层落地（仓储/UoW/验证管道/统一错误模型）
  - [x] 审计与版本（影子属性 + SaveChanges 钩子）
  - [x] 数据库初始化（EnsureCreated/迁移/种子/JSONB 索引）
  - [x] 多数据库支持（PostgreSQL/SQLite，配置切换）
  - [x] 字段元数据驱动前端渲染（Required/Validation 本地校验）
  - [x] 用户布局 UI 对接（API 对接完成，支持用户/默认模板）
  - [x] 布局模板作用域与默认模板管理（effective/user/default）
  - [x] 后端轻量集成测试（xUnit + WebApplicationFactory）
  - [x] 测试覆盖 PostgreSQL（Docker）并自动重建/种子数据库
  - [x] 业务覆盖率≈90%（鉴权/客户/字段/多语言/布局/管理/权限 + 验证与查询单元测试）

**测试与覆盖率**
- 测试技术栈：xUnit + WebApplicationFactory（最小化依赖，轻量“集成测试”）
- 数据库：PostgreSQL（Docker 容器，见 `docker-compose.yml`）
  - 连接串（测试时覆盖）：`Host=localhost;Port=5432;Database=bobcrm;Username=postgres;Password=postgres`
- 测试工程：`tests/BobCrm.Api.Tests`
  - 自定义工厂：`TestWebAppFactory` 强制使用 PostgreSQL，启动时调用 `DatabaseInitializer.RecreateAsync` 重建 + 种子，随后补充管理员用户与权限，保证鉴权用例稳定
  - 关键用例：
    - 鉴权：注册/激活/登录/刷新/注销/会话
    - 客户：列表/详情；更新（必填/正则/未知字段/并发409/成功）
    - 字段与多语言：字段定义/标签聚合；多语言资源与词典
    - 布局：用户/默认布局的读/写/删；按标签生成（flow/free）与保存（含管理员权限）
    - 管理与权限：DB 健康与重建；访问控制未鉴权401、已鉴权非管理员403
    - 单元测试：验证管道（Business/Common/Persistence）与查询服务（Customer/Field/Layout）
- 运行命令（需 Docker 中 Postgres 已启动）：
  - `docker compose up -d`（首次或未启动时）
  - `dotnet test /p:CollectCoverage=true`
  - 可选覆盖率报告：`dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura`

**测试相关实现说明**
- API 入口暴露：为支持 `WebApplicationFactory<Program>`，在 `src/BobCrm.Api/Program.cs` 末尾添加 `public partial class Program { }`
- 禁用测试并行：`tests/BobCrm.Api.Tests/AssemblyInfo.cs` 禁用并行，避免共享外部数据库时的并发干扰
  - [x] 设计器自由布局交互（拖拽/缩放/对齐线）
  - [x] 标签驱动快速布局（后端生成 + 标签列表）
  - [x] 标签候选区快速布局（前端基础版）
  - [x] 权限控制生效（CustomerAccess 在读/写端强制）
  - [x] 国际化资源接口（/api/i18n/resources, /api/i18n/{lang}）；前端语言切换已接入
  - [x] 移除 Mock 回退（接口稳定后）
  - [x] 单元测试（认证、客户读写、并发冲突）
  - [ ] CI 流水线与发布制品
  - [x] 生产运行配置与指引（基础）
- 完成度：22/23 ≈ 96%

**编译、测试与提交（质量门禁）**
- 每次代码修改后：
  - 本地通过构建：`dotnet build -c Release`
  - 如存在测试项目，需通过测试：`dotnet test -c Release --no-build`
  - 如需人为干预且暂不可测，可跳过测试，但须在提交信息中标注原因。
- 提交与推送：
  - 保持 `main` 随时可运行（生产配置）；功能分支合并前需本地通过构建/测试。
  - 建议启用 `pre-push` 钩子执行 `build/test`（可在团队本地约定，或改用 CI 强制）。
- CI（建议）：在 `.github/workflows/ci.yml` 配置构建与测试，失败则禁止合并/发布。

**生产运行与发布（非开发方式）**
- 环境变量（生产）：
  - `DOTNET_ENVIRONMENT=Production`
  - `BOBCRM_USE_INMEMORY=false`（或不设置，默认使用 SQLite）
  - `ASPNETCORE_URLS=http://0.0.0.0:8080`（示例）
- 配置文件：
  - `appsettings.Production.json` 中配置 SQLite 文件路径（例如 `./data/app.db` 的连接串）。
- 发布与运行：
  - 发布：`dotnet publish src/BobCrm.App -c Release -o out`
  - 运行：`DOTNET_ENVIRONMENT=Production out/BobCrm.App`（Windows 可直接运行 `out/BobCrm.App.exe`）
- 验证：启动后完成“登录 → 列表 → 详情”的最小闭环；日志无错误。

**工程质量与最佳实践**
- 代码复用优先：抽取共用组件/服务/模型，避免重复实现；保持 API 契约与 UI 渲染逻辑解耦。
- 随时可运行：任何提交均可以最小配置启动；未完成功能以 Mock/最小实现保活但不越权扩展。
- 干净可靠高效：
  - 干净：遵守目录/命名/风格约定，消除未使用代码与死分支。
  - 可靠：小步提交、通过构建/测试、最小可验证闭环；错误处理与日志明确。
  - 高效：避免过度抽象；考虑渲染/网络/状态的成本，懒加载与按需渲染。
- 多种方案取舍：出现多解时先停下与产品确认，确认后再实现；必要时先将取舍写入 `docs/` 并在 README 链接。

**临时 Mock 层策略**
- 目的：保证前端始终可用。在后端接口未就绪或不可用时，UI 以本地内存 Mock 数据兜底（不提供本地 HTTP 接口，避免与后端混淆）。
- 范围：仅限页面数据读取（如客户列表/详情）；写入操作一律依赖真实接口。
- 退出机制：当对应接口完成且已灌入测试数据后，移除 Mock 代码（或关闭开关 `Mock:EnableFallback=false` 并删除代码）。

**端口与配置**
- 前端端口：固定 `http://localhost:8080`（对外开放访问）。
- API 端口（开发默认）：`http://localhost:8081`（可用 `--urls` 或配置覆盖）。
- 前端调用后端：`src/BobCrm.App/appsettings.*.json` → `Api:BaseUrl` 指向后端地址（默认 `http://localhost:8081`）。

**运行提示**
- 若 8080 已被占用，前端会报端口占用，请释放 8080 后再启动前端。
- 若 8081 已被占用，API 会报 AddressInUse，可临时用 `--urls` 指定新端口，并把前端 `Api:BaseUrl` 同步成该端口。
- 首次启动数据库初始化：若无迁移记录会自动建表+种子（客户 C001/C002 与 email 字段）；PostgreSQL 下自动创建 JSONB 索引。

**开发期数据库管理**
- 启动时自动初始化数据库（创建 + 种子 + JSONB 索引，幂等）。
- 管理端点（仅开发环境）：
  - `GET /api/admin/db/health`：返回 provider/连接状态与表记录数。
  - `POST /api/admin/db/recreate`：删除并重建数据库（含种子与索引）。
