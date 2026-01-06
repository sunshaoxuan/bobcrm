# ARCH-30 Task 4.2: 更新 API 接口文档

**任务编号**: Task 4.2
**阶段**: 阶段4 - 文档同步
**状态**: 待开始
**预计工时**: 1 小时
**依赖**: 阶段0-3 实施完成

---

## 1. 任务目标

更新 `docs/reference/API-01-接口文档.md`，记录所有支持 `lang` 参数的 API 端点变更。

---

## 2. 需更新的端点

### 2.1 高频 API（阶段1）

| 端点 | 新增参数 | 行为变更 |
|------|----------|----------|
| `GET /api/access/functions/me` | `lang` | 单语模式返回单一 displayName |
| `GET /api/templates/menu-bindings` | `lang` | 使用用户语言替代系统语言 |
| `GET /api/entities` | `lang` | 单语模式返回单一 displayName |

### 2.2 中频 API（阶段2）

| 端点 | 新增参数 | 行为变更 |
|------|----------|----------|
| `GET /api/entity-definitions` | `lang` | 实体定义列表多语支持 |
| `GET /api/entity-definitions/{id}` | `lang` | 实体定义详情多语支持 |
| `GET /api/enums` | `lang` | 枚举列表多语支持 |
| `GET /api/enums/{id}` | `lang` | 枚举详情多语支持 |
| `GET /api/entity-domains` | `lang` | 实体域多语支持 |
| `GET /api/access/functions` | `lang` | 权限功能列表多语支持 |

### 2.3 低频 API（阶段3）

| 端点 | 新增参数 | 行为变更 |
|------|----------|----------|
| `POST /api/dynamic-entities/{type}/query` | `lang` | 返回 `meta.fields` |
| `GET /api/dynamic-entities/{type}/{id}` | `lang`, `includeMeta` | 可选返回字段元数据 |

---

## 3. 文档更新模板

### 3.1 通用 lang 参数说明（添加到文档开头）

```markdown
## 多语 API 规范

### lang 参数

支持多语的 API 端点均接受 `lang` 查询参数：

| 参数 | 类型 | 可选值 | 默认行为 |
|------|------|--------|----------|
| `lang` | string | `zh`, `ja`, `en` | 不传则返回多语字典（向后兼容） |

**单语模式**（提供 lang 参数）：
- `displayName`: 返回指定语言的字符串
- `displayNameTranslations`: 不返回（null）
- 响应体积减少约 66%

**多语模式**（不提供 lang 参数）：
- `displayNameKey`: 资源键（接口字段）
- `displayNameTranslations`: 多语字典（自定义字段）
- 保持向后兼容
```

### 3.2 单个端点文档模板

```markdown
### GET /api/access/functions/me

获取当前用户的功能菜单。

**认证**: 需要 Bearer Token

**查询参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `lang` | string | 否 | 语言代码（zh/ja/en） |

**响应示例**（`lang=zh`）:
\`\`\`json
{
  "code": "SUCCESS",
  "data": [
    {
      "code": "CUSTOMER",
      "displayName": "客户管理",
      "icon": "user",
      "children": [...]
    }
  ]
}
\`\`\`

**响应示例**（不传 lang，向后兼容）:
\`\`\`json
{
  "code": "SUCCESS",
  "data": [
    {
      "code": "CUSTOMER",
      "displayNameTranslations": {
        "zh": "客户管理",
        "ja": "顧客管理",
        "en": "Customer Management"
      },
      "icon": "user",
      "children": [...]
    }
  ]
}
\`\`\`

**v2.0 变更**:
- 新增 `lang` 参数支持
- 单语模式响应体积减少约 66%
```

### 3.3 动态实体查询端点文档

```markdown
### POST /api/dynamic-entities/{fullTypeName}/query

查询动态实体数据。

**路径参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| `fullTypeName` | string | 实体完整类型名 |

**查询参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `lang` | string | 否 | 语言代码（zh/ja/en） |

**请求体**:
\`\`\`json
{
  "page": 1,
  "pageSize": 20,
  "filters": {},
  "sorts": []
}
\`\`\`

**响应示例**（`lang=zh`）:
\`\`\`json
{
  "code": "SUCCESS",
  "data": {
    "meta": {
      "fields": [
        {
          "propertyName": "Code",
          "displayNameKey": "LBL_FIELD_CODE",
          "displayName": "编码",
          "dataType": "String"
        },
        {
          "propertyName": "Name",
          "displayNameKey": "LBL_FIELD_NAME",
          "displayName": "名称",
          "dataType": "String"
        }
      ]
    },
    "data": [
      { "id": "...", "code": "C001", "name": "客户A" }
    ],
    "total": 100,
    "page": 1,
    "pageSize": 20
  }
}
\`\`\`

**v2.0 变更**:
- 新增 `lang` 参数支持
- 响应新增 `meta.fields` 字段元数据
```

---

## 4. 验收标准

- [ ] 文档包含多语 API 规范说明
- [ ] 所有改造端点已更新
- [ ] 包含单语/多语模式响应示例
- [ ] 标注 v2.0 变更说明

---

## 5. Git 提交规范

```bash
git add docs/reference/API-01-接口文档.md
git commit -m "docs(api): update API reference for ARCH-30 i18n changes

- Add multilingual API specification section
- Update 15+ endpoints with lang parameter docs
- Add response examples for single/multi language modes
- Document meta.fields for dynamic entity queries
- Ref: ARCH-30 Task 4.2"
```

---

**创建日期**: 2025-01-06
**最后更新**: 2025-01-06
**维护者**: ARCH-30 项目组
