# PLAN-23: v1.0 发布准备计划 (Release Preparation)

> **Status**: Draft
> **Owner**: User
> **Target Version**: v1.0.0

## 1. 目标 (Objectives)
本阶段旨在将 v0.9.x 的开发成果转化为可交付的 v1.0 产品。不再增加新的业务功能，而是聚焦于文档完整性、界面一致性和系统的整体交付质量。

## 2. 核心任务 (Key Tasks)

### 2.1 文档体系完善 (Documentation)
*   **[NEW] 最终用户操作手册 (`docs/guides/GUIDE-99-用户操作手册.md`)**
    *   面向最终业务用户，涵盖：登录、菜单导航、数据浏览、表单编辑、流程操作。
    *   不再是技术视角的 "Feature Guide"，而是业务视角的 "User Manual"。
*   **[FIX] 开发者文档同步**
    *   运行 `scripts/doc-check.ps1`，确保 `docs/contracts` 与代码完全一致。
    *   导出最新的 Swagger/OpenAPI 定义文件至 `docs/reference/openapi.json`。

### 2.2 UI/UX 视觉致敬 (Visual Polish)
*   **全站样式审计**
    *   检查所有 `Button`, `Card`, `Table` 是否统一使用了 Ant Design 组件。
    *   **去除硬编码颜色**: 扫描 `.css` 和 `.razor`，确保使用 CSS Variables (`var(--ant-primary-color)`) 而非 `#1890ff`。
*   **PageLoader 视觉回归**
    *   虽然重构了 PageLoader，需人工确认其 padding/margin 与系统其他页面（如 List 页）是否在视觉上对齐。

### 2.3 全面测试验证 (Comprehensive Verification)
*   **端到端回归测试 (E2E Regression)**:
    *   执行 `tests/e2e` 下的所有 Python 脚本，覆盖 Auth, Org, User, Form, Entity 等核心模块。
    *   确保关键业务链路 (Login -> Form Design -> Entity Data -> Render) 无中断。
*   **负载/压力测试 (Load Testing)**:
    *   **[NEW]** 创建 `tests/load/locustfile.py`。
    *   场景：针对 `GET /api/{entity}/{id}` (PageLoader 热点接口) 进行 50 并发持续压测。
    *   目标：P95 < 200ms, Error Rate < 1%。
*   **集成测试审查**:
    *   确认 `BobCrm.Api.Tests` 是否覆盖了所有 Critical API 的契约。

### 2.4 发布打包 (Release Packaging)
*   **版本号提升**: `v0.9.5` -> `v1.0.0`.
*   **更新日志**: 整理 `CHANGELOG.md`，汇总 v0.9.x 系列的所有重大变更。
*   **冒烟测试**: 执行一次完整的部署与启动流程验证。

## 3. 实施步骤 (Work Breakdown)

### Phase 1: 文档编写
- [ ] 编写 `GUIDE-99-用户操作手册.md`
- [ ] 运行文档一致性检查并修复差异

### Phase 2: UI 优化
- [ ] 执行全站样式扫描 (Grep check)
- [ ] 修复发现的样式不一致问题

### Phase 3: 发布
- [ ] 更新 `Directory.Build.props` / `package.json` 版本号
- [ ] 更新 `CHANGELOG.md`
- [ ] 标记 Git Tag `v1.0.0`

## 4. 验收标准 (Acceptance Criteria)
1.  用户手册覆盖核心业务路径。
2.  样式检查脚本未发现 `#hex` 颜色硬编码（特定配置除外）。
3.  项目版本号更新为 1.0.0。
