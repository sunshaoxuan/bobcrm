# 单元测试文档

本文档总结了为BobCRM动态实体系统编写的单元测试。

## 测试概览

### 测试文件列表

1. **PostgreSQLDDLGeneratorTests.cs** - DDL生成器测试
2. **CSharpCodeGeneratorTests.cs** - C#代码生成器测试
3. **RoslynCompilerTests.cs** - Roslyn编译器测试
4. **DynamicEntityServiceTests.cs** - 动态实体服务测试
5. **ReflectionPersistenceServiceTests.cs** - 反射持久化服务测试
6. **EntityPublishingAndDDLTests.cs** - 实体发布和DDL执行服务测试
7. **EntityDefinitionSynchronizerTests.cs** - 实体定义同步器测试
8. **IntegrationTests.cs** - 集成测试示例

### 测试统计

- **总测试文件**: 8个
- **单元测试数量**: 约100+个测试用例
- **测试框架**: xUnit
- **Mock框架**: Moq
- **断言库**: FluentAssertions
- **数据库**: EF Core InMemory

## 测试覆盖范围

### 1. PostgreSQLDDLGenerator (DDL生成器)

测试功能：
- ✅ 生成CREATE TABLE脚本
- ✅ 生成ALTER TABLE ADD COLUMN脚本
- ✅ 生成ALTER TABLE MODIFY COLUMN脚本
- ✅ 生成DROP TABLE脚本
- ✅ 生成接口字段（Base, Archive, Audit, Version, TimeVersion）
- ✅ 处理所有数据类型
- ✅ 生成默认值
- ✅ 生成唯一索引（Archive接口）
- ✅ 生成主键索引

**关键测试用例**:
- `GenerateCreateTableScript_ShouldGenerateValidDDL_WithBasicFields`
- `GenerateCreateTableScript_ShouldGenerateUniqueIndex_WhenArchiveInterfaceEnabled`
- `GenerateInterfaceFields_ShouldGenerateAuditFields`

### 2. CSharpCodeGenerator (C#代码生成器)

测试功能：
- ✅ 生成实体类代码
- ✅ 生成属性注解（Required, MaxLength, Column）
- ✅ 生成接口实现
- ✅ 生成可空值类型
- ✅ 生成默认值表达式
- ✅ 生成XML文档注释
- ✅ 生成接口定义代码
- ✅ 批量生成多个实体
- ✅ 生成DbContext扩展

**关键测试用例**:
- `GenerateEntityClass_ShouldGenerateValidCode_WithBasicFields`
- `GenerateEntityClass_ShouldIncludeInterfaces`
- `GenerateEntityClass_ShouldHandleAllDataTypes`

### 3. RoslynCompiler (Roslyn编译器)

测试功能：
- ✅ 编译有效的C#代码
- ✅ 检测语法错误
- ✅ 检测缺失引用
- ✅ 加载多个类型
- ✅ 编译带数据注解的实体类
- ✅ 批量编译多个源文件
- ✅ 在编译错误中包含文件路径
- ✅ 验证语法（不编译）
- ✅ 提供错误行号和列号

**关键测试用例**:
- `Compile_ShouldSucceed_WithValidCode`
- `Compile_ShouldFail_WithSyntaxError`
- `CompileMultiple_ShouldSucceed_WithMultipleSourceFiles`
- `ValidateSyntax_ShouldReturnValid_ForCorrectSyntax`

### 4. DynamicEntityService (动态实体服务)

测试功能：
- ✅ 生成实体代码
- ✅ 编译实体并缓存
- ✅ 验证实体代码语法
- ✅ 批量编译多个实体
- ✅ 跳过未发布的实体
- ✅ 重新编译实体
- ✅ 获取已加载实体列表
- ✅ 卸载实体
- ✅ 清空所有已加载实体
- ✅ 获取实体类型信息

**关键测试用例**:
- `CompileEntityAsync_ShouldCompileAndCache_WhenSuccessful`
- `CompileMultipleEntitiesAsync_ShouldCompileAllEntities`
- `RecompileEntityAsync_ShouldUnloadAndRecompile`

### 5. ReflectionPersistenceService (反射持久化服务)

测试功能：
- ✅ 验证错误处理（实体类型未加载）
- ✅ QueryOptions配置
- ✅ FilterCondition配置
- ✅ 服务可构造性

**注意**: 完整的CRUD操作测试需要实际编译的实体类型，更适合集成测试。

**关键测试用例**:
- `CreateAsync_ShouldThrowException_WhenEntityTypeNotLoaded`
- `QueryOptions_ShouldAllowComplexConfiguration`

### 6. EntityPublishingService & DDLExecutionService (发布和执行服务)

测试功能：
- ✅ 发布新实体的验证逻辑
- ✅ 发布修改的验证逻辑
- ✅ DDL脚本记录创建
- ✅ 处理无效SQL
- ✅ 获取DDL历史
- ✅ 回滚DDL验证
- ✅ 常量值验证

**关键测试用例**:
- `PublishNewEntityAsync_ShouldFail_WhenEntityNotDraft`
- `DDLExecutionService_ShouldCreateScriptRecord`
- `DDLExecutionService_ShouldHandleInvalidSQL`

### 7. EntityDefinitionSynchronizer (实体定义同步器)

测试功能：
- ✅ 同步系统实体
- ✅ 插入新实体
- ✅ 跳过已存在实体
- ✅ 重置系统实体验证
- ✅ 设置创建和更新日期
- ✅ EntitySource常量

**关键测试用例**:
- `SyncSystemEntitiesAsync_ShouldInsertNewEntities`
- `SyncSystemEntitiesAsync_ShouldSkipExistingEntities`
- `ResetSystemEntityAsync_ShouldThrow_WhenEntityIsNotSystemEntity`

### 8. IntegrationTests (集成测试)

提供的示例：
- ✅ EntityDefinition CRUD操作
- ✅ 实体发布流程
- ✅ 代码生成和编译
- ✅ DynamicEntity CRUD操作
- ✅ 完整工作流测试示例

**注意**: 所有集成测试都标记为Skip，因为它们需要完整的应用程序上下文。

## 运行测试

### 运行所有测试

```bash
cd tests/BobCrm.Api.Tests
dotnet test
```

### 运行特定测试文件

```bash
dotnet test --filter "FullyQualifiedName~PostgreSQLDDLGeneratorTests"
```

### 运行带覆盖率的测试

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### 查看详细输出

```bash
dotnet test --logger "console;verbosity=detailed"
```

## 测试设计原则

1. **单一职责**: 每个测试只验证一个功能点
2. **AAA模式**: Arrange（准备）、Act（执行）、Assert（断言）
3. **清晰命名**: 测试方法名清楚描述测试内容
4. **隔离性**: 测试之间相互独立，使用InMemory数据库
5. **可维护性**: 使用FluentAssertions提高可读性

## 测试命名约定

格式：`MethodName_ShouldExpectedBehavior_WhenCondition`

示例：
- `GenerateCreateTableScript_ShouldGenerateValidDDL_WithBasicFields`
- `Compile_ShouldFail_WithSyntaxError`
- `PublishNewEntityAsync_ShouldFail_WhenEntityNotDraft`

## Mock策略

- **ILogger**: 总是使用Mock（避免日志输出干扰测试）
- **AppDbContext**: 使用EF Core InMemory数据库
- **服务依赖**: 使用Moq创建Mock对象

## 已知限制

1. **Roslyn编译测试**: 某些测试依赖于实际的编译过程，无法完全Mock
2. **数据库DDL操作**: DDL执行测试在InMemory数据库中的行为可能与实际数据库不同
3. **反射持久化**: 完整的CRUD测试需要实际编译的实体类型
4. **集成测试**: 当前标记为Skip，需要完整的应用程序运行环境

## 改进建议

### 短期改进
1. 增加边界条件测试
2. 添加性能测试
3. 增加并发测试（锁机制）

### 长期改进
1. 实现完整的集成测试环境
2. 添加端到端测试
3. 增加测试覆盖率到90%+
4. 添加压力测试

## 测试覆盖率目标

- **代码覆盖率目标**: 80%+
- **关键路径覆盖**: 100%
- **错误处理覆盖**: 90%+

## 依赖包

```xml
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="Moq" Version="4.20.*" />
<PackageReference Include="FluentAssertions" Version="6.12.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.*" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.*" />
```

## 贡献指南

编写新测试时，请遵循：
1. 使用FluentAssertions进行断言
2. 保持测试简洁和专注
3. 添加有意义的测试注释
4. 确保测试可重复运行
5. 清理测试资源（实现IDisposable）

## 总结

本测试套件为BobCRM动态实体系统的核心功能提供了全面的测试覆盖。通过这些测试，我们可以：

✅ 确保DDL生成的正确性
✅ 验证代码生成的准确性
✅ 保证动态编译的可靠性
✅ 测试实体发布流程
✅ 验证数据持久化操作
✅ 确保系统实体同步的正确性

这些测试为系统的稳定性和可维护性提供了坚实的基础。
