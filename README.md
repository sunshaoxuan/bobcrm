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

### 常用命令
```powershell
# 停止开发环境
pwsh scripts/dev.ps1 -Action stop

# 运行测试
dotnet test
```

## 文档索引
完整文档已按“分类简写-两位编号-中文标题.md”归档，入口见：
- `docs/PROC-00-文档索引.md`

按类型分类的常用入口：
- 设计：`docs/design/`（如 `PROD-01`、`ARCH-20`、`ARCH-21`、`UI-01` 等）
- 指南：`docs/guides/`（如 `FRONT-01` 实体与动态数据操作指南、`TEST-01` 测试指南）
- 参考：`docs/reference/API-01-接口文档.md`
- 历史：`docs/history/`（差距/修复记录）
- 流程：`docs/process/`（PR 清单、文档规范等）
- 示例：`docs/examples/EX-01-订单管理示例.md`

## 贡献
- PR 检查清单：`docs/process/PROC-01-PR检查清单.md`
- 文档同步规范：`docs/process/PROC-02-文档同步规范.md`

## 变更日志
- CHANGELOG.md
