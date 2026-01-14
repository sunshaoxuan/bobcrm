# TEST-02: Batch 1 基础平台验证

> **关联计划**: [TEST-01](file:///c:/workspace/bobcrm/docs/test-cases/TEST-01-v1.0-全功能综合测试矩阵.md)
> **测试范围**: ENT-001~008, PUB-001~007
> **优先级**: **Blocker**
> **执行人**: QA 团队

## 1. 环境准备 (Environment Setup)
*   **Fixture**: `clean_db` (空数据库)
*   **User**: `Admin` (超级管理员)

## 2. 测试用例 (Test Cases)

### Case 1.1: 数据类型保真度 (Data Type Fidelity)
**目标**: 验证所有支持的数据类型能否正确映射到物理数据库和 C# 类型。
*   **输入**: 定义实体 `TypeTester`，包含：
    *   `F_String`: String (Length 50)
    *   `F_Int`: Int32
    *   `F_Dec`: Decimal (18,2)
    *   `F_Bool`: Boolean
    *   `F_Date`: Date
    *   `F_DateTime`: DateTime
*   **操作**: 执行发布 (Publish)。
*   **验证 (DB)**:
    *   `F_String` -> `varchar(50)` / `text`
    *   `F_Bool` -> `boolean`
*   **验证 (Runtime)**: 插入行 `{'F_String': 'Test', 'F_Int': 123}` -> 查询返回精确类型。

### Case 1.2: 约束与校验 (Constraints & Validation)
**目标**: 验证硬约束能否拒绝非法数据。
*   **输入**: 实体 `Constrainer` 包含 `ReqField` (Required)。
*   **Action 1**: 插入 `{}` (空对象)。
    *   **预期**: 400 BadRequest / Validation Error "ReqField is required"。
*   **Action 2**: 插入 `{'ReqField': 'A' * 5000}` (超长)。
    *   **预期**: 400 BadRequest / "Exceeds max length"。

### Case 1.3: Schema 演进 (Schema Evolution)
**目标**: 验证非破坏性更新。
*   **前置条件**: `EvoEntity` 存在 `ColA` (String 50)。已存在 1 条记录: `{'ColA': 'OldVal'}`。
*   **操作**:
    1.  修改 `ColA` 长度为 100。
    2.  新增 `ColB` (String, Default 'New')。
    3.  发布。
*   **验证 (DB)**: 列 `ColA` 类型变为 100。`ColB` 已创建。
*   **验证 (Data)**: 旧记录存在。`ColA`='OldVal', `ColB`='New'。

### Case 1.4: 级联发布 (Cascade Publishing)
**目标**: 验证依赖链解析。
*   **输入**:
    1.  创建 `ChildEnt` (Draft)。
    2.  创建 `ParentEnt` 包含字段 `ChildRef` (IsEntityRef=true -> ChildEnt)。
*   **操作**: 发布 `ParentEnt`。
*   **预期**: 系统自动检测 `ChildEnt` 依赖。两者均流转至 `Published` 状态。两张表均被创建。

## 3. 准出标准 (Exit Criteria)
*   所有 Case 通过 (PASS)。
*   无 "Internal Server Error" (500)。
*   Trace 日志显示 SQL 语句执行正确。
