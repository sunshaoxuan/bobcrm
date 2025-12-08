# PROC-03: 标准验证流程

> **版本**: 1.0
> **生效日期**: 2025-12-08
> **状态**: 执行中

---

## 1. 概述 (Overview)

本文档概述了验证 BobCRM 应用程序健康状况和功能的标准流程。请在以下情况下运行这些流程：

1.  设置新的开发环境后。
2.  进行重大重构或基础设施更改后。
3.  遇到“登录失败”或“网络错误”问题时。

## 2. 环境健康检查 (Environment Health Check)

运行自动化环境检查脚本以验证工具链和基本构建健康状况。

```powershell
./scripts/verify-setup.ps1
```

**检查清单:**
- [ ] .NET SDK 已安装且兼容
- [ ] PostgreSQL 可用（或 SQLite 回退）
- [ ] Node.js（如果适用于未来的前端分离）
- [ ] 解决方案无错误构建
- [ ] 单元测试通过

## 3. 认证与集成检查 (Authentication & Integration Check)

验证核心认证流程，测试整个前端到后端的路径和数据库种子数据。

```powershell
./scripts/verify-auth.ps1
```

**脚本功能说明:**
1.  **端口检查**: 验证端口 5200 (API) 和 3000 (App) 是否正在监听。
2.  **后端连接性**: Ping `http://localhost:5200/health`（或根目录）。
3.  **后端认证**: 尝试直接登录 `admin` 到 API (5200)。
    - *此处失败表示:* 数据库种子数据失败，或 `AccessService` 未初始化。
4.  **前端代理连接性**: Ping `http://localhost:3000`。
5.  **前端代理认证**: 尝试通过前端 (3000) -> 代理 -> API 登录 `admin`。
    - *此处失败表示:* 前端 `launchSettings` 或 `Program.cs` 代理配置错误，或 CORS 问题。

## 4. 手动冒烟测试 (Manual Smoke Test)

如果自动化检查通过，请执行手动冒烟测试。

1.  打开 `http://localhost:3000/login`
2.  使用 `admin` / `Admin@12345` 登录
3.  验证重定向到仪表板。
4.  检查 `http://localhost:3000/setup/admin` 返回 `{ exists: true }`。

## 5. 故障排除指南 (Troubleshooting Guide)

### 5.1 浏览器中“登录失败”，但 `verify-auth.ps1` 通过
- **原因**: 浏览器特定问题（缓存、JavaScript 错误、`localStorage` 配额）。
- **修复**: 清除 Cookie/存储，检查浏览器控制台 (F12) 是否有 JS 错误。

### 5.2 脚本中代理（端口 3000）“登录失败”
- **原因**: 前端应用无法连接到后端 API。
- **修复**:
    - 检查 `src/BobCrm.App/appsettings.Development.json` 中的 `Api:BaseUrl`。
    - 确保后端正在运行。
    - 检查 `http` 与 `https` 是否不匹配。

### 5.3 脚本中后端（端口 5200）“登录失败”
- **原因**: 数据库中不存在 Admin 用户或密码不匹配。
- **修复**:
    - 运行 `./scripts/reset-db.ps1`（如果有）或删除 DB 文件。
    - 重启后端以触发 `DatabaseInitializer`。
    - 检查 `api_*.log` 是否有种子数据错误。

### 5.4 “地址已被使用” (Address already in use)
- **修复**:
    - 杀掉残留进程：`taskkill /F /IM "BobCrm.Api.exe" /T` 或使用 `scripts/dev.ps1 -Action stop`。
