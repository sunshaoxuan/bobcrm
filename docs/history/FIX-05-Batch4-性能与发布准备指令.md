# FIX-05: Batch 4 - 性能压测与发布准备指令

## 任务背景
我们已经完成了 Batch 1/2/3 的全量测试与修复。系统在基础 CRUD、UI 渲染透明度、高级业务逻辑（级联/转换/校验）方面已达到 v1.0 发布标准。
最后一个阶段重点关注**系统性能基准**、**用户文档完备性**以及**最终的 UI 磨光**。

## 目标任务 (Goals)

### 1. 性能压力测试 (Performance Benchmark)
*   **工具**: 使用 Locust。
*   **场景**: 
    *   `PageLoader` 高频加载逻辑：模拟 20 个并发用户快速切换不同实体的详情页。
    *   验证 `PageLoaderViewModel` 重构后的内存占用与响应响应分布（P95 < 200ms）。
*   **交付物**: `PERF-REPORT-v1.0.md`，记录 RPS (Requests Per Second) 和错误率。

### 2. 用户操作手册 (User Documentation)
*   **文件**: [GUIDE-99-最终用户操作手册](file:///c:/workspace/bobcrm/docs/guides/GUIDE-99-最终用户操作手册.md) [NEW]
*   **内容**: 
    *   如何通过 Form Designer 调整布局。
    *   如何配置级联发布。
    *   如何为字段添加 Regex/Range 校验规则（基于 Batch 3 的实现）。

### 3. UI 一致性审计 (UI/UX Polish)
*   **重点**:
    *   检查 Ant Design 组件在不同页面下的样式一致性。
    *   确保所有 Toast 通知（Success/Error）的文案均已 i18n 化。
    *   修复 E2E 测试中发现的微小布局偏差。

### 4. 环境与工程化修复 (Chore)
*   **MSB3021**: 彻底解决编译时 `BobCrm.Api.dll` 被占用导致的 File Locking 问题（建议检查 `PostgreSQLDDLGenerator` 或 `DynamicEntityService` 是否存在未释放的连接或句柄）。

## 验收标准 (Definition of Done)
1. Locust 压测通过，无 SQL 连接泄露。
2. `GUIDE-99` 完成并经过 Markdown 语法查。
3. `dotnet build` 连续执行 5 次无 MSB3021 错误。
4. 所有 E2E 测试 (Batch 1/2/3) 仍然保持 Green 状态。

---
请在开始前确认环境已安装 `locust`。如果需要协助编写 Locust 脚本，请随时告知。
