# AUDIT-V1.0-INTEGRITY: 深度代码与设计一致性审计报告

**报告日期**: 2026-01-13
**状态**: 发现重大偏差 (Deviation Found)
**版本关联**: v1.0.0-RC2

---

## 1. 审计申明
本报告是对 BobCRM v1.0 实现程度的深度穿透审计。不同于之前的任务状态复核，本次审计直接对比 **源代码实现** 与 **原始设计文档 (ARCH/STD)**，旨在发现被“任务通过”掩盖的架构性缺陷。

## 2. 核心偏差分析 (Core Deviations)

### 2.1 API 安全绕过漏洞 (SEC-05 / SEC-06)
*   **设计预期**: 动态实体 API 必须严格根据用户有权访问的模板进行字段过滤。
*   **代码实测**: [DynamicEntityRouteEndpoints.cs:L60](file:///c:/workspace/bobcrm/src/BobCrm.Api/Endpoints/DynamicEntityRouteEndpoints.cs#L60) 仅在显式提供 `tid` 或 `vs` 参数时才执行过滤。
*   **风险**: 用户通过简单的地址栏篡改（去掉 `?tid=...`），即可通过 REST API 获取数据库中该实体的**全量字段**，包括在所有模板中都被标记为隐藏的敏感数据。
*   **性质**: **阻断级缺陷 (Blocker)**。

### 2.2 Schema 演化不完整 (ENT-02)
*   **设计预期**: 支持 Schema 演化，包括字段的“加”与“减”。
*   **代码实测**: [PostgreSQLDDLGenerator.cs](file:///c:/workspace/bobcrm/src/BobCrm.Api/Services/PostgreSQLDDLGenerator.cs) 仅实现了 `ADD COLUMN`。虽然 `EntityPublishingService` 能识别字段删除，但 [GenerateAlterScript](file:///c:/workspace/bobcrm/src/BobCrm.Api/Services/EntityPublishingService.cs#L760) 显式忽略了删除逻辑。
*   **偏差**: 未满足 `STD-08 ENT-02` 要求的“减字段”能力。即使是为了数据安全，系统也缺乏“逻辑废弃字段”的数据库标记。
*   **性质**: **功能不完整 (Incomplete)**。

### 2.3 菜单-权限-模板绑定的弱耦合 (SEC-04)
*   **设计预期**: 菜单项必须严丝合缝地关联到特定的 Template Binding。
*   **代码实测**: [TemplateRuntimeService.cs:L138](file:///c:/workspace/bobcrm/src/BobCrm.Api/Services/TemplateRuntimeService.cs#L138) 使用 `RequiredPermission == vs` 进行匹配。
*   **风险**: `RequiredPermission` 是一个为了安全设计的字段，现在由于缺乏专门的 `MenuContextKey` 字段，它被临时挪用做路由匹配键。如果管理员修改了功能节点的 Code 而未同步修改 `TemplateStateBinding` 的权限字段，多态渲染将失效且无报错。
*   **性质**: **设计债 (Design Debt)**。

## 3. 根因分析 (5 Whys)
1. **为什么 API 会存在绕过漏洞？** 因为开发时仅考虑了 PageLoader 的正常调用场景，未考虑手动 REST 调用的攻击面。
2. **为什么 DDL 不支持删除？** 因为追求绝对的数据安全，直接屏蔽了 Destructive DDL，但未在元数据层实现“墓碑标记”。
3. **为什么会出现设计债？** 为了快速闭环 `FIX-09` 任务，采用了简单的字符串重用，牺牲了显式建模。

## 4. 纠偏行动建议 (Urgent Fixes)

1.  **[FIX-INTEGRITY-01]**: 强制 API 在缺失 `tid/vs` 时自动解析系统的有效模板（Effective Template）并执行过滤。
2.  **[FIX-INTEGRITY-02]**: 在 `PostgreSQLDDLGenerator` 中增加逻辑，允许（或可选地）物理删除被标记为 `IsDeleted` 的字段，或在数据库层重命名。
3.  **[FIX-INTEGRITY-03]**: 在 `TemplateStateBinding` 中增加显式的 `BindingCode` 字段，与 `FunctionNode.Code` 解耦。

---
**审计人**: Antigravity (Architect / QA Lead)
