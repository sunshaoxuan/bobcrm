# Comprehensive Endpoint Catalog for BobCrm.Api

## Summary Statistics
- Total Endpoint Files: 16
- Total HTTP Endpoints: 80+
- Endpoints with Anonymous Return Types: ~40 (HIGH RISK)
- Endpoints with DTOs: ~20
- Mixed/Inconsistent Return Formats: ~15

---

## 1. ACCESS ENDPOINTS
File: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/AccessEndpoints.cs`

### GET /api/access/functions
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Ok()
- **Response Structure**:
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
- **Authentication**: RequireFunction("BAS.AUTH.ROLE.PERM")
- **Risk**: HIGH - Anonymous object with nested structure

### GET /api/access/functions/me
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Ok()
- **Response Structure**: List of FunctionNodeDto (nested tree structure)
- **Authentication**: Required
- **Risk**: MEDIUM - Returns FunctionNodeDto but inconsistent with other endpoints

### POST /api/access/functions
- **HTTP Method**: POST
- **Request DTO**: CreateFunctionRequest
- **Return Type**: FunctionNodeDto (Results.Ok)
- **Authentication**: RequireFunction("BAS.AUTH.ROLE.PERM")

### GET /api/access/roles
- **HTTP Method**: GET
- **Return Type**: RoleProfile objects (Results.Ok)
- **Includes**: Functions, DataScopes
- **Authentication**: RequireFunction("BAS.AUTH.ROLE.PERM")

### POST /api/access/roles
- **HTTP Method**: POST
- **Request DTO**: CreateRoleRequest
- **Return Type**: RoleProfile (Results.Ok)
- **Authentication**: RequireFunction("BAS.AUTH.ROLE.PERM")

### GET /api/access/roles/{roleId}
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Json
- **Response Structure**:
  ```json
  {
    "id": "guid",
    "functions": [...],
    "datascopes": [...]
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: RequireFunction("BAS.AUTH.ROLE.PERM")

### PUT /api/access/roles/{roleId}
- **HTTP Method**: PUT
- **Request DTO**: UpdateRoleRequest
- **Return Type**: ANONYMOUS OBJECT
- **Response**: RoleProfile instance
- **Authentication**: RequireFunction("BAS.AUTH.ROLE.PERM")

### DELETE /api/access/roles/{roleId}
- **HTTP Method**: DELETE
- **Return Type**: Results.NoContent()
- **Authentication**: RequireFunction("BAS.AUTH.ROLE.PERM")

### PUT /api/access/roles/{roleId}/permissions
- **HTTP Method**: PUT
- **Request DTO**: UpdatePermissionsRequest
- **Return Type**: ANONYMOUS OBJECT
- **Response**:
  ```json
  {
    "message": "Permissions updated successfully"
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: RequireFunction("BAS.AUTH.ROLE.PERM")

### POST /api/access/assignments
- **HTTP Method**: POST
- **Request DTO**: AssignRoleRequest
- **Return Type**: RoleAssignment object
- **Authentication**: RequireFunction("BAS.AUTH.USER.ROLE")

### GET /api/access/assignments/user/{userId}
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Response Structure**:
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
- **Risk**: HIGH - Anonymous object
- **Authentication**: RequireFunction("BAS.AUTH.USER.ROLE")

### DELETE /api/access/assignments/{assignmentId}
- **HTTP Method**: DELETE
- **Return Type**: Results.NoContent()
- **Authentication**: RequireFunction("BAS.AUTH.USER.ROLE")

---

## 2. ADMIN ENDPOINTS
File: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/AdminEndpoints.cs`

### GET /api/admin/db/health
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Json
- **Response Structure**:
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
- **Risk**: HIGH - Nested anonymous objects
- **Authentication**: None (Debug endpoint)

### POST /api/admin/db/recreate
- **HTTP Method**: POST
- **Return Type**: Results.Ok via ApiResponseExtensions.SuccessResponse()
- **Risk**: HIGH - Anonymous object

### GET /api/debug/users
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Response Structure**:
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
- **Risk**: HIGH - Anonymous object

### POST /api/debug/reset-setup
- **HTTP Method**: POST
- **Return Type**: ANONYMOUS OBJECT via ApiResponseExtensions.SuccessResponse()
- **Risk**: MEDIUM

### POST /api/admin/reset-password
- **HTTP Method**: POST
- **Request DTO**: ResetPasswordDto
- **Return Type**: ANONYMOUS OBJECT
- **Response Structure**:
  ```json
  {
    "status": "ok",
    "user": {
      "user": "string",
      "email": "string"
    }
  }
  ```
- **Risk**: HIGH - Nested anonymous objects

---

## 3. AUTH ENDPOINTS
File: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/AuthEndpoints.cs`

### POST /api/auth/register
- **HTTP Method**: POST
- **Request DTO**: RegisterDto
- **Return Type**: ANONYMOUS OBJECT via ApiResponseExtensions.SuccessResponse()
- **Risk**: MEDIUM

### GET /api/auth/activate
- **HTTP Method**: GET
- **Parameters**: userId, code
- **Return Type**: ANONYMOUS OBJECT via ApiResponseExtensions.SuccessResponse()
- **Risk**: MEDIUM

### POST /api/auth/login
- **HTTP Method**: POST
- **Request DTO**: LoginDto
- **Return Type**: ANONYMOUS OBJECT via Results.Json
- **Response Structure**:
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
- **Risk**: HIGH - Nested anonymous objects

### POST /api/auth/refresh
- **HTTP Method**: POST
- **Request DTO**: RefreshDto
- **Return Type**: ANONYMOUS OBJECT via Results.Json
- **Response Structure**:
  ```json
  {
    "accessToken": "string",
    "refreshToken": "string"
  }
  ```
- **Risk**: HIGH - Anonymous object

### POST /api/auth/logout
- **HTTP Method**: POST
- **Request DTO**: LogoutDto
- **Return Type**: ANONYMOUS OBJECT via ApiResponseExtensions.SuccessResponse()
- **Risk**: MEDIUM

### GET /api/auth/session
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Response Structure**:
  ```json
  {
    "valid": "boolean",
    "user": {
      "id": "string",
      "username": "string"
    }
  }
  ```
- **Risk**: HIGH - Anonymous nested object

### GET /api/auth/me
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Response Structure**:
  ```json
  {
    "id": "string",
    "userName": "string",
    "email": "string",
    "emailConfirmed": "boolean",
    "role": "string"
  }
  ```
- **Risk**: HIGH - Anonymous object

### POST /api/auth/change-password
- **HTTP Method**: POST
- **Request DTO**: ChangePasswordDto
- **Return Type**: ANONYMOUS OBJECT
- **Response**: 
  ```json
  {
    "message": "密码修改成功"
  }
  ```
- **Risk**: HIGH - Anonymous object

---

## 4. CUSTOMER ENDPOINTS
File: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/CustomerEndpoints.cs`

### GET /api/customers
- **HTTP Method**: GET
- **Return Type**: Results.Json (from ICustomerQueries.GetList())
- **Risk**: MEDIUM - Depends on query implementation
- **Authentication**: Required

### GET /api/customers/{id}
- **HTTP Method**: GET
- **Return Type**: Results.Json (from ICustomerQueries.GetDetail())
- **Risk**: MEDIUM - Depends on query implementation
- **Authentication**: Required

### POST /api/customers
- **HTTP Method**: POST
- **Request DTO**: CreateCustomerDto
- **Return Type**: ANONYMOUS OBJECT via Results.Json
- **Response Structure**:
  ```json
  {
    "id": "int",
    "code": "string",
    "name": "string"
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

### PUT /api/customers/{id}
- **HTTP Method**: PUT
- **Request DTO**: UpdateCustomerDto
- **Return Type**: ANONYMOUS OBJECT via Results.Json
- **Response Structure**:
  ```json
  {
    "status": "success",
    "newVersion": "int"
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

### GET /api/customers/{id}/access
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Json
- **Response Structure**:
  ```json
  [
    {
      "userId": "string",
      "canEdit": "boolean"
    }
  ]
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required (admin only)

### POST /api/customers/{id}/access
- **HTTP Method**: POST
- **Request DTO**: AccessUpsertDto
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Risk**: MEDIUM

---

## 5. DYNAMIC ENTITY ENDPOINTS
File: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/DynamicEntityEndpoints.cs`

### POST /api/dynamic-entities/{fullTypeName}/query
- **HTTP Method**: POST
- **Request DTO**: QueryRequest
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Response Structure**:
  ```json
  {
    "data": [],
    "total": "int",
    "page": "int",
    "pageSize": "int"
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

### GET /api/dynamic-entities/{fullTypeName}/{id}
- **HTTP Method**: GET
- **Return Type**: Entity object via Results.Ok
- **Risk**: MEDIUM - Dynamic type
- **Authentication**: Required

### POST /api/dynamic-entities/raw/{tableName}/query
- **HTTP Method**: POST
- **Request DTO**: QueryRequest
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Response Structure**:
  ```json
  {
    "data": [],
    "count": "int"
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

### POST /api/dynamic-entities/{fullTypeName}
- **HTTP Method**: POST
- **Request DTO**: Dictionary<string, object>
- **Return Type**: Entity object via Results.Created
- **Risk**: MEDIUM - Dynamic type
- **Authentication**: Required

### PUT /api/dynamic-entities/{fullTypeName}/{id}
- **HTTP Method**: PUT
- **Request DTO**: Dictionary<string, object>
- **Return Type**: Entity object via Results.Ok
- **Risk**: MEDIUM - Dynamic type
- **Authentication**: Required

### DELETE /api/dynamic-entities/{fullTypeName}/{id}
- **HTTP Method**: DELETE
- **Return Type**: Results.NoContent()
- **Authentication**: Required

### POST /api/dynamic-entities/{fullTypeName}/count
- **HTTP Method**: POST
- **Request DTO**: CountRequest
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Response**: 
  ```json
  {
    "count": "int"
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

---

## 6. ENTITY AGGREGATE ENDPOINTS
File: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/EntityAggregateEndpoints.cs`

### GET /api/entity-aggregates/{id}
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Response Structure**: Complex nested structure with master, subEntities, fields
- **Risk**: HIGH - Complex anonymous nested object
- **Authentication**: Required

### POST /api/entity-aggregates
- **HTTP Method**: POST
- **Request DTO**: SaveEntityDefinitionAggregateRequest
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Risk**: HIGH - Complex anonymous nested object
- **Authentication**: Required

### POST /api/entity-aggregates/validate
- **HTTP Method**: POST
- **Request DTO**: SaveEntityDefinitionAggregateRequest
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Response Structure**:
  ```json
  {
    "isValid": "boolean",
    "message": "string",
    "errors": []
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

### DELETE /api/entity-aggregates/sub-entities/{id}
- **HTTP Method**: DELETE
- **Return Type**: Results.NoContent()
- **Authentication**: Required

### GET /api/entity-aggregates/{id}/metadata-preview
- **HTTP Method**: GET
- **Return Type**: Results.Content (JSON string)
- **Risk**: MEDIUM - Raw JSON content
- **Authentication**: Required

### GET /api/entity-aggregates/{id}/code-preview
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Response Structure**:
  ```json
  {
    "FileName.cs": "code string"
  }
  ```
- **Risk**: HIGH - Anonymous object (dictionary)
- **Authentication**: Required

---

## 7. ENTITY DEFINITION ENDPOINTS
File: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/EntityDefinitionEndpoints.cs` (VERY LARGE FILE - ~930 lines)

### GET /api/entities
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Json
- **Response Structure**:
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
- **Risk**: HIGH - Anonymous object
- **Authentication**: None (public)

### GET /api/entities/all
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Json
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required (admin)

### GET /api/entities/{entityRoute}/validate
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Json
- **Response Structure**:
  ```json
  {
    "isValid": "boolean",
    "entityRoute": "string",
    "entity": {}
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: None (public)

### GET /api/entity-definitions
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Json
- **Response Structure**:
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
- **Risk**: HIGH - Complex anonymous object
- **Authentication**: Required

### GET /api/entity-definitions/{id}
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Json
- **Risk**: HIGH - Complex anonymous nested object
- **Authentication**: Required

### GET /api/entity-definitions/by-type/{entityType}
- **HTTP Method**: GET
- **Return Type**: EntityDefinition via Results.Json
- **Risk**: MEDIUM
- **Authentication**: Required

### GET /api/entity-definitions/{id}/referenced
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Json
- **Response Structure**:
  ```json
  {
    "isReferenced": "boolean",
    "referenceCount": "int",
    "referencedBy": {
      "formTemplates": "int"
    }
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

### POST /api/entity-definitions
- **HTTP Method**: POST
- **Request DTO**: CreateEntityDefinitionDto
- **Return Type**: EntityDefinition via Results.Created
- **Risk**: MEDIUM
- **Authentication**: Required

### PUT /api/entity-definitions/{id}
- **HTTP Method**: PUT
- **Request DTO**: UpdateEntityDefinitionDto
- **Return Type**: EntityDefinition via Results.Ok
- **Risk**: MEDIUM
- **Authentication**: Required

### DELETE /api/entity-definitions/{id}
- **HTTP Method**: DELETE
- **Return Type**: Results.NoContent()
- **Authentication**: Required

### POST /api/entity-definitions/{id}/publish
- **HTTP Method**: POST
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Response Structure**:
  ```json
  {
    "success": "boolean",
    "scriptId": "guid",
    "ddlScript": "string",
    "message": "string"
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

### POST /api/entity-definitions/{id}/publish-changes
- **HTTP Method**: POST
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Response Structure**:
  ```json
  {
    "success": "boolean",
    "scriptId": "guid",
    "ddlScript": "string",
    "changeAnalysis": {...},
    "message": "string"
  }
  ```
- **Risk**: HIGH - Complex anonymous nested object
- **Authentication**: Required

### GET /api/entity-definitions/{id}/preview-ddl
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Response Structure**:
  ```json
  {
    "entityId": "guid",
    "entityName": "string",
    "status": "string",
    "ddlScript": "string"
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

### GET /api/entity-definitions/{id}/ddl-history
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Ok (array)
- **Response Structure**:
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
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

### GET /api/entity-definitions/{id}/generate-code
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Response Structure**:
  ```json
  {
    "entityId": "guid",
    "code": "string",
    "message": "string"
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

### POST /api/entity-definitions/{id}/compile
- **HTTP Method**: POST
- **Return Type**: ANONYMOUS OBJECT via Results.Ok/BadRequest
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

### POST /api/entity-definitions/compile-batch
- **HTTP Method**: POST
- **Request DTO**: CompileBatchDto
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

### GET /api/entity-definitions/{id}/validate-code
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Response Structure**:
  ```json
  {
    "isValid": "boolean",
    "errors": [...]
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

### GET /api/entity-definitions/loaded-entities
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Response Structure**:
  ```json
  {
    "count": "int",
    "entities": []
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

### GET /api/entity-definitions/type-info/{fullTypeName}
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

### DELETE /api/entity-definitions/loaded-entities/{fullTypeName}
- **HTTP Method**: DELETE
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Response**: 
  ```json
  {
    "message": "string"
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

### POST /api/entity-definitions/{id}/recompile
- **HTTP Method**: POST
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

---

## 8. FIELD ACTION ENDPOINTS
File: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/FieldActionEndpoints.cs`

### POST /api/actions/rdp/download
- **HTTP Method**: POST
- **Request DTO**: RdpDownloadRequest
- **Return Type**: Results.File (binary RDP file)
- **Risk**: LOW - File download endpoint
- **Authentication**: Required

### POST /api/actions/file/validate
- **HTTP Method**: POST
- **Request DTO**: FileValidationRequest
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Response Structure**:
  ```json
  {
    "exists": "boolean",
    "type": "string",
    "size": "long?",
    "extension": "string?",
    "lastModified": "datetime?"
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

### POST /api/actions/mailto/generate
- **HTTP Method**: POST
- **Request DTO**: MailtoRequest
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Response**:
  ```json
  {
    "link": "string"
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

---

## 9. FILE ENDPOINTS
File: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/FileEndpoints.cs`

### POST /api/files/upload
- **HTTP Method**: POST
- **Request**: Form file upload
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Response Structure**:
  ```json
  {
    "key": "string",
    "url": "string"
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required (Authorize)

### GET /api/files/{*key}
- **HTTP Method**: GET
- **Return Type**: Results.Stream (binary content)
- **Risk**: LOW - File download endpoint
- **Authentication**: None

### DELETE /api/files/{*key}
- **HTTP Method**: DELETE
- **Return Type**: Results.NoContent()
- **Authentication**: Required (Authorize)

---

## 10. I18N ENDPOINTS
File: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/I18nEndpoints.cs`

### GET /api/i18n/version
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Json
- **Response**:
  ```json
  {
    "version": "int/string"
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: None

### GET /api/i18n/resources
- **HTTP Method**: GET
- **Return Type**: List<LocalizationResource> via Results.Json
- **Risk**: MEDIUM - Depends on model structure
- **Authentication**: Required
- **Features**: ETag caching, 304 Not Modified

### GET /api/i18n/{lang}
- **HTTP Method**: GET
- **Return Type**: Dictionary<string, string> via Results.Json
- **Risk**: MEDIUM - Dynamic dictionary
- **Authentication**: None
- **Features**: ETag caching, 304 Not Modified

### GET /api/i18n/languages (DUPLICATE ROUTE)
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Json
- **Response Structure**:
  ```json
  [
    {
      "code": "string",
      "name": "string"
    }
  ]
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: None

---

## 11. LAYOUT ENDPOINTS
File: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/LayoutEndpoints.cs` (VERY LARGE - ~680 lines)

### GET /api/fields
- **HTTP Method**: GET
- **Return Type**: Results.Json (from IFieldQueries.GetDefinitions())
- **Risk**: MEDIUM
- **Authentication**: Required

### GET /api/fields/tags
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Json
- **Response Structure**:
  ```json
  [
    {
      "tag": "string",
      "count": "int"
    }
  ]
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

### GET /api/layout/{customerId} [DEPRECATED]
- **HTTP Method**: GET
- **Return Type**: Results.Json (from ILayoutQueries)
- **Risk**: MEDIUM
- **Authentication**: Required

### POST /api/layout/{customerId} [DEPRECATED]
- **HTTP Method**: POST
- **Request**: System.Text.Json.JsonElement
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Risk**: MEDIUM

### DELETE /api/layout/{customerId} [DEPRECATED]
- **HTTP Method**: DELETE
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Risk**: MEDIUM

### GET /api/layout
- **HTTP Method**: GET
- **Return Type**: Results.Json (from ILayoutQueries)
- **Risk**: MEDIUM
- **Authentication**: Required

### GET /api/layout/customer
- **HTTP Method**: GET
- **Return Type**: Results.Json (from ILayoutQueries)
- **Risk**: MEDIUM - Alias endpoint
- **Authentication**: Required

### POST /api/layout
- **HTTP Method**: POST
- **Request**: System.Text.Json.JsonElement
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Risk**: MEDIUM
- **Authentication**: Required

### POST /api/layout/customer
- **HTTP Method**: POST
- **Request**: System.Text.Json.JsonElement
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Risk**: MEDIUM - Alias endpoint
- **Authentication**: Required

### DELETE /api/layout
- **HTTP Method**: DELETE
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Risk**: MEDIUM
- **Authentication**: Required

### DELETE /api/layout/customer
- **HTTP Method**: DELETE
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Risk**: MEDIUM - Alias endpoint
- **Authentication**: Required

### GET /api/layout/entity/{entityType}
- **HTTP Method**: GET
- **Return Type**: Results.Json (from ILayoutQueries)
- **Risk**: MEDIUM
- **Authentication**: Required

### POST /api/layout/entity/{entityType}
- **HTTP Method**: POST
- **Request**: System.Text.Json.JsonElement
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Risk**: MEDIUM
- **Authentication**: Required

### DELETE /api/layout/entity/{entityType}
- **HTTP Method**: DELETE
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Risk**: MEDIUM
- **Authentication**: Required

### POST /api/layout/{customerId}/generate
- **HTTP Method**: POST
- **Request DTO**: GenerateLayoutRequest
- **Return Type**: ANONYMOUS OBJECT via Results.Json
- **Response Structure**:
  ```json
  {
    "mode": "string",
    "items": {...}
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

---

## 12. ORGANIZATION ENDPOINTS
File: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/OrganizationEndpoints.cs`

### GET /api/organizations/tree
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Risk**: MEDIUM - Depends on service implementation
- **Authentication**: Required

### POST /api/organizations
- **HTTP Method**: POST
- **Request DTO**: CreateOrganizationRequest
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Risk**: MEDIUM - Depends on service response
- **Authentication**: Required

### PUT /api/organizations/{id}
- **HTTP Method**: PUT
- **Request DTO**: UpdateOrganizationRequest
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Risk**: MEDIUM
- **Authentication**: Required

### DELETE /api/organizations/{id}
- **HTTP Method**: DELETE
- **Return Type**: Results.Ok()
- **Risk**: MEDIUM
- **Authentication**: Required

---

## 13. SETTINGS ENDPOINTS
File: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/SettingsEndpoints.cs`

### GET /api/settings/system
- **HTTP Method**: GET
- **Return Type**: SystemSettingsDto via Results.Ok
- **Risk**: LOW - Typed DTO
- **Authentication**: Required (admin role)

### PUT /api/settings/system
- **HTTP Method**: PUT
- **Request DTO**: UpdateSystemSettingsRequest
- **Return Type**: SystemSettingsDto via Results.Ok
- **Risk**: LOW - Typed DTO
- **Authentication**: Required (admin role)

### GET /api/settings/user
- **HTTP Method**: GET
- **Return Type**: UserSettingsSnapshot via Results.Ok
- **Risk**: LOW - Typed return
- **Authentication**: Required

### PUT /api/settings/user
- **HTTP Method**: PUT
- **Request DTO**: UpdateUserSettingsRequest
- **Return Type**: UserSettingsSnapshot via Results.Ok
- **Risk**: LOW - Typed return
- **Authentication**: Required

---

## 14. SETUP ENDPOINTS
File: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/SetupEndpoints.cs`

### GET /api/setup/admin
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Response Structure**:
  ```json
  {
    "username": "string",
    "email": "string",
    "exists": "boolean"
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: None (AllowAnonymous)

### POST /api/setup/admin
- **HTTP Method**: POST
- **Request DTO**: AdminSetupDto
- **Return Type**: ANONYMOUS OBJECT via Results.Ok/ApiResponseExtensions
- **Risk**: MEDIUM
- **Authentication**: None (AllowAnonymous)

---

## 15. TEMPLATE ENDPOINTS
File: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/TemplateEndpoints.cs` (~440 lines)

### GET /api/templates
- **HTTP Method**: GET
- **Parameters**: entityType (query), groupBy (query)
- **Return Type**: ANONYMOUS OBJECT via Results.Json
- **Response Structure**: Grouped or flat array depending on groupBy parameter
- **Risk**: HIGH - Anonymous object, inconsistent response structure
- **Authentication**: Required

### GET /api/templates/{id}
- **HTTP Method**: GET
- **Return Type**: FormTemplate via Results.Json
- **Risk**: MEDIUM - Depends on model structure
- **Authentication**: Required

### POST /api/templates
- **HTTP Method**: POST
- **Request DTO**: CreateTemplateRequest
- **Return Type**: FormTemplate via Results.Created
- **Risk**: MEDIUM
- **Authentication**: Required

### PUT /api/templates/{id}
- **HTTP Method**: PUT
- **Request DTO**: UpdateTemplateRequest
- **Return Type**: FormTemplate via Results.Ok
- **Risk**: MEDIUM
- **Authentication**: Required

### DELETE /api/templates/{id}
- **HTTP Method**: DELETE
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Risk**: MEDIUM
- **Authentication**: Required

### GET /api/templates/effective/{entityType}
- **HTTP Method**: GET
- **Return Type**: FormTemplate via Results.Json
- **Risk**: MEDIUM
- **Authentication**: Required

### GET /api/templates/bindings/{entityType}
- **HTTP Method**: GET
- **Parameters**: usageType (query)
- **Return Type**: TemplateBinding (ToDto()) via Results.Ok
- **Risk**: MEDIUM
- **Authentication**: None

### PUT /api/templates/bindings
- **HTTP Method**: PUT
- **Request DTO**: UpsertTemplateBindingRequest
- **Return Type**: TemplateBinding (ToDto()) via Results.Ok
- **Risk**: MEDIUM
- **Authentication**: Required

### POST /api/templates/runtime/{entityType}
- **HTTP Method**: POST
- **Request DTO**: TemplateRuntimeRequest
- **Return Type**: ANONYMOUS OBJECT (runtime context) via Results.Ok
- **Risk**: HIGH - Anonymous object structure depends on service
- **Authentication**: Required

---

## 16. USER ENDPOINTS
File: `/home/user/bobcrm/src/BobCrm.Api/Endpoints/UserEndpoints.cs`

### GET /api/users
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Results.Ok (mapped user summaries)
- **Response Structure**: List of UserSummaryDto
- **Risk**: MEDIUM - Uses DTO, but mapping is inline
- **Authentication**: Required

### GET /api/users/{id}
- **HTTP Method**: GET
- **Return Type**: UserDetailDto via Results.Ok
- **Risk**: LOW - Typed DTO
- **Authentication**: Required

### POST /api/users
- **HTTP Method**: POST
- **Request DTO**: CreateUserRequest
- **Return Type**: UserDetailDto via Results.Ok
- **Risk**: LOW - Typed DTO
- **Authentication**: Required

### PUT /api/users/{id}
- **HTTP Method**: PUT
- **Request DTO**: UpdateUserRequest
- **Return Type**: UserDetailDto via Results.Ok
- **Risk**: LOW - Typed DTO
- **Authentication**: Required

### PUT /api/users/{id}/roles
- **HTTP Method**: PUT
- **Request DTO**: UpdateUserRolesRequest
- **Return Type**: ANONYMOUS OBJECT via Results.Ok
- **Response Structure**:
  ```json
  {
    "success": "boolean",
    "roles": [...]
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: Required

---

## 17. ENTITY ADVANCED FEATURES CONTROLLER
File: `/home/user/bobcrm/src/BobCrm.Api/Controllers/EntityAdvancedFeaturesController.cs`

### GET /api/entity-advanced/{entityId}/children
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Ok()
- **Response Structure**:
  ```json
  {
    "entityId": "guid",
    "entityName": "string",
    "structureType": "string",
    "childCount": "int",
    "children": [...]
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: None (Controller default)

### POST /api/entity-advanced/{entityId}/configure-master-detail
- **HTTP Method**: POST
- **Request DTO**: MasterDetailConfigRequest
- **Return Type**: ANONYMOUS OBJECT via Ok()
- **Response Structure**:
  ```json
  {
    "message": "string",
    "entityId": "guid",
    "structureType": "string"
  }
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: None

### POST /api/entity-advanced/{entityId}/generate-aggvo
- **HTTP Method**: POST
- **Return Type**: ANONYMOUS OBJECT via Ok()
- **Response Structure**:
  ```json
  {
    "entity": "string",
    "aggVOClassName": "string",
    "aggVOCode": "string",
    "voCode": "string",
    "childVOCodes": {...}
  }
  ```
- **Risk**: HIGH - Complex anonymous nested object
- **Authentication**: None

### POST /api/entity-advanced/{entityId}/evaluate-migration
- **HTTP Method**: POST
- **Request DTO**: List<FieldMetadata>
- **Return Type**: ANONYMOUS OBJECT via Ok()
- **Response Structure**:
  ```json
  {
    "entityName": "string",
    "tableName": "string",
    "affectedRows": "int",
    "riskLevel": "string",
    "isSafe": "boolean",
    "operations": [],
    "warnings": [],
    "errors": []
  }
  ```
- **Risk**: HIGH - Complex anonymous nested object
- **Authentication**: None

### GET /api/entity-advanced/master-candidates
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Ok() (array)
- **Response Structure**:
  ```json
  [
    {
      "id": "guid",
      "entityName": "string",
      "fullTypeName": "string",
      "structureType": "string",
      "displayName": "string",
      "currentChildCount": "int"
    }
  ]
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: None

### GET /api/entity-advanced/detail-candidates
- **HTTP Method**: GET
- **Return Type**: ANONYMOUS OBJECT via Ok() (array)
- **Response Structure**:
  ```json
  [
    {
      "id": "guid",
      "entityName": "string",
      "fullTypeName": "string",
      "structureType": "string",
      "displayName": "string",
      "fieldCount": "int"
    }
  ]
  ```
- **Risk**: HIGH - Anonymous object
- **Authentication**: None

---

## SUMMARY OF ISSUES

### HIGH RISK ENDPOINTS (Anonymous Objects - 40+)

**Location Pattern**: Most endpoints use `Results.Json()`, `Results.Ok()` with inline anonymous types

**Top Issues**:
1. AccessEndpoints:
   - GET /api/access/functions - Anonymous tree structure
   - GET /api/access/roles/{roleId} - Anonymous object
   - PUT /api/access/roles/{roleId}/permissions - Anonymous object
   - GET /api/access/assignments/user/{userId} - Anonymous array

2. AuthEndpoints:
   - POST /api/auth/login - Nested anonymous object with user
   - POST /api/auth/refresh - Anonymous token response
   - GET /api/auth/session - Anonymous nested object
   - GET /api/auth/me - Anonymous user object

3. CustomerEndpoints:
   - POST /api/customers - Anonymous id/code/name response
   - PUT /api/customers/{id} - Anonymous status/version response
   - GET /api/customers/{id}/access - Anonymous array of access objects

4. DynamicEntityEndpoints:
   - POST /api/dynamic-entities/{fullTypeName}/query - Anonymous pagination object
   - POST /api/dynamic-entities/{fullTypeName}/count - Anonymous count object

5. EntityDefinitionEndpoints:
   - GET /api/entities - Multiple anonymous objects
   - POST /api/entity-definitions/{id}/publish - Anonymous DDL response
   - Many compilation and code generation endpoints

6. EntityAdvancedFeaturesController:
   - Multiple endpoints returning anonymous objects for master-detail configuration

### RECOMMENDATIONS

1. **Create DTOs for all anonymous returns**:
   - LoginResponseDto
   - TokenRefreshResponseDto
   - EntityListDto
   - PaginationResultDto
   - etc.

2. **Standardize response envelopes**:
   - Use consistent ApiResponse<T> wrapper
   - Include metadata (timestamp, version, etc.)

3. **Deprecated endpoints removal**:
   - LayoutEndpoints has multiple [DEPRECATED] endpoints with old routes
   - Consider removing in next major version

4. **Inconsistent naming**:
   - Some endpoints use `Results.Json()`, others `Results.Ok()`
   - Some use ApiResponseExtensions.SuccessResponse()
   - Should standardize on one pattern

5. **Missing DTOs**:
   - SystemSettingsDto exists, but other similar domains don't
   - Create comprehensive DTO set for each domain

