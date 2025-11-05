using BobCrm.Api.Models;

namespace BobCrm.Api.Services;

/// <summary>
/// 实体元数据服务 - 管理可用于创建模板的根实体
/// 采用配置化方式，而非完全动态反射，以便精确控制哪些实体可用
/// </summary>
public class EntityMetadataService
{
    private readonly List<EntityMetadata> _entities;

    public EntityMetadataService()
    {
        // 初始化实体元数据列表
        // 原则：只有根实体（非聚合子实体）才能创建独立模板
        _entities = new List<EntityMetadata>
        {
            new EntityMetadata
            {
                EntityType = "customer",
                DisplayNameKey = "ENTITY_CUSTOMER",
                DescriptionKey = "ENTITY_CUSTOMER_DESC",
                ApiEndpoint = "/api/customers",
                IsRootEntity = true,
                IsEnabled = true,
                Order = 1,
                Icon = "user",
                Category = "core"
            },
            new EntityMetadata
            {
                EntityType = "product",
                DisplayNameKey = "ENTITY_PRODUCT",
                DescriptionKey = "ENTITY_PRODUCT_DESC",
                ApiEndpoint = "/api/products",
                IsRootEntity = true,
                IsEnabled = false, // 暂未实现Product API，标记为禁用
                Order = 2,
                Icon = "shopping",
                Category = "sales"
            },
            new EntityMetadata
            {
                EntityType = "order",
                DisplayNameKey = "ENTITY_ORDER",
                DescriptionKey = "ENTITY_ORDER_DESC",
                ApiEndpoint = "/api/orders",
                IsRootEntity = true,
                IsEnabled = false, // 暂未实现Order API
                Order = 3,
                Icon = "file-text",
                Category = "sales"
            },
            new EntityMetadata
            {
                EntityType = "contact",
                DisplayNameKey = "ENTITY_CONTACT",
                DescriptionKey = "ENTITY_CONTACT_DESC",
                ApiEndpoint = "/api/contacts",
                IsRootEntity = true,
                IsEnabled = false, // 暂未实现Contact API
                Order = 4,
                Icon = "contacts",
                Category = "core"
            },
            new EntityMetadata
            {
                EntityType = "opportunity",
                DisplayNameKey = "ENTITY_OPPORTUNITY",
                DescriptionKey = "ENTITY_OPPORTUNITY_DESC",
                ApiEndpoint = "/api/opportunities",
                IsRootEntity = true,
                IsEnabled = false, // 暂未实现Opportunity API
                Order = 5,
                Icon = "dollar",
                Category = "sales"
            }
        };
    }

    /// <summary>
    /// 获取所有可用的根实体（已启用且为根实体）
    /// </summary>
    public List<EntityMetadata> GetAvailableRootEntities()
    {
        return _entities
            .Where(e => e.IsRootEntity && e.IsEnabled)
            .OrderBy(e => e.Order)
            .ToList();
    }

    /// <summary>
    /// 获取所有根实体（包括未启用的）
    /// </summary>
    public List<EntityMetadata> GetAllRootEntities()
    {
        return _entities
            .Where(e => e.IsRootEntity)
            .OrderBy(e => e.Order)
            .ToList();
    }

    /// <summary>
    /// 根据类型获取实体元数据
    /// </summary>
    public EntityMetadata? GetEntityMetadata(string entityType)
    {
        return _entities.FirstOrDefault(e => 
            e.EntityType.Equals(entityType, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 验证实体类型是否可用于创建模板
    /// </summary>
    public bool IsValidEntityType(string entityType)
    {
        var entity = GetEntityMetadata(entityType);
        return entity != null && entity.IsRootEntity && entity.IsEnabled;
    }
}

