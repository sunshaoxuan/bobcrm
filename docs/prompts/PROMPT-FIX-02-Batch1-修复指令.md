# PROMPT-FIX-02: Batch 1 缺陷修复指令

> **目标**: 修复 Batch 1 发现的平台级 Blocker
> **依据**: Batch 1 测试结果 (1 Pass / 3 Fail)
> **接收人**: Cursor / Antigravity Developer

## 1. 缺陷清单
目前 Batch 1 测试暴露了三个严重架构缺陷，必须在进入 Batch 2 前修复：

1.  **Validation Failure (Case 1.2)**: API 对空值(`{}`)和超长字符串未拦截，直接返回 201。
2.  **Concurrency Failure (Case 1.3)**: 实体元数据并发更新(Schema Evolution)时持续抛出 409，导致演进中断。
3.  **Cascade Failure (Case 1.4)**: 发布父实体时，引用的 Draft 子实体未自动发布。

## 2. 修复方案 (Technical Strategy)

### Fix 1: 实现服务端校验 (Dynamic Validation)
在 `ReflectionPersistenceService.CreateAsync/UpdateAsync` 中注入校验逻辑：
*   **逻辑**: 在生成 SQL 之前，根据 `EntityDefinition` 的 `Fields` 元数据校验输入字典 `data`。
*   **检查点**:
    *   `IsRequired`: 如果字段必填且 `data` 中缺失或为 null/empty -> Throw `ValidationException`。
    *   `Length`: 如果字段是 String 且 `data[key].Length > MaxLength` -> Throw `ValidationException`。
*   **异常处理**: 捕获 `ValidationException` 并返回 HTTP 400。

### Fix 2: 优化元数据更新并发控制 (Metadata Concurrency)
针对 `UpdateEntityDefinition` (Case 1.3的瓶颈)：
*   **原因分析**: 测试脚本可能在极短时间内连续更新元数据，但客户端未正确获取最新的 `RowVersion`。
*   **策略**:
    *   **Server**: 确保 `EntityDefinitions` 表使用了乐观锁 (RowVersion)。返回 409 是正确的，但要确保 Update 接口返回最新的 DB 状态。
    *   **Test Script**: 修改 `test_batch1_evolution.py`，增加 "Get-Modify-Update" 循环重试机制 (Polly-like retry)，而非盲目重试。确保每次 Update 前都使用最新的 `RowVersion`。

### Fix 3: 实现级联发布 (Cascade Publishing)
在 `EntityPublishService.PublishAsync` 中实现递归逻辑：
*   **逻辑**:
    1.  获取当前实体的所有字段。
    2.  找到 `DataType == "EntityRef" || DataType == "EntityRefCollection"` 的字段。
    3.  解析目标实体 ID (`RefEntityId`)。
    4.  检查目标实体状态。如果 `Status == Draft`：
        *   **递归调用**: `await PublishAsync(targetId)`。
    5.  执行当前实体的发布（建表）。

## 3. 验收标准
1.  重新运行 Batch 1 测试：
    ```powershell
    pytest tests/e2e/cases/05_entity_modeling -k batch1
    ```
2.  结果必须为 **4 passed**。

请执行修复。
