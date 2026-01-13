# ARCH-33: v1.0 体系补完架构 (System Completeness Architecture)

> **状态**: 拟定 (Draft)
> **优先级**: High (Blocker for v1.0)
> **目标**: 补全实体校验、系统服务、状态鉴权这三大缺失拼图，支撑全功能验收。

---

## 1. 可扩展校验系统 (Extensible Validation System)

针对用户指出的“实体引擎缺失高级校验”问题，引入 `IEntityValidator` 插件机制。

### 1.1 架构设计
*   **后端核心**: `BobCrm.Api.Services.Validation`
    *   `IPropertyValidator`: 属性验证器接口 (Validate(value) -> Valid/Invalid)。
    *   `ValidatorRegistry`: 验证器注册中心 (Email, IP, Phone, Regex)。
    *   `EntityValidationService`: 编排服务，在 `DynamicEntityService` 保存前调用。
*   **元数据扩展**:
    *   在 `FieldMetadata` 中增加 `ValidatorType` (string) 和 `ValidatorRules` (json)。
*   **前端适配**:
    *   设计器属性面板增加 "Validation Rules" 配置块。
    *   运行时 `PageLoader` 解析规则并注入 `AntDesign.Form` 的 Rules。

### 1.2 预置验证器 (Built-in Validators)
| 代号 | 名称 | 参数示例 |
|---|---|---|
| `Email` | 邮箱 | - |
| `Regex` | 正则 | `{"Pattern": "^CN-.*"}` |
| `Range` | 数值范围 | `{"Min": 18, "Max": 60}` |
| `Unique`| 唯一性 | `{"Scope": "Global"}` (需 DB 查询) |

---

## 2. 状态感知鉴权 (State-Aware Authorization)

针对“授权需包含状态”的需求，升级权限描述符格式。

### 2.1 权限描述符升级 (Permission Descriptor)
旧格式: `order.edit`
新格式: `order.edit:Draft,Rejected` (仅在 Draft 或 Rejected 状态下拥有 edit 权限)

### 2.2 鉴权逻辑变更
1.  **PermissionService**: 解析 `User.Permissions` 时，识别冒号后的状态约束。
2.  **Runtime Check**:
    ```csharp
    // 旧
    RequirePermission("order.edit");
    // 新
    RequirePermission("order.edit", context: entity.Status);
    ```
3.  **UI 适配**:
    *   `RolePermissionTree`: 在勾选功能节点时，弹出“适用状态”多选框（需读取该实体关联的 StateBinding 或 Enum）。

---

## 3. 系统服务门面 (System Service Facade)

针对“邮件、通知、队列”缺失的问题，定义标准接口以解耦实现。

### 3.1 核心接口 (`BobCrm.Api.Abstractions.System`)
*   **IEmailSender**: `SendAsync(to, subject, body)`
*   **INotificationClient**: `PushAsync(userId, message)` (SignalR 封装)
*   **IBackgroundQueue**: `Enqueue<T>(job)` (基于 Channel 或 Hangfire)

### 3.2 默认实现 (v1.0 Scope)
*   `SmtpEmailSender`: 基于 `appsettings.json` 的 SMTP 实现。
*   `InMemoryQueue`: 内存级异步队列 (非持久化，仅用于演示/开发)。
*   `HubNotificationClient`: 现有的 SignalR Hub 包装。

---

## 4. 路由与多态异常处理 (Routing & Polymorphism)

针对“无模板时的友好提示”，增强 `TemplateRuntimeService` 的回退逻辑。

### 4.1 异常拦截设计
*   **场景**: 用户有点 `order.view` 权限，但当前 Order 处于 `Archived` 状态，且该状态未配置任何模板。
*   **行为**:
    1.  检测到 TemplateId 为空。
    2.  不再抛出 500，而是返回系统预置的 `FallbackTemplate` (包含 "No view configured for state {Status}" 提示)。
    3.  前端渲染该 Fallback 模板（通常是一个 Empty State 组件）。

### 4.2 路由后置作业
在 `PageLoader` 完成状态匹配后：
1.  检查 `AppliedScopes` 是否包含当前记录状态。
2.  若不包含且无默认模板，重定向至 `ErrorPage(Code=NO_TEMPLATE)`。

---

## 5. 验收标准变更 (Acceptance Criteria Update)

*   **PC-010**: 字段配置 Email 校验后，非法格式无法保存。
*   **IS-007**: 角色配置 `order.edit:Draft`，验证 Approved 状态下无法编辑。
*   **SYS-001**: 调用 `IEmailSender` 能正确生成本地日志或发送邮件。
