# REVIEW-06: v0.8.0 代码分析与建议

**日期**: 2025-11-21
**评审人**: Antigravity
**范围**: `TemplateService`, `FieldPermissionService`, `AppDbContext`

## 1. 代码质量评估

### 1.1 TemplateService (`src/BobCrm.Api/Services/TemplateService.cs`)
*   **优势**:
    *   **逻辑分离**: 成功将业务逻辑与控制器/端点层解耦。
    *   **健壮性**: 良好地处理了“系统默认”与“用户默认”等边缘情况。`ClearExistingUserDefaultsAsync` 方法确保数据一致性。
    *   **日志**: `Information` 和 `Warning` 级别的全面日志有助于调试。
    *   **复制逻辑**: `CopyTemplateAsync` 方法正确处理了模板属性的深拷贝。
*   **建议**:
    *   **性能**: `GetTemplatesAsync` 检索用户的所有模板，然后在某些情况下（如 `groupBy`）在内存中过滤。对于大型数据集，考虑将更多过滤移动到数据库查询中。
    *   **验证**: `LayoutJson` 存储为字符串。考虑在保存前添加验证步骤以确保它是有效的 JSON。

### 1.2 FieldPermissionService (`src/BobCrm.Api/Services/FieldPermissionService.cs`)
*   **优势**:
    *   **安全模型**: 为具有多个角色的用户实施“联合”（最宽松）策略，这是标准且可预测的。
    *   **缓存潜力**: 服务对每次权限检查都查询数据库。这是一个高频路径。
*   **关键建议**:
    *   **缓存**: **必须** 为 `GetUserFieldPermissionAsync` 实现缓存。在列表视图的每一行的每个字段上检查权限将导致 N+1 查询问题和严重的性能下降。
    *   **批处理**: `GetReadableFieldsAsync` 很好，但我们需要一种方法一次性获取 *多个* 实体或字段的权限，以优化前端加载。

### 1.3 AppDbContext (`src/BobCrm.Api/Infrastructure/AppDbContext.cs`)
*   **优势**:
    *   **架构**: `FieldPermission` 实体定义正确。
*   **建议**:
    *   **索引**: 确保 `FieldPermission` 在 `(RoleId, EntityType, FieldName)` 上有复合索引以进行快速查找。（目前似乎依赖于单个或部分索引，请验证这一点）。

## 2. 缺失实现 (差距分析)

### 2.1 字段级安全 UI
*   **差距**: 后端已就绪，但前端 (`RolePermissionTree.razor`) **零** 集成。
*   **影响**: 用户无法配置字段权限。该功能不可用。
*   **建议**:
    1.  修改 `RolePermissionTree.razor` 以加载 `FieldPermissions`。
    2.  在树中的实体节点下为每个字段添加 UI 切换（读/写）。

### 2.2 仪表盘引擎
*   **差距**: 功能完全缺失。
*   **影响**: v0.8.0 未交付承诺的仪表盘 MVP。
*   **建议**: 推迟到 v0.9.0。

## 3. 行动计划
1.  **立即**: 为 `FieldPermissionService` 实现缓存。
2.  **下个 Sprint**: 在 `RolePermissionTree` 中构建 `FieldPermission` UI。
3.  **下个 Sprint**: 启动仪表盘引擎。
