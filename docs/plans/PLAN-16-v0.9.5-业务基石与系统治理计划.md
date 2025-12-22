# PLAN-16: v0.9.5 业务基石与系统治理计划

## 1. 目标
在 v0.9.0 解决核心技术闭环后，本阶段目标是将“底座平台”转型为“可用产品”。重点补齐系统治理（日志、设置）与核心业务（客户 360View）的最后拼图。

## 2. 关键任务拆解

### Phase 1: 系统治理与可观测性 (System Governance)
*   **TASK-01: 审计日志 UI (Audit Trail UI)**
    *   实现 `AuditLogManagement.razor`，基于 `AuditLogs` 表展示实体变更历史（谁在什么时候改了什么）。
    *   支持按实体类型、操作人、时间范围筛选。
*   **TASK-02: 异步任务监控 (Job Monitor)**
    *   实现 `BackgroundJobMonitor.razor`，展示实体发布、数据库对齐等长时任务的执行状态与日志。

### Phase 2: 系统设置增强 (Advanced Settings)
*   **TASK-03: 多语资源在线编辑器 (I18n Resource Editor)**
    *   在 `Settings.razor` 中集成 `LocalizationResource` 编辑功能，支持管理员直接在 UI 修改翻译。
*   **TASK-04: 邮件与通知配置 (SMTP & Messaging)**
    *   实现 SMTP 服务器配置界面与连通性测试。
    *   定义“系统通知”下发机制。

### Phase 3: 业务域深化 (Business Core)
*   **TASK-05: 客户 360View 深度聚合 (Customer 360)**
    *   利用 AggVO 模式，将客户基础档案与其关联的“联系人”、“活动记录”、“机会”进行深度聚合。
    *   设计并发布一套“主子孙”结构的客户详情标准模板。
*   **TASK-06: 实体引用 (Lookup) UI 增强**
    *   在 DataGrid 中支持 Lookup 字段的翻译展示（非外键 ID，而是 DisplayName）。
    *   在编辑页提供标准的 `EntitySelector` 模态框交互。

## 3. 验收标准
1.  **可追溯**：任何实体的手动修改都能在“审计日志”中查到原始值与新值。
2.  **可维护**：管理员无需修改代码或 JSON 文件，即可通过 UI 更新界面翻译。
3.  **业务闭环**：能够通过 AggVO 一次性保存含多名联系人的客户档案。
4.  **一致性**：所有新功能模块严格遵循 `UI-01` 玻璃态设计规范。

## 4. 下一里程碑预告 (v1.0.0)
*   **智能分析**：仪表盘设计器与报表预览。
*   **流程自动化**：集成 Elsa Workflow 驱动审批流。
*   **生产加固**：性能压测与安全性审计。
