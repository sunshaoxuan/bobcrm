# TC-FORM-003 模板绑定

| 项目 | 内容 |
|------|------|
| **用例编号** | TC-FORM-003 |
| **用例名称** | 模板绑定 |
| **所属领域** | 表单设计 |
| **测试类型** | 功能测试 |
| **优先级** | P1 - 重要功能 |
| **版本** | 1.0 |

---

## 1. 测试场景描述

### 1.1 业务背景
设计完成的表单模板需要绑定到实体类型，以便在数据管理时使用对应的表单。

### 1.2 涉及页面
- `/templates/bindings` - 模板绑定配置页面

---

## 2. 前置条件

| 条件编号 | 条件描述 |
|----------|----------|
| PRE-001 | 已创建并设计好模板（TC-FORM-001, TC-FORM-002） |
| PRE-002 | 已发布实体定义（TC-ENT-003） |

---

## 3. 测试数据

```json
{
  "binding": {
    "entityType": "TestProduct",
    "templateId": "ProductForm",
    "usageType": "edit"  // create / edit / view
  }
}
```

---

## 4. 测试步骤

### 场景 A: 查看绑定列表

| 步骤 | 操作描述 | 预期结果 | 截图 |
|------|----------|----------|------|
| A1 | 导航到 `/templates/bindings` | 显示绑定配置页面 | ✓ |
| A2 | 确认显示已有绑定关系 | 绑定列表可见 | - |

### 场景 B: 创建新绑定

| 步骤 | 操作描述 | 预期结果 | 截图 |
|------|----------|----------|------|
| B1 | 点击"添加绑定"按钮 | 显示绑定配置表单 | - |
| B2 | 选择实体类型 `TestProduct` | 实体选择成功 | - |
| B3 | 选择模板 `ProductForm` | 模板选择成功 | - |
| B4 | 选择使用场景（编辑） | 场景选择成功 | - |
| B5 | 保存绑定 | 绑定创建成功 | ✓ |

### 场景 C: 验证绑定生效

| 步骤 | 操作描述 | 预期结果 | 截图 |
|------|----------|----------|------|
| C1 | 导航到 `TestProduct` 数据页面 | 进入数据管理 | - |
| C2 | 点击编辑某条记录 | 使用 `ProductForm` 模板显示 | ✓ |

### 场景 D: 修改绑定

| 步骤 | 操作描述 | 预期结果 | 截图 |
|------|----------|----------|------|
| D1 | 编辑已有绑定 | 显示编辑表单 | - |
| D2 | 更换模板 | 选择成功 | - |
| D3 | 保存 | 更新成功 | ✓ |

### 场景 E: 删除绑定

| 步骤 | 操作描述 | 预期结果 | 截图 |
|------|----------|----------|------|
| E1 | 删除绑定 | 绑定移除 | ✓ |
| E2 | 访问实体数据页面 | 使用默认表单或显示无绑定提示 | - |

---

## 5. 预期结果汇总

| 结果编号 | 场景 | 预期结果 |
|----------|------|----------|
| EXP-001 | 查看绑定 | 正确显示绑定列表 |
| EXP-002 | 创建绑定 | 绑定创建成功 |
| EXP-003 | 验证生效 | 数据页面使用正确模板 |
| EXP-004 | 修改绑定 | 绑定更新成功 |
| EXP-005 | 删除绑定 | 绑定删除成功 |

---

## 6. AI 自动化测试注释

```yaml
test_type: e2e
requires_auth: admin
depends_on: [TC-FORM-002, TC-ENT-003]

cleanup:
  strategy: database
  steps:
    # 删除测试创建的绑定关系
    - sql: DELETE FROM TemplateBindings WHERE EntityTypeName = 'TestProduct'
      description: 删除 TestProduct 的模板绑定
    - sql: DELETE FROM TemplateBindings WHERE TemplateId IN (SELECT Id FROM Templates WHERE Name = 'ProductForm')
      description: 删除 ProductForm 的所有绑定
  on_error: ignore
  
cleanup_order: both

verify_cleanup:
  - sql: SELECT COUNT(*) FROM TemplateBindings WHERE EntityTypeName = 'TestProduct'
    expected: 0
```
