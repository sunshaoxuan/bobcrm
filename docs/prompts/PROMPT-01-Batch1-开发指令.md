# PROMPT-01: Batch 1 开发指令

> **目标**: 完成 v1.0 平台基础能力的测试实现
> **依据**: [TEST-02-Batch1-基础平台验证](file:///c:/workspace/bobcrm/docs/test-cases/TEST-02-Batch1-基础平台验证.md)
> **接收人**: Cursor / Antigravity Developer

## 1. 任务概述
我们需要落地 **Batch 1 (Foundation)** 的自动化测试。这是这一系列测试的第一步，必须极其稳固。

## 2. 基础设施改造 (Infrastructure)
修改 `tests/e2e/conftest.py`，增加 `clean_platform` Fixture：

```python
@pytest.fixture
def clean_platform():
    """
    清理 Batch 1 测试产生的动态实体元数据和物理表。
    不要清理 System Admin 用户。
    """
    # 1. 物理表清理 (Hard Drop)
    # List of tables defined in TEST-02
    target_tables = ["TypeTester", "Constrainer", "EvoEntity", "ParentEnt", "ChildEnt"]
    for tbl in target_tables:
        db_helper.execute_query(f'DROP TABLE IF EXISTS "{tbl}" CASCADE')
        db_helper.execute_query(f'DROP TABLE IF EXISTS {tbl.lower()} CASCADE')
    
    # 2. 元数据清理 (Metadata)
    # 注意顺序：先删 Field，再删 Entity
    cleanup_sql = """
    DELETE FROM "FieldMetadatas" 
    WHERE "EntityDefinitionId" IN (
        SELECT "Id" FROM "EntityDefinitions" 
        WHERE "EntityName" IN ('TypeTester', 'Constrainer', 'EvoEntity', 'ParentEnt', 'ChildEnt')
    );
    DELETE FROM "EntityDefinitions" 
    WHERE "EntityName" IN ('TypeTester', 'Constrainer', 'EvoEntity', 'ParentEnt', 'ChildEnt');
    """
    db_helper.execute_query(cleanup_sql)
    
    yield
    # Post-check (Optional)
```

## 3. 测试脚本实现 (Implementation)
请在 `tests/e2e/cases/05_entity_modeling/` 下创建以下文件（如文件太长可拆分）：

### A. `test_batch1_basics.py`
覆盖 **ENT-001** (Data Types) 和 **ENT-002/003** (Constraints)。
*   **Case 1.1**: 构造包含所有类型的 `TypeTester` 实体 -> Publish -> 查询 `information_schema.columns` 验证物理类型 (如 `character varying(50)` vs `text`).
*   **Case 1.2**: 构造 `Constrainer` -> 尝试 API 提交非法数据 -> Assert 400.

### B. `test_batch1_evolution.py`
覆盖 **PUB-002~007** (Evolution & Cascade)。
*   **Case 1.3**:
    1.  Publish `EvoEntity` (V1). Insert Data.
    2.  Modify Entity (V2: Add Column, Lengthen String). Publish.
    3.  Assert DB Schema updated AND Old Data remains.
*   **Case 1.4**:
    1.  Create `ChildEnt` (Draft).
    2.  Create `ParentEnt` (Refers Child).
    3.  Publish Parent -> Assert Child also Published.

## 4. 执行标准 (SOP)
执行代码前，必须运行：
```powershell
# 1. Kill old processes
taskkill /F /IM "BobCrm.Api.exe" /T
# 2. Start fresh
./scripts/dev.ps1 -Action start -Detached
# 3. Run tests
pytest tests/e2e/cases/05_entity_modeling/test_batch1_*.py
```

## 5. 注意事项
*   使用 `utils.api.api_helper` 进行 HTTP 操作。
*   使用 `utils.db.db_helper` 进行断言。
*   不要修改 `BobCrm.Api` 的 C# 代码，只编写 Python 测试代码。
