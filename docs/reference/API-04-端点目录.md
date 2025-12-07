# BobCrm.Api 综合端点目录

## 统计摘要
- 端点文件总数: 16
- HTTP 端点总数: 80+
- 具有匿名返回类型的端点: ~40 (高风险)
- 具有 DTO 的端点: ~20
- 混合/不一致的返回格式: ~15

---

## 1. 访问端点 (ACCESS ENDPOINTS)
文件: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/AccessEndpoints.cs`

### GET /api/access/functions
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Ok)
- **响应结构**:
  ```json
  [
    {
      "id": "guid",
      "parentId": "guid?",
      "code": "string",
      "name": "string",
      "route": "string",
      "icon": "string",
      "isMenu": "boolean",
      "sortOrder": "int",
      "children": []
    }
  ]
  ```
- **认证**: RequireFunction("BAS.AUTH.ROLE.PERM")
- **风险**: 高 - 具有嵌套结构的匿名对象

### GET /api/access/functions/me
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Ok)
- **响应结构**: FunctionNodeDto 列表 (嵌套树结构)
- **认证**: 必需
- **风险**: 中 - 返回 FunctionNodeDto 但与其他端点不一致

### POST /api/access/functions
- **HTTP 方法**: POST
- **请求 DTO**: CreateFunctionRequest
- **返回类型**: FunctionNodeDto (Results.Ok)
- **认证**: RequireFunction("BAS.AUTH.ROLE.PERM")

### GET /api/access/roles
- **HTTP 方法**: GET
- **返回类型**: RoleProfile 对象 (Results.Ok)
- **包含**: Functions, DataScopes
- **认证**: RequireFunction("BAS.AUTH.ROLE.PERM")

### POST /api/access/roles
- **HTTP 方法**: POST
- **请求 DTO**: CreateRoleRequest
- **返回类型**: RoleProfile (Results.Ok)
- **认证**: RequireFunction("BAS.AUTH.ROLE.PERM")

### GET /api/access/roles/{roleId}
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Json)
- **响应结构**:
  ```json
  {
    "id": "guid",
    "functions": [...],
    "datascopes": [...]
  }
  ```
- **风险**: 高 - 匿名对象
- **认证**: RequireFunction("BAS.AUTH.ROLE.PERM")

### PUT /api/access/roles/{roleId}
- **HTTP 方法**: PUT
- **请求 DTO**: UpdateRoleRequest
- **返回类型**: 匿名对象
- **响应**: RoleProfile 实例
- **认证**: RequireFunction("BAS.AUTH.ROLE.PERM")

### DELETE /api/access/roles/{roleId}
- **HTTP 方法**: DELETE
- **返回类型**: Results.NoContent()
- **认证**: RequireFunction("BAS.AUTH.ROLE.PERM")

### PUT /api/access/roles/{roleId}/permissions
- **HTTP 方法**: PUT
- **请求 DTO**: UpdatePermissionsRequest
- **返回类型**: 匿名对象
- **响应**:
  ```json
  {
    "message": "Permissions updated successfully"
  }
  ```
- **风险**: 高 - 匿名对象
- **认证**: RequireFunction("BAS.AUTH.ROLE.PERM")

### POST /api/access/assignments
- **HTTP 方法**: POST
- **请求 DTO**: AssignRoleRequest
- **返回类型**: RoleAssignment 对象
- **认证**: RequireFunction("BAS.AUTH.USER.ROLE")

### GET /api/access/assignments/user/{userId}
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Ok)
- **响应结构**:
  ```json
  [
    {
      "id": "guid",
      "roleId": "guid",
      "roleCode": "string",
      "roleName": "string",
      "organizationId": "guid",
      "validFrom": "datetime?",
      "validTo": "datetime?"
    }
  ]
  ```
- **风险**: 高 - 匿名对象
- **认证**: RequireFunction("BAS.AUTH.USER.ROLE")

### DELETE /api/access/assignments/{assignmentId}
- **HTTP 方法**: DELETE
- **返回类型**: Results.NoContent()
- **认证**: RequireFunction("BAS.AUTH.USER.ROLE")

---

## 2. 管理端点 (ADMIN ENDPOINTS)
文件: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/AdminEndpoints.cs`

### GET /api/admin/db/health
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Json)
- **响应结构**:
  ```json
  {
    "provider": "string",
    "canConnect": "boolean",
    "counts": {
      "customers": "int",
      "fieldDefinitions": "int",
      "fieldValues": "int",
      "userLayouts": "int"
    }
  }
  ```
- **风险**: 高 - 嵌套匿名对象
- **认证**: 无 (调试端点)

### POST /api/admin/db/recreate
- **HTTP 方法**: POST
- **返回类型**: Results.Ok (ApiResponseExtensions.SuccessResponse())
- **风险**: 高 - 匿名对象

### GET /api/debug/users
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Ok)
- **响应结构**:
  ```json
  [
    {
      "id": "string",
      "username": "string",
      "email": "string",
      "emailConfirmed": "boolean",
      "hasPassword": "boolean"
    }
  ]
  ```
- **风险**: 高 - 匿名对象

### POST /api/debug/reset-setup
- **HTTP 方法**: POST
- **返回类型**: 匿名对象 (ApiResponseExtensions.SuccessResponse())
- **风险**: 中

### POST /api/admin/reset-password
- **HTTP 方法**: POST
- **请求 DTO**: ResetPasswordDto
- **返回类型**: 匿名对象
- **响应结构**:
  ```json
  {
    "status": "ok",
    "user": {
      "user": "string",
      "email": "string"
    }
  }
  ```
- **风险**: 高 - 嵌套匿名对象

---

## 3. 认证端点 (AUTH ENDPOINTS)
文件: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/AuthEndpoints.cs`

### POST /api/auth/register
- **HTTP 方法**: POST
- **请求 DTO**: RegisterDto
- **返回类型**: 匿名对象 (ApiResponseExtensions.SuccessResponse())
- **风险**: 中

### GET /api/auth/activate
- **HTTP 方法**: GET
- **参数**: userId, code
- **返回类型**: 匿名对象 (ApiResponseExtensions.SuccessResponse())
- **风险**: 中

### POST /api/auth/login
- **HTTP 方法**: POST
- **请求 DTO**: LoginDto
- **返回类型**: 匿名对象 (Results.Json)
- **响应结构**:
  ```json
  {
    "accessToken": "string",
    "refreshToken": "string",
    "user": {
      "id": "string",
      "username": "string",
      "role": "string"
    }
  }
  ```
- **风险**: 高 - 嵌套匿名对象

### POST /api/auth/refresh
- **HTTP 方法**: POST
- **请求 DTO**: RefreshDto
- **返回类型**: 匿名对象 (Results.Json)
- **响应结构**:
  ```json
  {
    "accessToken": "string",
    "refreshToken": "string"
  }
  ```
- **风险**: 高 - 匿名对象

### POST /api/auth/logout
- **HTTP 方法**: POST
- **请求 DTO**: LogoutDto
- **返回类型**: 匿名对象 (ApiResponseExtensions.SuccessResponse())
- **风险**: 中

### GET /api/auth/session
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Ok)
- **响应结构**:
  ```json
  {
    "valid": "boolean",
    "user": {
      "id": "string",
      "username": "string"
    }
  }
  ```
- **风险**: 高 - 匿名嵌套对象

### GET /api/auth/me
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Ok)
- **响应结构**:
  ```json
  {
    "id": "string",
    "userName": "string",
    "email": "string",
    "emailConfirmed": "boolean",
    "role": "string"
  }
  ```
- **风险**: 高 - 匿名对象

### POST /api/auth/change-password
- **HTTP 方法**: POST
- **请求 DTO**: ChangePasswordDto
- **返回类型**: 匿名对象
- **响应**: 
  ```json
  {
    "message": "密码修改成功"
  }
  ```
- **风险**: 高 - 匿名对象

---

## 4. 客户端点 (CUSTOMER ENDPOINTS)
文件: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/CustomerEndpoints.cs`

### GET /api/customers
- **HTTP 方法**: GET
- **返回类型**: Results.Json (来自 ICustomerQueries.GetList())
- **风险**: 中 - 取决于查询实现
- **认证**: 必需

### GET /api/customers/{id}
- **HTTP 方法**: GET
- **返回类型**: Results.Json (来自 ICustomerQueries.GetDetail())
- **风险**: 中 - 取决于查询实现
- **认证**: 必需

### POST /api/customers
- **HTTP 方法**: POST
- **请求 DTO**: CreateCustomerDto
- **返回类型**: 匿名对象 (Results.Json)
- **响应结构**:
  ```json
  {
    "id": "int",
    "code": "string",
    "name": "string"
  }
  ```
- **风险**: 高 - 匿名对象
- **认证**: 必需

### PUT /api/customers/{id}
- **HTTP 方法**: PUT
- **请求 DTO**: UpdateCustomerDto
- **返回类型**: 匿名对象 (Results.Json)
- **响应结构**:
  ```json
  {
    "status": "success",
    "newVersion": "int"
  }
  ```
- **风险**: 高 - 匿名对象
- **认证**: 必需

### GET /api/customers/{id}/access
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Json)
- **响应结构**:
  ```json
  [
    {
      "userId": "string",
      "canEdit": "boolean"
    }
  ]
  ```
- **风险**: 高 - 匿名对象
- **认证**: 必需 (仅管理员)

### POST /api/customers/{id}/access
- **HTTP 方法**: POST
- **请求 DTO**: AccessUpsertDto
- **返回类型**: 匿名对象 (Results.Ok)
- **风险**: 中

---

## 5. 动态实体端点 (DYNAMIC ENTITY ENDPOINTS)
文件: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/DynamicEntityEndpoints.cs`

### POST /api/dynamic-entities/{fullTypeName}/query
- **HTTP 方法**: POST
- **请求 DTO**: QueryRequest
- **返回类型**: 匿名对象 (Results.Ok)
- **响应结构**:
  ```json
  {
    "data": [],
    "total": "int",
    "page": "int",
    "pageSize": "int"
  }
  ```
- **风险**: 高 - 匿名对象
- **认证**: 必需

### GET /api/dynamic-entities/{fullTypeName}/{id}
- **HTTP 方法**: GET
- **返回类型**: 实体对象 (Results.Ok)
- **风险**: 中 - 动态类型
- **认证**: 必需

### POST /api/dynamic-entities/raw/{tableName}/query
- **HTTP 方法**: POST
- **请求 DTO**: QueryRequest
- **返回类型**: 匿名对象 (Results.Ok)
- **响应结构**:
  ```json
  {
    "data": [],
    "count": "int"
  }
  ```
- **风险**: 高 - 匿名对象
- **认证**: 必需

### POST /api/dynamic-entities/{fullTypeName}
- **HTTP 方法**: POST
- **请求 DTO**: Dictionary<string, object>
- **返回类型**: 实体对象 (Results.Created)
- **风险**: 中 - 动态类型
- **认证**: 必需

### PUT /api/dynamic-entities/{fullTypeName}/{id}
- **HTTP 方法**: PUT
- **请求 DTO**: Dictionary<string, object>
- **返回类型**: 实体对象 (Results.Ok)
- **风险**: 中 - 动态类型
- **认证**: 必需

### DELETE /api/dynamic-entities/{fullTypeName}/{id}
- **HTTP 方法**: DELETE
- **返回类型**: Results.NoContent()
- **认证**: 必需

### POST /api/dynamic-entities/{fullTypeName}/count
- **HTTP 方法**: POST
- **请求 DTO**: CountRequest
- **返回类型**: 匿名对象 (Results.Ok)
- **响应**: 
  ```json
  {
    "count": "int"
  }
  ```
- **风险**: 高 - 匿名对象
- **认证**: 必需

---

## 6. 实体聚合端点 (ENTITY AGGREGATE ENDPOINTS)
文件: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/EntityAggregateEndpoints.cs`

### GET /api/entity-aggregates/{id}
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Ok)
- **响应结构**: 复杂的嵌套结构，包含 master, subEntities, fields
- **风险**: 高 - 复杂的匿名嵌套对象
- **认证**: 必需

### POST /api/entity-aggregates
- **HTTP 方法**: POST
- **请求 DTO**: SaveEntityDefinitionAggregateRequest
- **返回类型**: 匿名对象 (Results.Ok)
- **风险**: 高 - 复杂的匿名嵌套对象
- **认证**: 必需

### POST /api/entity-aggregates/validate
- **HTTP 方法**: POST
- **请求 DTO**: SaveEntityDefinitionAggregateRequest
- **返回类型**: 匿名对象 (Results.Ok)
- **响应结构**:
  ```json
  {
    "isValid": "boolean",
    "message": "string",
    "errors": []
  }
  ```
- **风险**: 高 - 匿名对象
- **认证**: 必需

### DELETE /api/entity-aggregates/sub-entities/{id}
- **HTTP 方法**: DELETE
- **返回类型**: Results.NoContent()
- **认证**: 必需

### GET /api/entity-aggregates/{id}/metadata-preview
- **HTTP 方法**: GET
- **返回类型**: Results.Content (JSON 字符串)
- **风险**: 中 - 原始 JSON 内容
- **认证**: 必需

### GET /api/entity-aggregates/{id}/code-preview
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Ok)
- **响应结构**:
  ```json
  {
    "FileName.cs": "code string"
  }
  ```
- **风险**: 高 - 匿名对象 (字典)
- **认证**: 必需

---

## 7. 实体定义端点 (ENTITY DEFINITION ENDPOINTS)
文件: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/EntityDefinitionEndpoints.cs` (非常大的文件 - ~930 行)

### GET /api/entities
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Json)
- **响应结构**:
  ```json
  [
    {
      "entityType": "string",
      "entityName": "string",
      "displayName": "string",
      "description": "string",
      "apiEndpoint": "string",
      "icon": "string",
      "category": "string",
      "isRootEntity": "boolean"
    }
  ]
  ```
- **风险**: 高 - 匿名对象
- **认证**: 无 (公开)

### GET /api/entities/all
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Json)
- **风险**: 高 - 匿名对象
- **认证**: 必需 (管理员)

### GET /api/entities/{entityRoute}/validate
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Json)
- **响应结构**:
  ```json
  {
    "isValid": "boolean",
    "entityRoute": "string",
    "entity": {}
  }
  ```
- **风险**: 高 - 匿名对象
- **认证**: 无 (公开)

### GET /api/entity-definitions
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Json)
- **响应结构**:
  ```json
  [
    {
      "id": "guid",
      "namespace": "string",
      "entityName": "string",
      "fullTypeName": "string",
      ...
      "fieldCount": "int",
      "interfaces": []
    }
  ]
  ```
- **风险**: 高 - 复杂的匿名对象
- **认证**: 必需

### GET /api/entity-definitions/{id}
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Json)
- **风险**: 高 - 复杂的匿名嵌套对象
- **认证**: 必需

### GET /api/entity-definitions/by-type/{entityType}
- **HTTP 方法**: GET
- **返回类型**: EntityDefinition (Results.Json)
- **风险**: 中
- **认证**: 必需

### GET /api/entity-definitions/{id}/referenced
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Json)
- **响应结构**:
  ```json
  {
    "isReferenced": "boolean",
    "referenceCount": "int",
    "referencedBy": {
      "formTemplates": "int"
    }
  }
  ```
- **风险**: 高 - 匿名对象
- **认证**: 必需

### POST /api/entity-definitions
- **HTTP 方法**: POST
- **请求 DTO**: CreateEntityDefinitionDto
- **返回类型**: EntityDefinition (Results.Created)
- **风险**: 中
- **认证**: 必需

### PUT /api/entity-definitions/{id}
- **HTTP 方法**: PUT
- **请求 DTO**: UpdateEntityDefinitionDto
- **返回类型**: EntityDefinition (Results.Ok)
- **风险**: 中
- **认证**: 必需

### DELETE /api/entity-definitions/{id}
- **HTTP 方法**: DELETE
- **返回类型**: Results.NoContent()
- **认证**: 必需

### POST /api/entity-definitions/{id}/publish
- **HTTP 方法**: POST
- **返回类型**: 匿名对象 (Results.Ok)
- **响应结构**:
  ```json
  {
    "success": "boolean",
    "scriptId": "guid",
    "ddlScript": "string",
    "message": "string"
  }
  ```
- **风险**: 高 - 匿名对象
- **认证**: 必需

### POST /api/entity-definitions/{id}/publish-changes
- **HTTP 方法**: POST
- **返回类型**: 匿名对象 (Results.Ok)
- **响应结构**:
  ```json
  {
    "success": "boolean",
    "scriptId": "guid",
    "ddlScript": "string",
    "changeAnalysis": {...},
    "message": "string"
  }
  ```
- **风险**: 高 - 复杂的匿名嵌套对象
- **认证**: 必需

### GET /api/entity-definitions/{id}/preview-ddl
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Ok)
- **响应结构**:
  ```json
  {
    "entityId": "guid",
    "entityName": "string",
    "status": "string",
    "ddlScript": "string"
  }
  ```
- **风险**: 高 - 匿名对象
- **认证**: 必需

### GET /api/entity-definitions/{id}/ddl-history
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Ok) (数组)
- **响应结构**:
  ```json
  [
    {
      "id": "guid",
      "scriptType": "string",
      "status": "string",
      "createdAt": "datetime",
      "executedAt": "datetime?",
      "createdBy": "string",
      "errorMessage": "string?",
      "scriptPreview": "string"
    }
  ]
  ```
- **风险**: 高 - 匿名对象
- **认证**: 必需

### GET /api/entity-definitions/{id}/generate-code
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Ok)
- **响应结构**:
  ```json
  {
    "entityId": "guid",
    "code": "string",
    "message": "string"
  }
  ```
- **风险**: 高 - 匿名对象
- **认证**: 必需

### POST /api/entity-definitions/{id}/compile
- **HTTP 方法**: POST
- **返回类型**: 匿名对象 (Results.Ok/BadRequest)
- **风险**: 高 - 匿名对象
- **认证**: 必需

### POST /api/entity-definitions/compile-batch
- **HTTP 方法**: POST
- **请求 DTO**: CompileBatchDto
- **返回类型**: 匿名对象 (Results.Ok)
- **风险**: 高 - 匿名对象
- **认证**: 必需

### GET /api/entity-definitions/{id}/validate-code
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Ok)
- **响应结构**:
  ```json
  {
    "isValid": "boolean",
    "errors": [...]
  }
  ```
- **风险**: 高 - 匿名对象
- **认证**: 必需

### GET /api/entity-definitions/loaded-entities
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Ok)
- **响应结构**:
  ```json
  {
    "count": "int",
    "entities": []
  }
  ```
- **风险**: 高 - 匿名对象
- **认证**: 必需

### GET /api/entity-definitions/type-info/{fullTypeName}
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Ok)
- **风险**: 高 - 匿名对象
- **认证**: 必需

### DELETE /api/entity-definitions/loaded-entities/{fullTypeName}
- **HTTP 方法**: DELETE
- **返回类型**: 匿名对象 (Results.Ok)
- **响应**: 
  ```json
  {
    "message": "string"
  }
  ```
- **风险**: 高 - 匿名对象
- **认证**: 必需

### POST /api/entity-definitions/{id}/recompile
- **HTTP 方法**: POST
- **返回类型**: 匿名对象 (Results.Ok)
- **风险**: 高 - 匿名对象
- **认证**: 必需

---

## 8. 字段动作端点 (FIELD ACTION ENDPOINTS)
文件: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/FieldActionEndpoints.cs`

### POST /api/actions/rdp/download
- **HTTP 方法**: POST
- **请求 DTO**: RdpDownloadRequest
- **返回类型**: Results.File (二进制 RDP 文件)
- **风险**: 低 - 文件下载端点
- **认证**: 必需

### POST /api/actions/file/validate
- **HTTP 方法**: POST
- **请求 DTO**: FileValidationRequest
- **返回类型**: 匿名对象 (Results.Ok)
- **响应结构**:
  ```json
  {
    "exists": "boolean",
    "type": "string",
    "size": "long?",
    "extension": "string?",
    "lastModified": "datetime?"
  }
  ```
- **风险**: 高 - 匿名对象
- **认证**: 必需

### POST /api/actions/mailto/generate
- **HTTP 方法**: POST
- **请求 DTO**: MailtoRequest
- **返回类型**: 匿名对象 (Results.Ok)
- **响应**:
  ```json
  {
    "link": "string"
  }
  ```
- **风险**: 高 - 匿名对象
- **认证**: 必需

---

## 9. 文件端点 (FILE ENDPOINTS)
文件: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/FileEndpoints.cs`

### POST /api/files/upload
- **HTTP 方法**: POST
- **请求**: 表单文件上传
- **返回类型**: 匿名对象 (Results.Ok)
- **响应结构**:
  ```json
  {
    "key": "string",
    "url": "string"
  }
  ```
- **风险**: 高 - 匿名对象
- **认证**: 必需

### GET /api/files/{*key}
- **HTTP 方法**: GET
- **返回类型**: Stream
- **风险**: 低
- **认证**: 无 (公开)

### DELETE /api/files/{*key}
- **HTTP 方法**: DELETE
- **返回类型**: Results.NoContent()
- **风险**: 低
- **认证**: 必需

---

## 10. 国际化端点 (I18N ENDPOINTS)
文件: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/I18nEndpoints.cs`

### GET /api/i18n/version
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Json)
- **风险**: 高 - 匿名对象
- **认证**: 无

### GET /api/i18n/resources
- **HTTP 方法**: GET
- **返回类型**: List<LocalizationResource>
- **风险**: 中
- **认证**: 无

### GET /api/i18n/{lang}
- **HTTP 方法**: GET
- **返回类型**: Dictionary<string,string>
- **风险**: 中
- **认证**: 无

### GET /api/i18n/languages
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Json)
- **风险**: 高 - 匿名对象
- **认证**: 无

---

## 11. 布局端点 (LAYOUT ENDPOINTS) [6 个已废弃]
文件: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/LayoutEndpoints.cs`

### GET /api/fields
- **HTTP 方法**: GET
- **返回类型**: Results.Json (IQuery)
- **风险**: 中
- **认证**: 必需

### GET /api/fields/tags
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Json)
- **风险**: 高 - 匿名对象
- **认证**: 必需

### GET /api/layout/{id} [已废弃]
- **HTTP 方法**: GET
- **返回类型**: Results.Json (IQuery)
- **风险**: 中
- **认证**: 必需

### POST /api/layout/{id} [已废弃]
- **HTTP 方法**: POST
- **请求 DTO**: JsonElement
- **返回类型**: 匿名对象 (Results.Ok)
- **风险**: 中
- **认证**: 必需

### DELETE /api/layout/{id} [已废弃]
- **HTTP 方法**: DELETE
- **返回类型**: 匿名对象 (Results.Ok)
- **风险**: 中
- **认证**: 必需

### GET /api/layout
- **HTTP 方法**: GET
- **返回类型**: Results.Json (IQuery)
- **风险**: 中
- **认证**: 必需

### GET /api/layout/customer
- **HTTP 方法**: GET
- **返回类型**: Results.Json (IQuery)
- **风险**: 中
- **认证**: 必需

### POST /api/layout
- **HTTP 方法**: POST
- **请求 DTO**: JsonElement
- **返回类型**: 匿名对象 (Results.Ok)
- **风险**: 中
- **认证**: 必需

### POST /api/layout/customer
- **HTTP 方法**: POST
- **请求 DTO**: JsonElement
- **返回类型**: 匿名对象 (Results.Ok)
- **风险**: 中
- **认证**: 必需

### DELETE /api/layout
- **HTTP 方法**: DELETE
- **返回类型**: 匿名对象 (Results.Ok)
- **风险**: 中
- **认证**: 必需

### DELETE /api/layout/customer
- **HTTP 方法**: DELETE
- **返回类型**: 匿名对象 (Results.Ok)
- **风险**: 中
- **认证**: 必需

### GET /api/layout/entity/{type}
- **HTTP 方法**: GET
- **返回类型**: Results.Json (IQuery)
- **风险**: 中
- **认证**: 必需

### POST /api/layout/entity/{type}
- **HTTP 方法**: POST
- **请求 DTO**: JsonElement
- **返回类型**: 匿名对象 (Results.Ok)
- **风险**: 中
- **认证**: 必需

### DELETE /api/layout/entity/{type}
- **HTTP 方法**: DELETE
- **返回类型**: 匿名对象 (Results.Ok)
- **风险**: 中
- **认证**: 必需

### POST /api/layout/{id}/generate
- **HTTP 方法**: POST
- **请求 DTO**: GenerateLayoutRequest
- **返回类型**: 匿名对象 (Results.Json)
- **风险**: 高 - 匿名对象
- **认证**: 必需

---

## 12. 组织端点 (ORGANIZATION ENDPOINTS)
文件: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/OrganizationEndpoints.cs`

### GET /api/organizations/tree
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Ok)
- **风险**: 中
- **认证**: 必需

### POST /api/organizations
- **HTTP 方法**: POST
- **请求 DTO**: CreateOrganizationRequest
- **返回类型**: 匿名对象 (Results.Ok)
- **风险**: 中
- **认证**: 必需

### PUT /api/organizations/{id}
- **HTTP 方法**: PUT
- **请求 DTO**: UpdateOrganizationRequest
- **返回类型**: 匿名对象 (Results.Ok)
- **风险**: 中
- **认证**: 必需

### DELETE /api/organizations/{id}
- **HTTP 方法**: DELETE
- **返回类型**: Results.Ok
- **风险**: 中
- **认证**: 必需

---

## 13. 设置端点 (SETTINGS ENDPOINTS)
文件: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/SettingsEndpoints.cs`

### GET /api/settings/system
- **HTTP 方法**: GET
- **返回类型**: SystemSettingsDto (Results.Ok)
- **风险**: 低
- **认证**: 必需

### PUT /api/settings/system
- **HTTP 方法**: PUT
- **请求 DTO**: UpdateSystemSettingsRequest
- **返回类型**: SystemSettingsDto (Results.Ok)
- **风险**: 低
- **认证**: 必需

### GET /api/settings/user
- **HTTP 方法**: GET
- **返回类型**: UserSettingsSnapshot (Results.Ok)
- **风险**: 低
- **认证**: 必需

### PUT /api/settings/user
- **HTTP 方法**: PUT
- **请求 DTO**: UpdateUserSettingsRequest
- **返回类型**: UserSettingsSnapshot (Results.Ok)
- **风险**: 低
- **认证**: 必需

---

## 14. 设置端点 (SETUP ENDPOINTS)
文件: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/SetupEndpoints.cs`

### GET /api/setup/admin
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Ok)
- **风险**: 高 - 匿名对象
- **认证**: 无

### POST /api/setup/admin
- **HTTP 方法**: POST
- **请求 DTO**: AdminSetupDto
- **返回类型**: 匿名对象 (Results.Ok)
- **风险**: 中
- **认证**: 无

---

## 15. 模板端点 (TEMPLATE ENDPOINTS)
文件: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/TemplateEndpoints.cs`

### GET /api/templates
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Results.Json)
- **风险**: 高 - 匿名对象
- **认证**: 必需

### GET /api/templates/{id}
- **HTTP 方法**: GET
- **返回类型**: FormTemplate (Results.Json)
- **风险**: 中
- **认证**: 必需

### POST /api/templates
- **HTTP 方法**: POST
- **请求 DTO**: CreateTemplateRequest
- **返回类型**: FormTemplate (Results.Created)
- **风险**: 中
- **认证**: 必需

### PUT /api/templates/{id}
- **HTTP 方法**: PUT
- **请求 DTO**: UpdateTemplateRequest
- **返回类型**: FormTemplate (Results.Ok)
- **风险**: 中
- **认证**: 必需

### DELETE /api/templates/{id}
- **HTTP 方法**: DELETE
- **返回类型**: 匿名对象 (Results.Ok)
- **风险**: 中
- **认证**: 必需

### GET /api/templates/effective/{type}
- **HTTP 方法**: GET
- **返回类型**: FormTemplate (Results.Json)
- **风险**: 中
- **认证**: 必需

### GET /api/templates/bindings/{type}
- **HTTP 方法**: GET
- **返回类型**: TemplateBinding (ToDto)
- **风险**: 中
- **认证**: 必需

### PUT /api/templates/bindings
- **HTTP 方法**: PUT
- **请求 DTO**: UpsertTemplateBindingRequest
- **返回类型**: TemplateBinding (ToDto)
- **风险**: 中
- **认证**: 必需

### POST /api/templates/runtime/{type}
- **HTTP 方法**: POST
- **请求 DTO**: TemplateRuntimeRequest
- **返回类型**: 匿名对象 (Results.Ok)
- **风险**: 高 - 匿名对象
- **认证**: 必需

---

## 16. 用户端点 (USER ENDPOINTS)
文件: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/UserEndpoints.cs`

### GET /api/users
- **HTTP 方法**: GET
- **返回类型**: Mapped (Results.Ok)
- **风险**: 中
- **认证**: 必需

### GET /api/users/{id}
- **HTTP 方法**: GET
- **返回类型**: UserDetailDto (Results.Ok)
- **风险**: 低
- **认证**: 必需

### POST /api/users
- **HTTP 方法**: POST
- **请求 DTO**: CreateUserRequest
- **返回类型**: UserDetailDto (Results.Ok)
- **风险**: 低
- **认证**: 必需

### PUT /api/users/{id}
- **HTTP 方法**: PUT
- **请求 DTO**: UpdateUserRequest
- **返回类型**: UserDetailDto (Results.Ok)
- **风险**: 低
- **认证**: 必需

### PUT /api/users/{id}/roles
- **HTTP 方法**: PUT
- **请求 DTO**: UpdateUserRolesRequest
- **返回类型**: 匿名对象 (Results.Ok)
- **风险**: 高 - 匿名对象
- **认证**: 必需

---

## 17. 实体高级功能控制器 (ENTITY ADVANCED FEATURES CONTROLLER)
文件: `/home/user/bobcrm/src/BobCrm.Api/Controllers/EntityAdvancedFeaturesController.cs`

### GET /api/entity-advanced/{id}/children
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Ok())
- **风险**: 高 - 匿名对象
- **认证**: 必需

### POST /api/entity-advanced/{id}/configure-master-detail
- **HTTP 方法**: POST
- **请求 DTO**: MasterDetailConfigRequest
- **返回类型**: 匿名对象 (Ok())
- **风险**: 高 - 匿名对象
- **认证**: 必需

### POST /api/entity-advanced/{id}/generate-aggvo
- **HTTP 方法**: POST
- **返回类型**: 匿名对象 (Ok())
- **风险**: 高 - 匿名对象
- **认证**: 必需

### POST /api/entity-advanced/{id}/evaluate-migration
- **HTTP 方法**: POST
- **请求 DTO**: List<FieldMetadata>
- **返回类型**: 匿名对象 (Ok())
- **风险**: 高 - 匿名对象
- **认证**: 必需

### GET /api/entity-advanced/master-candidates
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Ok())
- **风险**: 高 - 匿名对象
- **认证**: 必需

### GET /api/entity-advanced/detail-candidates
- **HTTP 方法**: GET
- **返回类型**: 匿名对象 (Ok())
- **风险**: 高 - 匿名对象
- **认证**: 必需
