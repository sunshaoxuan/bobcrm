using System.Text;

using BobCrm.Api.Base.Aggregates;

using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Services;

/// <summary>
/// 子实体代码生成器
/// 根据SubEntityDefinition生成C#类代码
/// </summary>

public class SubEntityCodeGenerator : ISubEntityCodeGenerator

{

    private readonly ILogger<SubEntityCodeGenerator> _logger;

    public SubEntityCodeGenerator(ILogger<SubEntityCodeGenerator> logger)

    {

        _logger = logger;

    }

    /// <summary>
    /// 为聚合中的所有子实体生成C#类
    /// </summary>

    public async Task GenerateSubEntitiesAsync(

        EntityDefinitionAggregate aggregate,

        CancellationToken cancellationToken = default)

    {

        _logger.LogInformation("Generating code for {Count} sub-entities of {EntityName}",

            aggregate.SubEntities.Count, aggregate.Root.EntityName);

        // 确定生成代码的基础目录

        var baseDirectory = Path.Combine(

            Directory.GetCurrentDirectory(),

            "..", "..",

            "generated",

            "Entities",

            aggregate.Root.Namespace.Replace("BobCrm.Base.", ""),

            aggregate.Root.EntityName);

        // 确保目录存在

        Directory.CreateDirectory(baseDirectory);

        // 生成子实体类

        foreach (var subEntity in aggregate.SubEntities)

        {

            var code = GenerateSubEntityClass(aggregate.Root, subEntity);

            var filePath = Path.Combine(baseDirectory, $"{subEntity.Code}.cs");

            await File.WriteAllTextAsync(filePath, code, cancellationToken);

            _logger.LogInformation("Generated sub-entity class: {FilePath}", filePath);

        }

        // 生成AggVO类

        var aggVoCode = GenerateAggregateVoClass(aggregate.Root, aggregate.SubEntities.ToList());

        var aggVoFilePath = Path.Combine(baseDirectory, $"{aggregate.Root.EntityName}AggVo.cs");

        await File.WriteAllTextAsync(aggVoFilePath, aggVoCode, cancellationToken);

        _logger.LogInformation("Generated AggVo class: {FilePath}", aggVoFilePath);

        _logger.LogInformation("Code generation completed. Files saved to: {Directory}", baseDirectory);

    }

    /// <summary>
    /// 生成单个子实体的C#类代码
    /// </summary>

    public string GenerateSubEntityClass(

        EntityDefinition mainEntity,

        SubEntityDefinition subEntity)

    {

        var sb = new StringBuilder();

        // Using statements

        sb.AppendLine("using System;");

        sb.AppendLine("using System.ComponentModel.DataAnnotations;");

        sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");

        sb.AppendLine();

        // Namespace

        sb.AppendLine($"namespace {mainEntity.Namespace}");

        sb.AppendLine("{");

        // XML文档注释

        sb.AppendLine("    /// <summary>");

        var displayName = MultilingualTextHelper.Resolve(subEntity.DisplayName, subEntity.Code);

        sb.AppendLine($"    /// {displayName}（子实体）");

        var description = MultilingualTextHelper.Resolve(subEntity.Description, string.Empty);

        if (!string.IsNullOrEmpty(description))

        {

            sb.AppendLine($"    /// {description}");

        }

        sb.AppendLine($"    /// 所属主实体: {mainEntity.EntityName}");

        sb.AppendLine($"    /// 自动生成于: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

        sb.AppendLine("    /// </summary>");

        // Table attribute

        var tableName = $"{mainEntity.DefaultTableName}_{subEntity.Code}";

        sb.AppendLine($"    [Table(\"{tableName}\")]");

        // Class declaration

        sb.AppendLine($"    public class {subEntity.Code}");

        sb.AppendLine("    {");

        // 主键ID（自动添加）

        sb.AppendLine("        /// <summary>");

        sb.AppendLine("        /// 主键ID");

        sb.AppendLine("        /// </summary>");

        sb.AppendLine("        [Key]");

        sb.AppendLine("        public Guid Id { get; set; } = Guid.NewGuid();");

        sb.AppendLine();

        // 外键字段（指向主实体）

        var foreignKeyField = subEntity.ForeignKeyField ?? $"{mainEntity.EntityName}Id";

        sb.AppendLine("        /// <summary>");

        sb.AppendLine($"        /// 所属{mainEntity.EntityName}的ID（外键）");

        sb.AppendLine("        /// </summary>");

        sb.AppendLine("        [Required]");

        sb.AppendLine($"        public Guid {foreignKeyField} {{ get; set; }}");

        sb.AppendLine();

        // 生成字段属性

        foreach (var field in subEntity.Fields.OrderBy(f => f.SortOrder))

        {

            GenerateProperty(sb, field);

        }

        // 导航属性（指向主实体）

        sb.AppendLine("        /// <summary>");

        sb.AppendLine($"        /// 导航属性：所属{mainEntity.EntityName}");

        sb.AppendLine("        /// </summary>");

        sb.AppendLine($"        public {mainEntity.EntityName}? {mainEntity.EntityName} {{ get; set; }}");

        sb.AppendLine();

        sb.AppendLine("    }");

        sb.AppendLine("}");

        return sb.ToString();

    }

    /// <summary>
    /// 生成AggVO类代码
    /// </summary>

    public string GenerateAggregateVoClass(

        EntityDefinition mainEntity,

        List<SubEntityDefinition> subEntities)

    {

        var sb = new StringBuilder();

        // Using statements

        sb.AppendLine("using System;");

        sb.AppendLine("using System.Collections.Generic;");

        sb.AppendLine();

        // Namespace

        sb.AppendLine($"namespace {mainEntity.Namespace}");

        sb.AppendLine("{");

        // XML文档注释

        sb.AppendLine("    /// <summary>");

        var displayName = MultilingualTextHelper.Resolve(mainEntity.DisplayName, mainEntity.EntityName);

        sb.AppendLine($"    /// {displayName}聚合VO");

        sb.AppendLine($"    /// 包含主实体及其所有子实体");

        sb.AppendLine($"    /// 自动生成于: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

        sb.AppendLine("    /// </summary>");

        // Class declaration

        sb.AppendLine($"    public class {mainEntity.EntityName}AggVo");

        sb.AppendLine("    {");

        // 主实体属性

        sb.AppendLine("        /// <summary>");

        sb.AppendLine($"        /// {displayName}（主实体）");

        sb.AppendLine("        /// </summary>");

        sb.AppendLine($"        public {mainEntity.EntityName} Master {{ get; set; }} = null!;");

        sb.AppendLine();

        // 子实体列表属性

        foreach (var subEntity in subEntities.OrderBy(s => s.SortOrder))

        {

            var subDisplayName = MultilingualTextHelper.Resolve(subEntity.DisplayName, subEntity.Code);

            var propertyName = subEntity.CollectionPropertyName ?? subEntity.Code;

            sb.AppendLine("        /// <summary>");

            sb.AppendLine($"        /// {subDisplayName}列表");

            sb.AppendLine("        /// </summary>");

            sb.AppendLine($"        public List<{subEntity.Code}> {propertyName} {{ get; set; }} = new();");

            sb.AppendLine();

        }

        sb.AppendLine("    }");

        sb.AppendLine("}");

        return sb.ToString();

    }

    private void GenerateProperty(StringBuilder sb, FieldMetadata field)

    {

        var displayName = MultilingualTextHelper.Resolve(field.DisplayName, field.PropertyName);

        // XML注释

        sb.AppendLine("        /// <summary>");

        sb.AppendLine($"        /// {displayName}");

        sb.AppendLine("        /// </summary>");

        // Attributes

        var attributes = new List<string>();

        if (field.IsRequired)

        {

            attributes.Add("[Required]");

        }

        if (field.DataType == FieldDataType.String && field.Length.HasValue)

        {

            attributes.Add($"[MaxLength({field.Length.Value})]");

        }

        foreach (var attr in attributes)

        {

            sb.AppendLine($"        {attr}");

        }

        // Property declaration

        var csType = MapDataTypeToCSharp(field.DataType);

        var nullableSuffix = !field.IsRequired && IsValueType(field.DataType) ? "?" : "";

        var defaultValue = GetDefaultValue(field);

        sb.AppendLine($"        public {csType}{nullableSuffix} {field.PropertyName} {{ get; set; }}{defaultValue}");

        sb.AppendLine();

    }

    private string MapDataTypeToCSharp(string dataType)

    {

        return dataType switch

        {

            FieldDataType.String => "string",

            FieldDataType.Int32 => "int",

            FieldDataType.Int64 => "long",

            FieldDataType.Decimal => "decimal",

            FieldDataType.DateTime => "DateTime",

            FieldDataType.Date => "DateOnly",

            FieldDataType.Boolean => "bool",

            FieldDataType.Guid => "Guid",

            _ => "object"

        };

    }

    private bool IsValueType(string dataType)

    {

        return dataType switch

        {

            FieldDataType.Int32 => true,

            FieldDataType.Int64 => true,

            FieldDataType.Decimal => true,

            FieldDataType.DateTime => true,

            FieldDataType.Date => true,

            FieldDataType.Boolean => true,

            FieldDataType.Guid => true,

            _ => false

        };

    }

    private string GetDefaultValue(FieldMetadata field)

    {

        if (!string.IsNullOrWhiteSpace(field.DefaultValue))

        {

            return field.DataType switch

            {

                FieldDataType.String => $" = \"{field.DefaultValue}\";",

                FieldDataType.Boolean => $" = {field.DefaultValue.ToLower()};",

                _ => $" = {field.DefaultValue};"

            };

        }

        if (field.IsRequired && field.DataType == FieldDataType.String)

        {

            return " = string.Empty;";

        }

        if (field.DataType == FieldDataType.Guid && field.IsRequired)

        {

            return " = Guid.NewGuid();";

        }

        return string.Empty;

    }

}

