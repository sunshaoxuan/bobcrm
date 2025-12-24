# PLAN-17: API 响应契约治理实施方案

**版本**: 1.0  
**状态**: 待执行  
**目标**: 消除匿名对象返回，收敛响应包装模型，实现 100% Swagger 契约覆盖。

---

## 1. 治理标准 (Standard)

### 1.1 唯一响应模型
所有 API 必须使用以下命名空间下的类：
- **基类**: `BobCrm.Api.Contracts.BaseResponse`
- **成功(无数据)**: `BobCrm.Api.Contracts.SuccessResponse`
- **成功(带数据)**: `BobCrm.Api.Contracts.SuccessResponse<T>`
- **分页**: `BobCrm.Api.Contracts.PagedResponse<T>`
- **错误**: `BobCrm.Api.Contracts.ErrorResponse`

> [!IMPORTANT]  
> 废弃 `BobCrm.Api.Contracts.DTOs.ApiResponse` 下的 Record 定义，禁止在新代码中使用。

### 1.2 强类型约束
- 禁止使用 `Results.Ok(new { ... })`。
- 禁止使用 `SuccessResponse<object>` 返回动态结构。
- 每个端点必须显式使用 `.Produces<T>()` 声明返回类型。

---

## 2. 待重构区域 (Refactoring Scope)

| 端点文件 | 违规项 | 整改动作 |
| :--- | :--- | :--- |
| `AuthEndpoints.cs` | 响应包装已完成 | 补全 `.Produces<T>` 标注。 |
| `EntityDefinitionEndpoints.cs` | 匿名对象：路由校验、引用检查 | 提取 `EntityRouteValidationResponse`, `EntityReferenceCheckResponse`。 |
| `UserEndpoints.cs` | 直接返回 DTO 列表 | 使用 `SuccessResponse<List<UserSummaryDto>>` 包装。 |
| `DynamicEntityEndpoints.cs` | 匿名对象：QueryMeta, Count, RawQuery | 提取 `DynamicEntityQueryMetaResponse`, `EntityCountResponse` 等。 |

---

## 3. 分阶段执行 Prompts (AI Developer Guides)

本方案建议分为三个批次执行，每个批次对应的 AI 提示词如下：

### 第一批次：地基稳固 (Foundation Cleanup)
**任务**: 移除冗余模型，统一拦截器逻辑。
> **Prompt**: "作为高级 .NET 开发者，请根据 `PLAN-17` 整改 API 基础响应。1. 废弃并删除 `src/BobCrm.Api/Contracts/DTOs/ApiResponse/` 中的所有冗余 Record。2. 修正 `ApiResponseExtensions.cs`，确保其辅助方法全量返回 `SuccessResponse` 系列。3. 验证全局引用是否已对齐至 `BaseResponse` 系列。"

### 第二批次：核心重构 (Core DTO-fication)
**任务**: 消除 `EntityDefinition` 的匿名结构。
> **Prompt**: "请执行 `PLAN-17` 的第二阶段任务。1. 在 `Contracts/Responses/Entity/` 目录下为 `EntityDefinitionEndpoints.cs` 中的路由校验和引用检查提取强类型 DTO。2. 确保相关端点返回 `SuccessResponse<T>`。3. 为受影响端点补全 `.Produces<SuccessResponse<T>>()`。必须遵循 STD-04 '一文件一类型' 规范。"

### 第三批次：一致性补全 (Consistency Completion)
**任务**: 包装 `User` 与 `DynamicEntity` 散点。
> **Prompt**: "请根据 `PLAN-17` 完成最后的 API 整改。1. 将 `UserEndpoints.cs` 中的所有直接 DTO 返回包装入 `SuccessResponse<T>`。2. 在 `DynamicEntityEndpoints.cs` 中，为 Count 和 RawQuery 创建专属 Response 类。3. 补全各端点的 Http 状态码标注（200, 400, 401 等）。"

---

## 4. 验收与 QA

1. **静态审计**: 运行扫描脚本，确认 `Results.Ok(new {` 数量为 0。
2. **Swagger 校验**: 检查 `/swagger/index.html`，确认所有端点 Schema 均可展开且无 `object` 类型。
3. **集成测试**: 运行 `dotnet test` 确保重构未破坏现有业务逻辑。
