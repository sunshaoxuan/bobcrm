# 接口文档（v0.4）

> 本文档与《客户信息管理系统设计文档》配套，描述对外 API 契约。所有变更需先更新本文件。
>
> **v0.3 更新**: 补充用户管理、认证补充、设置、初始化等高优先级端点文档
>
> **v0.4 更新**: 补充系统管理、组织管理、数据集、字段权限、实体聚合、管理端点、字段操作等完整端点文档

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

- 获取当前用户信息
  - GET `/api/auth/me`
  - Header: `Authorization: Bearer {JWT}`
  - Resp:
    ```json
    {
      "data": {
        "id": "user-guid",
        "userName": "admin",
        "email": "admin@example.com",
        "emailConfirmed": true,
        "role": "admin"
      }
    }
    ```

- 修改密码
  - POST `/api/auth/change-password`
  - Header: `Authorization: Bearer {JWT}`
  - Body:
    ```json
    {
      "currentPassword": "old-password",
      "newPassword": "new-password"
    }
    ```
  - Resp: `{ "message": "密码修改成功" }`
  - 错误: 400（密码不符合要求）、401（未认证）、404（用户不存在）

## 用户管理

> 需要权限：`BAS.AUTH.USERS`

- 用户列表
  - GET `/api/users`
  - Header: `Authorization: Bearer {JWT}`
  - Resp:
    ```json
    {
      "data": [
        {
          "id": "user-guid",
          "userName": "admin",
          "email": "admin@example.com",
          "emailConfirmed": true,
          "isLocked": false,
          "roles": [{ "roleProfileId": "role-guid", "roleCode": "SYS.ADMIN", "roleName": "系统管理员" }]
        }
      ]
    }
    ```

- 用户详情
  - GET `/api/users/{id}`
  - Header: `Authorization: Bearer {JWT}`
  - Resp:
    ```json
    {
      "data": {
        "id": "user-guid",
        "userName": "admin",
        "email": "admin@example.com",
        "emailConfirmed": true,
        "isLocked": false,
        "phoneNumber": null,
        "twoFactorEnabled": false,
        "lockoutEnd": null,
        "roles": [{ "roleProfileId": "role-guid", "roleCode": "SYS.ADMIN", "roleName": "系统管理员" }]
      }
    }
    ```
  - 错误: 404（用户不存在）

- 创建用户
  - POST `/api/users`
  - Header: `Authorization: Bearer {JWT}`
  - Body:
    ```json
    {
      "userName": "newuser",
      "email": "newuser@example.com",
      "password": "Password@123",
      "emailConfirmed": true,
      "roles": [{ "roleProfileId": "role-guid" }]
    }
    ```
  - Resp: 同用户详情
  - 错误: 400（验证失败）

- 更新用户
  - PUT `/api/users/{id}`
  - Header: `Authorization: Bearer {JWT}`
  - Body:
    ```json
    {
      "userName": "updatedname",
      "email": "updated@example.com"
    }
    ```
  - Resp: 同用户详情
  - 错误: 400（验证失败）、404（用户不存在）

- 更新用户角色
  - PUT `/api/users/{id}/roles`
  - Header: `Authorization: Bearer {JWT}`
  - 权限: 需要 `BAS.AUTH.USER.ROLE`
  - Body:
    ```json
    {
      "roles": [
        { "roleProfileId": "role-guid-1" },
        { "roleProfileId": "role-guid-2" }
      ]
    }
    ```
  - Resp:
    ```json
    {
      "data": {
        "userId": "user-guid",
        "assignedRoles": ["SYS.ADMIN", "DATA.VIEWER"]
      }
    }
    ```
  - 错误: 404（用户不存在）

## 系统初始化

- 获取管理员信息
  - GET `/api/setup/admin`
  - 认证: 无（匿名访问）
  - Resp:
    ```json
    {
      "data": {
        "exists": true,
        "username": "admin",
        "email": "admin@example.com"
      }
    }
    ```
  - 说明: 用于检查系统是否已初始化管理员账户

- 配置管理员账户
  - POST `/api/setup/admin`
  - 认证: 无（仅首次设置或默认管理员未修改时可用）
  - Body:
    ```json
    {
      "username": "admin",
      "email": "admin@company.com",
      "password": "SecurePassword@123"
    }
    ```
  - Resp: `{ "message": "管理员账户已创建" }` 或 `{ "message": "管理员账户已更新" }`
  - 错误: 400（验证失败）、403（已存在自定义管理员，无法覆盖）

## 系统设置

> 需要权限：管理员角色（admin）

- 获取系统设置
  - GET `/api/settings/system`
  - Header: `Authorization: Bearer {JWT}`
  - Resp:
    ```json
    {
      "data": {
        "companyName": "BobCRM",
        "defaultTheme": "light",
        "defaultPrimaryColor": "#1890ff",
        "defaultLanguage": "zh",
        "defaultHomeRoute": "/dashboard",
        "defaultNavDisplayMode": "icon-text",
        "timeZoneId": "Asia/Shanghai",
        "allowSelfRegistration": false,
        "smtpHost": "smtp.example.com",
        "smtpPort": 587,
        "smtpUsername": "noreply@example.com",
        "smtpEnableSsl": true,
        "smtpFromAddress": "noreply@example.com",
        "smtpDisplayName": "BobCRM",
        "smtpPasswordConfigured": true
      }
    }
    ```

- 更新系统设置
  - PUT `/api/settings/system`
  - Header: `Authorization: Bearer {JWT}`
  - Body: 同上响应结构（可选字段：`smtpPassword`）
  - Resp: 同上

- SMTP 测试
  - POST `/api/settings/system/smtp/test`
  - Header: `Authorization: Bearer {JWT}`
  - Body:
    ```json
    {
      "to": "test@example.com"
    }
    ```
  - Resp: `{ "message": "Sent" }`
  - 错误: 400（收件人必填或 SMTP 未配置）

## 用户设置

- 获取用户设置
  - GET `/api/settings/user`
  - Header: `Authorization: Bearer {JWT}`
  - Resp:
    ```json
    {
      "data": {
        "system": { /* SystemSettingsDto */ },
        "effective": {
          "theme": "dark",
          "primaryColor": "#52c41a",
          "language": "ja",
          "homeRoute": "/customers",
          "navDisplayMode": "icon"
        },
        "overrides": {
          "theme": "dark",
          "primaryColor": null,
          "language": "ja",
          "homeRoute": null,
          "navDisplayMode": "icon"
        }
      }
    }
    ```
  - 说明: `effective` 为生效值（用户覆盖 > 系统默认），`overrides` 为用户自定义值

- 更新用户设置
  - PUT `/api/settings/user`
  - Header: `Authorization: Bearer {JWT}`
  - Body:
    ```json
    {
      "theme": "dark",
      "primaryColor": "#52c41a",
      "language": "ja",
      "homeRoute": "/customers",
      "navDisplayMode": "icon"
    }
    ```
  - Resp: 同上

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
  - Resp:
    ```json
    {
      "success": true,
      "data": { "status": "success", "newVersion": 3 },
      "version": "1.0",
      "timestamp": "2025-12-27T00:00:00Z",
      "traceId": "00-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx-xxxxxxxxxxxxxxxx-00"
    }
    ```
  - 并发：如提供了 `expectedVersion` 且与服务器当前版本不一致，将返回 409 冲突错误：
    ```json
    {
      "success": false,
      "code": "CONCURRENCY_CONFLICT",
      "message": "版本冲突，请刷新后重试",
      "details": { "version": ["版本冲突，请刷新后重试"] },
      "version": "1.0",
      "timestamp": "2025-12-27T00:00:00Z",
      "traceId": "00-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx-xxxxxxxxxxxxxxxx-00"
    }
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

- 数据库配置（说明）
  - 目前未提供 `/api/admin/dbconfig` 管理端点（以配置文件/环境变量为准）
  - 生产环境请参考 `docs/guides/GUIDE-01-部署指南.md`，使用 `ConnectionStrings__Default` 等配置启动

## 认证头

- 需要鉴权的接口统一使用：`Authorization: Bearer {accessToken}`
- 错误响应（统一约定）
  - 结构：
    ```json
    {
      "success": false,
      "code": "VALIDATION_FAILED|BUSINESS_RULE_VIOLATION|PERSISTENCE_ERROR|...",
      "message": "说明（可本地化）",
      "details": { "email": ["邮箱必填"] },
      "version": "1.0",
      "timestamp": "2025-12-27T00:00:00Z",
      "traceId": "00-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx-xxxxxxxxxxxxxxxx-00"
    }
    ```
  - 语义：
    - 业务层失败 → `BUSINESS_RULE_VIOLATION`
    - 通用层失败 → `VALIDATION_FAILED`
    - 持久化失败 → `PERSISTENCE_ERROR`

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

## 系统管理

> 需要权限：根据功能节点配置

### 审计日志

- 审计日志列表
  - GET `/api/system/audit-logs`
  - 权限: `SYS.AUDIT`
  - Query: `page`, `pageSize`, `module?`, `operationType?`, `actor?`, `actorId?`, `fromUtc?`, `toUtc?`
  - Resp:
    ```json
    {
      "data": [
        {
          "id": "guid",
          "module": "User",
          "operationType": "Create",
          "actor": "admin",
          "actorId": "user-guid",
          "targetId": "target-guid",
          "details": "Created user xyz",
          "createdAtUtc": "2025-12-27T00:00:00Z"
        }
      ],
      "page": 1,
      "pageSize": 20,
      "total": 100
    }
    ```

- 审计日志模块列表
  - GET `/api/system/audit-logs/modules`
  - 权限: `SYS.AUDIT`
  - Query: `limit?`（默认 200）
  - Resp: `{ "data": ["User", "Customer", "EntityDefinition", ...] }`

### 后台任务

- 任务列表
  - GET `/api/system/jobs`
  - 权限: `SYS.JOBS`
  - Query: `page`, `pageSize`
  - Resp:
    ```json
    {
      "data": [
        {
          "id": "job-guid",
          "name": "EntityPublish",
          "status": "Running|Completed|Failed|Cancelled",
          "progress": 75,
          "startedAt": "2025-12-27T00:00:00Z",
          "completedAt": null
        }
      ],
      "page": 1,
      "pageSize": 20,
      "total": 50
    }
    ```

- 任务详情
  - GET `/api/system/jobs/{id}`
  - 权限: `SYS.JOBS`
  - Resp: 同上单个任务对象
  - 错误: 404（任务不存在）

- 任务日志
  - GET `/api/system/jobs/{id}/logs`
  - 权限: `SYS.JOBS`
  - Query: `limit?`（默认 500）
  - Resp:
    ```json
    {
      "data": [
        { "timestamp": "2025-12-27T00:00:00Z", "level": "Info", "message": "Starting..." }
      ]
    }
    ```

- 取消任务
  - POST `/api/system/jobs/{id}/cancel`
  - 权限: `SYS.JOBS`
  - Resp: `{ "message": "任务取消请求已提交" }`
  - 错误: 400（任务不可取消）

### I18n 资源管理

- 资源列表
  - GET `/api/system/i18n`
  - 权限: `SYS.I18N`
  - Query: `page`, `pageSize`, `key?`, `culture?`
  - Resp:
    ```json
    {
      "data": [
        { "key": "LBL_CUSTOMER", "zh": "客户", "ja": "顧客", "en": "Customer" }
      ],
      "page": 1,
      "pageSize": 20,
      "total": 1000
    }
    ```

- 保存资源
  - POST `/api/system/i18n`
  - 权限: `SYS.I18N`
  - Body:
    ```json
    {
      "key": "LBL_NEW_KEY",
      "translations": { "zh": "新键", "en": "New Key" },
      "force": false
    }
    ```
  - Resp: `{ "message": "保存成功" }`
  - 错误: 400（受保护的键）

- 刷新缓存
  - POST `/api/system/i18n/reload`
  - 权限: `SYS.I18N`
  - Resp: `{ "message": "缓存已刷新" }`

### 系统信息

- 运行信息
  - GET `/api/system/info`
  - 权限: `SYS.ADMIN`
  - Resp:
    ```json
    {
      "data": {
        "startedAtUtc": "2025-12-27T00:00:00Z",
        "workingSetBytes": 512000000,
        "version": "1.0.0",
        "dbProvider": "Npgsql.EntityFrameworkCore.PostgreSQL"
      }
    }
    ```

## 组织管理

- 组织树
  - GET `/api/organizations/tree`
  - Header: `Authorization: Bearer {JWT}`
  - Resp:
    ```json
    {
      "data": [
        {
          "id": "org-guid",
          "parentId": null,
          "code": "ROOT",
          "name": "总公司",
          "level": 0,
          "pathCode": "ROOT",
          "children": [
            { "id": "child-guid", "parentId": "org-guid", "code": "SALES", "name": "销售部", "level": 1, "pathCode": "ROOT.SALES", "children": [] }
          ]
        }
      ]
    }
    ```

- 创建组织
  - POST `/api/organizations`
  - Header: `Authorization: Bearer {JWT}`
  - Body:
    ```json
    {
      "parentId": "parent-guid",
      "code": "NEW_ORG",
      "name": "新部门"
    }
    ```
  - Resp: 同组织节点对象

- 更新组织
  - PUT `/api/organizations/{id}`
  - Header: `Authorization: Bearer {JWT}`
  - Body: `{ "name": "更新后名称" }`
  - Resp: 同组织节点对象

- 删除组织
  - DELETE `/api/organizations/{id}`
  - Header: `Authorization: Bearer {JWT}`
  - Resp: `{ "message": "删除成功" }`

## Lookup 解析

- 解析外键
  - POST `/api/lookups/resolve`
  - Header: `Authorization: Bearer {JWT}`
  - Body:
    ```json
    {
      "target": "customers",
      "ids": ["id1", "id2", "id3"],
      "displayField": "name"
    }
    ```
  - Resp:
    ```json
    {
      "data": {
        "id1": "客户A",
        "id2": "客户B",
        "id3": "客户C"
      }
    }
    ```
  - 说明: 批量解析外键 ID 为显示名称，最多 2000 个

## 数据集

- 数据集列表
  - GET `/api/datasets`
  - Header: `Authorization: Bearer {JWT}`
  - Resp:
    ```json
    {
      "data": [
        {
          "id": 1,
          "code": "DS_CUSTOMERS",
          "name": "客户数据集",
          "description": "查询所有客户",
          "sourceType": "Entity",
          "entityType": "customers"
        }
      ]
    }
    ```

- 数据集详情
  - GET `/api/datasets/{id}`
  - GET `/api/datasets/by-code/{code}`
  - Header: `Authorization: Bearer {JWT}`
  - Resp: 同上单个数据集对象
  - 错误: 404（数据集不存在）

- 创建数据集
  - POST `/api/datasets`
  - Header: `Authorization: Bearer {JWT}`
  - Body:
    ```json
    {
      "code": "DS_NEW",
      "name": "新数据集",
      "description": "描述",
      "sourceType": "Entity",
      "entityType": "customers",
      "queryDefinition": { "filters": [], "sorts": [] }
    }
    ```
  - Resp: 201 Created，返回数据集对象

- 更新数据集
  - PUT `/api/datasets/{id}`
  - Header: `Authorization: Bearer {JWT}`
  - Body: 同创建请求
  - Resp: 更新后的数据集对象

- 删除数据集
  - DELETE `/api/datasets/{id}`
  - Header: `Authorization: Bearer {JWT}`
  - Resp: `{ "message": "删除成功" }`

- 执行数据集查询
  - POST `/api/datasets/{id}/execute`
  - Header: `Authorization: Bearer {JWT}`
  - Body:
    ```json
    {
      "parameters": { "status": "Active" },
      "page": 1,
      "pageSize": 50
    }
    ```
  - Resp:
    ```json
    {
      "data": {
        "data": [...],
        "total": 100,
        "page": 1,
        "pageSize": 50
      }
    }
    ```

- 数据集字段元数据
  - GET `/api/datasets/{id}/fields`
  - Header: `Authorization: Bearer {JWT}`
  - Resp:
    ```json
    {
      "data": [
        { "name": "Id", "type": "Guid", "nullable": false },
        { "name": "Code", "type": "String", "nullable": false },
        { "name": "Name", "type": "String", "nullable": true }
      ]
    }
    ```

## 字段权限

> 字段级别的读写权限控制

### 角色字段权限

- 获取角色字段权限
  - GET `/api/field-permissions/role/{roleId}`
  - Header: `Authorization: Bearer {JWT}`
  - Resp:
    ```json
    {
      "data": [
        {
          "id": 1,
          "roleId": "role-guid",
          "entityType": "customers",
          "fieldName": "Phone",
          "canRead": true,
          "canWrite": false
        }
      ]
    }
    ```

- 获取角色在实体的字段权限
  - GET `/api/field-permissions/role/{roleId}/entity/{entityType}`
  - Header: `Authorization: Bearer {JWT}`
  - Resp: 同上

- 创建/更新字段权限
  - POST `/api/field-permissions/role/{roleId}/entity/{entityType}/field/{fieldName}`
  - Header: `Authorization: Bearer {JWT}`
  - Body:
    ```json
    {
      "canRead": true,
      "canWrite": false,
      "remarks": "只读权限"
    }
    ```
  - Resp: 返回字段权限对象

- 批量设置字段权限
  - POST `/api/field-permissions/role/{roleId}/entity/{entityType}/bulk`
  - Header: `Authorization: Bearer {JWT}`
  - Body:
    ```json
    {
      "permissions": [
        { "fieldName": "Phone", "canRead": true, "canWrite": false },
        { "fieldName": "Email", "canRead": true, "canWrite": true }
      ]
    }
    ```
  - Resp: `{ "message": "批量设置成功" }`

- 删除字段权限
  - DELETE `/api/field-permissions/{permissionId}`
  - DELETE `/api/field-permissions/role/{roleId}`（删除角色所有权限）
  - Header: `Authorization: Bearer {JWT}`
  - Resp: `{ "message": "删除成功" }`

### 用户字段权限检查

- 获取用户字段权限
  - GET `/api/field-permissions/user/entity/{entityType}/field/{fieldName}`
  - Header: `Authorization: Bearer {JWT}`
  - Resp: 返回权限对象或 null

- 检查读权限
  - GET `/api/field-permissions/user/entity/{entityType}/field/{fieldName}/can-read`
  - Header: `Authorization: Bearer {JWT}`
  - Resp: `{ "data": { "allowed": true } }`

- 检查写权限
  - GET `/api/field-permissions/user/entity/{entityType}/field/{fieldName}/can-write`
  - Header: `Authorization: Bearer {JWT}`
  - Resp: `{ "data": { "allowed": false } }`

- 获取可读字段列表
  - GET `/api/field-permissions/user/entity/{entityType}/readable-fields`
  - Header: `Authorization: Bearer {JWT}`
  - Resp: `{ "data": ["Id", "Code", "Name", "Phone"] }`

- 获取可写字段列表
  - GET `/api/field-permissions/user/entity/{entityType}/writable-fields`
  - Header: `Authorization: Bearer {JWT}`
  - Resp: `{ "data": ["Name", "Email"] }`

## 实体聚合

> 管理主实体与子实体的聚合关系（Master-Detail）

- 获取实体聚合
  - GET `/api/entity-aggregates/{id}`
  - Header: `Authorization: Bearer {JWT}`
  - Resp:
    ```json
    {
      "data": {
        "master": {
          "id": "entity-guid",
          "namespace": "Core",
          "entityName": "Order",
          "displayName": "订单",
          "status": "Published"
        },
        "subEntities": [
          {
            "id": "sub-guid",
            "code": "OrderItem",
            "displayName": "订单明细",
            "sortOrder": 1,
            "foreignKeyField": "OrderId",
            "fields": [
              { "id": "field-guid", "propertyName": "ProductId", "dataType": "Guid" }
            ]
          }
        ]
      }
    }
    ```
  - 错误: 404（实体不存在）

- 保存实体聚合
  - POST `/api/entity-aggregates`
  - Header: `Authorization: Bearer {JWT}`
  - Body: 包含 master 和 subEntities 的完整聚合对象
  - Resp: 保存后的聚合对象
  - 说明: 同时生成子实体代码和发布元数据

- 验证实体聚合
  - POST `/api/entity-aggregates/validate`
  - Header: `Authorization: Bearer {JWT}`
  - Body: 同保存请求
  - Resp:
    ```json
    {
      "data": {
        "isValid": true,
        "message": "验证通过"
      }
    }
    ```
    或
    ```json
    {
      "data": {
        "isValid": false,
        "errors": [
          { "propertyPath": "SubEntities[0].Code", "message": "编码重复" }
        ]
      }
    }
    ```

- 删除子实体
  - DELETE `/api/entity-aggregates/sub-entities/{id}`
  - Header: `Authorization: Bearer {JWT}`
  - Resp: `{ "message": "删除成功" }`

- 预览元数据
  - GET `/api/entity-aggregates/{id}/metadata-preview`
  - Header: `Authorization: Bearer {JWT}`
  - Resp: JSON 格式的元数据预览

- 预览生成代码
  - GET `/api/entity-aggregates/{id}/code-preview`
  - Header: `Authorization: Bearer {JWT}`
  - Resp:
    ```json
    {
      "data": {
        "OrderItem.cs": "public class OrderItem { ... }",
        "OrderAggVo.cs": "public class OrderAggVo { ... }"
      }
    }
    ```

## 管理端点

> 仅限开发环境使用

### 数据库管理

- 数据库健康检查
  - GET `/api/admin/db/health`
  - Resp:
    ```json
    {
      "data": {
        "provider": "Npgsql.EntityFrameworkCore.PostgreSQL",
        "canConnect": true,
        "counts": {
          "customers": 100,
          "fieldDefinitions": 50,
          "fieldValues": 500,
          "userLayouts": 10
        }
      }
    }
    ```

- 重建数据库
  - POST `/api/admin/db/recreate`
  - Resp: `{ "message": "数据库已重建" }`
  - ⚠️ 危险操作：删除所有数据

### 密码管理

- 重置管理员密码
  - POST `/api/admin/reset-password`
  - Body: `{ "newPassword": "NewPassword@123" }`
  - Resp: `{ "message": "密码重置成功" }`
  - 错误: 404（管理员不存在）

### 模板管理

- 重新生成所有默认模板
  - POST `/api/admin/templates/regenerate-defaults`
  - Resp:
    ```json
    {
      "data": {
        "entities": 10,
        "updated": 20,
        "message": "默认模板已重新生成"
      }
    }
    ```

- 重新生成指定实体模板
  - POST `/api/admin/templates/{entityRoute}/regenerate`
  - Resp:
    ```json
    {
      "data": {
        "entity": "customers",
        "created": 2,
        "updated": 1
      }
    }
    ```

- 重置指定实体模板
  - POST `/api/admin/templates/{entityRoute}/reset`
  - Resp: 包含删除和创建的模板详情
  - ⚠️ 危险操作：删除所有模板后重建

- 重置所有模板
  - POST `/api/admin/templates/reset-all`
  - Resp: 包含所有实体的重置详情
  - ⚠️ 危险操作：删除所有模板后重建

## 字段操作

> 字段特殊操作（RDP、文件、邮件）

- RDP 文件下载
  - POST `/api/actions/rdp/download`
  - Header: `Authorization: Bearer {JWT}`
  - Body:
    ```json
    {
      "host": "192.168.1.100",
      "port": 3389,
      "username": "admin",
      "password": "secret",
      "domain": "DOMAIN",
      "width": 1920,
      "height": 1080,
      "redirectDrives": false,
      "redirectClipboard": true
    }
    ```
  - Resp: `.rdp` 文件下载
  - 错误: 400（主机必填）

- 文件路径验证
  - POST `/api/actions/file/validate`
  - Header: `Authorization: Bearer {JWT}`
  - Body: `{ "path": "C:\\Users\\file.txt" }`
  - Resp:
    ```json
    {
      "data": {
        "exists": true,
        "type": "file",
        "size": 1024,
        "extension": ".txt",
        "lastModified": "2025-12-27T00:00:00Z"
      }
    }
    ```

- 生成邮件链接
  - POST `/api/actions/mailto/generate`
  - Header: `Authorization: Bearer {JWT}`
  - Body:
    ```json
    {
      "email": "user@example.com",
      "subject": "主题",
      "body": "正文内容",
      "cc": "cc@example.com",
      "bcc": "bcc@example.com"
    }
    ```
  - Resp: `{ "data": { "link": "mailto:user%40example.com?subject=..." } }`
