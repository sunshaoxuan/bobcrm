namespace BobCrm.App.Models;

/// <summary>
/// 实体定义DTO
/// </summary>
public class EntityDefinitionDto
{
    public Guid Id { get; set; }
    public string Namespace { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string FullTypeName { get; set; } = string.Empty;
    public string EntityRoute { get; set; } = string.Empty;
    public string DisplayNameKey { get; set; } = string.Empty;

    /// <summary>
    /// 显示名（多语言）- 从 API 加载
    /// </summary>
    public Dictionary<string, string>? DisplayName { get; set; }

    public string? DescriptionKey { get; set; }

    /// <summary>
    /// 描述（多语言）- 从 API 加载
    /// </summary>
    public Dictionary<string, string>? Description { get; set; }

    public string ApiEndpoint { get; set; } = string.Empty;
    public string StructureType { get; set; } = "Single";
    public string Status { get; set; } = "Draft";
    public string Source { get; set; } = "Custom";
    public bool IsLocked { get; set; }
    public bool IsRootEntity { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Order { get; set; }
    public string? Icon { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    public List<FieldMetadataDto> Fields { get; set; } = new();
    public List<EntityInterfaceDto> Interfaces { get; set; } = new();
}

/// <summary>
/// 字段元数据DTO
/// </summary>
public class FieldMetadataDto
{
    public Guid Id { get; set; }
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// 显示名Key（自动生成，保留用于向后兼容）
    /// </summary>
    public string DisplayNameKey { get; set; } = string.Empty;

    /// <summary>
    /// 显示名（多语言）
    /// </summary>
    public MultilingualTextDto? DisplayName { get; set; }
    
    /// <summary>
    /// 显示名（多语言，从API返回的Dictionary格式）
    /// </summary>
    public Dictionary<string, string>? DisplayNameDict { get; set; }

    public string DataType { get; set; } = "String";
    public int? Length { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool IsRequired { get; set; }
    public bool IsEntityRef { get; set; }
    public Guid? ReferencedEntityId { get; set; }
    public string? TableName { get; set; }
    public int SortOrder { get; set; }
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; }
}

/// <summary>
/// 实体接口DTO
/// </summary>
public class EntityInterfaceDto
{
    public Guid Id { get; set; }
    public string InterfaceType { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// 创建实体定义请求
/// </summary>
public class CreateEntityDefinitionRequest
{
    public string Namespace { get; set; } = "BobCrm.Domain.Custom";
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// 显示名（多语言）
    /// </summary>
    public MultilingualTextDto DisplayName { get; set; } = new();

    /// <summary>
    /// 描述（多语言）
    /// </summary>
    public MultilingualTextDto? Description { get; set; }

    public string StructureType { get; set; } = "Single";
    public List<string> Interfaces { get; set; } = new();
    public List<FieldMetadataDto> Fields { get; set; } = new();
}

/// <summary>
/// 更新实体定义请求
/// </summary>
public class UpdateEntityDefinitionRequest
{
    /// <summary>
    /// 显示名（多语言）
    /// </summary>
    public MultilingualTextDto DisplayName { get; set; } = new();

    /// <summary>
    /// 描述（多语言）
    /// </summary>
    public MultilingualTextDto? Description { get; set; }

    public string? Icon { get; set; }
    public string? Category { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Order { get; set; }
    public List<FieldMetadataDto> Fields { get; set; } = new();
    public List<string> Interfaces { get; set; } = new();
}

/// <summary>
/// DDL历史记录DTO
/// </summary>
public class DDLHistoryDto
{
    public Guid Id { get; set; }
    public string ScriptType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? ErrorMessage { get; set; }
    public string ScriptPreview { get; set; } = string.Empty;
}

/// <summary>
/// 代码生成响应
/// </summary>
public class CodeGenerationResponse
{
    public Guid EntityId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 编译响应
/// </summary>
public class CompilationResponse
{
    public bool Success { get; set; }
    public string AssemblyName { get; set; } = string.Empty;
    public List<string> LoadedTypes { get; set; } = new();
    public List<CompilationErrorDto> Errors { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 编译错误DTO
/// </summary>
public class CompilationErrorDto
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public string? FilePath { get; set; }
}

/// <summary>
/// 发布响应
/// </summary>
public class PublishResponse
{
    public bool Success { get; set; }
    public Guid ScriptId { get; set; }
    public string DDLScript { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public ChangeAnalysisDto? ChangeAnalysis { get; set; }
}

/// <summary>
/// 变更分析DTO
/// </summary>
public class ChangeAnalysisDto
{
    public List<FieldMetadataDto> NewFields { get; set; } = new();
    public Dictionary<string, int> LengthIncreases { get; set; } = new();
    public bool HasDestructiveChanges { get; set; }
}

/// <summary>
/// 已加载实体响应
/// </summary>
public class LoadedEntitiesResponse
{
    public int Count { get; set; }
    public List<string> Entities { get; set; } = new();
}

/// <summary>
/// 实体类型信息响应
/// </summary>
public class EntityTypeInfoResponse
{
    public string FullName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public bool IsLoaded { get; set; }
    public List<PropertyTypeInfoDto> Properties { get; set; } = new();
    public List<string> Interfaces { get; set; } = new();
}

/// <summary>
/// 属性类型信息DTO
/// </summary>
public class PropertyTypeInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
}

/// <summary>
/// 数据类型常量
/// </summary>
public static class FieldDataType
{
    public const string String = "String";
    public const string Integer = "Integer";
    public const string Long = "Long";
    public const string Decimal = "Decimal";
    public const string Boolean = "Boolean";
    public const string DateTime = "DateTime";
    public const string Date = "Date";
    public const string Text = "Text";
    public const string Guid = "Guid";
}

/// <summary>
/// 实体接口类型常量
/// </summary>
public static class EntityInterfaceType
{
    public const string Base = "Base";
    public const string Archive = "Archive";
    public const string Audit = "Audit";
    public const string Version = "Version";
    public const string TimeVersion = "TimeVersion";
}

/// <summary>
/// 实体状态常量
/// </summary>
public static class EntityStatus
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Modified = "Modified";
}

/// <summary>
/// 实体来源常量
/// </summary>
public static class EntitySource
{
    public const string System = "System";
    public const string Custom = "Custom";
}
