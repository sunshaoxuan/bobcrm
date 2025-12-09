# TEST-06: 模板链路集成测试

> **版本**: 1.0  
> **生效日期**: 2025-12-09  
> **状态**: 进行中  
> **适用对象**: AI 自助化测试 / 开发自测

---

## 1. 概述

覆盖“列表模板 → 详情模板 → 编辑保存”链路的端到端集成测试，验证 PageLoader/RuntimeWidgetRenderer、ListTemplateHost 以及设计器保存后的运行态可用性。

**验证目标**：
- 登录态可访问模板入口页并加载列表模板宿主。
- 列表点击行后，详情页通过 PageLoader 渲染 runtime-layout，至少包含 1 个 Widget。
- 进入编辑模式后可修改字段并保存，退出编辑模式无错误告警。
- 测试步骤具备可重复性，可通过 Playwright 自动化脚本回放。

---

## 2. 前置条件（严格遵守 `STD-06`）

1) **清理环境**  
```powershell
taskkill /F /IM "BobCrm.Api.exe" /T
taskkill /F /IM "BobCrm.App.exe" /T
taskkill /F /IM "dotnet.exe" /T
```

2) **静态检查**  
```powershell
./scripts/verify-setup.ps1
# 预期：Exit code 0，输出 “All checks passed”
```

3) **启动测试环境**  
```powershell
./scripts/dev.ps1 -Action start -Detached
Start-Sleep -Seconds 15
./scripts/verify-auth.ps1   # 预期全部 PASS
```

> 说明：默认基准地址 `http://localhost:3000`，如需覆盖远端环境请设置 `BASE_URL`。

---

## 3. 用例列表

### TC-01：登录成功进入仪表盘
- **步骤**：浏览器访问 `/login`，输入 `admin` / `Admin@12345`，点击提交。  
- **预期**：跳转 `/dashboard`，无错误提示。

### TC-02：列表模板加载成功
- **步骤**：访问 `/customer/list`。  
- **预期**：出现 `.list-template-host` 或列表宿主区域，表格渲染 ≥1 行数据。

### TC-03：点击行进入详情模板
- **步骤**：在表格点击首行。  
- **预期**：页面切换到详情视图，`.runtime-layout` 渲染完成且包含至少 1 个 Widget，标题文本非空。

### TC-04：进入编辑模式并保存
- **步骤**：点击“编辑”按钮进入编辑模式；修改首个可编辑输入框；点击“保存”。  
- **预期**：编辑模式标记消失（回到 `.runtime-layout` 普通态），无错误弹窗，修改值成功提交（若接口可返回状态码则应为 2xx）。

> 覆盖范围：ListTemplateHost → PageLoader → RuntimeWidgetRenderer → 编辑/保存回放。

---

## 4. 自动化执行方式

- **脚本**：`node scripts/test-template-e2e.js`  
- **可选环境变量**：  
  - `BASE_URL`：默认 `http://localhost:3000`  
  - `HEADLESS`：设为 `false` 可观察 UI  
  - `SCREENSHOT_DIR`：失败截图输出目录，默认 `artifacts`
  - `RECORD_VIDEO`：设为 `true` 将录制 Playwright 视频到 `SCREENSHOT_DIR`（默认 false）

**命令示例**：
```powershell
$env:BASE_URL="http://localhost:3000"
$env:HEADLESS="true"
$env:RECORD_VIDEO="true"   # 如需录屏
node scripts/test-template-e2e.js
```

---

## 5. 结束与清理

测试完成后务必关闭服务并释放端口：
```powershell
./scripts/dev.ps1 -Action stop
```

---

## 6. 故障排查指引

- 若 `verify-setup.ps1` 失败，先修复编译/单测/静态检查再执行集成测试。  
- 若列表无数据：检查种子数据或重置数据库 `./scripts/reset-database.ps1`。  
- 若编辑保存无响应：抓取网络面板确认保存接口返回码和报错信息。  
- Playwright 运行失败时，查看 `artifacts/template-test-error.png` 截图定位 UI 问题。
- 若需证据：检查 `artifacts/step*-*.png` 关键节点截图；如启用录屏，视频保存在 `SCREENSHOT_DIR` 下。
