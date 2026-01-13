# FIX-06: Batch 6 - 可靠性审计与全量回归指令

**生效日期**: 2026-01-13
**状态**: 执行中

---
## 任务背景
在 v1.0 发布前，系统必须证明其在“干净环境”下的绝对稳定性。目前 E2E 测试存在数据清理不彻底的问题（Teardown 漏洞），导致测试间干扰产生“脏数据”或 404 错误。
作为架构师，我要求在通过 RC1 之前完成一次高强度的可靠性审计。

## 目标任务 (Goals)

### 1. 完善全局数据清理机制 (Global Teardown Hardening)
*   **文件**: [conftest.py](file:///c:/workspace/bobcrm/tests/e2e/conftest.py)
*   **任务**:
    *   在 `tests/e2e/utils/db.py` 中新增 `drop_all_dynamic_content()` 函数。
    *   该函数应扫描数据库中所有非核心系统表（排除 `Users`, `Roles`, `Functions`, `Settings` 等），利用 SQL 动态递归删除所有 `Test_` 或 `Perf_` 开头的物理表，并清空对应的 `EntityDefinitions` 和 `FormTemplates` 元数据。
    *   在 `conftest.py` 的 session 级 fixture 中集成该逻辑，确保每次完整测试开始前环境为“白纸”状态。

### 2. 执行全量回归矩阵 (Full Regression Matrix)
*   **任务**: 依次执行以下 9 个分类的所有测试脚本，不得跳过：
    1.  `01_authentication/` (认证与设置)
    2.  `02_user_management/` (用户管理)
    3.  `03_organization/` (组织树与 PathCode)
    4.  `04_system_config/` (I18n 与 SMTP)
    5.  `05_entity_modeling/` (模型演化与发布)
    6.  `06_form_design/` (Batch 2/3 设计器与运行态)
    7.  `07_dynamic_data/` (高频 CRUD 与 Lookup)
    8.  `08_crm_features/` (线索转换与级联逻辑)
    9.  `09_dashboard/` (数据聚合预览)

### 3. 环境韧性测试 (Environment Resilience)
*   **任务**: 在服务重启的情况下，验证 E2E 自动初始化逻辑 (`ensure_admin_exists`) 是否能平滑完成数据库建表与种子数据填充，不产生重复键错误。

## 验收标准 (Definition of Done)
1. **100% 通过率**: 在执行 `global_cleanup` 后，上述所有 9 个分类的测试必须在同一轮次中全部 Passed。
2. **零残留**: 测试结束后，数据库中不应存在任何测试生成的物理表。
3. **质量报告**: 记录所有测试的耗时分布，并对任何 Flaky Test（偶发性失败）进行根因说明。

---
请在开始前确保 `BASE_URL` 和 `API_BASE` 指向干净的测试环境。
