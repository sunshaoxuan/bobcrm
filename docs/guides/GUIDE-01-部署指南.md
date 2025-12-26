# 部署指南

## 概述
本指南介绍 BobCRM 应用程序的部署配置要求，特别关注生产环境的安全设置。

## 环境配置

### 安全性配置 (Critical)

在非开发环境（即 environment 不为 `Development`）下，系统强制要求配置以下安全参数。若未正确配置，应用程序将**拒绝启动**。

#### 1. JWT 签名密钥 (`Jwt:Key`)
- **要求**: 必须提供，且长度足够（建议 32 字符以上）。
- **默认行为**:
  - Development：允许缺省，运行时会使用内置的开发用默认密钥（仅用于本地调试）。
  - 非 Development：若缺省则**启动失败**。
- **配置示例**（`appsettings.json`）:
  ```json
  "Jwt": {
    "Key": "YOUR_STRONG_SECRET_KEY_HERE_MIN_32_CHARS"
  }
  ```
- **环境变量示例**（Windows / Linux）:
  - `Jwt__Key=YOUR_STRONG_SECRET_KEY_HERE_MIN_32_CHARS`

#### 2. CORS 跨域策略 (`Cors:AllowedOrigins`)
- **要求**: 必须显式指定允许的跨域源（Origin）。
- **默认行为**:
  - Development：允许任意 Origin（通过 `SetIsOriginAllowed`），并启用 `AllowCredentials`，便于本地联调。
  - 非 Development：若未配置允许列表则**启动失败**；配置后将按列表限制 Origin，并启用 `AllowCredentials`。
- **配置格式**（两种均支持）:
  - 数组：`Cors:AllowedOrigins: [ "https://a", "https://b" ]`
  - 兼容格式：分号分隔字符串 `Cors:AllowedOrigins="https://a;https://b"`
- **配置示例**（`appsettings.json`）:
  ```json
  "Cors": {
    "AllowedOrigins": [
      "https://bobcrm.yourcompany.com",
      "https://admin.yourcompany.com"
    ]
  }
  ```
- **环境变量示例**（数组写法）:
  - `Cors__AllowedOrigins__0=https://bobcrm.yourcompany.com`
  - `Cors__AllowedOrigins__1=https://admin.yourcompany.com`

## 数据库配置
确保 `ConnectionStrings:Default` 指向有效的 PostgreSQL 数据库。

## 启动检查
建议通过以下方式确认部署配置已生效：
- 明确设置环境：`ASPNETCORE_ENVIRONMENT=Production`（或 Staging 等非 Development 值）。
- 若缺少 `Jwt:Key` / `Cors:AllowedOrigins`，进程应直接以异常退出，并提示缺失的配置项。
- 在前端域名下访问 API（或通过反向代理），确认跨域请求符合预期（仅允许白名单 Origin）。
