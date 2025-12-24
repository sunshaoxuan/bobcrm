using System.ComponentModel.DataAnnotations;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Base.Attributes;
using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Base;

/// <summary>
/// 客户实体 - 实现IBizEntity以支持统一的实体定义系统
/// </summary>
public class Customer : IBizEntity
{
    public int Id { get; set; }
    [Required, MaxLength(64)] public string Code { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    [Localizable(Required = false, MaxLength = 256, Hint = "LBL_FIELD_NAME")]
    public string Name { get; set; } = string.Empty;

    public int Version { get; set; } = 1;
    public string? ExtData { get; set; }

    /// <summary>
    /// 提供Customer实体的初始定义
    /// 系统启动时会自动调用此方法并同步到EntityDefinition表
    /// </summary>
    public static EntityDefinition GetInitialDefinition()
    {
        var type = typeof(Customer);
        var definition = new EntityDefinition
        {
            Namespace = type.Namespace ?? "BobCrm.Api.Base",
            EntityName = type.Name,
            FullTypeName = type.FullName ?? "BobCrm.Api.Base.Customer",
            EntityRoute = "customer",
            DisplayName = SystemEntityI18n.Dict("ENTITY_CUSTOMER"),
            Description = SystemEntityI18n.Dict("ENTITY_CUSTOMER_DESC"),
            ApiEndpoint = "/api/customers",
            StructureType = EntityStructureType.Single,
            Status = EntityStatus.Published,
            Source = EntitySource.System,
            IsRootEntity = true,
            IsEnabled = true,
            Order = 1,
            Icon = "user",
            Category = "core"
        };

        // 定义字段元数据
        definition.Fields = new List<FieldMetadata>
        {
            new FieldMetadata
            {
                PropertyName = "Id",
                DisplayName = SystemEntityI18n.Dict("LBL_FIELD_ID"),
                DataType = FieldDataType.Integer,
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
                Length = 256,
                IsRequired = true,
                SortOrder = 3,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "Version",
                DisplayName = SystemEntityI18n.Dict("LBL_FIELD_VERSION"),
                DataType = FieldDataType.Integer,
                IsRequired = true,
                DefaultValue = "1",
                SortOrder = 4,
                Source = FieldSource.System
            },
            new FieldMetadata
            {
                PropertyName = "ExtData",
                DisplayName = SystemEntityI18n.Dict("LBL_FIELD_EXT_DATA"),
                DataType = FieldDataType.Text,
                IsRequired = false,
                SortOrder = 5,
                Source = FieldSource.System
            }
        };

        // 定义实现的接口
        definition.Interfaces = new List<EntityInterface>
        {
            new EntityInterface
            {
                InterfaceType = EntityInterfaceType.Base,
                IsEnabled = true,
                IsLocked = false
            },
            new EntityInterface
            {
                InterfaceType = EntityInterfaceType.Archive,
                IsEnabled = true,
                IsLocked = false
            },
            new EntityInterface
            {
                InterfaceType = EntityInterfaceType.Version,
                IsEnabled = true,
                IsLocked = false
            }
        };

        return definition;
    }
}

