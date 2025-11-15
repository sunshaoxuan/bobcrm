using BobCrm.Api.Infrastructure;
using BobCrm.Api.Base.Aggregates;
using BobCrm.Api.Base.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BobCrm.Api.Services.Aggregates;

/// <summary>
/// AggVO 聚合根服务
/// 提供 Save、Load、Delete 等操作的实现
/// </summary>
public class AggVOService : IAggVOService
{
    private readonly AppDbContext _context;
    private readonly ReflectionPersistenceService _persistenceService;
    private readonly ILogger<AggVOService> _logger;

    public AggVOService(
        AppDbContext context,
        ReflectionPersistenceService persistenceService,
        ILogger<AggVOService> logger)
    {
        _context = context;
        _persistenceService = persistenceService;
        _logger = logger;
    }

    /// <summary>
    /// 保存聚合（级联保存主实体和所有子实体）
    /// </summary>
    /// <param name="aggVO">聚合根对象</param>
    /// <returns>主实体ID</returns>
    public async Task<int> SaveAggVOAsync(AggBaseVO aggVO)
    {
        // 验证数据
        var errors = aggVO.Validate();
        if (errors.Any())
        {
            throw new InvalidOperationException($"Validation failed: {string.Join(", ", errors)}");
        }

        // 获取主实体类型名称
        var headType = aggVO.GetHeadEntityType();
        var headTypeName = headType.FullName!;

        // 获取主实体定义
        var masterEntity = await _context.EntityDefinitions
            .Include(e => e.Fields)
            .FirstOrDefaultAsync(e => e.FullTypeName == headTypeName);

        if (masterEntity == null)
        {
            throw new ArgumentException($"Entity definition not found for type '{headTypeName}'");
        }

        // 查找所有子实体定义
        var childEntities = await _context.EntityDefinitions
            .Include(e => e.Fields)
            .Where(e => e.ParentEntityId == masterEntity.Id && e.AutoCascadeSave)
            .OrderBy(e => e.Order)
            .ToListAsync();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. 保存主实体
            var headVO = aggVO.GetHeadVO();
            var masterData = ConvertVOToDictionary(headVO);

            int masterId = GetEntityId(masterData);

            if (masterId > 0)
            {
                // 更新主实体
                _logger.LogInformation("Updating master entity {EntityName} with ID {Id}", masterEntity.EntityName, masterId);
                await _persistenceService.UpdateAsync(headTypeName, masterId, masterData);
            }
            else
            {
                // 创建主实体
                _logger.LogInformation("Creating new master entity {EntityName}", masterEntity.EntityName);
                var created = await _persistenceService.CreateAsync(headTypeName, masterData);
                masterId = GetEntityId(created);

                // 更新 HeadVO 的 ID
                SetEntityId(headVO, masterId);
            }

            // 2. 级联保存子实体
            foreach (var childEntity in childEntities)
            {
                await SaveChildEntitiesAsync(aggVO, childEntity, masterId);
            }

            await transaction.CommitAsync();
            _logger.LogInformation("Successfully saved AggVO for {EntityName} with ID {Id}", masterEntity.EntityName, masterId);
            return masterId;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to save AggVO for {EntityName}", masterEntity.EntityName);
            throw;
        }
    }

    /// <summary>
    /// 加载聚合（级联加载主实体和所有子实体）
    /// </summary>
    public async Task LoadAggVOAsync(AggBaseVO aggVO, int masterId)
    {
        // 获取主实体类型名称
        var headType = aggVO.GetHeadEntityType();
        var headTypeName = headType.FullName!;

        // 获取主实体定义
        var masterEntity = await _context.EntityDefinitions
            .FirstOrDefaultAsync(e => e.FullTypeName == headTypeName);

        if (masterEntity == null)
        {
            throw new ArgumentException($"Entity definition not found for type '{headTypeName}'");
        }

        // 查找所有子实体定义
        var childEntities = await _context.EntityDefinitions
            .Where(e => e.ParentEntityId == masterEntity.Id)
            .OrderBy(e => e.Order)
            .ToListAsync();

        try
        {
            // 1. 加载主实体
            var masterData = await _persistenceService.GetByIdAsync(headTypeName, masterId);
            if (masterData is null)
            {
                throw new InvalidOperationException($"Record ({headTypeName}) with ID {masterId} was not found.");
            }
            var headVO = ConvertDictionaryToVO(masterData, headType);
            aggVO.SetHeadVO(headVO);

            // 2. 级联加载子实体
            foreach (var childEntity in childEntities)
            {
                await LoadChildEntitiesAsync(aggVO, childEntity, masterId);
            }

            _logger.LogInformation("Successfully loaded AggVO for {EntityName} with ID {Id}", masterEntity.EntityName, masterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load AggVO for {EntityName} with ID {Id}", masterEntity.EntityName, masterId);
            throw;
        }
    }

    /// <summary>
    /// 删除聚合（级联删除主实体和所有子实体）
    /// </summary>
    public async Task DeleteAggVOAsync(AggBaseVO aggVO)
    {
        var masterId = aggVO.GetHeadId();
        if (masterId <= 0)
        {
            throw new InvalidOperationException("Cannot delete AggVO without a valid ID");
        }

        // 获取主实体类型名称
        var headType = aggVO.GetHeadEntityType();
        var headTypeName = headType.FullName!;

        // 获取主实体定义
        var masterEntity = await _context.EntityDefinitions
            .FirstOrDefaultAsync(e => e.FullTypeName == headTypeName);

        if (masterEntity == null)
        {
            throw new ArgumentException($"Entity definition not found for type '{headTypeName}'");
        }

        // 查找所有子实体定义
        var childEntities = await _context.EntityDefinitions
            .Where(e => e.ParentEntityId == masterEntity.Id)
            .OrderBy(e => e.Order)
            .ToListAsync();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. 根据级联删除行为处理子实体
            foreach (var childEntity in childEntities)
            {
                await HandleCascadeDeleteAsync(childEntity, masterId);
            }

            // 2. 删除主实体
            await _persistenceService.DeleteAsync(headTypeName, masterId);

            await transaction.CommitAsync();
            _logger.LogInformation("Successfully deleted AggVO for {EntityName} with ID {Id}", masterEntity.EntityName, masterId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to delete AggVO for {EntityName} with ID {Id}", masterEntity.EntityName, masterId);
            throw;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// 保存子实体
    /// </summary>
    private async Task SaveChildEntitiesAsync(AggBaseVO aggVO, EntityDefinition childEntity, int masterId)
    {
        var childTypeName = childEntity.FullTypeName;
        var childType = Type.GetType(childTypeName);

        if (childType == null)
        {
            _logger.LogWarning("Child entity type '{ChildTypeName}' not found", childTypeName);
            return;
        }

        // 获取子实体列表
        var childEntities = aggVO.GetSubEntities(childType);
        if (childEntities == null || !childEntities.Any())
        {
            _logger.LogDebug("No child entities of type {ChildType} to save", childEntity.EntityName);
            return;
        }

        _logger.LogInformation("Saving {Count} child entities of type {ChildType}", childEntities.Count, childEntity.EntityName);

        foreach (var childVO in childEntities)
        {
            // 如果子实体也是 AggVO（主子孙结构），递归保存
            if (childVO is AggBaseVO childAggVO)
            {
                // 设置子AggVO的主实体外键
                var childHeadVO = childAggVO.GetHeadVO();
                var childHeadData = ConvertVOToDictionary(childHeadVO);

                if (!string.IsNullOrEmpty(childEntity.ParentForeignKeyField))
                {
                    childHeadData[childEntity.ParentForeignKeyField] = masterId;
                }

                var updatedChildHeadVO = ConvertDictionaryToVO(childHeadData, childHeadVO.GetType());
                childAggVO.SetHeadVO(updatedChildHeadVO);

                // 递归保存子AggVO
                await SaveAggVOAsync(childAggVO);
            }
            else
            {
                // 普通子实体
                var childData = ConvertVOToDictionary(childVO);

                // 设置外键
                if (!string.IsNullOrEmpty(childEntity.ParentForeignKeyField))
                {
                    childData[childEntity.ParentForeignKeyField] = masterId;
                }

                int childId = GetEntityId(childData);

                if (childId > 0)
                {
                    // 更新子实体
                    await _persistenceService.UpdateAsync(childTypeName, childId, childData);
                }
                else
                {
                    // 创建子实体
                    var created = await _persistenceService.CreateAsync(childTypeName, childData);
                    childId = GetEntityId(created);

                    // 更新 VO 的 ID
                    SetEntityId(childVO, childId);
                }
            }
        }
    }

    /// <summary>
    /// 加载子实体
    /// </summary>
    private async Task LoadChildEntitiesAsync(AggBaseVO aggVO, EntityDefinition childEntity, int masterId)
    {
        if (string.IsNullOrEmpty(childEntity.ParentForeignKeyField))
        {
            _logger.LogWarning("Parent foreign key field not configured for child entity {ChildEntity}", childEntity.EntityName);
            return;
        }

        var childTypeName = childEntity.FullTypeName;

        // 查询子实体
        var filter = new FilterCondition
        {
            Field = childEntity.ParentForeignKeyField,
            Operator = FilterOperator.Equals,
            Value = masterId
        };

        var queryOptions = new QueryOptions
        {
            Filters = new List<FilterCondition> { filter }
        };

        var childDataList = await _persistenceService.QueryAsync(childTypeName, queryOptions);

        if (!childDataList.Any())
        {
            _logger.LogDebug("No child entities of type {ChildType} found for master ID {MasterId}", childEntity.EntityName, masterId);
            return;
        }

        _logger.LogInformation("Loaded {Count} child entities of type {ChildType}", childDataList.Count, childEntity.EntityName);

        var childType = Type.GetType(childTypeName);
        if (childType == null)
        {
            _logger.LogWarning("Child entity type '{ChildTypeName}' not found", childTypeName);
            return;
        }

        var childVOs = new List<object>();

        foreach (var childData in childDataList)
        {
            // 如果子实体也是主子结构（主子孙），需要加载为 AggVO
            if (childEntity.StructureType != EntityStructureType.Single)
            {
                // 创建子AggVO实例
                var childAggVOTypeName = $"{childTypeName.Replace("VO", "AggVO")}";
                var childAggVOType = Type.GetType(childAggVOTypeName);

                if (childAggVOType != null && Activator.CreateInstance(childAggVOType) is AggBaseVO childAggVO)
                {
                    var childId = GetEntityId(childData);
                    await LoadAggVOAsync(childAggVO, childId);
                    childVOs.Add(childAggVO);
                }
            }
            else
            {
                // 普通子实体
                var childVO = ConvertDictionaryToVO(childData, childType);
                childVOs.Add(childVO);
            }
        }

        // 设置到AggVO
        aggVO.SetSubEntities(childType, childVOs);
    }

    /// <summary>
    /// 处理级联删除
    /// </summary>
    private async Task HandleCascadeDeleteAsync(EntityDefinition childEntity, int masterId)
    {
        if (string.IsNullOrEmpty(childEntity.ParentForeignKeyField))
        {
            return;
        }

        var filter = new FilterCondition
        {
            Field = childEntity.ParentForeignKeyField,
            Operator = FilterOperator.Equals,
            Value = masterId
        };

        var queryOptions = new QueryOptions
        {
            Filters = new List<FilterCondition> { filter }
        };

        var children = await _persistenceService.QueryAsync(childEntity.FullTypeName, queryOptions);

        switch (childEntity.CascadeDeleteBehavior)
        {
            case CascadeDeleteBehavior.Cascade:
                // 级联删除所有子实体
                _logger.LogInformation("Cascade deleting {Count} child entities of type {ChildType}", children.Count, childEntity.EntityName);
                foreach (var child in children)
                {
                    var childId = GetEntityId(child);
                    if (childId > 0)
                    {
                        // 如果子实体也是 AggVO，递归删除
                        if (childEntity.StructureType != EntityStructureType.Single)
                        {
                            var childAggVOTypeName = $"{childEntity.FullTypeName.Replace("VO", "AggVO")}";
                            var childAggVOType = Type.GetType(childAggVOTypeName);

                            if (childAggVOType != null && Activator.CreateInstance(childAggVOType) is AggBaseVO childAggVO)
                            {
                                await LoadAggVOAsync(childAggVO, childId);
                                await DeleteAggVOAsync(childAggVO);
                                continue;
                            }
                        }

                        await _persistenceService.DeleteAsync(childEntity.FullTypeName, childId);
                    }
                }
                break;

            case CascadeDeleteBehavior.SetNull:
                // 将外键设置为NULL
                _logger.LogInformation("Setting foreign key to NULL for {Count} child entities of type {ChildType}", children.Count, childEntity.EntityName);
                foreach (var child in children)
                {
                    var childId = GetEntityId(child);
                    if (childId > 0)
                    {
                        var updateData = new Dictionary<string, object>
                        {
                            { childEntity.ParentForeignKeyField, DBNull.Value }
                        };
                        await _persistenceService.UpdateAsync(childEntity.FullTypeName, childId, updateData);
                    }
                }
                break;

            case CascadeDeleteBehavior.Restrict:
                // 如果存在子实体，阻止删除
                if (children.Any())
                {
                    throw new InvalidOperationException(
                        $"Cannot delete master entity because {children.Count} child entity(ies) of type '{childEntity.EntityName}' exist with cascade delete behavior 'Restrict'");
                }
                break;

            case CascadeDeleteBehavior.NoAction:
            default:
                // 不执行任何操作
                _logger.LogDebug("No action taken for {Count} child entities of type {ChildType}", children.Count, childEntity.EntityName);
                break;
        }
    }

    /// <summary>
    /// 将 VO 对象转换为字典
    /// </summary>
    private Dictionary<string, object> ConvertVOToDictionary(object vo)
    {
        var json = JsonSerializer.Serialize(vo);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// 将字典转换为 VO 对象
    /// </summary>
    private object ConvertDictionaryToVO(object dictOrEntity, Type voType)
    {
        // 如果已经是Dictionary，直接使用
        if (dictOrEntity is Dictionary<string, object> dict)
        {
            var json = JsonSerializer.Serialize(dict);
            return JsonSerializer.Deserialize(json, voType)!;
        }

        // 否则，先转换为Dictionary
        var convertedDict = ConvertVOToDictionary(dictOrEntity);
        var jsonStr = JsonSerializer.Serialize(convertedDict);
        return JsonSerializer.Deserialize(jsonStr, voType)!;
    }

    /// <summary>
    /// 获取实体ID
    /// </summary>
    private int GetEntityId(object entityOrDict)
    {
        Dictionary<string, object> data;

        // 如果已经是 Dictionary，直接使用
        if (entityOrDict is Dictionary<string, object> dict)
        {
            data = dict;
        }
        else
        {
            // 否则转换为 Dictionary
            data = ConvertVOToDictionary(entityOrDict);
        }

        if (data.TryGetValue("Id", out var idValue))
        {
            if (idValue is JsonElement jsonElement)
            {
                if (jsonElement.TryGetInt32(out var id))
                {
                    return id;
                }
            }
            else if (idValue is int intId)
            {
                return intId;
            }
            else
            {
                return Convert.ToInt32(idValue);
            }
        }
        return 0;
    }

    /// <summary>
    /// 设置实体ID
    /// </summary>
    private void SetEntityId(object vo, int id)
    {
        var idProp = vo.GetType().GetProperty("Id");
        if (idProp != null && idProp.CanWrite)
        {
            idProp.SetValue(vo, id);
        }
    }

    #endregion
}
