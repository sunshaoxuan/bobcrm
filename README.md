# bobcrm

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
- 用户布局：`GET /api/layout/{customerId}`、`POST /api/layout/{customerId}`
- 认证头：`Authorization: Bearer {token}`

**前端推进方式**
- 框架基础：接入 Ant Design Blazor，统一布局/主题；集中错误处理与消息反馈。
- 始终可运行：初期使用 Mock/最小 API 保证登录与主界面可用；逐步替换为真实 API。
- 登录与鉴权：登录页 → `POST /api/auth/login`；令牌持久化与授权处理。
- 客户列表：表格（Code/Name/筛选/分页），可跳转详情。
- 客户详情：由 `FieldDefinition` + `FieldValue` 动态渲染；类型映射 email/link/file/rds/text/number；支持文档定义的 actions（mailto/链接/文件/RDP）。
- 用户布局：读取/保存 `UserLayout.LayoutJson`，先做顺序/分组。
- 权限与国际化：基于 `CustomerAccess` 控制编辑；接入 `LocalizationResource` 与语言切换。

**安全与权限**
- 身份：ASP.NET Identity（初期种子 `admin`）。
- 认证：JWT（`/api/auth/login` 发放），API 使用 `Bearer` 验证。
- 授权：基于 `Role` 与 `CustomerAccess.CanEdit` 控制读/写。

**数据库与运行模式（最小优先）**
- 单项目内配置 `AppDbContext`：默认使用 SQLite 文件 `./data/app.db`；提供 InMemory 模式用于 CI。
- 模式切换：环境变量 `BOBCRM_USE_INMEMORY=true` 时使用 InMemory；否则使用 SQLite。
- 迁移：在 SQLite 模式下执行 `dotnet ef migrations add InitialCreate` 与 `dotnet ef database update`（项目为 `src/BobCrm.App`）。

**里程碑（交付顺序）**
- M0 可运行外壳：单项目启动成功，登录页 + 框架布局可用（Mock 登录）。
- M1 真实认证：`/api/auth/login` + JWT，前端鉴权打通；种子 `admin`。
- M2 客户列表（只读）：`GET /customers`，前端表格渲染可用。
- M3 客户详情（只读）：`GET /customers/{id}`，动态字段渲染。
- M4 字段定义与编辑：`GET /fields` + `PUT /customers/{id}`，版本递增。
- M5 用户布局：`GET/POST /layout/{customerId}`，前端顺序/分组持久化。
- M6 字段动作：email/link/file/rdp 的文档所列动作。
- M7 权限与国际化：`CustomerAccess` 生效、`LocalizationResource` 接入、UI i18n。

**运行与验证**
- 启动（默认最小）：`dotnet run --project src/BobCrm.App`
- 启动（CI/InMemory）：`$env:BOBCRM_USE_INMEMORY='true'; dotnet run --project src/BobCrm.App`
- 验证流程：登录 → 列表 → 详情 → 编辑保存（当 M4 完成）→ 切换语言 → 权限限制。

**任务清单（跟踪）**
- [ ] 单项目脚手架（Blazor Server + Minimal API）
- [ ] 引入 Ant Design Blazor（基础布局/导航/消息）
- [ ] Mock 登录保持 UI 可用（后续替换为真实认证）
- [ ] 真实认证：Identity + JWT 登录
- [ ] 客户列表 API + 页面（只读）
- [ ] 客户详情 API + 动态字段渲染（只读）
- [ ] 字段定义 API + 编辑/版本管理
- [ ] 用户布局读写（顺序/分组）
- [ ] 字段动作（email/link/file/rdp）
- [ ] 权限控制（CustomerAccess）与国际化资源
- [ ] CI 友好 InMemory 模式与基础测试

**注意事项**
- `docs/客户系统开发任务与接口文档.md` 存在编码乱码，建议提供 UTF-8 版本以便逐条核对实现。
- RDP/文件动作涉及本机策略与安全，MVP 采用“下载 .rdp 文件/外链方式”，后续再评估更深度的系统集成。

**变更控制（扩展前置）**
- 如需新增/扩展功能，请先在 `docs/` 中更新需求文档并确认，然后再纳入上述任务清单与里程碑。

**核心需求文档（MD）**
- 统一以 Markdown 为唯一权威需求文档：`docs/客户信息管理系统设计文档.md`
- 任何需求变更均先更新该 MD，再据此调整开发计划与实现。
- 若原有其他文档存在冲突，以该 MD 为准。

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
