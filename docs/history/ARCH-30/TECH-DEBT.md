# ARCH-30 技术债清单

**创建日期**: 2025-12-11  
**维护者**: 架构组  
**状态**: 🔴 活跃追踪中

---

## 📊 技术债总览

| 类别 | 数量 | 优先级分布 | 预计工作量 |
|------|------|-----------|----------|
| 多类文件 | 16个文件 | 🔴高:3 / ⚠️中:8 / ⏳低:5 | 8-12小时 |
| 类型总数 | 97个类型 | - | - |
| 已修正 | 0个文件 | - | - |
| 待修正 | 16个文件 | - | 8-12小时 |

---

## 🔴 技术债详情

### 1. 多类文件违规

**发现时间**: 2025-12-11 (Task 2.2 代码评审)  
**发现者**: 用户（架构师）  
**问题描述**: 16个文件包含多个公共类型（共97个类型），违反单一职责原则

---

## 📋 多类文件清单

### 🔴 高优先级（≥8个类型）

#### 1. AccessDtos.cs - 14个类型

**路径**: `src/BobCrm.Api/Contracts/AccessDtos.cs`  
**状态**: ⏳ 待修正  
**优先级**: 🔴 高  
**修正时间**: 阶段2后  
**预计工作量**: 2小时

**包含的类型**:
1. `FunctionNodeDto` (record)
2. `FunctionTemplateOptionDto` (record)
3. `FunctionNodeTemplateBindingDto` (record)
4. `CreateFunctionRequest` (class)
5. `UpdateFunctionRequest` (class)
6. `DeleteFunctionRequest` (class)
7. `MoveCardToColumnRequest` (record)
8. `RoleDto` (record)
9. `CreateRoleRequest` (class)
10. `UpdateRoleRequest` (class)
11. `RoleFunctionPermissionDto` (record)
12. `UpdateRolePermissionsRequest` (record)
13. `RoleAssignmentDto` (record)
14. `AssignRolesToUserRequest` (record)

**拆分计划**:
```
Contracts/DTOs/
├── FunctionNodeDto.cs
├── FunctionTemplateOptionDto.cs
├── FunctionNodeTemplateBindingDto.cs
├── RoleDto.cs
├── RoleFunctionPermissionDto.cs
└── RoleAssignmentDto.cs

Contracts/Requests/Access/
├── CreateFunctionRequest.cs
├── UpdateFunctionRequest.cs
├── DeleteFunctionRequest.cs
├── MoveCardToColumnRequest.cs
├── CreateRoleRequest.cs
├── UpdateRoleRequest.cs
├── UpdateRolePermissionsRequest.cs
└── AssignRolesToUserRequest.cs
```

**依赖影响**: 中等（Access 模块）

---

#### 2. DataSetDtos.cs - 12个类型

**路径**: `src/BobCrm.Api/Contracts/DTOs/DataSetDtos.cs`  
**状态**: ⏳ 待修正  
**优先级**: 🔴 高  
**修正时间**: 阶段2后  
**预计工作量**: 1.5小时

**拆分计划**:
```
Contracts/DTOs/
└── (12个独立文件)

Contracts/Requests/DataSet/
└── (Request类型)
```

**依赖影响**: 中等（DataSet 模块）

---

#### 3. TemplateDtos.cs - 8个类型

**路径**: `src/BobCrm.Api/Contracts/DTOs/TemplateDtos.cs`  
**状态**: ⏳ 待修正  
**优先级**: 🔴 高  
**修正时间**: 阶段2后  
**预计工作量**: 1小时

**拆分计划**:
```
Contracts/DTOs/
└── (8个独立文件)

Contracts/Requests/Template/
└── (Request类型)
```

**依赖影响**: 中等（Template 模块）

---

### ⚠️ 中优先级（5-7个类型）

#### 4. UserDtos.cs - 7个类型

**路径**: `src/BobCrm.Api/Contracts/UserDtos.cs`  
**状态**: ⏳ 待修正  
**优先级**: ⚠️ 中  
**修正时间**: 阶段3后  
**预计工作量**: 1小时

**依赖影响**: 低（用户模块）

---

#### 5. EnumDefinitionDto.cs - 7个类型 ⭐

**路径**: `src/BobCrm.Api/Contracts/DTOs/EnumDefinitionDto.cs`  
**状态**: ⏳ 待修正  
**优先级**: ⚠️ 中 → 🔴 **高**（Task 2.2 修正）  
**修正时间**: Task 2.2 修正期间  
**预计工作量**: 0.5小时

**包含的类型**:
1. `EnumDefinitionDto` (class)
2. `EnumOptionDto` (class)
3. `CreateEnumDefinitionRequest` (class)
4. `CreateEnumOptionRequest` (class)
5. `UpdateEnumDefinitionRequest` (class)
6. `UpdateEnumOptionsRequest` (class)
7. `UpdateEnumOptionRequest` (class)

**拆分计划**:
```
Contracts/DTOs/
├── EnumDefinitionDto.cs
└── EnumOptionDto.cs

Contracts/Requests/Enum/
├── CreateEnumDefinitionRequest.cs
├── UpdateEnumDefinitionRequest.cs
├── CreateEnumOptionRequest.cs
├── UpdateEnumOptionsRequest.cs
└── UpdateEnumOptionRequest.cs
```

**依赖影响**: 低（当前任务）

**修正原因**: Task 2.2 代码评审不合格，必须修正

---

#### 6. AuthDtos.cs - 5个类型

**路径**: `src/BobCrm.Api/Contracts/DTOs/AuthDtos.cs`  
**状态**: ⏳ 待修正  
**优先级**: ⚠️ 中  
**修正时间**: 阶段3后  
**预计工作量**: 0.5小时

**依赖影响**: 低（认证模块）

---

#### 7. SettingsDtos.cs - 5个类型

**路径**: `src/BobCrm.Api/Contracts/DTOs/SettingsDtos.cs`  
**状态**: ⏳ 待修正  
**优先级**: ⚠️ 中  
**修正时间**: 阶段3后  
**预计工作量**: 0.5小时

**依赖影响**: 低（设置模块）

---

#### 8. AdminDtos.cs - 5个类型

**路径**: `src/BobCrm.Api/Contracts/DTOs/AdminDtos.cs`  
**状态**: ⏳ 待修正  
**优先级**: ⚠️ 中  
**修正时间**: 阶段3后  
**预计工作量**: 0.5小时

**依赖影响**: 低（管理模块）

---

#### 9. CustomerDtos.cs - 4个类型

**路径**: `src/BobCrm.Api/Contracts/DTOs/CustomerDtos.cs`  
**状态**: ⏳ 待修正  
**优先级**: ⚠️ 中  
**修正时间**: 按需  
**预计工作量**: 0.5小时

**依赖影响**: 低（客户模块）

---

#### 10. LayoutDtos.cs - 4个类型

**路径**: `src/BobCrm.Api/Contracts/DTOs/LayoutDtos.cs`  
**状态**: ⏳ 待修正  
**优先级**: ⚠️ 中  
**修正时间**: 按需  
**预计工作量**: 0.5小时

**依赖影响**: 低（布局模块）

---

#### 11. ApiResponse.cs - 4个类型

**路径**: `src/BobCrm.Api/Contracts/DTOs/ApiResponse.cs`  
**状态**: ⏳ 待修正  
**优先级**: ⚠️ 中  
**修正时间**: 按需  
**预计工作量**: 0.5小时

**依赖影响**: 低（响应包装）

---

### ⏳ 低优先级（2-3个类型）

#### 12. OrganizationDtos.cs - 3个类型

**路径**: `src/BobCrm.Api/Contracts/OrganizationDtos.cs`  
**状态**: ⏳ 待修正  
**优先级**: ⏳ 低  
**修正时间**: 按需  
**预计工作量**: 0.3小时

---

#### 13-16. 其他4个文件 - 各2个类型

**路径**:
- `src/BobCrm.Api/Contracts/SuccessResponse.cs` (2个类型)
- `src/BobCrm.Api/Contracts/Requests/Entity/CreateEntityDefinitionDto.cs` (2个类型)
- `src/BobCrm.Api/Contracts/Requests/Entity/UpdateEntityDefinitionDto.cs` (2个类型)
- `src/BobCrm.Api/Contracts/Responses/Entity/CompileResultDto.cs` (2个类型)

**状态**: ⏳ 待修正  
**优先级**: ⏳ 低  
**修正时间**: 按需  
**预计工作量**: 0.2小时 × 4 = 0.8小时

**备注**: 2个类型的文件违规程度较轻，可以延后处理

---

## 📅 修正计划

### 阶段1: Task 2.2 修正（立即）

**时间**: 2025-12-11  
**目标**: 修复 Task 2.2 代码评审不合格问题

**任务**:
- [x] 发现多类文件问题
- [x] 创建技术债清单（本文档）
- [x] 创建代码规范文档
- [ ] 拆分 `EnumDefinitionDto.cs`（7个类型 → 7个文件）
- [ ] 验证编译通过
- [ ] 重新评审

**预计工作量**: 0.5小时

---

### 阶段2: Phase 2 完成后（2周内）

**时间**: 2025-12-25 前  
**目标**: 拆分高优先级文件（≥8个类型）

**任务**:
- [ ] 拆分 `AccessDtos.cs` (14个类型) - 2小时
- [ ] 拆分 `DataSetDtos.cs` (12个类型) - 1.5小时
- [ ] 拆分 `TemplateDtos.cs` (8个类型) - 1小时

**预计工作量**: 4.5小时

---

### 阶段3: Phase 3 完成后（1个月内）

**时间**: 2026-01-15 前  
**目标**: 拆分中优先级文件（5-7个类型）

**任务**:
- [ ] 拆分 `UserDtos.cs` (7个类型) - 1小时
- [ ] 拆分 `AuthDtos.cs` (5个类型) - 0.5小时
- [ ] 拆分 `SettingsDtos.cs` (5个类型) - 0.5小时
- [ ] 拆分 `AdminDtos.cs` (5个类型) - 0.5小时
- [ ] 拆分 `CustomerDtos.cs` (4个类型) - 0.5小时
- [ ] 拆分 `LayoutDtos.cs` (4个类型) - 0.5小时
- [ ] 拆分 `ApiResponse.cs` (4个类型) - 0.5小时

**预计工作量**: 4小时

---

### 阶段4: 按需处理

**时间**: 修改时顺便处理  
**目标**: 拆分低优先级文件（2-3个类型）

**任务**:
- [ ] 拆分 `OrganizationDtos.cs` (3个类型)
- [ ] 拆分其他4个2类型文件

**预计工作量**: 1小时

---

## 📊 进度追踪

### 总体进度

| 阶段 | 文件数 | 类型数 | 状态 | 完成日期 |
|------|--------|--------|------|---------|
| 阶段1 | 1 | 7 | ⏳ 进行中 | - |
| 阶段2 | 3 | 34 | ⏳ 待开始 | - |
| 阶段3 | 7 | 38 | ⏳ 待开始 | - |
| 阶段4 | 5 | 18 | ⏳ 待开始 | - |
| **总计** | **16** | **97** | **0%** | - |

---

### 按优先级统计

| 优先级 | 文件数 | 类型数 | 状态 | 完成率 |
|--------|--------|--------|------|--------|
| 🔴 高 | 4 | 41 | ⏳ 0/4 | 0% |
| ⚠️ 中 | 7 | 38 | ⏳ 0/7 | 0% |
| ⏳ 低 | 5 | 18 | ⏳ 0/5 | 0% |
| **总计** | **16** | **97** | **0/16** | **0%** |

---

## 🔍 检测方法

**PowerShell 脚本**（检测多类文件）:

```powershell
$files = Get-ChildItem -Path src/BobCrm.Api/Contracts -Recurse -Filter "*.cs"
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $matches = [regex]::Matches(
        $content, 
        '^\s*(public|internal|private|protected)?\s*(sealed|abstract|static)?\s*(class|record|struct|interface|enum)\s+\w+', 
        [System.Text.RegularExpressions.RegexOptions]::Multiline
    )
    if ($matches.Count -gt 1) {
        Write-Host "$($file.FullName.Replace((Get-Location).Path + '\', '')): $($matches.Count) types"
    }
}
```

**使用方法**:
```bash
cd c:\workspace\bobcrm
# 运行上述 PowerShell 脚本
```

---

## 📝 修正模板

### 拆分步骤

1. **创建目标目录**
   ```bash
   mkdir -p Contracts/Requests/{Domain}
   ```

2. **拆分类型到独立文件**
   - 每个公共类型独立为一个文件
   - 保留原有命名空间
   - 保留原有注释和注解

3. **更新 using 引用**
   - 搜索所有引用该文件的代码
   - 添加必要的 `using` 语句

4. **验证编译**
   ```bash
   dotnet build BobCrm.sln
   ```

5. **提交代码**
   ```bash
   git add .
   git commit -m "refactor: split {FileName} into {N} files

- Split {FileName} ({N} types) into {N} separate files
- Moved Request types to Contracts/Requests/{Domain}/
- Updated all references with proper using statements

Ref: ARCH-30 Tech Debt - Single Type Per File"
   ```

---

## 🎯 验收标准

### 每个文件拆分后

- [ ] 每个 `.cs` 文件只包含一个公共类型
- [ ] 文件名与类型名一致
- [ ] 目录结构符合约定（DTOs / Requests / Responses）
- [ ] `dotnet build BobCrm.sln` 成功
- [ ] 所有测试通过
- [ ] Git 提交符合规范

---

### 阶段完成后

- [ ] 检测脚本输出为空（无违规文件）
- [ ] 更新本文档进度
- [ ] 更新代码规范文档（如有必要）

---

## 🚨 风险评估

### 风险1: 引用更新遗漏

**风险等级**: ⚠️ 中  
**影响**: 编译错误  
**缓解措施**: 使用 IDE 重构功能，验证编译通过

---

### 风险2: 合并冲突

**风险等级**: ⚠️ 中  
**影响**: Git 合并冲突  
**缓解措施**: 
- 按阶段拆分，避免大批量改动
- 及时提交，保持小批次
- 在低活跃期进行拆分

---

### 风险3: 测试破坏

**风险等级**: ⏳ 低  
**影响**: 测试失败  
**缓解措施**: 每次拆分后运行全量测试

---

## 📚 参考文档

- [STD-04 开发规范](../../process/STD-04-开发规范.md) § 3.4（单一类型原则）
- [Task 2.2 代码评审](task-2.2-review.md)
- [ARCH-30 设计文档](../../design/ARCH-30-实体字段显示名多语元数据驱动设计.md)

---

## 🔄 修订历史

| 版本 | 日期 | 修改内容 | 修改人 |
|------|------|---------|--------|
| v1.0 | 2025-12-11 | 初版发布（Task 2.2 评审发现） | 架构组 |

---

**维护者**: 架构组  
**最后更新**: 2025-12-11  
**下次审查**: Task 2.2 修正完成后

