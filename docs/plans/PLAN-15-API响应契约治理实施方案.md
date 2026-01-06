# PLAN-15: API 响应契约规范化治理技术规格书

**版本**: 1.1  
**状态**: 待执行 (指令级规范)  
**目标**: 建立确定性的 API 响应模型，消除运行时匿名类型，确保 100% OpenAPI 兼容。

---

## 1. 技术准则 (Technical Standards)

### 1.1 强类型响应规范
严禁在任何 API 端点中使用匿名对象返回。所有响应必须封装在以下固定的 DTO 基类中：
- **命名空间**: `BobCrm.Api.Contracts`
- **无载荷成功**: `SuccessResponse`
- **强类型载荷成功**: `SuccessResponse<T>`
- **分页结果**: `PagedResponse<T>`
- **错误/异常**: `ErrorResponse`

### 1.2 Swagger/OpenAPI 描述规范
每个端点定义必须完整包含元数据标注：
- 必须声明 `.Produces<T>(int statusCode)`。
- 对于成功路径，T 必须是 `SuccessResponse<TActualData>` 形式，严禁使用 `SuccessResponse<object>`。

---

## 2. 详细实施步骤 (Implementation Instructions)

### 2.1 基础设施清理 (Batch 1)
1. **删除冗余**: 移除 `src/BobCrm.Api/Contracts/DTOs/ApiResponse/` 目录下的所有文件。该目录下的 Record 模型与系统标准冲突。
2. **辅助类重构**: 
   - 修正 `ApiResponseExtensions.cs`，将其内部静态方法（如 `SuccessResponse`）的返回类型与 `src/BobCrm.Api/Contracts/BaseResponse` 系列对齐。
3. **依赖修复**: 搜索项目中对 `BobCrm.Api.Contracts.DTOs` 下响应 Record 的引用，将其替换为标准 Class 响应模型。

### 2.2 响应 DTO 固化工程 (Batch 2)
在 `src/BobCrm.Api/Contracts/Responses/` 下按模块创建以下强类型类，取代现有匿名结构：

| 目标端点 | 推荐新增类名 | 属性定义要求 |
| :--- | :--- | :--- |
| `ValidateEntityRoute` | `EntityRouteValidationResponse` | `bool IsValid`, `string EntityRoute`, `object? Entity` (此处 object 为元数据描述符) |
| `CheckEntityReferenced` | `EntityReferenceCheckResponse` | `bool IsReferenced`, `int ReferenceCount`, `ReferenceDetailsDto Details` |
| `CountDynamicEntities` | `EntityCountResponse` | `long Count` |
| `UpdateUserRoles` | `UserRolesUpdateResponse` | `bool Success`, `List<UserRoleDetailsDto> Roles` |

### 2.3 端点逻辑重构 (Batch 3)
顺序执行以下四个文件的重构：
1. **`UserEndpoints.cs`**:
   - 将 `List<UserSummaryDto>` 等列表返回包装进 `SuccessResponse<T>`。
2. **`EntityDefinitionEndpoints.cs`**:
   - 替换所有 `Results.Ok(new { ... })` 为上述 2.2 定义的强类型类。
3. **`DynamicEntityEndpoints.cs`**:
   - 统一查询、计数、原始 SQL 的返回包装，消除内嵌的匿名对象。
4. **元数据标注**:
   - 全面审视上述文件中的端点配置，补全 `.Produces<SuccessResponse<T>>(200)` 等标注。

---

## 3. 模式参考 (Code Patterns)

### 3.1 违规示例 (Bad Case)
```csharp
// 违规：使用匿名对象，Swagger 无法识别 Data 内部结构
return Results.Ok(new { count = totalCount }); 
```

### 3.2 规范示例 (Good Case)
```csharp
// 规范：使用强类型 DTO，显式声明 Produces
return Results.Ok(new SuccessResponse<EntityCountResponse>(new EntityCountResponse { Count = totalCount }));
// ... 后接 .Produces<SuccessResponse<EntityCountResponse>>(StatusCodes.Status200OK)
```

---

## 4. 验证与审计 (Verification)

1. **构建校验**: 执行 `dotnet build` 确保 DTO 替换后无类型不匹配错误。
2. **契约审计**: 访问 `/swagger/v1/swagger.json`，检索 `SuccessResponse` 相关节点的 `schema`，确保载荷类型不为 `object`。
3. **集成验证**: 执行现有测试，确认 DTO 序列化路径未受破坏。
