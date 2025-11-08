# 接口文档（v0.2）

> 本文档与《客户信息管理系统设计文档》配套，描述对外 API 契约。所有变更需先更新本文件。

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
