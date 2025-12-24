using System.ComponentModel.DataAnnotations;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Base;

namespace BobCrm.Api.Base.Models;

/// <summary>
/// 角色档案 - 绑定组织并聚合功能/数据范围
/// </summary>
public class RoleProfile : IBizEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 所属组织（为空表示全局角色）
    /// </summary>
    public Guid? OrganizationId { get; set; }

    [Required, MaxLength(64)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? Description { get; set; }

    public bool IsSystem { get; set; }
    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<RoleFunctionPermission> Functions { get; set; } = new();
    public List<RoleDataScope> DataScopes { get; set; } = new();
    public List<RoleAssignment> Assignments { get; set; } = new();
    public List<FieldPermission> FieldPermissions { get; set; } = new();

    /// <summary>
    /// 提供RoleProfile实体的初始定义
    /// 系统启动时会自动调用此方法并同步到EntityDefinition表
    /// </summary>
    public static EntityDefinition GetInitialDefinition()
    {
        var type = typeof(RoleProfile);
        var definition = new EntityDefinition
        {
            Namespace = type.Namespace ?? "BobCrm.Api.Base.Models",
            EntityName = type.Name,
            FullTypeName = type.FullName ?? "BobCrm.Api.Base.Models.RoleProfile",
            EntityRoute = "role",
            DisplayName = SystemEntityI18n.Dict("ENTITY_ROLE_PROFILE"),
            Description = SystemEntityI18n.Dict("ENTITY_ROLE_PROFILE_DESC"),
            ApiEndpoint = "/api/roles",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            Source = EntitySource.System,
            IsRootEntity = true,
            IsEnabled = true,
            Order = 11,
            Icon = "team",
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
                PropertyName = "OrganizationId",
                DisplayName = SystemEntityI18n.Dict("LBL_FIELD_ORGANIZATION_ID"),
                DataType = FieldDataType.Guid,
                IsRequired = false,
                SortOrder = 2,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "Code",
                DisplayName = SystemEntityI18n.Dict("LBL_FIELD_CODE"),
                DataType = FieldDataType.String,
                Length = 64,
                IsRequired = true,
                SortOrder = 3,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "Name",
                DisplayName = SystemEntityI18n.Dict("LBL_FIELD_NAME"),
                DataType = FieldDataType.String,
                Length = 128,
                IsRequired = true,
                SortOrder = 4,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "Description",
                DisplayName = SystemEntityI18n.Dict("LBL_DESCRIPTION"),
                DataType = FieldDataType.String,
                Length = 256,
                IsRequired = false,
                SortOrder = 5,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "IsSystem",
                DisplayName = SystemEntityI18n.Dict("LBL_SYSTEM_ROLE"),
                DataType = FieldDataType.Boolean,
                IsRequired = true,
                SortOrder = 6,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "IsEnabled",
                DisplayName = SystemEntityI18n.Dict("LBL_ENABLED"),
                DataType = FieldDataType.Boolean,
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
