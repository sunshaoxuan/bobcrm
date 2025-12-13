# BobCRM 集成测试用例文档

## 文档概述

本文档集包含 BobCRM 系统的完整集成测试用例，按照领域驱动设计原则进行分类，覆盖所有业务流程画面。

## 文档结构

```
docs/test-cases/
├── README.md                           # 本文件 - 测试用例总览
├── 01-authentication/                  # 认证与授权领域
│   ├── TC-AUTH-001-系统初始化.md
│   ├── TC-AUTH-002-用户登录.md
│   ├── TC-AUTH-003-用户注册激活.md
│   └── TC-AUTH-004-用户登出.md
├── 02-user-management/                 # 用户与权限管理领域
│   ├── TC-USER-001-用户列表.md
│   ├── TC-USER-002-角色管理.md
│   └── TC-USER-003-个人资料.md
├── 03-organization/                    # 组织管理领域
│   └── TC-ORG-001-组织结构.md
├── 04-system-config/                   # 系统配置领域
│   ├── TC-SYS-001-菜单管理.md
│   ├── TC-SYS-002-系统设置.md
│   └── TC-SYS-003-枚举管理.md
├── 05-entity-modeling/                 # 实体建模领域
│   ├── TC-ENT-001-实体定义创建.md
│   ├── TC-ENT-002-实体字段配置.md
│   ├── TC-ENT-003-实体发布.md
│   └── TC-ENT-004-主从配置.md
├── 06-form-design/                     # 表单设计领域
│   ├── TC-FORM-001-模板管理.md
│   ├── TC-FORM-002-表单设计器.md
│   └── TC-FORM-003-模板绑定.md
└── 07-dynamic-data/                    # 动态数据管理领域
    └── TC-DATA-001-动态实体CRUD.md
```

## 测试用例编号规范

| 前缀 | 领域 | 示例 |
|------|------|------|
| TC-AUTH | 认证与授权 | TC-AUTH-001 |
| TC-USER | 用户管理 | TC-USER-001 |
| TC-ORG | 组织管理 | TC-ORG-001 |
| TC-SYS | 系统配置 | TC-SYS-001 |
| TC-ENT | 实体建模 | TC-ENT-001 |
| TC-FORM | 表单设计 | TC-FORM-001 |
| TC-DATA | 动态数据 | TC-DATA-001 |

## 测试用例模板规范

每个测试用例文档遵循 STD（Software Test Documentation）规范，包含以下章节：

1. **测试用例标识** - 唯一编号、名称、版本
2. **测试场景描述** - 业务背景和测试目的
3. **前置条件** - 测试执行前需要满足的条件
4. **测试数据** - 测试所需的数据定义
5. **测试步骤** - 详细的操作流程描述
6. **预期结果** - 每个步骤的预期输出
7. **截图要求** - 标记需要截图的关键节点
8. **后置条件** - 测试后的清理操作

## 测试环境要求

- **基础 URL**: `http://localhost:3000`
- **数据库**: SQLite (自动创建)
- **浏览器**: Chrome/Edge (支持 ES6+)
