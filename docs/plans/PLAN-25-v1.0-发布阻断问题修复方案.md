# PLAN-25: v1.0 发布阻断问题修复方案 (Remediation Plan)

> **版本**: 1.0
> **类型**: 紧急修复方案
> **依据**: AUDIT-09 审计报告
> **目标**: 解除 4 个 Critical Blockers，重启发布流程

---

## 1. 基础设施修复 (Infrastructure)

### 1.1 API 测试套件死锁 / 超时 (Critical)
**现象**: `BobCrm.Api.Tests` 运行超过 15 分钟无响应，日志显示 `AuditLog` 持续插入。
**根本原因**: 疑似 `AuditLogService` 在记录日志时触发了隐式保存或递归调用，或 `InMemory` 数据库锁争用。
**修复指令 (To Developer)**:
1.  **检查循环依赖**: 检查 `AuditLogService` 是否监听了 `DbContext` 的 `SavedChanges` 事件，导致"记录日志 -> 触发保存 -> 再次记录日志"的死循环。
2.  **禁用测试级审计**: 在 `CustomWebApplicationFactory` 中，针对集成测试环境 (`Enviroment="Testing"`) 禁用或 Mock 掉后台审计任务。
3.  **强制并行配置**: 确认 `xunit.runner.json` 中 `maxParallelThreads` 设置合理（建议移除或设为 -1 自动），减少串行等待。

### 1.2 App 覆盖率 0% (Critical)
**现象**: `coverage-app.xml` 包含大量数据但仅覆盖 `BobCrm.Api` 命名空间，`BobCrm.App` 覆盖率为 0。
**根本原因**: `BobCrm.App.Tests` 引用了 Api 项目，默认收集器未区分程序集。`coverlet` 可能未正确加载 `BobCrm.App.dll` 的 PDB。
**修复指令 (To Developer)**:
1.  **明确 Include 过滤器**: 在运行测试时显式指定程序集。
    ```powershell
    dotnet test tests\BobCrm.App.Tests\BobCrm.App.Tests.csproj --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Include="[BobCrm.App]*"
    ```
2.  **检查 PDB 生成**: 确保 `BobCrm.App.csproj` 在 Debug 模式下生成 Full PDB。
    ```xml
    <PropertyGroup Condition="'$(Configuration)'=='Debug'">
        <DebugType>full</DebugType>
    </PropertyGroup>
    ```

### 1.3 存证缺失 (Compliance)
**现象**: `docs/history/test-results/` 不存在。
**修复指令 (To Developer)**:
1.  **创建目录结构**: 编写脚本自动创建 `PC-001` 等对应的空文件夹。
2.  **自动化脚本**: 修改 `tests/e2e/run_all.ps1`，使其在运行结束后自动将截图和日志归档到对应 ID 的文件夹。

---

## 2. 功能缺陷修复 (Functional)

### 2.1 实体映射逻辑回归 (Blocker)
**现象**: `InterfaceFieldMappingTests.GetFields_Base_ShouldContainRequiredPrimaryKeyI` 失败。
**影响**: 实体定义无法正确映射到接口（如 `ICustomer`），导致多态功能失效。
**修复指令 (To Developer)**:
1.  **定位代码**: `src/BobCrm.Api/Base/Models/EntityInterface.cs` 或 `EntityDefinitionExtensions.cs`。
2.  **逻辑修正**: 确保在 `GetInterfaceFields()` 方法中，始终包含 `Id` (PrimaryKey) 字段，即使接口定义中未显式列出（隐含继承）。

---

## 3. 测试验证提示 (Verification Prompts)

**请使用以下 Prompt 指导 AI 助理进行回归验证：**

> **任务**: 执行 v1.0 阻断修复回归测试
> **步骤**:
> 1.  **API 冒烟**: 运行 `dotnet test tests\BobCrm.Api.Tests`，确保 3 分钟内完成且无死锁。
> 2.  **App 覆盖率**: 使用修复后的 Include 参数运行 App 测试，并解析 xml 确认 `BobCrm.App` 节点 line-rate > 0。
> 3.  **功能验证**: 单独运行 `dotnet test --filter "InterfaceFieldMappingTests"` 确保通过。
> 4.  **存证检查**: 运行 E2E 脚本后，检查 `docs/history/test-results` 是否有文件生成。

---

**批准**: QA Lead
