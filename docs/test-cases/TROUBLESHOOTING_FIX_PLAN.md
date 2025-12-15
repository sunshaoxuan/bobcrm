# E2E 测试问题修正计划

**创建日期**: 2025-12-13  
**状态**: 🚧 进行中  
**关联文档**: [TROUBLESHOOTING_LOG.md](./TROUBLESHOOTING_LOG.md)

---

## 📋 问题概览

| 问题编号 | 模块 | 状态 | 优先级 | 预计工作量 |
|----------|------|------|--------|-----------|
| ISSUE-011 | TC-ORG-001 | Pending | P1 | 0.5h |
| ISSUE-012 | TC-ORG-001 | Pending | P1 | 0.5h |

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

### ISSUE-012: 数据库清理失败（表名错误）

**问题描述**:
- 测试清理代码使用错误的表名：`Organizations`
- 实际表名为：`OrganizationNodes`
- 导致 SQL 执行失败：`relation "organizations" does not exist`

**根本原因**:
- 测试代码与数据库模型不一致
- 文档中的表名引用错误

**影响范围**:
- `tests/e2e/cases/03_organization/test_org_001_structure.py` 第12行
- `docs/test-cases/03-organization/TC-ORG-001-组织结构.md` 第106、108、115行
- `docs/test-cases/CLEANUP-STRATEGY.md` 第39行

**解决方案**:
1. **立即修正**：将所有 `Organizations` 替换为 `OrganizationNodes`
2. **验证**：确认数据库表名与代码一致

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

### 步骤 3: 更新问题记录

**文件**: `docs/test-cases/TROUBLESHOOTING_LOG.md`

**更新 ISSUE-011 和 ISSUE-012 的状态为 Fixed**:
- 添加修正日期
- 添加修正说明
- 更新状态列

---

## ✅ 验收标准

### ISSUE-011 验收标准
- [ ] 测试代码不再使用硬编码文本选择器
- [ ] 使用图标或结构选择器
- [ ] 测试在不同语言环境下都能正常运行
- [ ] 按钮点击不再超时

### ISSUE-012 验收标准
- [ ] 所有 `Organizations` 表名已替换为 `OrganizationNodes`
- [ ] 测试清理代码执行成功
- [ ] 文档中的表名引用已修正
- [ ] 数据库查询验证通过

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
**状态**: 🚧 待实施

