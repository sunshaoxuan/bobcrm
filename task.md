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
  - [x] 生成覆盖率报告并补齐剩余盲区 (PageLoader 重构完成，覆盖率提升)

## Phase 5: v1.0 发布准备 (v1.0 Preparation)
- [x] **Plan**: 确认 `PLAN-23-v1.0-发布准备计划.md`
- [x] **Test-Exec-B1**: 执行 Batch 1 - **Passed**
    - [x] **Fix-B1**: 修复平台核心缺陷 (Validation/Cascade/Concurrency) - **Done**
- [x] **Test-Exec-B2**: 执行 Batch 2 (Visual Testing Focus) - **Passed**
    - [x] **Fix-B2**: 解决 Tabbox 渲染黑盒问题 (ARCH-33/FIX-02) - **Done**
    - [x] **Fix-B2**: 解决 E2E 环境下组件 "不可见" (Visibility) 故障 (ARCH-34/FIX-03) - **Done**
    - [x] **Fix-B2**: 解决文件锁问题 (File Locking / MSB3021) - **Done**
    - [x] **Verify**: 所有测试必须有视频/截图存证 (Evidence)
- [x] **Test-Exec-B3**: 执行 Batch 3 (Business Logic) - **Passed**
    - [x] **Fix-B3**: 修复高级校验逻辑 (Regex/Range) - **Done**
    - [x] **Verify**: 级联发布与实体转换验证 - **Done**


- [x] **Perf**: 执行 PageLoader 压力测试 (Locust) - **Passed**
- [x] **Docs**: 编写最终用户操作手册 (GUIDE-99) - **Done**
- [/] **UI**: 全站样式一致性审计与修复 (Polish) - **In Progress**
- [ ] **Release**: 版本号提升与 CHANGELOG 更新


## Phase 6: v1.0 可靠性审计与全量回归 (Reliability & Regression)
- [x] **Test-Exec-Full**: 执行全量 9 分类 E2E 回归测试 (FIX-06) - **Passed**
- [x] **Fix-Teardown**: 完善 E2E 全量数据清理逻辑 (Global Teardown) - **Done**
- [x] **Verify-Clean**: 确保在“干净环境”下 100% 通过 - **Passed** (30/30 Tests)
- [!] **Audit-Gap**: 识别平台核心链路盖死角 (STD-08) - **Identified**

## Phase 7: v1.0 最终视觉与部署审查 (Final Polish & Launch)
- [ ] **UI-Polish**: 全站组件间距、边框与主题色一致性审计 (Batch 7)
- [ ] **Release**: 提升正式版本号并锁定二进制基准
- [ ] **Launch**: 部署验证与冒烟测试

## Phase 8: v1.0 平台功能闭环验收 (Functional Closure)
- [x] **Gap-Analysis**: 深度审计核心链路偏差 (AUDIT-GAP-01) - **Done**
- [x] **Instruction-Set**: 生成高强度闭环纠偏指令 (FIX-09) - **Done**
- [x] **Functional-Alignment**: 实现权限/菜单/多态渲染全链路对齐 (FIX-09) - **Passed**

## Phase 9: v1.0 最终质量验收与审计 (Final Acceptance)
- [ ] **Acceptance-Verify**: 按照 STD-08 标准执行最后验收
- [ ] **Final-Audit**: 签发最终 v1.0 平台全功能通过审计报告

---

## 历史归档 (v0.9.x Completed)
> 详情见 `history/task-v0.9.x-archive.md`
