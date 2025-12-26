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

#### 3. JWT Issuer/Audience 一致性验证 (Critical)
- **风险**: 若 API 配置了 `Jwt:Issuer` / `Jwt:Audience`，但生成 Token 的客户端（或 IdentityServer）未设置相同值，会导致 Token 签名有效但校验失败（401 Unauthorized）。
- **验证方法**:
  1. 解码客户端生成的 Token（可使用 jwt.io 或本地工具）。
  2. 检查 `iss` claim 是否等于 API 配置的 `Jwt:Issuer`。
  3. 检查 `aud` claim 是否包含 API 配置的 `Jwt:Audience`。
- **自检命令（Python）**:
  ```bash
  # TOKEN=xxxx.yyyy.zzzz
  python3 - <<'PY'
import os, json, base64
token = os.environ.get("TOKEN", "")
parts = token.split(".")
if len(parts) < 2:
    raise SystemExit("TOKEN is missing or invalid (expected header.payload.signature).")
payload = parts[1] + "==="
payload = payload.replace("-", "+").replace("_", "/")
data = json.loads(base64.b64decode(payload))
print("iss =", data.get("iss"))
print("aud =", data.get("aud"))
PY
  ```
  - 期望：`iss == BobCrm.Api` 且 `aud` 包含 `BobCrm.Client`（数组或字符串）。

## 数据库配置
系统支持两种数据库模式：
- **PostgreSQL（推荐生产使用）**：设置 `Db:Provider=postgres`，并提供 `ConnectionStrings:Default`。
- **SQLite（默认）**：若不显式设置 `Db:Provider`，将使用 SQLite；连接串缺省时默认 `Data Source=./data/app.db`。生产环境使用 SQLite 时请确保挂载持久化卷并保证容器内 `./data` 可写。


## 生产环境变量清单 (必填)

在部署到 Staging/Production 环境时，请务必设置以下环境变量。

| 环境变量名 | 示例值 / 说明 | 必需 |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | 是 |
| `ASPNETCORE_URLS` | `http://+:5200` | 否（建议容器显式设置） |
| `Db__Provider` | `postgres` / `sqlite` | 否（默认 `sqlite`；用 Postgres 时必填） |
| `ConnectionStrings__Default` | `Host=pg-host;Database=bobcrm;Username=u;Password=p` | 用 Postgres 时必填 |
| `Jwt__Key` | `YourStrongSecretKey32CharsMin!!` | 是 |
| `Jwt__Issuer` | `BobCrm.Api` | 是（当前验证开启 `ValidateIssuer`） |
| `Jwt__Audience` | `BobCrm.Client` | 是（当前验证开启 `ValidateAudience`） |
| `Cors__AllowedOrigins__0` | `https://crm.yourdomain.com` | 是 |
| `S3__ServiceUrl` | `http://minio-host:9000` | 否 (如需 S3) |
| `S3__AccessKey` | `minioadmin` | 否 |
| `S3__SecretKey` | `minioadmin` | 否 |

> **注意**: 环境变量层级使用双下划线 `__` 分隔（兼容 Linux/Docker）。

## 部署文件参考

项目根目录包含以下参考文件：

- **`docker-compose.yml`**: 仅包含依赖服务（PostgreSQL + MinIO），不含应用本身。在生产环境部署应用容器时，需自行编写应用的 docker-compose 定义或 K8s yaml。
- **`appsettings.json`**: 基础配置模板。生产环境推荐**避免**直接修改此文件，而是通过环境变量或挂载 `appsettings.Production.json` 覆盖。

### 生产环境 docker-compose 示例 (应用层)

```yaml
version: "3.8"
services:
  bobcrm-api:
    image: your-repo/bobcrm-api:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:5200
      - ConnectionStrings__Default=Host=pg;Database=bobcrm;Username=postgres;Password=postgres
      - Jwt__Key=ThisIsAVeryStrongKeyForProductionUse123!
      - Jwt__Issuer=BobCrm.Api
      - Jwt__Audience=BobCrm.Client
      - Cors__AllowedOrigins__0=https://your-frontend-domain.com
      - Db__Provider=postgres
    ports:
      - "5200:5200"
    depends_on:
      - postgres
```

## 部署前自检 ("Dry Run")

建议在正式部署前，使用以下命令在本地或 CI/CD 流水线中模拟生产环境启动检查。

### 1. 验证“缺失配置导致启动失败”
*目的：确保安全门禁生效（即必须报错退出，而不是以弱安全模式启动）。*

**方式 A：直接运行（推荐 CI 使用）**
```bash
ASPNETCORE_ENVIRONMENT=Production \
  ASPNETCORE_URLS=http://127.0.0.1:5200 \
  dotnet run -c Release --project src/BobCrm.Api/BobCrm.Api.csproj
```
**预期**：进程应立即退出，并提示缺失的 `Jwt:Key` 或 `Cors:AllowedOrigins`。

**方式 B：容器镜像（如你已自行构建并发布镜像）**
```bash
# 故意不传 Jwt__Key 和 Cors，预期应立即 Crash
docker run --rm -e ASPNETCORE_ENVIRONMENT=Production your-repo/bobcrm-api:latest
```
**预期输出包含:**
> `Unhandled exception. System.InvalidOperationException: Jwt:Key is required in non-development environments.`
> (或者 `Cors:AllowedOrigins must be configured`)

### 2. 验证“正确配置能正常启动”
**方式 A：直接运行（推荐 CI 使用）**
```bash
ASPNETCORE_ENVIRONMENT=Production \
  ASPNETCORE_URLS=http://127.0.0.1:5200 \
  ConnectionStrings__Default="Data Source=/tmp/bobcrm.db" \
  Jwt__Key=TestKeyForVerificationOnly1234567890 \
  Jwt__Issuer=BobCrm.Api \
  Jwt__Audience=BobCrm.Client \
  Cors__AllowedOrigins__0=http://localhost \
  dotnet run -c Release --project src/BobCrm.Api/BobCrm.Api.csproj
```

**方式 B：容器镜像（如你已自行构建并发布镜像）**
```bash
docker run --rm -d --name bobcrm-check \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://+:5200 \
  -e Jwt__Key=TestKeyForVerificationOnly1234567890 \
  -e Jwt__Issuer=BobCrm.Api \
  -e Jwt__Audience=BobCrm.Client \
  -e Cors__AllowedOrigins__0=http://localhost \
  -p 5200:5200 \
  your-repo/bobcrm-api:latest

# 等待几秒后检查日志
docker logs bobcrm-check
```
**预期输出**：无 `Jwt:Key is required...` / `Cors:AllowedOrigins must be configured...` 异常，并能看到应用启动日志。

## 常用运维命令
- **查看健康状态**: `curl http://localhost:5200/health` (返回 Healthy)
- **查看详细存活状态**: `curl http://localhost:5200/health/live`
