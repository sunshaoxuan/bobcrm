using System.ComponentModel.DataAnnotations;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Domain.Attributes;

namespace BobCrm.Api.Domain;

/// <summary>
/// 客户实体 - 实现IEntityMetadataProvider以支持自动注册到元数据表
/// </summary>
public class Customer : IEntityMetadataProvider
{
    public int Id { get; set; }
    [Required, MaxLength(64)] public string Code { get; set; } = string.Empty;
    
    [Required, MaxLength(256)]
    [Localizable(Required = false, MaxLength = 256, Hint = "客户名称")]
    public string Name { get; set; } = string.Empty;
    
    public int Version { get; set; } = 1;
    public string? ExtData { get; set; }

    /// <summary>
    /// 提供Customer实体的元数据
    /// 系统启动时会自动调用此方法并注册到EntityMetadata表
    /// </summary>
    public static Data.Entities.EntityMetadata GetMetadata()
    {
        return new Data.Entities.EntityMetadata
        {
            EntityType = "customer",
            DisplayNameKey = "ENTITY_CUSTOMER",
            DescriptionKey = "ENTITY_CUSTOMER_DESC",
            ApiEndpoint = "/api/customers",
            IsRootEntity = true,
            IsEnabled = true,
            Order = 1,
            Icon = "user",
            Category = "core",
            CreatedAt = DateTime.UtcNow
        };
    }
}

