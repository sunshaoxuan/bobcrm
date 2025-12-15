# TC-ENT-002 实体字段配置

| 项目 | 内容 |
|------|------|
| **用例编号** | TC-ENT-002 |
| **用例名称** | 实体字段配置 |
| **所属领域** | 实体建模 |
| **测试类型** | 功能测试 |
| **优先级** | P0 - 关键路径 |
| **版本** | 1.0 |

---

## 1. 测试场景描述

### 1.1 业务背景
创建实体后，需要为其添加字段定义，包括字段类型、验证规则、显示属性等。

### 1.2 涉及页面
- `/entity-definitions/edit/{Id}` - 实体编辑页面

---

## 2. 前置条件

| 条件编号 | 条件描述 |
|----------|----------|
| PRE-001 | 已创建 `TestProduct` 实体（TC-ENT-001） |

---

## 3. 测试数据

```json
{
  "fields": [
    {
      "name": "ProductName",
      "displayName": "产品名称",
      "dataType": "string",
      "maxLength": 200,
      "isRequired": true
    },
    {
      "name": "Price",
      "displayName": "价格",
      "dataType": "decimal",
      "precision": 2,
      "isRequired": true
    },
    {
      "name": "Category",
      "displayName": "分类",
      "dataType": "enum",
      "enumTypeName": "ProductCategory"
    },
    {
      "name": "Description",
      "displayName": "描述",
      "dataType": "text",
      "isRequired": false
    },
    {
      "name": "CreatedAt",
      "displayName": "创建时间",
      "dataType": "datetime",
      "defaultValue": "now()"
    }
  ]
}
```

---

## 4. 测试步骤

### 场景 A: 进入字段配置

| 步骤 | 操作描述 | 预期结果 | 截图 |
|------|----------|----------|------|
| A1 | 在实体列表中点击 `TestProduct` 编辑 | 进入实体编辑页面 | ✓ |
| A2 | 导航到字段配置标签页 | 显示字段列表（可能为空） | - |

### 场景 B: 添加字符串字段

| 步骤 | 操作描述 | 预期结果 | 截图 |
|------|----------|----------|------|
| B1 | 点击"添加字段"按钮 | 显示字段配置表单 | ✓ |
| B2 | 输入字段名 `ProductName` | 输入成功 | - |
| B3 | 选择数据类型 `String` | 类型选择成功 | - |
| B4 | 设置最大长度 200 | 配置成功 | - |
| B5 | 勾选"必填" | 勾选成功 | - |
| B6 | 保存字段 | 字段添加到列表 | ✓ |

### 场景 C: 添加数值字段

| 步骤 | 操作描述 | 预期结果 | 截图 |
|------|----------|----------|------|
| C1 | 添加 `Price` 字段，类型 `Decimal` | 字段配置成功 | - |
| C2 | 设置精度为 2 | 配置成功 | - |
| C3 | 保存 | 字段添加成功 | ✓ |

### 场景 D: 添加枚举字段

| 步骤 | 操作描述 | 预期结果 | 截图 |
|------|----------|----------|------|
| D1 | 添加 `Category` 字段，类型 `Enum` | 显示枚举选择 | - |
| D2 | 选择或创建枚举类型 | 枚举关联成功 | - |
| D3 | 保存 | 字段添加成功 | ✓ |

### 场景 E: 添加日期时间字段

| 步骤 | 操作描述 | 预期结果 | 截图 |
|------|----------|----------|------|
| E1 | 添加 `CreatedAt` 字段，类型 `DateTime` | 字段配置成功 | - |
| E2 | 设置默认值 | 配置成功 | - |
| E3 | 保存 | 字段添加成功 | ✓ |

### 场景 F: 编辑已有字段

| 步骤 | 操作描述 | 预期结果 | 截图 |
|------|----------|----------|------|
| F1 | 点击 `ProductName` 的编辑按钮 | 显示编辑表单 | - |
| F2 | 修改显示名称 | 输入成功 | - |
| F3 | 保存 | 更新成功 | ✓ |

### 场景 G: 删除字段

| 步骤 | 操作描述 | 预期结果 | 截图 |
|------|----------|----------|------|
| G1 | 点击 `Description` 的删除按钮 | 显示确认对话框 | - |
| G2 | 确认删除 | 字段从列表中移除 | ✓ |

---

## 5. 预期结果汇总

| 结果编号 | 场景 | 预期结果 |
|----------|------|----------|
| EXP-001 | 添加字符串字段 | 字段创建成功，配置正确 |
| EXP-002 | 添加数值字段 | 精度配置正确 |
| EXP-003 | 添加枚举字段 | 枚举类型关联成功 |
| EXP-004 | 添加日期字段 | 默认值配置正确 |
| EXP-005 | 编辑字段 | 字段更新成功 |
| EXP-006 | 删除字段 | 字段移除成功 |

---

## 6. AI 自动化测试注释

```yaml
test_type: e2e
requires_auth: admin
depends_on: TC-ENT-001
continues_to: TC-ENT-003

# 此测试是实体建模流程的中间步骤，统一在 TC-ENT-003 清理
cleanup:
  strategy: deferred
  defer_to: TC-ENT-003
  reason: 字段配置将在后续测试中继续使用
  
# 如果需要单独运行此测试，使用以下清理
standalone_cleanup:
  steps:
    - sql: DELETE FROM FieldDefinitions WHERE EntityDefinitionId IN (SELECT Id FROM EntityDefinitions WHERE Name = 'TestProduct')
    - sql: DELETE FROM EntityDefinitions WHERE Name = 'TestProduct'
  on_error: ignore
```
