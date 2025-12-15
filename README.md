# BobCRM

基于 .NET 8 的 Blazor Server 客户信息管理系统（CRM）。本仓库聚焦于最小依赖、可快速验证与持续交付。

## 环境要求
- .NET 8 SDK
- Docker（PostgreSQL）
- PowerShell 7+

## 快速开始
```bash
git clone <repository-url>
cd bobcrm
```

```powershell
# 环境自检（.NET、Docker、端口、权限等）
pwsh scripts/verify-setup.ps1

# 启动数据库（默认 Postgres: postgres/postgres）
docker compose up -d

# 启动开发（前后端/最小 API）
pwsh scripts/dev.ps1 -Action start
```

- 前端：http://localhost:3000
- 接口文档（Swagger）：http://localhost:5200/swagger
- 默认账号：`admin` / `Admin@12345`

## 内置功能
- **组织档案**：树形组织维护（增删改、PathCode 自动生成）。
- **角色管理**：角色建档、功能/数据范围分配，与 FunctionNodes 菜单联动。
- **用户档案**：用户列表/详情/创建、密码与锁定状态维护，以及角色勾选与即时生效。
- **动态实体 + 模板化**：实体定义、模板设计/绑定、运行态渲染。
- **多语言界面**：内置中/日/英词条，可通过 `I18nService` 下发。

### 常用命令
```powershell
# 停止开发环境
pwsh scripts/dev.ps1 -Action stop

# 运行测试
dotnet test
```

## 文档索引
完整文档已按"分类简写-两位编号-中文标题.md"归档，入口见：
- `docs/process/PROC-00-文档索引.md`

按类型分类的常用入口：
- 设计：`docs/design/`（如 `PROD-01`、`ARCH-20`、`ARCH-21`、`UI-01` 等）
- 指南：`docs/guides/`（如 `GUIDE-11` 实体定义与动态实体操作指南、`TEST-01` 测试指南）
- 参考：`docs/reference/API-01-接口文档.md`
- 历史：`docs/history/`（差距/修复记录）
- 流程：`docs/process/`（PR 清单、文档规范等）
- 示例：`docs/examples/EX-01-订单管理示例.md`

## 贡献
- PR 检查清单：`docs/process/PROC-01-PR检查清单.md`
- 文档同步规范：`docs/process/PROC-02-文档同步规范.md`

## 变更日志
- CHANGELOG.md
