using System.ComponentModel.DataAnnotations;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Base;

namespace BobCrm.Api.Base.Models;

/// <summary>
/// 组织节点（树形结构）
/// </summary>
public class OrganizationNode : IBizEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// 节点编码，在同一层级内唯一
    /// </summary>
    [Required, MaxLength(64)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 节点名称
    /// </summary>
    [Required, MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 父节点
    /// </summary>
    public Guid? ParentId { get; set; }

    public OrganizationNode? Parent { get; set; }

    /// <summary>
    /// 子节点集合
    /// </summary>
    public List<OrganizationNode> Children { get; set; } = new();

    /// <summary>
    /// 树形层级（根为0）
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// 递归展开编码，每层追加 2 位（例如 01.02.03）
    /// </summary>
    [Required, MaxLength(256)]
    public string PathCode { get; set; } = string.Empty;

    /// <summary>
    /// 同级排序
    /// </summary>
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 提供OrganizationNode实体的初始定义
    /// 系统启动时会自动调用此方法并同步到EntityDefinition表
    /// </summary>
    public static EntityDefinition GetInitialDefinition()
    {
        var type = typeof(OrganizationNode);
        var definition = new EntityDefinition
        {
            Namespace = type.Namespace ?? "BobCrm.Api.Base.Models",
            EntityName = type.Name,
            FullTypeName = type.FullName ?? "BobCrm.Api.Base.Models.OrganizationNode",
            EntityRoute = "organization-nodes",
            DisplayName = SystemEntityI18n.Dict("ENTITY_ORGANIZATION_NODE"),
            Description = SystemEntityI18n.Dict("ENTITY_ORGANIZATION_NODE_DESC"),
            ApiEndpoint = "/api/organization-nodes",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            Source = EntitySource.System,
            IsRootEntity = true,
            IsEnabled = true,
            Order = 10,
            Icon = "apartment",
            Category = "system"
        };

        // 定义字段元数据
        definition.Fields = new List<FieldMetadata>
        {
            new FieldMetadata
            {
                PropertyName = "Id",
                DisplayName = SystemEntityI18n.Dict("LBL_FIELD_ID"),
                DataType = FieldDataType.Guid,
                IsRequired = true,
                SortOrder = 1,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "Code",
                DisplayName = SystemEntityI18n.Dict("LBL_FIELD_CODE"),
                DataType = FieldDataType.String,
                Length = 64,
                IsRequired = true,
                SortOrder = 2,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "Name",
                DisplayName = SystemEntityI18n.Dict("LBL_FIELD_NAME"),
                DataType = FieldDataType.String,
                Length = 128,
                IsRequired = true,
                SortOrder = 3,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "ParentId",
                DisplayName = SystemEntityI18n.Dict("LBL_FIELD_PARENT_ID"),
                DataType = FieldDataType.Guid,
                IsRequired = false,
                SortOrder = 4,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "Level",
                DisplayName = SystemEntityI18n.Dict("LBL_FIELD_LEVEL"),
                DataType = FieldDataType.Integer,
                IsRequired = true,
                SortOrder = 5,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "PathCode",
                DisplayName = SystemEntityI18n.Dict("LBL_FIELD_PATH_CODE"),
                DataType = FieldDataType.String,
                Length = 256,
                IsRequired = true,
                SortOrder = 6,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "SortOrder",
                DisplayName = SystemEntityI18n.Dict("LBL_SORT_ORDER"),
                DataType = FieldDataType.Integer,
                IsRequired = true,
                SortOrder = 7,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "CreatedAt",
                DisplayName = SystemEntityI18n.Dict("LBL_FIELD_CREATED_AT"),
                DataType = FieldDataType.DateTime,
                IsRequired = true,
                SortOrder = 8,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "UpdatedAt",
                DisplayName = SystemEntityI18n.Dict("LBL_FIELD_UPDATED_AT"),
                DataType = FieldDataType.DateTime,
                IsRequired = true,
                SortOrder = 9,
                Source = FieldSource.System
            }
        };

        // 定义实现的接口
        definition.Interfaces = new List<EntityInterface>();

        return definition;
    }
}
