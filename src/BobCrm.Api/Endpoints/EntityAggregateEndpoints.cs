using BobCrm.Api.Base.Aggregates;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// 实体聚合端点 - 支持主子实体的聚合管理
/// </summary>
public static class EntityAggregateEndpoints
{
    public static IEndpointRouteBuilder MapEntityAggregateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/entity-aggregates")
            .WithTags("实体聚合管理")
            .WithOpenApi()
            .RequireAuthorization();

        // 获取聚合（主实体 + 子实体）
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] EntityDefinitionAggregateService service,
            CancellationToken cancellationToken) =>
        {
            var aggregate = await service.LoadAggregateAsync(id, cancellationToken);
            if (aggregate == null)
            {
                return Results.NotFound(new { message = $"实体定义 {id} 不存在" });
            }

            // 转换为DTO
            var dto = MapToAggregateDto(aggregate);
            return Results.Ok(dto);
        })
        .WithName("GetEntityAggregate")
        .WithSummary("获取实体聚合")
        .WithDescription("获取实体定义及其所有子实体");

        // 保存聚合（新建或更新）
        group.MapPost("", async (
            [FromBody] SaveEntityDefinitionAggregateRequest request,
            [FromServices] EntityDefinitionAggregateService aggregateService,
            [FromServices] ISubEntityCodeGenerator codeGenerator,
            [FromServices] IAggregateMetadataPublisher metadataPublisher,
            CancellationToken cancellationToken) =>
        {
            try
            {
                // 1. 构建聚合
                var aggregate = await BuildAggregateFromRequest(request, aggregateService, cancellationToken);

                // 2. 保存聚合
                var savedAggregate = await aggregateService.SaveAggregateAsync(aggregate, cancellationToken);

                // 3. 生成代码
                await codeGenerator.GenerateSubEntitiesAsync(savedAggregate, cancellationToken);

                // 4. 发布元数据
                await metadataPublisher.PublishAsync(savedAggregate, cancellationToken);

                // 5. 返回结果
                var dto = MapToAggregateDto(savedAggregate);
                return Results.Ok(dto);
            }
            catch (ValidationException ex)
            {
                return Results.BadRequest(new
                {
                    message = "验证失败",
                    errors = ex.Errors.Select(e => new
                    {
                        propertyPath = e.PropertyPath,
                        message = e.Message
                    })
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "保存聚合失败",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("SaveEntityAggregate")
        .WithSummary("保存实体聚合")
        .WithDescription("保存实体定义及其子实体，自动生成代码和元数据");

        // 验证聚合（不保存）
        group.MapPost("/validate", async (
            [FromBody] SaveEntityDefinitionAggregateRequest request,
            [FromServices] EntityDefinitionAggregateService aggregateService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var aggregate = await BuildAggregateFromRequest(request, aggregateService, cancellationToken);
                var validationResult = aggregateService.ValidateAggregate(aggregate);

                if (validationResult.IsValid)
                {
                    return Results.Ok(new { isValid = true, message = "验证通过" });
                }
                else
                {
                    return Results.Ok(new
                    {
                        isValid = false,
                        errors = validationResult.Errors.Select(e => new
                        {
                            propertyPath = e.PropertyPath,
                            message = e.Message
                        })
                    });
                }
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "验证失败",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("ValidateEntityAggregate")
        .WithSummary("验证实体聚合")
        .WithDescription("验证实体定义及其子实体，但不保存");

        // 删除子实体
        group.MapDelete("/sub-entities/{id:guid}", async (
            Guid id,
            [FromServices] EntityDefinitionAggregateService service,
            CancellationToken cancellationToken) =>
        {
            await service.DeleteSubEntityAsync(id, cancellationToken);
            return Results.NoContent();
        })
        .WithName("DeleteSubEntity")
        .WithSummary("删除子实体")
        .WithDescription("删除指定的子实体及其字段");

        // 生成元数据预览
        group.MapGet("/{id:guid}/metadata-preview", async (
            Guid id,
            [FromServices] EntityDefinitionAggregateService service,
            [FromServices] IAggregateMetadataPublisher publisher,
            CancellationToken cancellationToken) =>
        {
            var aggregate = await service.LoadAggregateAsync(id, cancellationToken);
            if (aggregate == null)
            {
                return Results.NotFound();
            }

            var metadataJson = publisher.GenerateMetadataJson(aggregate);
            return Results.Content(metadataJson, "application/json");
        })
        .WithName("PreviewAggregateMetadata")
        .WithSummary("预览聚合元数据")
        .WithDescription("生成并预览聚合的JSON元数据");

        // 生成代码预览
        group.MapGet("/{id:guid}/code-preview", async (
            Guid id,
            [FromServices] EntityDefinitionAggregateService service,
            [FromServices] ISubEntityCodeGenerator codeGenerator,
            CancellationToken cancellationToken) =>
        {
            var aggregate = await service.LoadAggregateAsync(id, cancellationToken);
            if (aggregate == null)
            {
                return Results.NotFound();
            }

            var codePreview = new Dictionary<string, string>();

            // 生成子实体代码
            foreach (var subEntity in aggregate.SubEntities)
            {
                var code = codeGenerator.GenerateSubEntityClass(aggregate.Root, subEntity);
                codePreview[$"{subEntity.Code}.cs"] = code;
            }

            // 生成AggVO代码
            var aggVoCode = codeGenerator.GenerateAggregateVoClass(aggregate.Root, aggregate.SubEntities.ToList());
            codePreview[$"{aggregate.Root.EntityName}AggVo.cs"] = aggVoCode;

            return Results.Ok(codePreview);
        })
        .WithName("PreviewGeneratedCode")
        .WithSummary("预览生成的代码")
        .WithDescription("预览为子实体生成的C#代码");

        return app;
    }

    private static async Task<EntityDefinitionAggregate> BuildAggregateFromRequest(
        SaveEntityDefinitionAggregateRequest request,
        EntityDefinitionAggregateService service,
        CancellationToken cancellationToken)
    {
        EntityDefinition root;

        if (request.Id == Guid.Empty)
        {
            // 新建
            root = new EntityDefinition
            {
                Id = Guid.NewGuid(),
                Namespace = request.Namespace,
                EntityName = request.EntityName,
                DisplayName = request.DisplayName,
                Description = request.Description,
                Status = EntityStatus.Draft,
                StructureType = EntityStructureType.MasterDetail,
                CreatedAt = DateTime.UtcNow
            };
        }
        else
        {
            // 更新：加载现有实体
            var existingAggregate = await service.LoadAggregateAsync(request.Id, cancellationToken);
            if (existingAggregate == null)
            {
                throw new InvalidOperationException($"实体定义 {request.Id} 不存在");
            }

            root = existingAggregate.Root;
            root.DisplayName = request.DisplayName;
            root.Description = request.Description;
            root.UpdatedAt = DateTime.UtcNow;
        }

        var aggregate = new EntityDefinitionAggregate(root);

        // 添加子实体
        foreach (var subEntityDto in request.SubEntities)
        {
            var subEntity = aggregate.AddSubEntity(
                subEntityDto.Code,
                subEntityDto.DisplayName,
                subEntityDto.Description,
                subEntityDto.SortOrder);

            // 设置其他属性
            subEntity.DefaultSortField = subEntityDto.DefaultSortField;
            subEntity.IsDescending = subEntityDto.IsDescending;
            subEntity.ForeignKeyField = subEntityDto.ForeignKeyField;
            subEntity.CollectionPropertyName = subEntityDto.CollectionPropertyName;
            subEntity.CascadeDeleteBehavior = subEntityDto.CascadeDeleteBehavior;

            // 如果是更新现有子实体，保留ID
            if (subEntityDto.Id != Guid.Empty)
            {
                subEntity.Id = subEntityDto.Id;
            }

            // 添加字段
            foreach (var fieldDto in subEntityDto.Fields)
            {
                var field = aggregate.AddFieldToSubEntity(
                    subEntity.Id,
                    fieldDto.PropertyName,
                    fieldDto.DisplayName ?? new Dictionary<string, string?>(),
                    fieldDto.DataType,
                    fieldDto.IsRequired,
                    fieldDto.Length,
                    fieldDto.SortOrder);

                // 设置其他属性
                field.Precision = fieldDto.Precision;
                field.Scale = fieldDto.Scale;
                field.DefaultValue = fieldDto.DefaultValue;
                field.ValidationRules = fieldDto.ValidationRules;

                // 如果是更新现有字段，保留ID
                if (fieldDto.Id != Guid.Empty)
                {
                    field.Id = fieldDto.Id;
                }
            }
        }

        return aggregate;
    }

    private static object MapToAggregateDto(EntityDefinitionAggregate aggregate)
    {
        return new
        {
            master = new
            {
                id = aggregate.Root.Id,
                @namespace = aggregate.Root.Namespace,
                entityName = aggregate.Root.EntityName,
                displayName = aggregate.Root.DisplayName,
                description = aggregate.Root.Description,
                status = aggregate.Root.Status,
                createdAt = aggregate.Root.CreatedAt,
                updatedAt = aggregate.Root.UpdatedAt
            },
            subEntities = aggregate.SubEntities.Select(s => new
            {
                id = s.Id,
                code = s.Code,
                displayName = s.DisplayName,
                description = s.Description,
                sortOrder = s.SortOrder,
                defaultSortField = s.DefaultSortField,
                isDescending = s.IsDescending,
                foreignKeyField = s.ForeignKeyField,
                collectionPropertyName = s.CollectionPropertyName,
                cascadeDeleteBehavior = s.CascadeDeleteBehavior,
                fields = s.Fields.Select(f => new
                {
                    id = f.Id,
                    propertyName = f.PropertyName,
                    displayName = f.DisplayName,
                    dataType = f.DataType,
                    length = f.Length,
                    precision = f.Precision,
                    scale = f.Scale,
                    isRequired = f.IsRequired,
                    defaultValue = f.DefaultValue,
                    validationRules = f.ValidationRules,
                    sortOrder = f.SortOrder
                })
            })
        };
    }
}

/// <summary>
/// 保存聚合请求
/// </summary>
public class SaveEntityDefinitionAggregateRequest
{
    public Guid Id { get; set; }
    public string Namespace { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public Dictionary<string, string?> DisplayName { get; set; } = new();
    public Dictionary<string, string?>? Description { get; set; }
    public List<SubEntityDto> SubEntities { get; set; } = new();
}

/// <summary>
/// 子实体DTO
/// </summary>
public class SubEntityDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Dictionary<string, string?> DisplayName { get; set; } = new();
    public Dictionary<string, string?>? Description { get; set; }
    public int SortOrder { get; set; }
    public string? DefaultSortField { get; set; }
    public bool IsDescending { get; set; }
    public string? ForeignKeyField { get; set; }
    public string? CollectionPropertyName { get; set; }
    public string CascadeDeleteBehavior { get; set; } = "Cascade";
    public List<FieldMetadataDto> Fields { get; set; } = new();
}

/// <summary>
/// 字段元数据DTO（用于API传输）
/// </summary>
public class FieldMetadataDto
{
    public Guid Id { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public Dictionary<string, string?> DisplayName { get; set; } = new();
    public string DataType { get; set; } = string.Empty;
    public int? Length { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; }
    public int SortOrder { get; set; }
}
