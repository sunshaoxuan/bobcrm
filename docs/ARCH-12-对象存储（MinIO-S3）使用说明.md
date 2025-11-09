# 对象存储（MinIO/S3）使用说明

本项目在本地采用 MinIO 作为 S3 兼容对象存储，生产可切换至 AWS S3 或任意兼容服务。

## 1. Docker 本地环境

已在 `docker-compose.yml:1` 增加 MinIO 服务与一次性桶初始化：

- 服务：`minio`（容器内 9000/9001；宿主机映射为 9100（S3 API）、9101（Console））
- 初始化：`minio-create-bucket` 使用 `mc` 创建默认桶 `bobcrm` 并设置匿名下载（便于快速预览）
- 缺省凭证：`minioadmin` / `minioadmin`

启动命令：
```bash
docker compose up -d minio minio-create-bucket
```
控制台地址：http://localhost:9101

## 2. 后端配置

在 API 侧引入了 `IFileStorageService` 与 S3 实现（MinIO 兼容）：

- 代码路径：
  - `src/BobCrm.Api/Services/Storage/IFileStorageService.cs`
  - `src/BobCrm.Api/Services/Storage/S3FileStorageService.cs`
  - 注册：`src/BobCrm.Api/Program.cs: 添加 Configure<S3Options> 与 AddSingleton<IFileStorageService>`
- 包：`AWSSDK.S3`（在 `src/BobCrm.Api/BobCrm.Api.csproj` 中声明）
- 关键配置（`appsettings.Development.json` 示例）：
```json
{
  "S3": {
    "ServiceUrl": "http://localhost:9100",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "BucketName": "bobcrm",
    "Region": "us-east-1"
  }
}
```

> MinIO 需要 `ForcePathStyle=true`；已在实现中配置，Region 随意但需提供。

## 3. API 端点

- POST `/api/files/upload`（multipart/form-data，字段名 `file`，可选 `prefix`）
  - 返回：`{ key, url }`
- GET `/api/files/{key}` 下载
- DELETE `/api/files/{key}` 删除（需鉴权）

> 端点位于：`src/BobCrm.Api/Endpoints/FileEndpoints.cs`，已在 `Program.cs` 中注册。

## 4. 前端使用

示例页面：`src/BobCrm.App/Components/Pages/Files.razor:1`，支持选择文件后直传 `/api/files/upload` 并显示返回的下载 URL。

## 5. 生产建议

- 替换为云上 S3 时：
  - 使用官方 Endpoint 与 Region，去掉 `ServiceUrl` 仅保留 Region 即可
  - 收紧桶策略，关闭匿名下载；文件下载可以走签名 URL
- 安全：
  - 最小权限凭证，独立访问密钥
  - 限制上传大小与类型（后端白名单）
  - 审计与对象生命周期策略（过期/归档）

---

附：如需在接口文档中补充对象存储端点，请在 `docs/API-01-接口文档.md` 的“文件存储”章节追加说明（示例请求、响应、错误码）。
