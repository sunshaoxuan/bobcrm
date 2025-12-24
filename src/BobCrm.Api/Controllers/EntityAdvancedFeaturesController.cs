using BobCrm.Api.Infrastructure;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Services.CodeGeneration;
using BobCrm.Api.Services.DataMigration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Controllers;

/// <summary>
/// 实体高级功能API
/// 提供主子表配置、数据迁移评估、AggVO代码生成等高级功能
/// </summary>
[ApiController]
[Route("api/entity-advanced")]
public class EntityAdvancedFeaturesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAggVOCodeGenerator _aggVOCodeGenerator;
    private readonly IDataMigrationEvaluator _migrationEvaluator;
    private readonly ILogger<EntityAdvancedFeaturesController> _logger;

    public EntityAdvancedFeaturesController(
        AppDbContext context,
        IAggVOCodeGenerator aggVOCodeGenerator,
        IDataMigrationEvaluator migrationEvaluator,
        ILogger<EntityAdvancedFeaturesController> logger)
    {
        _context = context;
        _aggVOCodeGenerator = aggVOCodeGenerator;
        _migrationEvaluator = migrationEvaluator;
        _logger = logger;
    }

    /// <summary>
    /// 获取实体的所有子实体
    /// </summary>
    [HttpGet("{entityId:guid}/children")]
    public async Task<IActionResult> GetChildEntities(Guid entityId)
    {
        var entity = await _context.EntityDefinitions
            .FirstOrDefaultAsync(e => e.Id == entityId);

        if (entity == null)
        {
            return NotFound(new { error = "Entity not found" });
        }

        var children = await _context.EntityDefinitions
            .Where(e => e.ParentEntityId == entityId)
            .OrderBy(e => e.Order)
            .Select(e => new
            {
                e.Id,
                e.EntityName,
                e.FullTypeName,
                e.StructureType,
                e.ParentForeignKeyField,
                e.ParentCollectionProperty,
                e.CascadeDeleteBehavior,
                e.AutoCascadeSave,
                FieldCount = e.Fields.Count
            })
            .ToListAsync();

        return Ok(new
        {
            entityId,
            entityName = entity.EntityName,
            structureType = entity.StructureType,
            childCount = children.Count,
            children
        });
    }

    /// <summary>
    /// 配置主子表关系
    /// </summary>
    [HttpPost("{entityId:guid}/configure-master-detail")]
    public async Task<IActionResult> ConfigureMasterDetail(
        Guid entityId,
        [FromBody] MasterDetailConfigRequest request)
    {
        var entity = await _context.EntityDefinitions
            .FirstOrDefaultAsync(e => e.Id == entityId);

        if (entity == null)
        {
            return NotFound(new { error = "Entity not found" });
        }

        // 更新实体配置
        entity.StructureType = request.StructureType;

        // 如果有子实体配置，更新子实体
        if (request.Children != null)
        {
            foreach (var childConfig in request.Children)
            {
                var childEntity = await _context.EntityDefinitions
                    .FirstOrDefaultAsync(e => e.Id == childConfig.ChildEntityId);

                if (childEntity != null)
                {
                    childEntity.ParentEntityId = entityId;
                    childEntity.ParentEntityName = entity.EntityName;
                    childEntity.ParentForeignKeyField = childConfig.ForeignKeyField;
                    childEntity.ParentCollectionProperty = childConfig.CollectionProperty;
                    childEntity.CascadeDeleteBehavior = childConfig.CascadeDeleteBehavior ?? CascadeDeleteBehavior.NoAction;
                    childEntity.AutoCascadeSave = childConfig.AutoCascadeSave ?? true;
                }
            }
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Configured master-detail structure for entity {EntityName}", entity.EntityName);

        return Ok(new
        {
            message = "Master-detail configuration updated successfully",
            entityId,
            structureType = entity.StructureType
        });
    }

    /// <summary>
    /// 生成 AggVO 代码
    /// </summary>
    [HttpPost("{entityId:guid}/generate-aggvo")]
    public async Task<IActionResult> GenerateAggVO(Guid entityId)
    {
        var entity = await _context.EntityDefinitions
            .Include(e => e.Fields)
            .FirstOrDefaultAsync(e => e.Id == entityId);

        if (entity == null)
        {
            return NotFound(new { error = "Entity not found" });
        }

        if (entity.StructureType == EntityStructureType.Single)
        {
            return BadRequest(new { error = "Entity is not a master-detail structure" });
        }

        // 获取子实体
        var childEntities = await _context.EntityDefinitions
            .Include(e => e.Fields)
            .Where(e => e.ParentEntityId == entityId)
            .OrderBy(e => e.Order)
            .ToListAsync();

        if (!childEntities.Any())
        {
            return BadRequest(new { error = "No child entities configured" });
        }

        try
        {
            var aggVOCode = _aggVOCodeGenerator.GenerateAggVOClass(entity, childEntities);
            var voCode = _aggVOCodeGenerator.GenerateVOClass(entity);

            // 生成子实体的 VO 代码
            var childVOCodes = new Dictionary<string, string>();
            foreach (var childEntity in childEntities)
            {
                childVOCodes[childEntity.EntityName] = _aggVOCodeGenerator.GenerateVOClass(childEntity);
            }

            return Ok(new
            {
                entity = entity.EntityName,
                aggVOClassName = $"{entity.EntityName}AggVO",
                aggVOCode,
                voCode,
                childVOCodes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate AggVO for entity {EntityName}", entity.EntityName);
            return StatusCode(500, new { error = $"Failed to generate AggVO: {ex.Message}" });
        }
    }

    /// <summary>
    /// 评估数据迁移影响
    /// </summary>
    [HttpPost("{entityId:guid}/evaluate-migration")]
    public async Task<IActionResult> EvaluateMigration(
        Guid entityId,
        [FromBody] List<FieldMetadata> newFields)
    {
        try
        {
            var impact = await _migrationEvaluator.EvaluateImpactAsync(entityId, newFields);

            return Ok(new
            {
                entityName = impact.EntityName,
                tableName = impact.TableName,
                affectedRows = impact.AffectedRows,
                riskLevel = impact.RiskLevel,
                isSafe = impact.IsSafe,
                operations = impact.Operations,
                warnings = impact.Warnings,
                errors = impact.Errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate migration for entity {EntityId}", entityId);
            return StatusCode(500, new { error = $"Failed to evaluate migration: {ex.Message}" });
        }
    }

    /// <summary>
    /// 获取所有可用作主实体的实体列表
    /// </summary>
    [HttpGet("master-candidates")]
    public async Task<IActionResult> GetMasterCandidates()
    {
        var candidates = await _context.EntityDefinitions
            .Where(e => e.Status == EntityStatus.Published && e.IsRootEntity)
            .OrderBy(e => e.EntityName)
            .Select(e => new
            {
                e.Id,
                e.EntityName,
                e.FullTypeName,
                e.StructureType,
                e.DisplayName,
                CurrentChildCount = _context.EntityDefinitions.Count(c => c.ParentEntityId == e.Id)
            })
            .ToListAsync();

        return Ok(candidates);
    }

    /// <summary>
    /// 获取所有可用作子实体的实体列表
    /// </summary>
    [HttpGet("detail-candidates")]
    public async Task<IActionResult> GetDetailCandidates()
    {
        var candidates = await _context.EntityDefinitions
            .Where(e => e.Status == EntityStatus.Published && !e.ParentEntityId.HasValue)
            .OrderBy(e => e.EntityName)
            .Select(e => new
            {
                e.Id,
                e.EntityName,
                e.FullTypeName,
                e.StructureType,
                e.DisplayName,
                FieldCount = e.Fields.Count
            })
            .ToListAsync();

        return Ok(candidates);
    }
}
