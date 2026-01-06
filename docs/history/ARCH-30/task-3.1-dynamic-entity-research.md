# ARCH-30 Task 3.1: 动态实体查询机制研究

**任务编号**: Task 3.1
**阶段**: 阶段3 - 低频API改造（动态实体查询优化）
**状态**: 待开始
**预计工时**: 0.5 天
**依赖**: 阶段0-2 完成

---

## 1. 任务目标

深入研究 BobCRM 动态实体系统的查询机制，为 Task 3.2/3.3 的多语字段解析方案提供技术基础。

### 1.1 研究范围

| 模块 | 关键文件 | 研究重点 |
|------|----------|----------|
| 代码生成 | `CSharpCodeGenerator.cs` | 实体类生成逻辑 |
| 动态编译 | `RoslynCompiler.cs` | 运行时编译机制 |
| 数据查询 | `DynamicEntityService.cs` | 查询管道与返回结构 |
| 元数据 | `EntityDefinition.cs`, `FieldMetadata.cs` | 字段元数据存储 |

### 1.2 输出目标

- 技术调研报告：`docs/history/AUDIT-04-ARCH-30-动态实体多语研究报告.md`
- 确定多语字段解析的最佳切入点
- 评估现有架构的改造成本

---

## 2. 前置条件

- [ ] 阶段0-2 代码已合并到主分支
- [ ] 本地开发环境可运行
- [ ] 已阅读 ARCH-30 主设计文档第3.2节

---

## 3. 研究步骤

### 3.1 代码生成链路分析

**目标文件**: `src/BobCrm.Api/Services/CodeGeneration/CSharpCodeGenerator.cs`

研究要点：
1. `GenerateEntityClass()` 方法如何生成实体类代码
2. 字段属性是如何从 `FieldMetadata` 转换为 C# 属性的
3. 多语字段（`DisplayName`）是否被编入生成代码
4. `ExtData` 扩展字段的处理方式

记录模板：
```markdown
### 代码生成分析

**入口方法**: `GenerateEntityClass(EntityDefinition entity)`

**字段处理流程**:
1. [描述第一步]
2. [描述第二步]
3. ...

**发现**:
- [发现1]
- [发现2]

**结论**:
[是否可以在此层注入多语解析？成本评估]
```

### 3.2 动态编译机制分析

**目标文件**: `src/BobCrm.Api/Services/RoslynCompiler.cs`

研究要点：
1. `Compile()` 方法的完整流程
2. 编译后程序集的加载与管理
3. 类型反射机制如何获取属性元数据
4. 是否有属性特性（Attribute）可用于标记多语字段

### 3.3 数据查询管道分析

**目标文件**: `src/BobCrm.Api/Services/DynamicEntityService.cs`

研究要点：
1. `QueryAsync()` 方法的实现逻辑
2. 查询结果的序列化方式
3. 是否存在 DTO 转换层
4. 字段元数据在查询流程中的可访问性

关键问题：
- 查询返回的是原始实体对象还是 DTO？
- 如何在不修改返回数据的前提下附加字段元数据？

### 3.4 元数据存储分析

**目标文件**:
- `src/BobCrm.Api/Base/Models/EntityDefinition.cs`
- `src/BobCrm.Api/Base/Models/FieldMetadata.cs`

研究要点：
1. `FieldMetadata.DisplayName` 的存储格式
2. `FieldMetadata.DisplayNameKey` 的使用方式
3. 接口字段 vs 自定义字段的元数据差异
4. 元数据缓存机制（如有）

---

## 4. 调研报告模板

完成研究后，按以下模板撰写报告：

```markdown
# AUDIT-04: ARCH-30 动态实体多语研究报告

**调研日期**: YYYY-MM-DD
**调研者**: [姓名/AI]

## 1. 执行摘要

[1-2段总结核心发现和建议]

## 2. 代码生成链路

### 2.1 流程图
[Mermaid 或文字描述的流程图]

### 2.2 关键发现
- [发现1]
- [发现2]

### 2.3 多语切入点评估
[是否适合在此层注入？成本？]

## 3. 动态编译机制

### 3.1 编译流程
[详细描述]

### 3.2 类型元数据
[如何获取？]

### 3.3 评估
[改造可行性]

## 4. 数据查询管道

### 4.1 查询流程
[详细描述]

### 4.2 返回结构
[当前返回什么？]

### 4.3 元数据注入点
[在哪里可以注入 meta.fields？]

## 5. 方案建议

### 5.1 推荐方案
[基于研究，推荐哪种方案？]

### 5.2 实施路径
[具体步骤]

### 5.3 风险与缓解
[潜在风险及应对]

## 6. 参考资料

- [相关源码文件列表]
- [相关设计文档]
```

---

## 5. 验收标准

### 5.1 交付物检查

- [ ] 调研报告 `docs/history/AUDIT-04-ARCH-30-动态实体多语研究报告.md` 已创建
- [ ] 报告覆盖所有研究要点
- [ ] 包含明确的方案建议
- [ ] 包含代码引用（文件名:行号）

### 5.2 质量标准

- [ ] 报告结构清晰，可独立阅读
- [ ] 技术分析准确，有代码佐证
- [ ] 建议具有可操作性

---

## 6. Git 提交规范

```bash
git add docs/history/AUDIT-04-ARCH-30-动态实体多语研究报告.md
git commit -m "docs(research): add dynamic entity i18n research report

- Analyze codegen/compile/query pipeline
- Identify field metadata & i18n resolve points
- Recommend meta.fields approach for Stage 3
- Ref: ARCH-30 Task 3.1"
```

---

## 7. 后续任务

完成本任务后，继续：
- **Task 3.2**: 基于研究结论，确认字段级多语解析方案
- **Task 3.3**: 实施 API 改造

---

**创建日期**: 2025-01-06
**最后更新**: 2025-01-06
**维护者**: ARCH-30 项目组
