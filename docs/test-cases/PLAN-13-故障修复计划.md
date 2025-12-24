# E2E 测试问题修正计划

**创建日期**: 2025-12-13  
**最后更新**: 2025-12-13  
**状态**: ✅ 已完成  
**关联文档**: [TROUBLESHOOTING_LOG.md](./TROUBLESHOOTING_LOG.md)

---

## 📋 问题概览

| 问题编号 | 模块 | 状态 | 优先级 | 预计工作量 | 实际工作量 |
|----------|------|------|--------|-----------|-----------|
| ISSUE-011 | TC-ORG-001 | Fixed | P1 | 0.5h | 0.5h |
| ISSUE-012 | TC-ORG-001 | Fixed | P1 | 0.5h | 0.5h |
| ISSUE-013 | TC-ORG-001 | Fixed | P1 | 0.5h | 0.5h |
| ISSUE-014 | TC-ORG-001 | Fixed | P1 | 0.5h | 0.5h |
| ISSUE-015 | TC-ORG-001 | Fixed | P1 | 1h | 1h |

---

## 🔍 问题详细分析

### ISSUE-011: 按钮点击超时（New Organization）

**问题描述**:
- 测试用例使用硬编码英文文本选择器：`button:has-text('New Organization')`
- UI 使用 I18n 多语言，按钮文本可能是中文/日文/英文
- 导致选择器匹配失败，按钮点击超时

**根本原因**:
- 测试代码依赖硬编码文本，未考虑多语言场景
- 选择器策略不当，应使用结构选择器而非文本选择器

**影响范围**:
- `tests/e2e/cases/03_organization/test_org_001_structure.py` 第18行
- 可能影响其他使用硬编码文本选择器的测试用例

**解决方案**:
1. **立即修正**：改用图标选择器 `button:has(.anticon-plus)` 或 `button:has(Icon[type="plus"])`
2. **长期优化**：建立选择器规范，优先使用结构选择器（data-testid、图标、类名等）

---

### ISSUE-012: 数据库清理失败（表名需要双引号）

**问题描述**:
- PostgreSQL 中 PascalCase 表名需要双引号转义
- 测试清理代码使用 `OrganizationNodes` 未加引号
- 导致 SQL 执行失败：`relation "organizations" does not exist` 或语法错误

**根本原因**:
- PostgreSQL 对大小写敏感，PascalCase 表名和列名需要双引号
- 测试代码未考虑 PostgreSQL 的命名规则

**影响范围**:
- `tests/e2e/cases/03_organization/test_org_001_structure.py` 第14行
- `docs/test-cases/03-organization/TC-ORG-001-组织结构.md` 清理 SQL
- `docs/test-cases/CLEANUP-STRATEGY.md` 第39行

**解决方案**:
1. **立即修正**：使用带双引号的表名和列名：`"OrganizationNodes"`, `"Code"`, `"Name"`, `"ParentId"`
2. **验证**：确认 SQL 在 PostgreSQL 中正确执行

---

## 🛠️ 修正方案

### 步骤 1: 修正测试代码选择器（ISSUE-011）

**文件**: `tests/e2e/cases/03_organization/test_org_001_structure.py`

**修改前**:
```python
page.click("button:has-text('New Organization')")
```

**修改后**:
```python
# 使用图标选择器，不依赖文本
page.click("button:has(.anticon-plus)")
# 或者使用更精确的选择器
page.click("button:has(Icon[type='plus'])")
```

**验证**:
- 测试在不同语言环境下都能正常运行
- 按钮点击不再超时

---

### 步骤 2: 修正数据库表名（ISSUE-012）

#### 2.1 修正测试代码

**文件**: `tests/e2e/cases/03_organization/test_org_001_structure.py`

**修改前**:
```python
db_helper.execute_query("DELETE FROM Organizations WHERE Code IN ('HQ', 'TECH')")
```

**修改后**:
```python
db_helper.execute_query("DELETE FROM OrganizationNodes WHERE Code IN ('HQ', 'TECH')")
```

#### 2.2 修正测试用例文档

**文件**: `docs/test-cases/03-organization/TC-ORG-001-组织结构.md`

**修改位置**:
- 第106行：`DELETE FROM Organizations WHERE ParentId IS NOT NULL ...`
- 第108行：`DELETE FROM Organizations WHERE Name = '总公司'`
- 第115行：`SELECT COUNT(*) FROM Organizations WHERE Code IN ...`

**修改为**:
- `DELETE FROM OrganizationNodes WHERE ParentId IS NOT NULL ...`
- `DELETE FROM OrganizationNodes WHERE Name = '总公司'`
- `SELECT COUNT(*) FROM OrganizationNodes WHERE Code IN ...`

#### 2.3 修正清理策略文档

**文件**: `docs/test-cases/CLEANUP-STRATEGY.md`

**修改位置**:
- 第39行：`DELETE FROM Organizations WHERE Code IN ('HQ', 'TECH')`

**修改为**:
- `DELETE FROM OrganizationNodes WHERE Code IN ('HQ', 'TECH')`

---

### 步骤 6: 更新问题记录

**文件**: `docs/test-cases/TROUBLESHOOTING_LOG.md`

**更新所有问题的状态为 Fixed**:
- ISSUE-011: Fixed (2025-12-13)
- ISSUE-012: Fixed (2025-12-13) - 添加双引号
- ISSUE-013: Fixed (2025-12-13) - 使用 .field-control 选择器
- ISSUE-014: Fixed (2025-12-13) - 使用正则匹配
- ISSUE-015: Fixed (2025-12-13) - 重写流程

---

## ✅ 验收标准

### ISSUE-011 验收标准
- [x] 测试代码不再使用硬编码文本选择器
- [x] 使用图标或结构选择器
- [x] 测试在不同语言环境下都能正常运行
- [x] 按钮点击不再超时

### ISSUE-012 验收标准
- [x] 所有表名和列名已添加双引号（PostgreSQL PascalCase 要求）
- [x] 测试清理代码执行成功
- [x] 文档中的 SQL 语句已修正
- [x] 数据库查询验证通过

### ISSUE-013 验收标准
- [x] 输入框选择器使用 `.field-control` 类选择器
- [x] 使用 `nth()` 选择正确的输入框（第一个是 Code，第二个是 Name）
- [x] 表单填写不再超时

### ISSUE-014 验收标准
- [x] Save 按钮选择器使用正则匹配 `/保存|Save/`
- [x] 支持多语言环境（中文、日文、英文）
- [x] 按钮点击不再超时

### ISSUE-015 验收标准
- [x] 子组织创建流程与 UI 实现一致
- [x] 流程包括：选择父节点 → 点击 AddChild → 表格行输入 → 行内 Save
- [x] 测试能够成功创建子组织

---

## 📝 实施计划

### 阶段 1: 代码修正（立即执行）
1. 修正 `test_org_001_structure.py` 中的选择器和表名
2. 运行测试验证修正效果

### 阶段 2: 文档修正（立即执行）
1. 修正 `TC-ORG-001-组织结构.md` 中的表名
2. 修正 `CLEANUP-STRATEGY.md` 中的表名
3. 更新 `TROUBLESHOOTING_LOG.md` 中的问题状态

### 阶段 3: 验证与测试（立即执行）
1. 运行 E2E 测试验证修正
2. 在不同语言环境下测试
3. 验证数据库清理功能

---

## 🔄 后续优化建议

### 1. 建立选择器规范
- **优先使用结构选择器**：`data-testid`、图标、类名
- **避免文本选择器**：不依赖硬编码文本
- **多语言兼容**：所有选择器应支持多语言环境

### 2. 建立表名验证机制
- 在测试代码中添加表名常量
- 使用代码生成工具同步表名
- 在 CI/CD 中添加表名一致性检查

### 3. 完善测试文档
- 在测试用例文档中明确表名和选择器规范
- 添加多语言测试场景说明
- 建立问题预防机制

---

## 📌 相关资源

- [TROUBLESHOOTING_LOG.md](./TROUBLESHOOTING_LOG.md) - 问题记录
- [CLEANUP-STRATEGY.md](./CLEANUP-STRATEGY.md) - 清理策略
- [TC-ORG-001-组织结构.md](./03-organization/TC-ORG-001-组织结构.md) - 测试用例

---

**最后更新**: 2025-12-13  
**负责人**: 测试团队  
**状态**: ✅ 已完成

---

## ✅ 修正完成总结

### 已修正的问题

1. **ISSUE-011**: 按钮选择器 ✅
   - 改用图标选择器 `button:has(.anticon-plus)`
   - 支持多语言环境

2. **ISSUE-012**: 数据库表名 ✅
   - 添加双引号：`"OrganizationNodes"`, `"Code"`, `"Name"`, `"ParentId"`
   - PostgreSQL PascalCase 命名规则

3. **ISSUE-013**: 输入框选择器 ✅
   - 改用 `.field-control` 类选择器
   - 使用 `nth()` 选择正确的输入框

4. **ISSUE-014**: Save 按钮选择器 ✅
   - 使用正则匹配 `/保存|Save/`
   - 支持多语言

5. **ISSUE-015**: 子组织创建流程 ✅
   - 重写流程匹配 UI 实现
   - 包括选择父节点、点击 AddChild、表格行输入、行内 Save

### 修正文件清单

- ✅ `tests/e2e/cases/03_organization/test_org_001_structure.py` - 完全重写测试逻辑
- ✅ `docs/test-cases/03-organization/TC-ORG-001-组织结构.md` - 修正 SQL 语句
- ✅ `docs/test-cases/CLEANUP-STRATEGY.md` - 修正表名引用
- ✅ `docs/test-cases/TROUBLESHOOTING_LOG.md` - 更新问题状态
- ✅ `docs/test-cases/TROUBLESHOOTING_FIX_PLAN.md` - 更新修正计划状态

