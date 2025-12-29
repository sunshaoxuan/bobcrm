# v0.9.5 开发计划 (业务基石与系统治理)

## Phase 1: 系统治理与可观测性 (System Governance)
- [x] TASK-01: 审计日志 UI (Audit Trail UI) - `AuditLogManagement.razor`.
- [x] TASK-02: 异步任务监控 (Job Monitor) - `BackgroundJobMonitor.razor`.

## Phase 2: 系统设置增强 (Advanced Settings)
- [x] TASK-03: 多语资源在线编辑器 (I18n Resource Editor) - 集成至 `Settings.razor`.
- [x] TASK-04: 邮件与通知配置 (SMTP & Messaging).

## Phase 3: 业务域深化 (Business Core)
- [x] TASK-05: 客户 360 视图 (Customer 360 View) - 聚合视图模板.
- [x] TASK-06: 查找体验增强 (Lookup UI Enhancement) - DataGrid/Dialog.

---

## Code Quality (PLAN-18) - P2
- [x] P2-001: TimeProvider 引入（commit: `90bd1b7`）
- [x] P2-002: Program.cs 启动配置拆分与校验（commit: `90bd1b7`）
- [x] P2-003: Swagger 文档中文化 + I18n 完整性检查（commit: `bd7d84a`）

---

## Code Quality (PLAN-18) - P3
- [x] P3-001: 统一 JS Interop 错误处理（commit: `02f0479`）
- [x] P3-002: 后端错误码标准化（commit: `41a1128`）
- [x] P3-003: 编译告警基线门禁（commit: `94d4595`）

---

## Test Coverage (PLAN-28)
- [ ] PLAN-28: 覆盖率深度测试（目标：BobCrm.Api 行覆盖率 ≥ 90%）
  - [x] Phase 1: 动态实体运行时（补齐 CRUD 端点深度用例）
  - [x] Phase 2: 模板与 UI 系统（补齐模板 CRUD/复制/应用用例）
  - [x] Phase 3: 系统与认证（补齐登录/刷新令牌/SystemEndpoints 边界用例）
  - [ ] 生成覆盖率报告并补齐剩余盲区（直至 ≥ 90%）

---

## 历史归档 (v0.9.x Completed)
> 详情见 `history/task-v0.9.x-archive.md`
