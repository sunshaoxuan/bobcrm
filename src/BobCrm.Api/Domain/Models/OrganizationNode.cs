using System.ComponentModel.DataAnnotations;
using BobCrm.Api.Abstractions;

namespace BobCrm.Api.Domain.Models;

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
            Namespace = type.Namespace ?? "BobCrm.Api.Domain.Models",
            EntityName = type.Name,
            FullTypeName = type.FullName ?? "BobCrm.Api.Domain.Models.OrganizationNode",
            EntityRoute = "organization-nodes",
            DisplayName = new Dictionary<string, string?>
            {
                { "ja", "組織ノード" },
                { "zh", "组织节点" },
                { "en", "Organization Node" }
            },
            Description = new Dictionary<string, string?>
            {
                { "ja", "組織のツリー構造を管理します" },
                { "zh", "管理组织的树形结构" },
                { "en", "Manage organization tree structure" }
            },
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
                DisplayName = new Dictionary<string, string?>
                {
                    { "ja", "ID" },
                    { "zh", "ID" },
                    { "en", "ID" }
                },
                DataType = FieldDataType.Guid,
                IsRequired = true,
                SortOrder = 1,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "Code",
                DisplayName = new Dictionary<string, string?>
                {
                    { "ja", "ノードコード" },
                    { "zh", "节点编码" },
                    { "en", "Node Code" }
                },
                DataType = FieldDataType.String,
                Length = 64,
                IsRequired = true,
                SortOrder = 2,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "Name",
                DisplayName = new Dictionary<string, string?>
                {
                    { "ja", "ノード名" },
                    { "zh", "节点名称" },
                    { "en", "Node Name" }
                },
                DataType = FieldDataType.String,
                Length = 128,
                IsRequired = true,
                SortOrder = 3,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "ParentId",
                DisplayName = new Dictionary<string, string?>
                {
                    { "ja", "親ノードID" },
                    { "zh", "父节点ID" },
                    { "en", "Parent Node ID" }
                },
                DataType = FieldDataType.Guid,
                IsRequired = false,
                SortOrder = 4,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "Level",
                DisplayName = new Dictionary<string, string?>
                {
                    { "ja", "レベル" },
                    { "zh", "层级" },
                    { "en", "Level" }
                },
                DataType = FieldDataType.Integer,
                IsRequired = true,
                SortOrder = 5,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "PathCode",
                DisplayName = new Dictionary<string, string?>
                {
                    { "ja", "パスコード" },
                    { "zh", "路径编码" },
                    { "en", "Path Code" }
                },
                DataType = FieldDataType.String,
                Length = 256,
                IsRequired = true,
                SortOrder = 6,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "SortOrder",
                DisplayName = new Dictionary<string, string?>
                {
                    { "ja", "並び順" },
                    { "zh", "排序" },
                    { "en", "Sort Order" }
                },
                DataType = FieldDataType.Integer,
                IsRequired = true,
                SortOrder = 7,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "CreatedAt",
                DisplayName = new Dictionary<string, string?>
                {
                    { "ja", "作成日時" },
                    { "zh", "创建时间" },
                    { "en", "Created At" }
                },
                DataType = FieldDataType.DateTime,
                IsRequired = true,
                SortOrder = 8,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "UpdatedAt",
                DisplayName = new Dictionary<string, string?>
                {
                    { "ja", "更新日時" },
                    { "zh", "更新时间" },
                    { "en", "Updated At" }
                },
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
