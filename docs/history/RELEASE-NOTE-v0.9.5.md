# Release Note - v0.9.5

发布日期：2025-12-25

## 概览
v0.9.5 聚焦于“系统治理 + 高级设置 + 业务基石”的闭环交付，为 v1.0.0 的契约治理与稳定性打底。

## 关键功能

### System Governance
- 审计日志管理：`/system/audit-logs`（权限：`SYS.AUDIT`），支持查询/筛选与变更详情查看。
- 后台任务监控：`/system/jobs`（权限：`SYS.JOBS`），支持状态列表、日志查看与自动刷新（按实现）。

### Advanced Settings
- I18n 资源编辑器：管理员可在 UI 中搜索/编辑翻译键并触发缓存刷新，无需直接操作数据库。
- SMTP & 通知设置：支持保存配置与发送测试邮件；密码字段加密存储且不会以明文回传到前端。

### Business Core
- Customer 360：在通用表单引擎中支持主从聚合视图（Related List/Tab + DataGrid）。
- Lookup UI 优化：外键字段显示友好名称（Name/Title 等），通过批量解析避免 N+1。

## 验证建议
- 运行：`dotnet build BobCrm.sln -c Release`
- 测试：`dotnet test BobCrm.sln -c Release`

