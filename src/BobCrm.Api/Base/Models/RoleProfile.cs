using System.ComponentModel.DataAnnotations;
using BobCrm.Api.Abstractions;

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
            DisplayName = new Dictionary<string, string?>
            {
                { "ja", "ロール" },
                { "zh", "角色" },
                { "en", "Role" }
            },
            Description = new Dictionary<string, string?>
            {
                { "ja", "ロールプロファイルを管理します" },
                { "zh", "管理角色档案" },
                { "en", "Manage role profiles" }
            },
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
                PropertyName = "OrganizationId",
                DisplayName = new Dictionary<string, string?>
                {
                    { "ja", "組織ID" },
                    { "zh", "组织ID" },
                    { "en", "Organization ID" }
                },
                DataType = FieldDataType.Guid,
                IsRequired = false,
                SortOrder = 2,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "Code",
                DisplayName = new Dictionary<string, string?>
                {
                    { "ja", "ロールコード" },
                    { "zh", "角色代码" },
                    { "en", "Role Code" }
                },
                DataType = FieldDataType.String,
                Length = 64,
                IsRequired = true,
                SortOrder = 3,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "Name",
                DisplayName = new Dictionary<string, string?>
                {
                    { "ja", "ロール名" },
                    { "zh", "角色名称" },
                    { "en", "Role Name" }
                },
                DataType = FieldDataType.String,
                Length = 128,
                IsRequired = true,
                SortOrder = 4,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "Description",
                DisplayName = new Dictionary<string, string?>
                {
                    { "ja", "説明" },
                    { "zh", "描述" },
                    { "en", "Description" }
                },
                DataType = FieldDataType.String,
                Length = 256,
                IsRequired = false,
                SortOrder = 5,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "IsSystem",
                DisplayName = new Dictionary<string, string?>
                {
                    { "ja", "システムロール" },
                    { "zh", "系统角色" },
                    { "en", "Is System Role" }
                },
                DataType = FieldDataType.Boolean,
                IsRequired = true,
                SortOrder = 6,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "IsEnabled",
                DisplayName = new Dictionary<string, string?>
                {
                    { "ja", "有効" },
                    { "zh", "启用" },
                    { "en", "Is Enabled" }
                },
                DataType = FieldDataType.Boolean,
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
