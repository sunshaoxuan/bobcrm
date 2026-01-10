# FIX-04: Batch 3 业务逻辑用例开发指令

**版本**: 1.0
**引用**: [TEST-BATCH-03-BUSINESS.md](file:///C:/Users/X02851/.gemini/antigravity/brain/58e464e1-4f05-4336-b41c-add9cb95040a/TEST-BATCH-03-BUSINESS.md)
**优先级**: 高

## 1. 任务目标
Batch 2 视觉渲染问题已解决。现在进入 **Batch 3: 业务深度逻辑验证** 阶段。你需要实现并运行针对复杂业务场景的 E2E 用例。

## 2. 开发任务清单

### 2.1 级联发布验证 (test_batch3_001_cascade_publish)
- **场景**: 修改实体定义（添加新字段）并选择“级联发布”。
- **验证**: 检查关联的模板（Detail/List）是否自动包含了新字段。
- **要点**: 
    - 使用 `page.goto` 进入 `EntityDefinitionEdit`。
    - 触发发布操作后，等待后台 Job 完成。
    - 预览模板确认字段同步。

### 2.2 实体匹配转换逻辑 (test_batch3_002_entity_conversion)
- **场景**: 模拟从线索 (Lead) 转换到客户 (Account) 的流程（如果已实现）或类似的跨实体数据路由。
- **验证**: 转换后的实体数据完整性。

### 2.3 高级校验与异常处理 (test_batch3_003_advanced_validation)
- **场景**: 验证自定义正则校验、跨字段联动校验。
- **验证**: 错误提示框是否在正确的位置弹出，且内容符合 I18n 预期。

## 3. 基础配套要求
- **继续保持**: 所有测试必须带 `evaluate` 物理坐标审计（如 FIX-03 所示）。
- **环境变量**: 确保 `e2e_mode=true` 全程开启。
- **存证**: 依然要求产出 `debug_page_screenshot.png` 和视频。

## 4. 交付物
- `tests/e2e/cases/06_form_design/test_batch3_business.py`。
- 填充成功的 [TEST-BATCH-03-BUSINESS.md]。

---
**架构评审人**: Antigravity
**日期**: 2026-01-10
