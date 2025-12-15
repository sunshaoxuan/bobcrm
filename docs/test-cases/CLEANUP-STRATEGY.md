# BobCRM 测试数据清理策略

## 概述

为确保集成测试的可重复性，每个测试用例执行完毕后必须清理其创建的测试数据。本文档定义了标准的清理策略和实现模式。

---

## 清理原则

1. **谁创建谁清理** - 每个测试用例负责清理其创建的数据
2. **逆序清理** - 按照创建的逆序删除，避免外键约束冲突
3. **幂等清理** - 清理操作可重复执行，不会因数据不存在而失败
4. **独立清理** - 清理失败不应影响后续测试的执行

---

## 清理策略按领域

### 1. 认证与授权 (TC-AUTH)

| 用例 | 创建的数据 | 清理策略 |
|------|-----------|----------|
| TC-AUTH-001 | admin 账户 | **不清理** - 系统初始化数据，仅重置数据库时清理 |
| TC-AUTH-002 | localStorage tokens | `localStorage.clear()` |
| TC-AUTH-003 | newuser 账户 | `DELETE FROM AspNetUsers WHERE UserName = 'newuser'` |
| TC-AUTH-004 | localStorage tokens | `localStorage.clear()` |

### 2. 用户管理 (TC-USER)

| 用例 | 创建的数据 | 清理策略 |
|------|-----------|----------|
| TC-USER-002 | TestRole 角色 | `DELETE FROM AspNetRoles WHERE Name = 'TestRole'` |

### 3. 组织管理 (TC-ORG)

| 用例 | 创建的数据 | 清理策略 |
|------|-----------|----------|
| TC-ORG-001 | 总公司、技术部 | `DELETE FROM "OrganizationNodes" WHERE "Code" IN ('HQ', 'TECH')` |

### 4. 系统配置 (TC-SYS)

| 用例 | 创建的数据 | 清理策略 |
|------|-----------|----------|
| TC-SYS-001 | 测试菜单项 | `DELETE FROM MenuItems WHERE Name = '测试菜单'` |
| TC-SYS-003 | OrderStatus 枚举 | `DELETE FROM EnumDefinitions WHERE Name = 'OrderStatus'` |

### 5. 实体建模 (TC-ENT)

| 用例 | 创建的数据 | 清理策略 |
|------|-----------|----------|
| TC-ENT-001~004 | TestProduct 实体 | 1. `DROP TABLE IF EXISTS test_products` <br> 2. `DELETE FROM EntityDefinitions WHERE Name = 'TestProduct'` |

### 6. 表单设计 (TC-FORM)

| 用例 | 创建的数据 | 清理策略 |
|------|-----------|----------|
| TC-FORM-001~003 | ProductForm 模板 | 1. `DELETE FROM TemplateBindings WHERE TemplateId = ...` <br> 2. `DELETE FROM Templates WHERE Name = 'ProductForm'` |

### 7. 动态数据 (TC-DATA)

| 用例 | 创建的数据 | 清理策略 |
|------|-----------|----------|
| TC-DATA-001 | 测试产品数据 | `DELETE FROM test_products WHERE ProductName LIKE '测试产品%'` |

---

## 清理实现模式

### 模式 A: API 清理（推荐）

通过调用 API 删除数据，利用业务逻辑处理级联删除。

```yaml
cleanup:
  - api: DELETE /api/entity-definitions/{id}
    description: 删除实体定义（自动处理字段、模板绑定等）
```

### 模式 B: 数据库直接清理

直接执行 SQL 语句，适用于需要绕过业务逻辑的场景。

```yaml
cleanup:
  - sql: |
      DELETE FROM TemplateBindings WHERE EntityTypeName = 'TestProduct';
      DELETE FROM Templates WHERE Name LIKE 'Test%';
      DROP TABLE IF EXISTS test_products;
      DELETE FROM EntityDefinitions WHERE Name = 'TestProduct';
```

### 模式 C: 数据库快照恢复

测试前保存数据库快照，测试后恢复。适用于大规模集成测试。

```yaml
setup:
  - action: create_db_snapshot
    name: pre_test_snapshot

cleanup:
  - action: restore_db_snapshot
    name: pre_test_snapshot
```

---

## 测试执行顺序与清理

```
┌─────────────────────────────────────────────────┐
│  全局 Setup                                      │
│  - 确保数据库干净                                 │
│  - 执行 TC-AUTH-001 创建初始管理员                │
└─────────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────┐
│  测试组: 认证                                    │
│  TC-AUTH-002 → TC-AUTH-003 → TC-AUTH-004        │
│  每个测试后执行各自的清理                         │
└─────────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────┐
│  测试组: 实体建模（需按顺序）                     │
│  TC-ENT-001 → TC-ENT-002 → TC-ENT-003           │
│  测试组完成后统一清理 TestProduct                 │
└─────────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────┐
│  全局 Teardown                                   │
│  - 清理所有测试残留数据                           │
│  - 可选：重置数据库到初始状态                     │
└─────────────────────────────────────────────────┘
```

---

## AI 自动化测试清理注释规范

每个测试用例的 YAML 注释应包含 `cleanup` 部分：

```yaml
# 完整示例
test_type: e2e
requires_auth: admin
depends_on: TC-ENT-002

setup:
  - ensure_logged_in: admin
  - ensure_entity_exists: TestProduct

cleanup:
  # 按创建逆序清理
  - api: DELETE /api/dynamic-entity/TestProduct?filter=ProductName:测试*
    on_error: ignore  # 数据可能不存在
  - sql: DROP TABLE IF EXISTS test_products
    on_error: ignore
  - api: DELETE /api/entity-definitions/by-name/TestProduct
    on_error: ignore

cleanup_order: after_test  # before_test | after_test | both
```

---

## 清理验证

每次测试运行后，验证清理是否成功：

```yaml
verify_cleanup:
  - sql: SELECT COUNT(*) FROM EntityDefinitions WHERE Name = 'TestProduct'
    expected: 0
  - sql: SELECT COUNT(*) FROM test_products
    expected: error  # 表应该不存在
```
