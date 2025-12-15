# TC-ENT-003 实体发布

| 项目 | 内容 |
|------|------|
| **用例编号** | TC-ENT-003 |
| **用例名称** | 实体发布 |
| **所属领域** | 实体建模 |
| **测试类型** | 功能测试 |
| **优先级** | P0 - 关键路径 |
| **版本** | 1.0 |

---

## 1. 测试场景描述

### 1.1 业务背景
实体定义完成后，需要发布才能生成实际的数据库表和运行时类型，使其可用于数据录入。

### 1.2 涉及页面
- `/entity-definitions/publish/{Id}` - 实体发布页面

---

## 2. 前置条件

| 条件编号 | 条件描述 |
|----------|----------|
| PRE-001 | 已完成 `TestProduct` 实体的字段配置（TC-ENT-002） |

---

## 3. 测试步骤

### 场景 A: 预览发布变更

| 步骤 | 操作描述 | 预期结果 | 截图 |
|------|----------|----------|------|
| A1 | 在实体编辑页面点击"发布"按钮 | 跳转到发布预览页面 | ✓ |
| A2 | 确认显示将要执行的变更 | 显示 SQL 预览或变更摘要 | ✓ |
| A3 | 查看表结构变更详情 | 显示将创建的表和列信息 | - |

### 场景 B: 执行发布

| 步骤 | 操作描述 | 预期结果 | 截图 |
|------|----------|----------|------|
| B1 | 点击"确认发布"按钮 | 显示发布进度 | - |
| B2 | 等待发布完成 | 显示发布成功消息 | ✓ |
| B3 | 确认实体状态变更为"已发布" | 状态标记更新 | - |

### 场景 C: 发布后验证

| 步骤 | 操作描述 | 预期结果 | 截图 |
|------|----------|----------|------|
| C1 | 导航到动态实体数据页面 | 可以看到 `TestProduct` 作为可用实体 | ✓ |
| C2 | 确认可以进行数据录入 | 显示数据录入界面 | - |

### 场景 D: 发布失败回滚（如配置错误）

| 步骤 | 操作描述 | 预期结果 | 截图 |
|------|----------|----------|------|
| D1 | 模拟发布失败场景 | 显示错误信息 | ✓ |
| D2 | 确认数据库未被部分变更 | 原有状态保持 | - |

---

## 4. 预期结果汇总

| 结果编号 | 场景 | 预期结果 |
|----------|------|----------|
| EXP-001 | 预览变更 | 正确显示即将执行的变更 |
| EXP-002 | 执行发布 | 发布成功，表结构创建 |
| EXP-003 | 发布验证 | 实体可用于数据录入 |
| EXP-004 | 发布回滚 | 失败时正确回滚 |

---

## 5. AI 自动化测试注释

```yaml
test_type: e2e
requires_auth: admin
depends_on: TC-ENT-002
database_check: table_exists("test_products")

# 此测试是实体建模流程的最后一步，统一在这里清理整个 TestProduct 实体
cleanup:
  strategy: database
  priority: critical  # 必须清理，否则影响后续测试
  steps:
    # 1. 先删除动态表数据
    - sql: DROP TABLE IF EXISTS test_products
      description: 删除发布时创建的数据表
    # 2. 删除模板绑定
    - sql: DELETE FROM TemplateBindings WHERE EntityTypeName = 'TestProduct'
      description: 删除模板绑定关系
    # 3. 删除字段定义
    - sql: DELETE FROM FieldDefinitions WHERE EntityDefinitionId IN (SELECT Id FROM EntityDefinitions WHERE Name = 'TestProduct')
      description: 删除字段定义
    # 4. 删除实体定义
    - sql: DELETE FROM EntityDefinitions WHERE Name = 'TestProduct'
      description: 删除实体元数据
  on_error: ignore
  
cleanup_order: after_test

verify_cleanup:
  - sql: SELECT COUNT(*) FROM EntityDefinitions WHERE Name = 'TestProduct'
    expected: 0
  - sql: SELECT name FROM sqlite_master WHERE type='table' AND name='test_products'
    expected: empty
```
