# STD-03: 通用编程规范

> **版本**: 1.0
> **适用范围**: 所有后端与前端代码

---

## 1. 架构设计原则

### 1.1 面向对象原则
- ✅ **封装**: 隐藏实现细节，暴露清晰接口
- ✅ **继承**: 合理使用继承，避免过深继承链（≤3层）
- ✅ **多态**: 利用接口和抽象类实现多态
- ✅ **单一职责**: 每个类只负责一个明确的职责

### 1.2 SOLID 原则
- **S** - Single Responsibility Principle（单一职责原则）
- **O** - Open/Closed Principle（开闭原则）
- **L** - Liskov Substitution Principle（里氏替换原则）
- **I** - Interface Segregation Principle（接口隔离原则）
- **D** - Dependency Inversion Principle（依赖倒置原则）

### 1.3 设计模式
优先使用以下设计模式：
- 工厂模式（Factory）
- 策略模式（Strategy）
- 模板方法模式（Template Method）
- 依赖注入（Dependency Injection）

---

## 2. 代码质量标准

### 2.1 编译要求
- ✅ **零警告**: 所有编译警告必须修复
- ✅ **零错误**: 代码必须能够成功编译
- ✅ **CI/CD 通过**: 所有持续集成检查必须通过
- ✅ **代码静态分析**: 通过代码分析工具检查

### 2.2 测试要求
- ✅ **单元测试覆盖率 ≥ 90%**
- ✅ **所有业务逻辑必须有测试**
- ✅ **边界条件和异常情况必须覆盖**
- ✅ **所有测试必须通过**
- ✅ **集成测试覆盖关键流程**

测试命名规范：
```text
// 格式：MethodName_Scenario_ExpectedBehavior
TEST_CASE EnsureTemplates_WithForce_ShouldCompletelyRegenerate()
{
    // Arrange
    // Act
    // Assert
}
```

### 2.3 文档注释
- ✅ **所有公开方法必须有文档注释**
- ✅ **复杂逻辑必须有行内注释**
- ✅ **参数和返回值必须说明**

示例：
```text
/**
 * 为实体确保默认模板和状态绑定
 * @param entityDefinition 实体定义
 * @param updatedBy 操作人
 * @param force 是否强制完全重新生成
 * @return 模板生成结果
 */
FUNCTION EnsureTemplates(entityDefinition, updatedBy, force)
{
    // 实现
}
```

### 2.4 异步编程
- ✅ **使用异步模式** (如 async/await)
- ✅ **方法名建议包含 Async 后缀** (视语言习惯而定)
- ✅ **传递取消令牌** (Cancellation Token)
- ✅ **避免无效的异步调用**

### 2.5 资源管理
- ✅ **显式释放资源**
- ✅ **实现清理接口** (如 IDisposable, AutoCloseable)
- ✅ **避免内存泄漏**

### 2.6 临时文件/产物
- ✅ **清理原则**: 允许本地生成临时输出，但用毕必须删除或加入忽略列表。
- ✅ **仓库清洁**: 仓库内只保留有长期价值的证据，调试中间产物须清理。
- ✅ **脚本自洁**: 自动化脚本应在结束时包含清理逻辑。

---

## 3. 编码规范

### 3.1 命名规范

| 类型 | 规则 | 示例 |
|-----|------|------|
| 类、接口、枚举 | PascalCase | `FormTemplate`, `ITemplateGenerator` |
| 方法、属性 | PascalCase | `EnsureTemplates`, `EntityType` |
| 局部变量、参数 | camelCase | `entityType`, `updatedBy` |
| 私有字段 | _camelCase | `_db`, `_logger` |
| 常量 | PascalCase | `DefaultPageSize` |
| 接口 | I前缀 (可选) | `IDefaultTemplateService` |

### 3.2 代码格式
- ✅ 缩进：4个空格
- ✅ 大括号：独占一行 (或符合语言标准)
- ✅ 每行最多120字符
- ✅ 文件编码：UTF-8

### 3.3 代码组织
```text
// 1. 导入/引用 (Import/Using)
IMPORT System.Collections

// 2. 命名空间/包 (Namespace/Package)
NAMESPACE Project.Api.Services

// 3. 类定义
/** 类文档 */
CLASS MyService
{
    // 4. 私有字段
    PRIVATE _db
    
    // 5. 构造函数
    CONSTRUCTOR(db)
    {
        _db = db
    }
    
    // 6. 公开方法
    PUBLIC ASYNC DoSomething() { }
    
    // 7. 私有方法
    PRIVATE Helper() { }
}
```

### 3.4 单一类型原则 (One Type Per File)

**核心原则**: 每个源文件应该只包含**一个公共类型**（class/interface/enum）。

#### 例外情况
1. **私有辅助类型**
2. **文件作用域类型**
3. **紧密相关的泛型特化**

---

## 4. 异常处理规范

### 4.1 原则
- ✅ **有意义的异常消息**
- ✅ **记录异常日志**
- ✅ **适当的异常类型**
- ✅ **不吞噬异常**

### 4.2 示例
```text
TRY
{
    DoSomething()
}
CATCH (EntityNotFoundException ex)
{
    LOG_WARNING("Entity not found")
    RETURN_RESPONSE(404, "Not Found")
}
CATCH (Exception ex)
{
    LOG_ERROR("Unexpected error")
    RETURN_RESPONSE(500, "Internal Error")
}
```

---

## 5. 日志规范

### 5.1 日志级别
- **Trace**: 详细跟踪信息
- **Debug**: 调试信息
- **Information**: 一般信息
- **Warning**: 警告信息
- **Error**: 错误信息
- **Critical**: 严重错误

### 5.2 日志示例
```text
LOG_INFO("[TemplateGenerator] Regenerated template {Id}", template.Id)
LOG_ERROR(ex, "[TemplateGenerator] Failed to generate template")
```

---

## 6. 代码审查清单

在提交前，请自查：
- [ ] 所有类和方法有文档注释
- [ ] 遵循单一职责原则（SRP）
- [ ] 依赖注入正确使用
- [ ] 异步方法正确使用
- [ ] 资源正确释放
- [ ] 异常处理完善
- [ ] 日志记录充分
- [ ] 单元测试覆盖率达标
- [ ] 所有测试通过
- [ ] 零编译警告和错误
