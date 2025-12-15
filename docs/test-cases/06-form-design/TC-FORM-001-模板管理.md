# TC-FORM-001 模板管理

| 项目 | 内容 |
|------|------|
| **用例编号** | TC-FORM-001 |
| **用例名称** | 模板管理 |
| **所属领域** | 表单设计 |
| **测试类型** | 功能测试 |
| **优先级** | P1 - 重要功能 |
| **版本** | 1.0 |

---

## 1. 测试场景描述

### 1.1 业务背景
系统支持创建可复用的表单模板，模板可以绑定到实体，用于数据录入界面。

### 1.2 涉及页面
- `/templates` - 模板列表页面

---

## 2. 前置条件

| 条件编号 | 条件描述 |
|----------|----------|
| PRE-001 | 以管理员身份登录 |

---

## 3. 测试数据

```json
{
  "template": {
    "name": "ProductForm",
    "displayName": "产品表单",
    "description": "用于产品数据录入的表单模板"
  }
}
```

---

## 4. 测试步骤

### 场景 A: 查看模板列表

| 步骤 | 操作描述 | 预期结果 | 截图 |
|------|----------|----------|------|
| A1 | 导航到 `/templates` | 显示模板列表页面 | ✓ |
| A2 | 确认列表显示模板信息 | 显示名称、描述、状态等 | - |

### 场景 B: 创建新模板

| 步骤 | 操作描述 | 预期结果 | 截图 |
|------|----------|----------|------|
| B1 | 点击"创建模板"按钮 | 显示创建表单或进入设计器 | ✓ |
| B2 | 输入模板名称 `ProductForm` | 输入成功 | - |
| B3 | 输入显示名称和描述 | 输入成功 | - |
| B4 | 保存模板 | 模板创建成功 | ✓ |

### 场景 C: 复制模板

| 步骤 | 操作描述 | 预期结果 | 截图 |
|------|----------|----------|------|
| C1 | 点击某模板的"复制"按钮 | 创建模板副本 | ✓ |
| C2 | 确认副本出现在列表 | 新模板可见 | - |

### 场景 D: 删除模板

| 步骤 | 操作描述 | 预期结果 | 截图 |
|------|----------|----------|------|
| D1 | 点击模板的删除按钮 | 显示确认对话框 | - |
| D2 | 确认删除 | 模板从列表中移除 | ✓ |

---

## 5. 预期结果汇总

| 结果编号 | 场景 | 预期结果 |
|----------|------|----------|
| EXP-001 | 查看列表 | 正确显示模板列表 |
| EXP-002 | 创建模板 | 模板创建成功 |
| EXP-003 | 复制模板 | 副本创建成功 |
| EXP-004 | 删除模板 | 模板删除成功 |

---

## 6. AI 自动化测试注释

```yaml
test_type: e2e
requires_auth: admin

cleanup:
  strategy: database
  steps:
    # 先删除模板绑定
    - sql: DELETE FROM TemplateBindings WHERE TemplateId IN (SELECT Id FROM Templates WHERE Name = 'ProductForm')
      description: 删除模板绑定关系
    # 再删除模板
    - sql: DELETE FROM Templates WHERE Name = 'ProductForm'
      description: 删除测试模板
    # 删除副本
    - sql: DELETE FROM Templates WHERE Name LIKE 'ProductForm%'
      description: 删除模板副本
  on_error: ignore
  
cleanup_order: both

verify_cleanup:
  - sql: SELECT COUNT(*) FROM Templates WHERE Name LIKE 'ProductForm%'
    expected: 0
```
