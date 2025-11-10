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
- docs/PROC-00-文档索引.md

常用直达：
- 产品设计：docs/PROD-01-客户信息管理系统设计文档.md
- 接口契约：docs/API-01-接口文档.md
- 架构说明：docs/ARCH-01-实体自定义与发布系统设计文档.md
- 界面规范：docs/UI-01-UIUE设计说明书.md
- 测试指南：docs/TEST-01-测试指南.md
- 多语设计：docs/I18N-01-多语机制设计文档.md

## 贡献
- PR 检查清单：docs/PROC-01-PR检查清单.md
- 文档同步规范：docs/PROC-02-文档同步规范.md

## 变更日志
- CHANGELOG.md
