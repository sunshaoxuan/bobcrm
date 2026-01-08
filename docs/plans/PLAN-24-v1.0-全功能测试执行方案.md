# PLAN-24: v1.0 全功能测试执行方案

> **版本**: 1.0 (Draft)
> **依据**: [TEST-01-v1.0-测试矩阵](file:///c:/workspace/bobcrm/docs/test-cases/TEST-01-v1.0-全功能测试矩阵.md)
> **标准**: [STD-06-集成测试规范](file:///c:/workspace/bobcrm/docs/process/STD-06-集成测试规范.md)
> **责任人**: QA 团队 (Agent)
> **生效日期**: 2026-01-08
> **状态**: 此文档符合 STD-02 规范

## 1. 概述
本方案旨在落地执行 `TEST-01` 定义的 30+ 业务场景。我们将采用 **"分批推进、严格清洗、证据留存"** 的策略，确保测试过程可追溯、结果可信。

## 2. 执行原则 (SOP - STD-06 Compliance)
即使是自动化测试，也必须严格遵守 `STD-06` 的五步铁律：

1.  **Clean**: `taskkill` 杀掉所有僵尸进程 (Api/App/Playwright)。
2.  **Verify**: 运行 `verify-setup.ps1` 确保编译无误、I18n 合规。
3.  **Start**: 使用隔离端口 (推荐: 5200/3000) 启动全新环境。
4.  **Test**: 分批运行 Pytest, 并录制 Trace/Video。
5.  **Shutdown**: 强制归零，防止污染下一轮。

## 3. 分批执行计划 (Phased Execution)

我们不一次性吃下所有测试，而是分为三轮次 (Batches)，每轮验证通过后才进行下一轮。

### Batch 1: 平台地基 (Foundation) - [STOP if Fail]
*   **参阅**: [TEST-02-Batch1-基础平台验证](file:///c:/workspace/bobcrm/docs/test-cases/TEST-02-Batch1-基础平台验证.md)
*   **Scope**: Entity & Publishing (ENT-*, PUB-*)
*   **Focus**: 验证 "造物能力"。如果实体造不出来，后面全是空谈。
*   **Case List**:
    *   `test_entity_types.py`: 覆盖所有数据类型定义。
    *   `test_publish_basic.py`: 覆盖建表、加字段、热发布。
    *   `test_publish_evolution.py`: 覆盖 schema 演进与数据保留。

### Batch 2: 应用组装 (Assembly)
*   **参阅**: [TEST-03-Batch2-应用组装验证](file:///c:/workspace/bobcrm/docs/test-cases/TEST-03-Batch2-应用组装验证.md)
*   **Scope**: Templates & Design (TPL-*)
*   **Focus**: 验证 "所见即所得"。
*   **Case List**:
    *   `test_template_codegen.py`: 默认模板生成。
    *   `test_form_designer.py`: 拖拽布局、属性配置、保存回显。
    *   `test_runtime_render.py`: 运行时表单渲染正确性 (Switch/Date/Input)。

### Batch 3: 业务闭环 (Business Loop)
*   **参阅**: [TEST-04-Batch3-业务闭环验证](file:///c:/workspace/bobcrm/docs/test-cases/TEST-04-Batch3-业务闭环验证.md)
*   **Scope**: Security & UX (SEC-*, UX-*)
*   **Focus**: 验证 "最终用户体验"。
*   **Case List**:
    *   `test_security_menus.py`: 菜单定义与侧边栏渲染。
    *   `test_security_access.py`: 路由拦截 (403) 与按钮权限。
    *   `test_ux_workflows.py`: 完整的 "HelpDesk" 业务操作流 (Zero-to-Hero)。

## 4. 交付物 (Deliverables)
1.  **Console Log**: 完整的 Pytest 控制台输出。
2.  **JUnit XML**: 机器可读报告 (`results.xml`)。
3.  **Screenshots**: 关键节点截图。
4.  **Validation Check**: 数据库状态校验记录。
