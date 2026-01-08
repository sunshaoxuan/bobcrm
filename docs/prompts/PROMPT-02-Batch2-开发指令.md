# PROMPT-02: Batch 2 开发指令

> **目标**: 完成 v1.0 应用组装层 (Assembly) 的测试实现
> **依据**: [TEST-03-Batch2-应用组装验证](file:///c:/workspace/bobcrm/docs/test-cases/TEST-03-Batch2-应用组装验证.md)
> **接收人**: Cursor / Antigravity Developer

## 1. 任务概述
Batch 1 奠定了数据模型基础，Batch 2 将验证 **"模板与设计器"**。这是低代码平台的 "脸面"。

## 2. 基础设施补强
修改 `tests/e2e/conftest.py`，增加 `standard_product` Fixture：
```python
@pytest.fixture
def standard_product(auth_admin):
    """
    预置一个标准 Product 实体，包含 String, Decimal, Bool 等典型字段。
    确保它已发布。
    """
    # 1. Cleaner: Ensure clean state
    # 2. Definer: Create Entity 'Product' (Name, Price, IsActive)
    # 3. Publisher: Publish 'Product'
    # 4. Return: EntityDefinitionId
```

## 3. 测试脚本实现
请在 `tests/e2e/cases/06_form_design/` 下创建以下文件：

### A. `test_batch2_templates.py` (TPL-001)
*   **Case 2.1**: 验证默认模板生成。
    *   Assert: API `/api/templates` 返回 List/Detail 两个默认模板。
    *   Assert: 模板内容 JSON 包含所有字段 (Name, Price, IsActive)。

### B. `test_batch2_designer.py` (TPL-002~004)
*   **Case 2.2**: 模拟设计器操作 (Playwright Drag & Drop 较难，可模拟 API Payload 提交)。
    *   **Action**: 加载默认 Detail 模板 -> 修改 JSON 结构 (把 Price 放入 TabContainer) -> 保存 (Update Template)。
    *   Assert: 再次 GET Template，确认结构已更新。

### C. `test_batch2_runtime.py` (TPL-005~006)
*   **Case 2.3**: 运行时渲染保真度。
    *   **Action**: 访问 `/app/product/new`。
    *   Assert: Selector `.ant-tabs` 存在 (因为 Case 2.2 加了 Tab)。
    *   Assert: `Price` 字段渲染为数字输入框 (`.ant-input-number`)。
*   **Case 2.4**: 校验反馈。
    *   **Action**: 空表单点击 Save。
    *   Assert: 出现校验错误提示 ("Name is required")。

## 4. 执行标准
```powershell
./scripts/dev.ps1 -Action start -Detached
pytest tests/e2e/cases/06_form_design/test_batch2_*.py
```

## 5. 注意事项
*   设计器测试如果 Drag-Drop 不稳定，优先保证 **API Payload 结构修改** 的逻辑正确性 (Case 2.2)。
*   运行时测试 (Case 2.3) 必须依赖 Case 2.2 的输出。建议合并在一个 Test File 中或使用 Shared Fixture。
