# PLAN-05: 系统治理与质量提升计划

**版本**: 1.0
**状态**: 规划中
**关联文档**: 
- `docs/reference/API-05-端点风险与改进建议.md`
- `docs/process/STD-01-开发质量标准.md`
- `docs/process/STD-02-文档编写规范.md`

---

## 1. 目标与背景

经过对现有系统的全面评估，我们发现虽然功能覆盖率较高，但在**API 契约稳定性**、**代码质量**、**文档规范**以及**UI/UX 细节**方面存在显著的系统性风险。

本计划旨在通过一个为期 6-8 周的治理阶段（v0.8.x - v0.9.x），彻底解决这些技术债务，为 v1.0 的正式发布打下坚实基础。

**核心目标**:
1.  **API 治理**: 消除匿名对象返回，实现 100% DTO 覆盖，统一响应格式。
2.  **质量提升**: 建立单元测试体系，核心业务覆盖率达到 80%+。
3.  **UI/UX 打磨**: 严格遵循 `STD-01`，消除原生弹窗，统一设计语言。
4.  **文档标准化**: 确保所有文档符合 `STD-02`，并保持实时更新。

---

## 2. 执行阶段

### 第一阶段：API 契约治理 (高优先级)
**周期**: 第 1-2 周
**负责人**: 后端开发

> **现状**: 50% 的端点返回匿名对象，前端极易崩溃，无法生成 SDK。

**任务**:
1.  **建立标准**:
    -   定义 `BaseResponse`, `SuccessResponse<T>`, `ErrorResponse`。
    -   强制所有 Controller/Endpoint 使用强类型 DTO。
2.  **核心重构 (Auth & EntityDefinition)**:
    -   重构 `AuthEndpoints.cs` (8 个端点)。
    -   重构 `EntityDefinitionEndpoints.cs` (20+ 个端点)。
    -   为上述端点编写契约测试。
3.  **OpenAPI 集成**:
    -   配置 Swagger/OpenAPI 以支持 DTO 文档生成。

### 第二阶段：前端与 UI/UX 升级 (中优先级)
**周期**: 第 3-4 周
**负责人**: 前端开发

> **现状**: 存在原生 `alert`/`confirm`，部分组件交互粗糙，I18n 不完整，表单设计器功能缺失。

**任务**:
1.  **交互标准化**:
    -   全局搜索并替换所有 `JS.InvokeVoidAsync("alert"...)` 为 `MessageService`。
    -   全局搜索并替换所有 `JS.InvokeVoidAsync("confirm"...)` 为 `ModalService`。
2.  **表单设计器深度优化**:
    -   **控件渲染**: 修复 `ButtonWidget` 渲染，使用 Ant Design 组件，修复样式错乱。
    -   **动作配置**: 在 `ButtonWidget` 中添加 `Action` (OpenUrl, Download, Script) 和 `Payload` 的属性编辑器。
    -   **实体树修复**: 修正 `EntityMetadataTree` 中系统/自定义属性的分类逻辑 (目前错误地归类为 Custom)。
    -   **视觉打磨**: 优化拖拽时的重影效果和放置占位符。
3.  **模板状态增强**:
    -   实现基于模板状态 (Draft, Published, Archived) 的细粒度控件控制。
    -   为每个控件添加 "可见性" 和 "可编辑性" 的状态配置矩阵。
4.  **系统设置升级**:
    -   重构 `Settings.razor`，提供更深度的系统定制能力（如 Logo 上传、高级主题配置）。
    -   增强用户个性化设置，区分继承自系统的默认值和用户覆盖值。
5.  **I18n 全面补全**:
    -   扫描所有 `.razor` 和 `.cs` 文件，提取硬编码字符串。
    -   预置缺失的系统级多语资源（如验证消息、通用按钮文本）。

### 第三阶段：测试与稳定性 (持续进行)
**周期**: 第 2-6 周
**负责人**: 全员

> **现状**: 核心业务逻辑缺乏单元测试保护。

**任务**:
1.  **基础设施**:
    -   建立 `BobCrm.Tests` 项目。
    -   配置 CI 流程运行测试。
2.  **核心覆盖**:
    -   `TemplateService` 测试。
    -   `DynamicEntityService` 测试。
    -   `FieldPermissionService` 测试。

---

## 3. 详细行动计划 (Action Items)

### 3.1 API 重构清单
- [ ] 创建 `BobCrm.Api.Contracts` 命名空间及基础类。
- [ ] **AuthEndpoints**:
    - [ ] `LoginResponse` DTO
    - [ ] `SessionResponse` DTO
    - [ ] `UserDetailResponse` DTO
- [ ] **EntityDefinitionEndpoints**:
    - [ ] `EntityDefinitionDto` (完整结构)
    - [ ] `PublishResultDto`
- [ ] **DynamicEntityEndpoints**:
    - [ ] `PagedResult<T>` 泛型包装
    - [ ] `DynamicEntityDto` (使用 `Dictionary<string, object>` 但有明确包装)

### 3.2 UI/UX 改进清单
- [ ] **FormDesigner.razor**:
    - [ ] 移除所有原生弹窗。
    - [ ] 修复 `ButtonWidget` 渲染与动作配置。
    - [ ] 修复 `EntityMetadataTree` 属性分类。
    - [ ] 实现控件级状态配置 (Visibility/Editability per state)。
- [ ] **Settings.razor**:
    - [ ] 重构 UI，支持 Logo 上传、高级主题。
    - [ ] 区分系统默认值与用户设置。
- [ ] **I18n**:
    - [ ] 全局扫描并提取硬编码字符串。
    - [ ] 补全 `zh-CN` 和 `en-US` 资源文件。

### 3.3 文档维护清单
- [ ] 保持 `API-06-端点摘要表.md` 与代码同步。
- [ ] 更新 `GUIDE-01` 等指南文档以反映新的 API 结构。

---

## 4. 验收标准

1.  **API**: 0 个端点返回匿名对象 (通过静态分析确认)。
2.  **测试**: 核心 Service 单元测试覆盖率 > 80%。
3.  **UI**: 0 个原生浏览器弹窗。
4.  **文档**: 所有文档符合 `STD-02` 规范，无英文文件名。

---

## 5. 风险管理

-   **兼容性风险**: API 重构将破坏现有前端代码。
    -   *对策*: 前后端同步修改，或者在短时间内（开发环境）允许破坏，快速修复前端。鉴于目前处于开发阶段，建议采用**快速破坏-快速修复**策略。
-   **进度风险**: 重构工作量大。
    -   *对策*: 按优先级执行，先保证核心链路（登录、实体定义、表单渲染）。
