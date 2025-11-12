using System.Text;
using BobCrm.Api.Domain.Models;

namespace BobCrm.Api.Services;

/// <summary>
/// C#代码生成器
/// 根据EntityDefinition生成完整的C#实体类代码
/// </summary>
public class CSharpCodeGenerator
{
    /// <summary>
    /// 生成C#实体类代码
    /// </summary>
    public virtual string GenerateEntityClass(EntityDefinition entity)
    {
        var sb = new StringBuilder();

        // Using statements
        sb.AppendLine("using System;");
        sb.AppendLine("using System.ComponentModel.DataAnnotations;");
        sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
        sb.AppendLine();

        // Namespace
        sb.AppendLine($"namespace {entity.Namespace}");
        sb.AppendLine("{");

        // XML文档注释
        sb.AppendLine("    /// <summary>");

        var displayName = MultilingualTextHelper.Resolve(entity.DisplayName, entity.EntityName);
        sb.AppendLine($"    /// {displayName}");

        var description = MultilingualTextHelper.Resolve(entity.Description, string.Empty);
        if (!string.IsNullOrEmpty(description))
        {
            sb.AppendLine($"    /// {description}");
        }
        sb.AppendLine($"    /// 自动生成于: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("    /// </summary>");

        // Table attribute
        sb.AppendLine($"    [Table(\"{entity.DefaultTableName}\")]");

        // Class declaration with interfaces
        var interfaces = GenerateInterfaceList(entity);
        var interfaceClause = interfaces.Any() ? $" : {string.Join(", ", interfaces)}" : "";
        sb.AppendLine($"    public class {entity.EntityName}{interfaceClause}");
        sb.AppendLine("    {");

        // Properties
        foreach (var field in entity.Fields.OrderBy(f => f.SortOrder))
        {
            GenerateProperty(sb, field, entity);
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// 生成接口列表
    /// </summary>
    private List<string> GenerateInterfaceList(EntityDefinition entity)
    {
        var interfaces = new List<string>();

        foreach (var entityInterface in entity.Interfaces.Where(i => i.IsEnabled))
        {
            var interfaceName = entityInterface.InterfaceType switch
            {
                EntityInterfaceType.Base => "IEntity",
                EntityInterfaceType.Archive => "IArchive",
                EntityInterfaceType.Audit => "IAuditable",
                EntityInterfaceType.Version => "IVersioned",
                EntityInterfaceType.TimeVersion => "ITimeVersioned",
                EntityInterfaceType.Organization => "IOrganizational",
                _ => null
            };

            if (interfaceName != null && !interfaces.Contains(interfaceName))
            {
                interfaces.Add(interfaceName);
            }
        }

        return interfaces;
    }

    /// <summary>
    /// 生成属性代码
    /// </summary>
    private void GenerateProperty(StringBuilder sb, FieldMetadata field, EntityDefinition entity)
    {
        sb.AppendLine();

        // XML文档注释
        sb.AppendLine("        /// <summary>");

        var fieldDisplayName = MultilingualTextHelper.Resolve(field.DisplayName, field.PropertyName);
        sb.AppendLine($"        /// {fieldDisplayName}");
        sb.AppendLine("        /// </summary>");

        // Data annotations
        var attributes = new List<string>();

        // Required attribute
        if (field.IsRequired)
        {
            attributes.Add("Required");
        }

        // MaxLength attribute
        if (field.Length.HasValue && field.DataType == FieldDataType.String)
        {
            attributes.Add($"MaxLength({field.Length})");
        }

        // Column attribute (for data type override)
        var columnType = GetColumnTypeAttribute(field);
        if (!string.IsNullOrEmpty(columnType))
        {
            attributes.Add($"Column(TypeName = \"{columnType}\")");
        }

        // ForeignKey attribute
        if (field.IsEntityRef && field.ReferencedEntityId.HasValue)
        {
            attributes.Add($"ForeignKey(nameof({field.PropertyName}))");
        }

        // Write attributes
        foreach (var attr in attributes)
        {
            sb.AppendLine($"        [{attr}]");
        }

        // Property declaration
        var csType = MapFieldTypeToCSharpType(field);
        var hasDefaultValue = !string.IsNullOrEmpty(field.DefaultValue);

        // 可空性规则：
        // 1. 必填或有默认值 → 非可空
        // 2. 非必填且无默认值的值类型 → 可空
        var shouldBeNullable = !field.IsRequired && !hasDefaultValue && IsValueType(field.DataType);
        var nullableModifier = shouldBeNullable ? "?" : string.Empty;
        var defaultValue = GetDefaultValueExpression(field);

        sb.AppendLine($"        public {csType}{nullableModifier} {field.PropertyName} {{ get; set; }}{defaultValue}");
    }

    /// <summary>
    /// 获取Column特性的TypeName
    /// </summary>
    private string? GetColumnTypeAttribute(FieldMetadata field)
    {
        return field.DataType switch
        {
            FieldDataType.Decimal when field.Precision.HasValue && field.Scale.HasValue
                => $"decimal({field.Precision},{field.Scale})",
            FieldDataType.Text => "text",
            _ => null
        };
    }

    /// <summary>
    /// 映射字段类型到C#类型
    /// </summary>
    private string MapFieldTypeToCSharpType(FieldMetadata field)
    {
        return field.DataType switch
        {
            FieldDataType.String => "string",  // 注意：Text 是 String 的别名
            FieldDataType.Int32 => "int",
            FieldDataType.Int64 => "long",
            FieldDataType.Decimal => "decimal",
            FieldDataType.Boolean => "bool",
            FieldDataType.DateTime => "DateTime",
            FieldDataType.Date => "DateOnly",
            FieldDataType.Guid => "Guid",
            _ => "object"
        };
    }

    /// <summary>
    /// 判断是否是值类型
    /// </summary>
    private bool IsValueType(string dataType)
    {
        return dataType switch
        {
            FieldDataType.Int32 => true,
            FieldDataType.Int64 => true,
            FieldDataType.Decimal => true,
            FieldDataType.Boolean => true,
            FieldDataType.DateTime => true,
            FieldDataType.Date => true,
            FieldDataType.Guid => true,
            _ => false
        };
    }

    /// <summary>
    /// 获取默认值表达式
    /// </summary>
    private string GetDefaultValueExpression(FieldMetadata field)
    {
        if (string.IsNullOrEmpty(field.DefaultValue))
        {
            // 引用类型默认初始化为空字符串
            if (field.DataType == FieldDataType.String)  // Text是String的别名，只需检查String
            {
                if (field.IsRequired)
                {
                    return " = string.Empty;";
                }
            }
            return string.Empty;
        }

        return field.DataType switch
        {
            FieldDataType.String => $" = \"{field.DefaultValue}\";",  // Text是String的别名
            FieldDataType.Int32 or FieldDataType.Int64 => $" = {field.DefaultValue};",
            FieldDataType.Decimal => $" = {field.DefaultValue}m;",
            FieldDataType.Boolean => $" = {field.DefaultValue.ToLower()};",
            FieldDataType.DateTime when field.DefaultValue.ToUpper() == "NOW" => " = DateTime.UtcNow;",
            FieldDataType.Date when field.DefaultValue.ToUpper() == "TODAY" => " = DateOnly.FromDateTime(DateTime.UtcNow);",
            FieldDataType.Guid when field.DefaultValue.ToUpper() == "NEWID" => " = Guid.NewGuid();",
            _ => string.Empty
        };
    }

    /// <summary>
    /// 生成接口定义代码
    /// </summary>
    public virtual string GenerateInterfaces()
    {
        return @"using System;

namespace BobCrm.Api.Domain
{
    /// <summary>
    /// 基础实体接口 - 所有实体都应实现此接口
    /// 包含逻辑删除支持（系统级安全机制）
    /// </summary>
    public interface IEntity
    {
        int Id { get; set; }
        bool IsDeleted { get; set; }
        DateTime? DeletedAt { get; set; }
        string? DeletedBy { get; set; }
    }

    /// <summary>
    /// 档案实体接口 - 包含Code和Name
    /// </summary>
    public interface IArchive
    {
        string Code { get; set; }
        string Name { get; set; }
    }

    /// <summary>
    /// 可审计接口 - 包含创建和修改信息
    /// </summary>
    public interface IAuditable
    {
        DateTime CreatedAt { get; set; }
        string? CreatedBy { get; set; }
        DateTime UpdatedAt { get; set; }
        string? UpdatedBy { get; set; }
        int Version { get; set; }
    }

    /// <summary>
    /// 版本管理接口
    /// </summary>
    public interface IVersioned
    {
        int Version { get; set; }
    }

    /// <summary>
    /// 时间版本接口 - 支持时间范围的版本管理
    /// </summary>
    public interface ITimeVersioned
    {
        DateTime ValidFrom { get; set; }
        DateTime? ValidTo { get; set; }
        int VersionNo { get; set; }
    }

    /// <summary>
    /// 组织维度接口 - 记录所属组织及其树路径
    /// </summary>
    public interface IOrganizational
    {
        Guid OrganizationId { get; set; }
    }
}";
    }

    /// <summary>
    /// 生成批量实体代码
    /// </summary>
    public Dictionary<string, string> GenerateMultipleEntities(List<EntityDefinition> entities)
    {
        var result = new Dictionary<string, string>();

        foreach (var entity in entities)
        {
            var code = GenerateEntityClass(entity);
            result[entity.FullTypeName] = code;
        }

        return result;
    }

    /// <summary>
    /// 生成实体的DbContext扩展代码
    /// </summary>
    public string GenerateDbContextExtension(List<EntityDefinition> entities)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine();
        sb.AppendLine("namespace BobCrm.Api.Infrastructure");
        sb.AppendLine("{");
        sb.AppendLine("    public partial class AppDbContext");
        sb.AppendLine("    {");

        foreach (var entity in entities)
        {
            sb.AppendLine($"        public DbSet<{entity.Namespace}.{entity.EntityName}> {entity.DefaultTableName} => Set<{entity.Namespace}.{entity.EntityName}>();");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
