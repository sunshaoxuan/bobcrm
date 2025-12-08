# TEST-05: 登录认证集成测试

> **版本**: 1.0
> **生效日期**: 2025-12-08
> **状态**: 执行中
> **适用对象**: AI 自动化测试驱动

---

## 1. 概述

本文档定义了 BobCRM 登录认证功能的集成测试流程。测试覆盖从前端代理到后端 API 的完整认证路径，用于验证用户登录功能的正确性。

**测试目标**:
- 验证后端 API 登录端点 (`/api/auth/login`) 正常工作
- 验证前端代理正确转发认证请求
- 验证默认管理员账户种子数据正确生成
- 验证错误凭据返回正确的错误响应

---

## 2. 前置条件

执行本测试前，**必须**完成以下步骤（参照 `STD-06-集成测试规范.md`）：

### 2.1 环境清理
```powershell
# 杀掉所有相关进程
taskkill /F /IM "BobCrm.Api.exe" /T
taskkill /F /IM "BobCrm.App.exe" /T
taskkill /F /IM "dotnet.exe" /T
```

### 2.2 静态验证
```powershell
# 确保编译和单元测试通过
./scripts/verify-setup.ps1
# 预期结果: Exit code: 0, "✓ 所有检查通过"
```

### 2.3 启动测试环境
```powershell
./scripts/dev.ps1 -Action start -Detached
Start-Sleep -Seconds 15  # 等待服务完全启动
```

---

## 3. 测试用例

### TC-01: 后端 API 直接登录 - 正确凭据

**目的**: 验证后端 API 能够正确处理有效的登录请求。

**步骤**:
```powershell
$body = @{ username = "admin"; password = "Admin@12345" } | ConvertTo-Json
$response = Invoke-RestMethod -Uri "http://localhost:5200/api/auth/login" -Method POST -Body $body -ContentType "application/json"
```

**预期结果**:
- HTTP 状态码: `200 OK`
- 响应体包含 `token` 字段（JWT 令牌）
- 响应体包含 `user` 对象，其中 `userName` 为 `"admin"`

**验证脚本**:
```powershell
if ($response.token -and $response.user.userName -eq "admin") {
    Write-Host "[PASS] TC-01: 后端登录成功"
} else {
    Write-Host "[FAIL] TC-01: 后端登录失败"
    exit 1
}
```

---

### TC-02: 后端 API 直接登录 - 错误密码

**目的**: 验证后端 API 能够正确拒绝无效凭据。

**步骤**:
```powershell
$body = @{ username = "admin"; password = "wrongpassword" } | ConvertTo-Json
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5200/api/auth/login" -Method POST -Body $body -ContentType "application/json"
    $tcResult = "FAIL"  # 不应该成功
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    $tcResult = if ($statusCode -eq 401) { "PASS" } else { "FAIL" }
}
```

**预期结果**:
- HTTP 状态码: `401 Unauthorized`

**验证脚本**:
```powershell
Write-Host "[${tcResult}] TC-02: 错误密码拒绝测试"
```

---

### TC-03: 前端代理登录 - 正确凭据

**目的**: 验证前端代理能够正确转发登录请求到后端 API。

**步骤**:
```powershell
$body = @{ username = "admin"; password = "Admin@12345" } | ConvertTo-Json
$response = Invoke-RestMethod -Uri "http://localhost:3000/api/auth/login" -Method POST -Body $body -ContentType "application/json"
```

**预期结果**:
- HTTP 状态码: `200 OK`
- 响应体包含 `token` 字段
- 响应体包含 `user` 对象

**验证脚本**:
```powershell
if ($response.token -and $response.user) {
    Write-Host "[PASS] TC-03: 前端代理登录成功"
} else {
    Write-Host "[FAIL] TC-03: 前端代理登录失败"
    exit 1
}
```

---

### TC-04: 前端代理登录 - 错误用户名

**目的**: 验证前端代理能够正确转发并处理无效用户的登录请求。

**步骤**:
```powershell
$body = @{ username = "nonexistent"; password = "anypassword" } | ConvertTo-Json
try {
    $response = Invoke-RestMethod -Uri "http://localhost:3000/api/auth/login" -Method POST -Body $body -ContentType "application/json"
    $tcResult = "FAIL"
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    $tcResult = if ($statusCode -eq 401) { "PASS" } else { "FAIL" }
}
```

**预期结果**:
- HTTP 状态码: `401 Unauthorized`

---

### TC-05: 浏览器 UI 登录流程

**目的**: 验证用户能够通过浏览器界面完成登录。

**步骤**:
1. 打开浏览器，导航到 `http://localhost:3000/login`
2. 在用户名输入框中输入 `admin`
3. 在密码输入框中输入 `Admin@12345`
4. 点击登录按钮
5. 等待页面跳转

**预期结果**:
- 页面成功跳转到仪表板 (`/dashboard` 或 `/`)
- 无错误提示显示
- 用户名显示在页面右上角

---

## 4. 测试后清理

测试完成后，**必须**执行以下清理步骤：

```powershell
./scripts/dev.ps1 -Action stop
```

---

## 5. 快速自动化脚本

可直接运行以下脚本执行全部测试：

```powershell
./scripts/verify-auth.ps1
```

**预期输出**:
```
=== BobCRM Integration Verification ===
[PASS] Port 5200 (API) is listening
[PASS] Port 3000 (App) is listening
[PASS] Backend Auth: Login successful
[PASS] Frontend Proxy Auth: Login successful
=== All Tests Passed ===
```

---

## 6. 故障排除

| 症状 | 可能原因 | 解决方案 |
|------|---------|---------|
| TC-01 失败，连接超时 | 后端未启动 | 检查 `scripts/dev.ps1 -Action start` 执行情况 |
| TC-01 失败，401 错误 | 数据库种子数据缺失 | 重启后端触发 `DatabaseInitializer` |
| TC-03 失败，连接超时 | 前端代理配置错误 | 检查 `BobCrm.App/Program.cs` 代理中间件 |
| TC-05 失败，页面不跳转 | JavaScript 错误 | 检查浏览器控制台 (F12) |
