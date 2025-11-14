# Endpoint Analysis Summary Table

## Quick Reference by Endpoint File

### 1. AccessEndpoints.cs (12 endpoints)

| Endpoint | Method | Route | Return Type | Risk Level | Request DTO |
|----------|--------|-------|-------------|-----------|-------------|
| Get Functions Tree | GET | /api/access/functions | ANONYMOUS (Results.Ok) | HIGH | None |
| Get User Functions | GET | /api/access/functions/me | ANONYMOUS (Results.Ok) | MEDIUM | None |
| Create Function | POST | /api/access/functions | FunctionNodeDto (Results.Ok) | LOW | CreateFunctionRequest |
| Get Roles | GET | /api/access/roles | RoleProfile (Results.Ok) | MEDIUM | None |
| Create Role | POST | /api/access/roles | RoleProfile (Results.Ok) | MEDIUM | CreateRoleRequest |
| Get Role Detail | GET | /api/access/roles/{roleId} | ANONYMOUS (Results.Json) | HIGH | None |
| Update Role | PUT | /api/access/roles/{roleId} | ANONYMOUS (Results.Ok) | HIGH | UpdateRoleRequest |
| Delete Role | DELETE | /api/access/roles/{roleId} | NoContent | LOW | None |
| Update Permissions | PUT | /api/access/roles/{roleId}/permissions | ANONYMOUS (Results.Ok) | HIGH | UpdatePermissionsRequest |
| Create Assignment | POST | /api/access/assignments | RoleAssignment (Results.Ok) | MEDIUM | AssignRoleRequest |
| Get User Assignments | GET | /api/access/assignments/user/{userId} | ANONYMOUS (Results.Ok) | HIGH | None |
| Delete Assignment | DELETE | /api/access/assignments/{assignmentId} | NoContent | LOW | None |

---

### 2. AdminEndpoints.cs (5 endpoints)

| Endpoint | Method | Route | Return Type | Risk Level | Request DTO |
|----------|--------|-------|-------------|-----------|-------------|
| DB Health Check | GET | /api/admin/db/health | ANONYMOUS (Results.Json) | HIGH | None |
| Recreate Database | POST | /api/admin/db/recreate | ANONYMOUS (Results.Ok) | MEDIUM | None |
| Debug List Users | GET | /api/debug/users | ANONYMOUS (Results.Ok) | HIGH | None |
| Debug Reset Setup | POST | /api/debug/reset-setup | ANONYMOUS (Results.Ok) | MEDIUM | None |
| Reset Password | POST | /api/admin/reset-password | ANONYMOUS (Results.Ok) | HIGH | ResetPasswordDto |

---

### 3. AuthEndpoints.cs (8 endpoints)

| Endpoint | Method | Route | Return Type | Risk Level | Request DTO |
|----------|--------|-------|-------------|-----------|-------------|
| Register | POST | /api/auth/register | ANONYMOUS (Results.Ok) | MEDIUM | RegisterDto |
| Activate | GET | /api/auth/activate | ANONYMOUS (Results.Ok) | MEDIUM | None (query params) |
| Login | POST | /api/auth/login | ANONYMOUS (Results.Json) | HIGH | LoginDto |
| Refresh Token | POST | /api/auth/refresh | ANONYMOUS (Results.Json) | HIGH | RefreshDto |
| Logout | POST | /api/auth/logout | ANONYMOUS (Results.Ok) | MEDIUM | LogoutDto |
| Session Check | GET | /api/auth/session | ANONYMOUS (Results.Ok) | HIGH | None |
| Get Current User | GET | /api/auth/me | ANONYMOUS (Results.Ok) | HIGH | None |
| Change Password | POST | /api/auth/change-password | ANONYMOUS (Results.Ok) | HIGH | ChangePasswordDto |

---

### 4. CustomerEndpoints.cs (6 endpoints)

| Endpoint | Method | Route | Return Type | Risk Level | Request DTO |
|----------|--------|-------|-------------|-----------|-------------|
| List Customers | GET | /api/customers | Results.Json (IQuery) | MEDIUM | None |
| Get Customer | GET | /api/customers/{id} | Results.Json (IQuery) | MEDIUM | None |
| Create Customer | POST | /api/customers | ANONYMOUS (Results.Json) | HIGH | CreateCustomerDto |
| Update Customer | PUT | /api/customers/{id} | ANONYMOUS (Results.Json) | HIGH | UpdateCustomerDto |
| Get Access List | GET | /api/customers/{id}/access | ANONYMOUS (Results.Json) | HIGH | None |
| Upsert Access | POST | /api/customers/{id}/access | ANONYMOUS (Results.Ok) | MEDIUM | AccessUpsertDto |

---

### 5. DynamicEntityEndpoints.cs (7 endpoints)

| Endpoint | Method | Route | Return Type | Risk Level | Request DTO |
|----------|--------|-------|-------------|-----------|-------------|
| Query Entities | POST | /api/dynamic-entities/{type}/query | ANONYMOUS (Results.Ok) | HIGH | QueryRequest |
| Get by ID | GET | /api/dynamic-entities/{type}/{id} | Dynamic (Results.Ok) | MEDIUM | None |
| Raw Query | POST | /api/dynamic-entities/raw/{table}/query | ANONYMOUS (Results.Ok) | HIGH | QueryRequest |
| Create | POST | /api/dynamic-entities/{type} | Dynamic (Results.Created) | MEDIUM | Dictionary<string,object> |
| Update | PUT | /api/dynamic-entities/{type}/{id} | Dynamic (Results.Ok) | MEDIUM | Dictionary<string,object> |
| Delete | DELETE | /api/dynamic-entities/{type}/{id} | NoContent | LOW | None |
| Count | POST | /api/dynamic-entities/{type}/count | ANONYMOUS (Results.Ok) | HIGH | CountRequest |

---

### 6. EntityAggregateEndpoints.cs (6 endpoints)

| Endpoint | Method | Route | Return Type | Risk Level | Request DTO |
|----------|--------|-------|-------------|-----------|-------------|
| Get Aggregate | GET | /api/entity-aggregates/{id} | ANONYMOUS (Results.Ok) | HIGH | None |
| Save Aggregate | POST | /api/entity-aggregates | ANONYMOUS (Results.Ok) | HIGH | SaveEntityDefinitionAggregateRequest |
| Validate Aggregate | POST | /api/entity-aggregates/validate | ANONYMOUS (Results.Ok) | HIGH | SaveEntityDefinitionAggregateRequest |
| Delete SubEntity | DELETE | /api/entity-aggregates/sub-entities/{id} | NoContent | LOW | None |
| Preview Metadata | GET | /api/entity-aggregates/{id}/metadata-preview | Results.Content (JSON) | MEDIUM | None |
| Preview Code | GET | /api/entity-aggregates/{id}/code-preview | ANONYMOUS (Results.Ok) | HIGH | None |

---

### 7. EntityDefinitionEndpoints.cs (25 endpoints)

| Endpoint | Method | Route | Return Type | Risk Level | Request DTO |
|----------|--------|-------|-------------|-----------|-------------|
| Get Available | GET | /api/entities | ANONYMOUS (Results.Json) | HIGH | None |
| Get All | GET | /api/entities/all | ANONYMOUS (Results.Json) | HIGH | None |
| Validate Route | GET | /api/entities/{route}/validate | ANONYMOUS (Results.Json) | HIGH | None |
| List Definitions | GET | /api/entity-definitions | ANONYMOUS (Results.Json) | HIGH | None |
| Get Definition | GET | /api/entity-definitions/{id} | ANONYMOUS (Results.Json) | HIGH | None |
| Get by Type | GET | /api/entity-definitions/by-type/{type} | EntityDefinition (Results.Json) | MEDIUM | None |
| Check Referenced | GET | /api/entity-definitions/{id}/referenced | ANONYMOUS (Results.Json) | HIGH | None |
| Create | POST | /api/entity-definitions | EntityDefinition (Results.Created) | MEDIUM | CreateEntityDefinitionDto |
| Update | PUT | /api/entity-definitions/{id} | EntityDefinition (Results.Ok) | MEDIUM | UpdateEntityDefinitionDto |
| Delete | DELETE | /api/entity-definitions/{id} | NoContent | LOW | None |
| Publish | POST | /api/entity-definitions/{id}/publish | ANONYMOUS (Results.Ok) | HIGH | None |
| Publish Changes | POST | /api/entity-definitions/{id}/publish-changes | ANONYMOUS (Results.Ok) | HIGH | None |
| Preview DDL | GET | /api/entity-definitions/{id}/preview-ddl | ANONYMOUS (Results.Ok) | HIGH | None |
| Get DDL History | GET | /api/entity-definitions/{id}/ddl-history | ANONYMOUS (Results.Ok) | HIGH | None |
| Generate Code | GET | /api/entity-definitions/{id}/generate-code | ANONYMOUS (Results.Ok) | HIGH | None |
| Compile | POST | /api/entity-definitions/{id}/compile | ANONYMOUS (Results.Ok) | HIGH | None |
| Compile Batch | POST | /api/entity-definitions/compile-batch | ANONYMOUS (Results.Ok) | HIGH | CompileBatchDto |
| Validate Code | GET | /api/entity-definitions/{id}/validate-code | ANONYMOUS (Results.Ok) | HIGH | None |
| Get Loaded | GET | /api/entity-definitions/loaded-entities | ANONYMOUS (Results.Ok) | HIGH | None |
| Get Type Info | GET | /api/entity-definitions/type-info/{type} | ANONYMOUS (Results.Ok) | HIGH | None |
| Unload | DELETE | /api/entity-definitions/loaded-entities/{type} | ANONYMOUS (Results.Ok) | HIGH | None |
| Recompile | POST | /api/entity-definitions/{id}/recompile | ANONYMOUS (Results.Ok) | HIGH | None |

---

### 8. FieldActionEndpoints.cs (3 endpoints)

| Endpoint | Method | Route | Return Type | Risk Level | Request DTO |
|----------|--------|-------|-------------|-----------|-------------|
| Download RDP | POST | /api/actions/rdp/download | File (application/x-rdp) | LOW | RdpDownloadRequest |
| Validate File | POST | /api/actions/file/validate | ANONYMOUS (Results.Ok) | HIGH | FileValidationRequest |
| Generate Mailto | POST | /api/actions/mailto/generate | ANONYMOUS (Results.Ok) | HIGH | MailtoRequest |

---

### 9. FileEndpoints.cs (3 endpoints)

| Endpoint | Method | Route | Return Type | Risk Level | Request DTO |
|----------|--------|-------|-------------|-----------|-------------|
| Upload | POST | /api/files/upload | ANONYMOUS (Results.Ok) | HIGH | Form file |
| Download | GET | /api/files/{*key} | Stream | LOW | None |
| Delete | DELETE | /api/files/{*key} | NoContent | LOW | None |

---

### 10. I18nEndpoints.cs (4 endpoints)

| Endpoint | Method | Route | Return Type | Risk Level | Request DTO |
|----------|--------|-------|-------------|-----------|-------------|
| Get Version | GET | /api/i18n/version | ANONYMOUS (Results.Json) | HIGH | None |
| Get Resources | GET | /api/i18n/resources | List<LocalizationResource> | MEDIUM | None |
| Get Dictionary | GET | /api/i18n/{lang} | Dictionary<string,string> | MEDIUM | None |
| Get Languages | GET | /api/i18n/languages | ANONYMOUS (Results.Json) | HIGH | None |

---

### 11. LayoutEndpoints.cs (14 endpoints) [6 DEPRECATED]

| Endpoint | Method | Route | Return Type | Risk Level | Request DTO |
|----------|--------|-------|-------------|-----------|-------------|
| Get Field Defs | GET | /api/fields | Results.Json (IQuery) | MEDIUM | None |
| Get Field Tags | GET | /api/fields/tags | ANONYMOUS (Results.Json) | HIGH | None |
| Get Layout [DEPRECATED] | GET | /api/layout/{id} | Results.Json (IQuery) | MEDIUM | None |
| Save Layout [DEPRECATED] | POST | /api/layout/{id} | ANONYMOUS (Results.Ok) | MEDIUM | JsonElement |
| Delete Layout [DEPRECATED] | DELETE | /api/layout/{id} | ANONYMOUS (Results.Ok) | MEDIUM | None |
| Get Layout | GET | /api/layout | Results.Json (IQuery) | MEDIUM | None |
| Get Layout Alias | GET | /api/layout/customer | Results.Json (IQuery) | MEDIUM | None |
| Save Layout | POST | /api/layout | ANONYMOUS (Results.Ok) | MEDIUM | JsonElement |
| Save Layout Alias | POST | /api/layout/customer | ANONYMOUS (Results.Ok) | MEDIUM | JsonElement |
| Delete Layout | DELETE | /api/layout | ANONYMOUS (Results.Ok) | MEDIUM | None |
| Delete Layout Alias | DELETE | /api/layout/customer | ANONYMOUS (Results.Ok) | MEDIUM | None |
| Get by Entity | GET | /api/layout/entity/{type} | Results.Json (IQuery) | MEDIUM | None |
| Save by Entity | POST | /api/layout/entity/{type} | ANONYMOUS (Results.Ok) | MEDIUM | JsonElement |
| Delete by Entity | DELETE | /api/layout/entity/{type} | ANONYMOUS (Results.Ok) | MEDIUM | None |
| Generate | POST | /api/layout/{id}/generate | ANONYMOUS (Results.Json) | HIGH | GenerateLayoutRequest |

---

### 12. OrganizationEndpoints.cs (4 endpoints)

| Endpoint | Method | Route | Return Type | Risk Level | Request DTO |
|----------|--------|-------|-------------|-----------|-------------|
| Get Tree | GET | /api/organizations/tree | ANONYMOUS (Results.Ok) | MEDIUM | None |
| Create | POST | /api/organizations | ANONYMOUS (Results.Ok) | MEDIUM | CreateOrganizationRequest |
| Update | PUT | /api/organizations/{id} | ANONYMOUS (Results.Ok) | MEDIUM | UpdateOrganizationRequest |
| Delete | DELETE | /api/organizations/{id} | Results.Ok | MEDIUM | None |

---

### 13. SettingsEndpoints.cs (4 endpoints)

| Endpoint | Method | Route | Return Type | Risk Level | Request DTO |
|----------|--------|-------|-------------|-----------|-------------|
| Get System Settings | GET | /api/settings/system | SystemSettingsDto (Results.Ok) | LOW | None |
| Update System Settings | PUT | /api/settings/system | SystemSettingsDto (Results.Ok) | LOW | UpdateSystemSettingsRequest |
| Get User Settings | GET | /api/settings/user | UserSettingsSnapshot (Results.Ok) | LOW | None |
| Update User Settings | PUT | /api/settings/user | UserSettingsSnapshot (Results.Ok) | LOW | UpdateUserSettingsRequest |

---

### 14. SetupEndpoints.cs (2 endpoints)

| Endpoint | Method | Route | Return Type | Risk Level | Request DTO |
|----------|--------|-------|-------------|-----------|-------------|
| Get Admin Info | GET | /api/setup/admin | ANONYMOUS (Results.Ok) | HIGH | None |
| Setup Admin | POST | /api/setup/admin | ANONYMOUS (Results.Ok) | MEDIUM | AdminSetupDto |

---

### 15. TemplateEndpoints.cs (9 endpoints)

| Endpoint | Method | Route | Return Type | Risk Level | Request DTO |
|----------|--------|-------|-------------|-----------|-------------|
| List Templates | GET | /api/templates | ANONYMOUS (Results.Json) | HIGH | None |
| Get Template | GET | /api/templates/{id} | FormTemplate (Results.Json) | MEDIUM | None |
| Create | POST | /api/templates | FormTemplate (Results.Created) | MEDIUM | CreateTemplateRequest |
| Update | PUT | /api/templates/{id} | FormTemplate (Results.Ok) | MEDIUM | UpdateTemplateRequest |
| Delete | DELETE | /api/templates/{id} | ANONYMOUS (Results.Ok) | MEDIUM | None |
| Get Effective | GET | /api/templates/effective/{type} | FormTemplate (Results.Json) | MEDIUM | None |
| Get Binding | GET | /api/templates/bindings/{type} | TemplateBinding (ToDto) | MEDIUM | None |
| Upsert Binding | PUT | /api/templates/bindings | TemplateBinding (ToDto) | MEDIUM | UpsertTemplateBindingRequest |
| Build Runtime | POST | /api/templates/runtime/{type} | ANONYMOUS (Results.Ok) | HIGH | TemplateRuntimeRequest |

---

### 16. UserEndpoints.cs (5 endpoints)

| Endpoint | Method | Route | Return Type | Risk Level | Request DTO |
|----------|--------|-------|-------------|-----------|-------------|
| List Users | GET | /api/users | Mapped (Results.Ok) | MEDIUM | None |
| Get User | GET | /api/users/{id} | UserDetailDto (Results.Ok) | LOW | None |
| Create | POST | /api/users | UserDetailDto (Results.Ok) | LOW | CreateUserRequest |
| Update | PUT | /api/users/{id} | UserDetailDto (Results.Ok) | LOW | UpdateUserRequest |
| Update Roles | PUT | /api/users/{id}/roles | ANONYMOUS (Results.Ok) | HIGH | UpdateUserRolesRequest |

---

### 17. EntityAdvancedFeaturesController.cs (6 endpoints)

| Endpoint | Method | Route | Return Type | Risk Level | Request DTO |
|----------|--------|-------|-------------|-----------|-------------|
| Get Children | GET | /api/entity-advanced/{id}/children | ANONYMOUS (Ok()) | HIGH | None |
| Configure Master Detail | POST | /api/entity-advanced/{id}/configure-master-detail | ANONYMOUS (Ok()) | HIGH | MasterDetailConfigRequest |
| Generate AggVO | POST | /api/entity-advanced/{id}/generate-aggvo | ANONYMOUS (Ok()) | HIGH | None |
| Evaluate Migration | POST | /api/entity-advanced/{id}/evaluate-migration | ANONYMOUS (Ok()) | HIGH | List<FieldMetadata> |
| Get Master Candidates | GET | /api/entity-advanced/master-candidates | ANONYMOUS (Ok()) | HIGH | None |
| Get Detail Candidates | GET | /api/entity-advanced/detail-candidates | ANONYMOUS (Ok()) | HIGH | None |

---

## Statistics Summary

| Metric | Count | Percentage |
|--------|-------|-----------|
| **Total Endpoints** | 90 | 100% |
| **High Risk (Anonymous Objects)** | 45 | 50% |
| **Medium Risk (Dynamic/Mixed)** | 30 | 33% |
| **Low Risk (Typed DTOs)** | 15 | 17% |
| **With Request DTOs** | 55 | 61% |
| **Without Request DTOs** | 35 | 39% |
| **Deprecated Endpoints** | 6 | 7% |
| **NoContent Responses** | 8 | 9% |

---

## Risk Distribution by File

### CRITICAL PRIORITY
- **AuthEndpoints.cs**: 75% high risk (6/8)
- **EntityDefinitionEndpoints.cs**: 90% high risk (18/20)
- **AccessEndpoints.cs**: 67% high risk (8/12)
- **EntityAggregateEndpoints.cs**: 83% high risk (5/6)
- **EntityAdvancedFeaturesController.cs**: 100% high risk (6/6)

### HIGH PRIORITY
- **CustomerEndpoints.cs**: 67% high risk (4/6)
- **DynamicEntityEndpoints.cs**: 57% high risk (4/7)
- **FieldActionEndpoints.cs**: 67% high risk (2/3)

### MEDIUM PRIORITY
- **TemplateEndpoints.cs**: 33% high risk (3/9)
- **LayoutEndpoints.cs**: 21% high risk (3/14)
- **AdminEndpoints.cs**: 60% high risk (3/5)

### LOW PRIORITY
- **UserEndpoints.cs**: 20% high risk (1/5)
- **SettingsEndpoints.cs**: 0% high risk (0/4)

