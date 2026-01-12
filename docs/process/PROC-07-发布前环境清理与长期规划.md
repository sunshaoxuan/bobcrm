# PROC-07: 发布前环境清理与长期规划 (Post-Batch 4)

## 1. 立即执行的清理作业 (Immediate Cleanup)

针对演期 E2E 和 性能测试产生的“残留数据”，执行以下清理以保证生产/演示环境的纯净。

### 1.1 自动化全局清理 (Automated Global Teardown)
*   **清理逻辑**: 核心回归测试套件必须在 `tests/e2e/conftest.py` 中集成全局清理逻辑。
*   **技术要求**:
    *   通过 `tests/e2e/utils/db.py` 动态扫描数据库。
    *   强制删除所有非核心系统表（排除基础权限与设置表）。
    *   清理 `EntityDefinitions` 与 `FormTemplates` 中的测试元数据。
*   **详见指令**: [FIX-06-Batch6-可靠性审计与全量回归指令.md](file:///c:/workspace/bobcrm/docs/history/FIX-06-Batch6-可靠性审计与全量回归指令.md)

### 1.2 工程目录清理
*   删除根目录下的所有临时日志文件 (`*.log`)。
*   确认 `GUIDE-99-最终用户操作手册.md` 已移至 `docs/guides/` 下。

---

## 2. 长期规划 (Future Roadmap - v1.1+)

### 阶段 1: 企业级特性增强 (Enterprise Readiness)
*   **数据隔离**: 实现真正的多租户 (Multi-tenancy) 逻辑隔离，基于 `OrganizationId` 进行全局查询拦截。
*   **高级设计器**: 增加“工作流 (Workflow) 设计器”，支持简单的审批链配置。
*   **深度集成**: 支持 Webhook 推送，允许动态实体变更时通知第三方系统。

### 阶段 2: 性能与可扩展性 (Scalability)
*   **分布式缓存**: 将现有的内存 I18n 和元数据缓存迁移至 Redis。
*   **水平扩展**: 优化 `AssemblyLoadContext` 的分布式同步，确保多实例部署时代码加载的一致性。

---

## 3. 质量审查结论 (Quality Review Conclusion)
目前系统正处于 **v1.0 严苛审计阶段 (Hardening Phase)**。在完成 `FIX-06` 定义的全量回归测试并验证“全球清理逻辑”有效性之前，**不授予 RC1 状态**。

> [!WARNING]
> 严禁在全量回归测试完成前进行生产环境部署。
