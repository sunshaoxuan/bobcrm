# Roslyn动态编译 - 安装说明

## 需要安装的NuGet包

为了使用Roslyn动态编译功能，需要安装以下NuGet包：

### 1. Microsoft.CodeAnalysis.CSharp

```bash
cd src/BobCrm.Api
dotnet add package Microsoft.CodeAnalysis.CSharp --version 4.8.0
```

或者直接编辑 `BobCrm.Api.csproj` 文件，添加：

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" />
</ItemGroup>
```

## 功能说明

安装完成后，以下功能将可用：

### 1. 代码生成
- `CSharpCodeGenerator` - 根据EntityDefinition生成C#实体类代码
- 支持数据注解（Required, MaxLength等）
- 支持接口实现（IEntity, IArchive, IAuditable等）
- 自动处理默认值和可空类型

### 2. 动态编译
- `RoslynCompiler` - 使用Roslyn将C#源代码编译为程序集
- 支持语法验证
- 支持单文件和多文件编译
- 完整的编译错误报告（代码、消息、行号、列号）

### 3. 动态实体管理
- `DynamicEntityService` - 管理动态实体的完整生命周期
- 代码生成 → 编译 → 加载 → 缓存 → 卸载
- 支持实体重新编译（热更新）
- 支持批量编译多个实体

## API端点

安装完成后，以下API端点将可用：

### 代码生成
- `GET /api/entity-definitions/{id}/generate-code` - 生成实体代码
- `GET /api/entity-definitions/{id}/validate-code` - 验证代码语法

### 编译
- `POST /api/entity-definitions/{id}/compile` - 编译单个实体
- `POST /api/entity-definitions/compile-batch` - 批量编译实体
- `POST /api/entity-definitions/{id}/recompile` - 重新编译实体

### 管理
- `GET /api/entity-definitions/loaded-entities` - 获取已加载实体列表
- `GET /api/entity-definitions/type-info/{fullTypeName}` - 获取实体类型信息
- `DELETE /api/entity-definitions/loaded-entities/{fullTypeName}` - 卸载实体

## 使用示例

### 1. 创建并发布实体定义
```bash
# 1. 创建实体定义
POST /api/entity-definitions
{
  "namespace": "BobCrm.Base.Custom",
  "entityName": "Product",
  "displayNameKey": "ENTITY_PRODUCT",
  "interfaces": ["Base", "Archive"],
  "fields": [...]
}

# 2. 发布实体（生成数据库表）
POST /api/entity-definitions/{id}/publish
```

### 2. 生成并编译代码
```bash
# 1. 生成C#代码（预览）
GET /api/entity-definitions/{id}/generate-code

# 2. 编译并加载到内存
POST /api/entity-definitions/{id}/compile

# 3. 查看已加载的实体
GET /api/entity-definitions/loaded-entities

# 4. 获取实体类型信息
GET /api/entity-definitions/type-info/BobCrm.Base.Custom.Product
```

### 3. 批量编译
```bash
POST /api/entity-definitions/compile-batch
{
  "entityIds": [
    "guid1",
    "guid2",
    "guid3"
  ]
}
```

## 注意事项

1. **性能考虑**
   - 编译操作是CPU密集型任务，建议在低负载时段执行
   - 已编译的程序集会缓存在内存中，重复加载时无需重新编译

2. **内存管理**
   - 动态加载的程序集会占用内存
   - 不再使用的实体应及时卸载
   - 可使用 `DELETE /api/entity-definitions/loaded-entities/{fullTypeName}` 卸载

3. **安全性**
   - 动态编译的代码未经过严格的安全审计
   - 建议只在受信任的环境中使用
   - 生产环境建议限制编译权限

4. **版本兼容性**
   - Microsoft.CodeAnalysis.CSharp 版本需要与项目的.NET版本匹配
   - 推荐使用4.8.0或更高版本

## 故障排除

### 编译失败
1. 检查生成的代码语法是否正确
2. 使用 `GET /api/entity-definitions/{id}/validate-code` 验证语法
3. 查看编译错误详情中的行号和错误信息

### 类型加载失败
1. 确认实体已成功编译
2. 检查 `GET /api/entity-definitions/loaded-entities` 是否包含该实体
3. 尝试重新编译 `POST /api/entity-definitions/{id}/recompile`

### 内存占用过高
1. 定期卸载不使用的实体
2. 考虑使用批量编译减少程序集数量
3. 监控 `GET /api/entity-definitions/loaded-entities` 返回的实体数量
