using BobCrm.Api.Domain.Models;
using System.Text;

namespace BobCrm.Api.Services.CodeGeneration;

/// <summary>
/// AggVO（聚合值对象）代码生成器
/// 根据主子表结构生成聚合根类
/// </summary>
public class AggVOCodeGenerator : IAggVOCodeGenerator
{
    /// <summary>
    /// 为主实体生成 AggVO 类代码
    /// </summary>
    /// <param name="masterEntity">主实体定义</param>
    /// <param name="childEntities">子实体定义列表</param>
    /// <returns>生成的C#代码</returns>
    public string GenerateAggVOClass(EntityDefinition masterEntity, List<EntityDefinition> childEntities)
    {
        if (masterEntity.StructureType == EntityStructureType.Single)
        {
            throw new ArgumentException($"Entity '{masterEntity.EntityName}' is not a master-detail structure");
        }

        var sb = new StringBuilder();
        var aggVOClassName = $"{masterEntity.EntityName}AggVO";
        var headVOClassName = $"{masterEntity.EntityName}VO";

        // Using statements
        sb.AppendLine("using BobCrm.Api.Domain.Aggregates;");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();

        // Namespace
        sb.AppendLine($"namespace {masterEntity.Namespace}.Aggregates;");
        sb.AppendLine();

        // Class comment
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {masterEntity.EntityName} 聚合根值对象");
        sb.AppendLine($"/// 包含主实体 {masterEntity.EntityName} 和 {childEntities.Count} 个子实体");
        sb.AppendLine("/// </summary>");

        // Class declaration
        sb.AppendLine($"public class {aggVOClassName} : AggBaseVO");
        sb.AppendLine("{");

        // 1. 主实体属性
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// 主实体：{masterEntity.EntityName}");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public {headVOClassName} HeadVO {{ get; set; }} = new {headVOClassName}();");
        sb.AppendLine();

        // 2. 子实体属性
        foreach (var childEntity in childEntities)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// 子实体：{childEntity.EntityName}");
            sb.AppendLine("    /// </summary>");

            // 如果子实体本身也是主子结构，则使用其 AggVO
            if (childEntity.StructureType != EntityStructureType.Single)
            {
                var childAggVOClassName = $"{childEntity.EntityName}AggVO";
                sb.AppendLine($"    public List<{childAggVOClassName}> {childEntity.EntityName}AggVOs {{ get; set; }} = new List<{childAggVOClassName}>();");
            }
            else
            {
                var childVOClassName = $"{childEntity.EntityName}VO";
                sb.AppendLine($"    public List<{childVOClassName}> {childEntity.EntityName}VOs {{ get; set; }} = new List<{childVOClassName}>();");
            }
            sb.AppendLine();
        }

        // 3. GetHeadEntityType 方法
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 获取主实体类型");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public override Type GetHeadEntityType() => typeof({headVOClassName});");
        sb.AppendLine();

        // 4. GetSubEntityTypes 方法
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 获取所有子实体类型");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public override List<Type> GetSubEntityTypes()");
        sb.AppendLine("    {");
        sb.AppendLine("        return new List<Type>");
        sb.AppendLine("        {");

        foreach (var childEntity in childEntities)
        {
            if (childEntity.StructureType != EntityStructureType.Single)
            {
                sb.AppendLine($"            typeof({childEntity.EntityName}AggVO),");
            }
            else
            {
                sb.AppendLine($"            typeof({childEntity.EntityName}VO),");
            }
        }

        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();

        // 5. GetHeadVO 方法
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 获取主实体VO");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public override object GetHeadVO() => HeadVO;");
        sb.AppendLine();

        // 6. SetHeadVO 方法
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 设置主实体VO");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public override void SetHeadVO(object headVO) => HeadVO = ({headVOClassName})headVO;");
        sb.AppendLine();

        // 7. SaveAsync 方法（占位实现）
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 保存聚合（主实体 + 所有子实体）");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public override async Task<int> SaveAsync()");
        sb.AppendLine("    {");
        sb.AppendLine("        // TODO: 实现级联保存逻辑");
        sb.AppendLine("        // 由 AggVOService 提供具体实现");
        sb.AppendLine("        throw new NotImplementedException(\"SaveAsync must be implemented by AggVOService\");");
        sb.AppendLine("    }");
        sb.AppendLine();

        // 8. LoadAsync 方法（占位实现）
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 加载聚合（主实体 + 所有子实体）");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public override async Task LoadAsync(int id)");
        sb.AppendLine("    {");
        sb.AppendLine("        // TODO: 实现级联加载逻辑");
        sb.AppendLine("        // 由 AggVOService 提供具体实现");
        sb.AppendLine("        throw new NotImplementedException(\"LoadAsync must be implemented by AggVOService\");");
        sb.AppendLine("    }");
        sb.AppendLine();

        // 9. DeleteAsync 方法（占位实现）
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 删除聚合（主实体 + 所有子实体）");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public override async Task DeleteAsync()");
        sb.AppendLine("    {");
        sb.AppendLine("        // TODO: 实现级联删除逻辑");
        sb.AppendLine("        // 由 AggVOService 提供具体实现");
        sb.AppendLine("        throw new NotImplementedException(\"DeleteAsync must be implemented by AggVOService\");");
        sb.AppendLine("    }");
        sb.AppendLine();

        // 10. 便捷方法：获取特定子实体列表
        foreach (var childEntity in childEntities)
        {
            var methodName = $"Get{childEntity.EntityName}List";
            string returnType;
            string propertyName;

            if (childEntity.StructureType != EntityStructureType.Single)
            {
                returnType = $"List<{childEntity.EntityName}AggVO>";
                propertyName = $"{childEntity.EntityName}AggVOs";
            }
            else
            {
                returnType = $"List<{childEntity.EntityName}VO>";
                propertyName = $"{childEntity.EntityName}VOs";
            }

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// 获取 {childEntity.EntityName} 子实体列表");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public {returnType} {methodName}() => {propertyName};");
            sb.AppendLine();
        }

        // Class closing
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// 批量生成多个主实体的 AggVO 类代码
    /// </summary>
    /// <param name="masterEntities">主实体定义列表</param>
    /// <param name="allEntities">所有实体定义</param>
    /// <returns>实体名称 -> 生成代码的字典</returns>
    public Dictionary<string, string> GenerateBatchAggVOClasses(
        List<EntityDefinition> masterEntities,
        List<EntityDefinition> allEntities)
    {
        var result = new Dictionary<string, string>();

        foreach (var masterEntity in masterEntities)
        {
            if (masterEntity.StructureType == EntityStructureType.Single)
            {
                continue; // 跳过单实体
            }

            // 查找该主实体的所有直接子实体
            var childEntities = allEntities
                .Where(e => e.ParentEntityId == masterEntity.Id)
                .OrderBy(e => e.Order)
                .ToList();

            if (childEntities.Any())
            {
                var code = GenerateAggVOClass(masterEntity, childEntities);
                result[masterEntity.EntityName] = code;
            }
        }

        return result;
    }

    /// <summary>
    /// 生成 AggVO 的辅助文件（VO 类定义）
    /// 如果 EntityVO 不存在，则生成简单的 VO 类
    /// </summary>
    public string GenerateVOClass(EntityDefinition entity)
    {
        var sb = new StringBuilder();
        var voClassName = $"{entity.EntityName}VO";

        sb.AppendLine("using System;");
        sb.AppendLine();
        sb.AppendLine($"namespace {entity.Namespace};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {entity.EntityName} 值对象");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public class {voClassName}");
        sb.AppendLine("{");

        // 生成属性（基于字段定义）
        foreach (var field in entity.Fields.OrderBy(f => f.SortOrder))
        {
            var csType = MapToCSharpType(field);
            sb.AppendLine($"    public {csType} {field.PropertyName} {{ get; set; }}");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private string MapToCSharpType(FieldMetadata field)
    {
        var baseType = field.DataType switch
        {
            FieldDataType.String => "string",
            FieldDataType.Integer => "int",
            FieldDataType.Long => "long",
            FieldDataType.Decimal => "decimal",
            FieldDataType.Boolean => "bool",
            FieldDataType.DateTime => "DateTime",
            FieldDataType.Date => "DateOnly",
            FieldDataType.Text => "string",
            FieldDataType.Guid => "Guid",
            _ => "object"
        };

        // 如果不是必填且是值类型，添加?
        if (!field.IsRequired && field.DataType != FieldDataType.String && field.DataType != FieldDataType.Text)
        {
            baseType += "?";
        }

        return baseType;
    }
}
