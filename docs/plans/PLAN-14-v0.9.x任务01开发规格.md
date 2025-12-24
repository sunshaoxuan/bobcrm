# DEV-SPEC: v0.9.x-TASK-01 实体关系建模 (Lookup)

## 1. 任务综述
本规范定义了 OneCRM v0.9.0 里程碑中“实体关系建模”的技术实施细节。执行者必须确保元数据驱动的架构能够支持 1:1/N:1 的 `Lookup` 关系，并由存储层生成对应外键。

## 2. 核心指令 (Context)
- **必须参考设计**: `docs/design/ARCH-01-实体自定义与发布系统设计文档.md`
- **必须参考计划**: `docs/plans/PLAN-15-v0.9.0-闭环开发计划.md`
- **严禁事项**: 逻辑层（Api/Models）严禁出现 `PostgreSQL` 或 `SQL` 等物理词汇，必须使用抽象术语。

## 3. 技术要求 (Requirements)

### A. 领域模型增强 (Domain Model)
- **目标文件**: `src/BobCrm.Api/Base/Models/FieldMetadata.cs`
- **修改内容**:
  - 新增 `LookupEntityName` (string): 引用实体的代码。
  - 新增 `LookupDisplayField` (string): 搜索展示字段。
  - 新增 `ForeignKeyAction` (enum): 枚举包含 Cascade, Restrict, SetNull。
- **DTO 同步**: 同步修改 `EntityFieldDto.cs` 以暴露上述字段。

### B. 存储层生成器改造 (Storage Layer)
- **目标文件**: `src/BobCrm.Api/Services/PostgreSQLDDLGenerator.cs` (逻辑名称：StorageDDLGenerator)
- **修改内容**:
  - 增强 `GenerateFieldSQL` 或类似方法，在检测到 `LookupEntityName` 时拼接外键约束语义。
  - 必须输出符合规范的 `CONSTRAINT FK_[Source]_[Target] FOREIGN KEY (...) REFERENCES ...` 脚本。

### C. 校验逻辑 (Validation)
- 在发布实体定义之前，必须验证引用实体是否存在，防止物理外键创建失败。

## 4. 质量门禁 (Quality Gates) - **强制性要求**

> [!IMPORTANT]
> **单元测试覆盖率必须 > 90%**
> 1. **针对性测试**: 为 DDL 生成逻辑编写所有分支的测试。
> 2. **异常测试**: 模拟无效外键引用、非法级联操作等边界条件。
> 3. **报告提交**: 任务完成后必须展示 `dotnet test` 的覆盖率结果。

## 5. 验收标准 (Acceptance Criteria)
1. 成功新增带 Lookup 的实体且 DDL 生成包含正确的 `FOREIGN KEY`。
2. 单元测试全落地且覆盖率达标。
3. 文档 `task.md` 进度更新。

---
**审批人 (Architect):** Antigravity
**发布日期:** 2025-12-22
