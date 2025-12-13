# 接口文档（v0.2）

> 本文档与《客户信息管理系统设计文档》配套，描述对外 API 契约。所有变更需先更新本文件。

## 多语参数（ARCH-30）

- 通用查询参数：`lang`（可选，`zh|ja|en`），示例：`?lang=zh`
- 双模式响应（仅对支持该参数的端点生效）
  - 单语模式：返回 `displayName`/`description`/`name` 等 `string`
  - 多语模式：返回 `displayNameTranslations`/`descriptionTranslations`/`nameTranslations` 等 `MultilingualText`（字典）
- `Accept-Language` 的处理
  - 仅以下端点在未传 `lang` 时会使用 `Accept-Language` 作为默认语言：
    - `GET /api/access/functions/me`
    - `GET /api/templates/menu-bindings`
    - `GET /api/entities`、`GET /api/entities/all`
  - 其余已改造端点：只有显式传 `?lang=xx` 才进入单语模式；未传 `lang` 时忽略 `Accept-Language`

## 向后兼容性（ARCH-30）

- 所有新增的 `lang`/`includeMeta` 查询参数均为可选
- 未传 `lang` 时：端点保持既有默认行为（多语模式或基于 `Accept-Language` 的单语模式，取决于端点）
- 动态实体 `GET /api/dynamic-entities/{fullTypeName}/{id}` 默认不返回 `meta`；仅 `includeMeta=true` 才返回 `{ meta, data }`

## 认证与会话

- 注册
  - POST `/api/auth/register`
  - Body: `{ "username": "u", "password": "p", "email": "e@x.com" }`
  - Resp: `{ "status": "ok" }`

- 邮件激活
  - GET `/api/auth/activate?code={code}`
  - Resp: `{ "status": "ok" }`

- 登录
  - POST `/api/auth/login`
  - Body: `{ "username": "u", "password": "p" }`
  - Resp:
    ```json
    {
      "accessToken": "{JWT}",
      "refreshToken": "{RT}",
      "user": { "id": 1, "username": "u", "role": "user" }
    }
    ```

- 刷新令牌
  - POST `/api/auth/refresh`
  - Body: `{ "refreshToken": "{RT}" }`
  - Resp: `{ "accessToken": "{JWT}", "refreshToken": "{RT2}" }`

- 登出
  - POST `/api/auth/logout`
  - Header: `Authorization: Bearer {JWT}`
  - Resp: `{ "status": "ok" }`

- 会话校验
  - GET `/api/auth/session`
  - Header: `Authorization: Bearer {JWT}`
  - Resp: `{ "valid": true, "user": { ... } }`

## 客户与字段

- 客户列表
  - GET `/api/customers`
  - Header: `Authorization: Bearer {JWT}`
  - Resp: `[{ "id":1, "code":"C001", "name":"客户A" }]`

- 客户详情
  - GET `/api/customers/{id}`
  - Resp:
    ```json
    {
      "id": 1,
      "code": "C001",
      "name": "客户A",
      "version": 2,
      "fields": [
        { "key": "email", "label": "邮箱", "type": "email", "value": "a@b.com" },
        { "key": "rds", "label": "RDS连接", "type": "rds", "value": { "ip": "10.0.0.1", "user": "admin" } }
      ]
    }
    ```

- 更新客户字段
  - PUT `/api/customers/{id}`
  - Body:
    ```json
    {
      "fields": [ { "key": "email", "value": "x@x.com" } ],
      "expectedVersion": 2 // 可选，用于并发控制
    }
    ```
  - Resp: `{ "status": "success", "newVersion": 3 }`
  - 并发：如提供了 `expectedVersion` 且与服务器当前版本不一致，将返回 409 冲突错误：
    ```json
    { "code": "ConcurrencyConflict", "message": "version mismatch", "details": [{"field":"version","code":"Mismatch"}] }
    ```

- 字段定义
  - GET `/api/fields`
  - Resp:
    ```json
    [
      {
        "key": "email",
        "label": "邮箱",
        "type": "email",
        "tags": ["常用"],
        "actions": [ { "icon": "mail", "title": "发邮件", "type": "click", "action": "mailto" } ]
      }
    ]
    ```
  - 管理（后续）：POST/PUT `/api/fields`（仅管理员）

## 布局与表单设计（仅两级：系统默认/用户）

- 获取布局
  - GET `/api/layout?scope=effective|user|default`
  - Resp: `{ /* LayoutJson */ }`

- 保存布局
  - POST `/api/layout?scope=user|default`（`default` 仅管理员）
  - Body: `{ /* LayoutJson */ }`
  - Resp: `{ "status": "ok" }`

- 标签与快速布局
  - 标签列表
    - GET `/api/fields/tags`
    - Resp: `[{ "tag":"常用", "count": 3 }]`
  - 基于标签生成布局（可选保存）
    - POST `/api/layout/generate`
    - Body:
      ```json
      { "tags": ["常用"], "mode": "flow|free", "save": true, "scope": "user|default" }
      ```
    - Resp（布局 JSON）:
      ```json
      { "mode": "flow", "items": { "email": {"order":0, "w":6}, "link": {"order":1, "w":6} } }
      ```

- 模板（可选后续）
  - GET `/api/layout/templates`
  - POST `/api/layout/templates`

## 国际化与权限

- 资源
  - GET `/api/i18n/resources`
  - Resp: `[{ "key":"Customer.Name", "zh":"名称", "ja":"名称", "en":"Name" }]`
  - GET `/api/i18n/{lang}`
  - Resp: `{ "LBL_CUSTOMER": "客户", ... }`

- 客户访问控制（仅管理员）
  - GET `/api/customers/{id}/access`
  - POST `/api/customers/{id}/access` Body: `{ "userId":"{uid}", "canEdit": true }`

## 管理与配置（数据库）

- 数据库配置（仅管理员，后续实现）
  - GET `/api/admin/dbconfig` → `{ "provider":"postgres|sqlite", "connectionString":"..." }`
  - POST `/api/admin/dbconfig` → 校验连接后保存

## 认证头

- 需要鉴权的接口统一使用：`Authorization: Bearer {accessToken}`
- 错误响应（统一约定）
  - 结构：
    ```json
    {
      "code": "ValidationFailed|BusinessRuleViolation|PersistenceError",
      "message": "说明",
      "details": [
        { "field": "email", "code": "Required", "message": "邮箱必填" }
      ]
    }
    ```
  - 语义：
    - 业务层失败 → `BusinessRuleViolation`
    - 通用层失败 → `ValidationFailed`
    - 持久化失败 → `PersistenceError`

## 文件存储（MinIO/S3）

- 上传文件
  - 方法：POST `/api/files/upload`
  - 认证：需要（Bearer）
  - 请求：`multipart/form-data`，字段名 `file`（必填），可选 `prefix`
  - 响应：
```json
{ "key": "uploads/2025/11/08/xxxxx-filename.txt", "url": "/api/files/uploads/2025/11/08/xxxxx-filename.txt" }
```

- 下载文件
  - 方法：GET `/api/files/{key}`
  - 认证：无（本地默认允许匿名下载，生产建议改为签名 URL）

- 删除文件
  - 方法：DELETE `/api/files/{key}`
  - 认证：需要（Bearer）

> 说明：本地通过 MinIO（S3 兼容）实现，详见 `docs/ARCH-12-对象存储（MinIO-S3）使用说明.md`。

## 权限与功能节点（ARCH-30）

- 我的功能树（高频）
  - GET `/api/access/functions/me`
  - Query: `lang`（可选）
  - Header（可选）：`Accept-Language`（未传 `lang` 时生效）
  - Resp（多语模式，未传 `lang` 且无 `Accept-Language`）：
    ```json
    [
      { "code": "SYS.SET.MENU", "displayNameTranslations": { "zh": "菜单管理", "ja": "メニュー管理", "en": "Menu Management" } }
    ]
    ```
  - Resp（单语模式，`?lang=ja` 或 `Accept-Language: ja`）：
    ```json
    [
      { "code": "SYS.SET.MENU", "displayName": "メニュー管理" }
    ]
    ```

- 功能树（管理端点，忽略 `Accept-Language`）
  - GET `/api/access/functions`
  - GET `/api/access/functions/manage`
  - Query: `lang`（可选；仅显式 `?lang=xx` 才单语）
  - Resp（多语模式，未传 `lang`）：
    ```json
    [
      { "code": "SYS.SET.MENU", "displayNameTranslations": { "zh": "菜单管理", "ja": "メニュー管理", "en": "Menu Management" } }
    ]
    ```
  - Resp（单语模式，`?lang=en`）：
    ```json
    [
      { "code": "SYS.SET.MENU", "displayName": "Menu Management" }
    ]
    ```

- 创建/更新功能节点（返回体同上双模式）
  - POST `/api/access/functions?lang=zh`
  - PUT `/api/access/functions/{id}?lang=zh`
  - Resp（多语模式，未传 `lang`，节选）：`{ "code": "TEST.FUNC", "displayNameTranslations": { "zh": "测试功能", "en": "Test Function" } }`
  - Resp（单语模式，`?lang=zh`，节选）：`{ "code": "TEST.FUNC", "displayName": "测试功能" }`

## 模板菜单绑定（ARCH-30）

- 获取菜单与模板交集（高频）
  - GET `/api/templates/menu-bindings`
  - Query: `lang`（可选）、`viewState`（可选，默认 `DetailView`）
  - Header（可选）：`Accept-Language`（未传 `lang` 时生效）
  - Resp（多语模式，未传 `lang` 且无 `Accept-Language`，节选）：
    ```json
    [
      {
        "Menu": {
          "code": "SYS.SET.MENU",
          "displayNameKey": "LBL_MENU_MANAGEMENT",
          "displayNameTranslations": { "zh": "菜单管理", "ja": "メニュー管理", "en": "Menu Management" }
        }
      }
    ]
    ```
  - Resp（单语模式，`?lang=ja` 或 `Accept-Language: ja`，节选）：
    ```json
    [
      { "Menu": { "code": "SYS.SET.MENU", "displayNameKey": "LBL_MENU_MANAGEMENT", "displayName": "メニュー管理" } }
    ]
    ```

## 实体元数据（ARCH-30）

- 可用实体列表（公共）
  - GET `/api/entities`
  - Query: `lang`（可选）
  - Header（可选）：`Accept-Language`（未传 `lang` 时生效）
  - Resp（多语模式，未传 `lang` 且无 `Accept-Language`）：
    ```json
    { "data": [ { "entityType": "customers", "displayNameTranslations": { "zh": "客户", "en": "Customer" } } ] }
    ```
  - Resp（单语模式，`?lang=en` 或 `Accept-Language: en`）：
    ```json
    { "data": [ { "entityType": "customers", "displayName": "Customer" } ] }
    ```

- 所有实体列表（管理员）
  - GET `/api/entities/all`
  - Query/Resp：同上

## 实体定义管理（ARCH-30）

- 实体定义列表（忽略 `Accept-Language`）
  - GET `/api/entity-definitions`
  - Query: `lang`（可选；仅显式 `?lang=xx` 才单语）
  - Resp（多语模式，未传 `lang`，节选）：
    ```json
    { "data": [ { "entityName": "Customer", "displayNameTranslations": { "zh": "客户", "en": "Customer" } } ] }
    ```
  - Resp（单语模式，`?lang=zh`，节选）：
    ```json
    { "data": [ { "entityName": "Customer", "displayName": "客户" } ] }
    ```

- 实体定义详情（字段元数据双模式）
  - GET `/api/entity-definitions/{id}`
  - Query: `lang`（可选；仅显式 `?lang=xx` 才单语）
  - Resp（多语模式，未传 `lang`，字段节选）：
    ```json
    {
      "data": {
        "fields": [
          { "propertyName": "Code", "displayNameKey": "LBL_FIELD_CODE" },
          { "propertyName": "CustomField", "displayNameTranslations": { "zh": "自定义字段", "en": "Custom Field" } }
        ]
      }
    }
    ```
  - Resp（单语模式，`?lang=zh`，字段节选）：
    ```json
    {
      "data": {
        "fields": [
          { "propertyName": "Code", "displayNameKey": "LBL_FIELD_CODE", "displayName": "编码" },
          { "propertyName": "CustomField", "displayName": "自定义字段" }
        ]
      }
    }
    ```

## 枚举定义（ARCH-30）

- 枚举列表/详情（忽略 `Accept-Language`）
  - GET `/api/enums?lang=zh`
  - GET `/api/enums/{id}?lang=zh`
  - GET `/api/enums/by-code/{code}?lang=zh`
  - GET `/api/enums/{id}/options?lang=zh`
  - Query: `lang`（可选；仅显式 `?lang=xx` 才单语）
  - Resp（多语模式，未传 `lang`，节选）：
    ```json
    { "displayNameTranslations": { "zh": "状态", "en": "Status" }, "options": [ { "displayNameTranslations": { "zh": "启用", "en": "Enabled" } } ] }
    ```
  - Resp（单语模式，`?lang=zh`，节选）：
    ```json
    { "displayName": "状态", "options": [ { "displayName": "启用" } ] }
    ```

## 实体域（ARCH-30）

- 域列表/详情（忽略 `Accept-Language`）
  - GET `/api/entity-domains?lang=zh`
  - GET `/api/entity-domains/{id}?lang=zh`
  - Query: `lang`（可选；仅显式 `?lang=xx` 才单语）
  - Resp（多语模式，未传 `lang`，节选）：
    ```json
    [ { "code": "core", "nameTranslations": { "zh": "核心", "en": "Core" } } ]
    ```
  - Resp（单语模式，`?lang=en`，节选）：
    ```json
    [ { "code": "core", "name": "Core" } ]
    ```

## 动态实体查询（ARCH-30）

- 查询列表（新增 `meta.fields`）
  - POST `/api/dynamic-entities/{fullTypeName}/query`
  - Query: `lang`（可选；仅显式 `?lang=xx` 才单语，未传 `lang` 忽略 `Accept-Language`）
  - Resp（多语模式，未传 `lang`，节选）：
    ```json
    {
      "meta": {
        "fields": [
          { "propertyName": "Code", "displayNameKey": "LBL_FIELD_CODE" },
          { "propertyName": "CustomField", "displayNameTranslations": { "zh": "自定义字段", "en": "Custom Field" } }
        ]
      },
      "data": [],
      "total": 0,
      "page": 1,
      "pageSize": 100
    }
    ```
  - Resp（单语模式，`?lang=zh`，节选）：
    ```json
    { "meta": { "fields": [ { "propertyName": "Code", "displayNameKey": "LBL_FIELD_CODE", "displayName": "编码" } ] } }
    ```

- 根据 ID 获取（向后兼容：默认不返回 `meta`）
  - GET `/api/dynamic-entities/{fullTypeName}/{id}`
  - Query: `lang`（可选）、`includeMeta`（可选，默认 `false`）
  - Resp（默认，`includeMeta=false`）：直接返回实体对象，例如：`{ "id": 1, "code": "C001" }`
  - Resp（`includeMeta=true`，节选）：
    ```json
    { "meta": { "fields": [ { "propertyName": "Code", "displayNameKey": "LBL_FIELD_CODE" } ] }, "data": { "id": 1, "code": "C001" } }
    ```
