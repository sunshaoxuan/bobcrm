using BobCrm.Api.Base.Aggregates;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Services;
using BobCrm.Api.Base;
using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.Responses.EntityAggregate;
using BobCrm.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// Entity aggregate endpoints - manage master/sub-entities together
/// </summary>
public static class EntityAggregateEndpoints
{
    public static IEndpointRouteBuilder MapEntityAggregateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/entity-aggregates")
            .WithTags("EntityAggregates")
            .WithOpenApi()
            .RequireAuthorization();

        // Get aggregate (master + sub entities)
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] EntityDefinitionAggregateService service,
            ILocalization loc,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var lang = LangHelper.GetLang(http);
            var aggregate = await service.LoadAggregateAsync(id, cancellationToken);
            if (aggregate == null)
            {
                return Results.NotFound(new ErrorResponse(loc.T("ERR_ENTITY_NOT_FOUND", lang), "ENTITY_NOT_FOUND"));
            }

            // 转换为DTO
            var dto = MapToAggregateDto(aggregate);
            return Results.Ok(new SuccessResponse<EntityAggregateDto>(dto));
        })
        .WithName("GetEntityAggregate")
        .WithSummary("Get entity aggregate")
        .WithDescription("Get entity definition and all sub entities")
        .Produces<SuccessResponse<EntityAggregateDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // Save aggregate (create or update)
        group.MapPost("", async (
            [FromBody] SaveEntityDefinitionAggregateRequest request,
            [FromServices] EntityDefinitionAggregateService aggregateService,
            [FromServices] ISubEntityCodeGenerator codeGenerator,
            [FromServices] IAggregateMetadataPublisher metadataPublisher,
            ILocalization loc,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var lang = LangHelper.GetLang(http);
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
            return Results.Ok(new SuccessResponse<EntityAggregateDto>(dto));
        })
        .WithName("SaveEntityAggregate")
        .WithSummary("Save entity aggregate")
        .WithDescription("Save entity definition and sub entities, generate code and metadata")
        .Produces<SuccessResponse<EntityAggregateDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        // Validate aggregate (no save)
        group.MapPost("/validate", async (
            [FromBody] SaveEntityDefinitionAggregateRequest request,
            [FromServices] EntityDefinitionAggregateService aggregateService,
            ILocalization loc,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var lang = LangHelper.GetLang(http);
            var aggregate = await BuildAggregateFromRequest(request, aggregateService, cancellationToken);
            var validationResult = aggregateService.ValidateAggregate(aggregate);

            if (validationResult.IsValid)
            {
                return Results.Ok(new SuccessResponse<EntityAggregateValidationResponseDto>(new EntityAggregateValidationResponseDto
                {
                    IsValid = true,
                    Message = loc.T("MSG_AGGREGATE_VALID", lang)
                }));
            }
            else
            {
                return Results.Ok(new SuccessResponse<EntityAggregateValidationResponseDto>(new EntityAggregateValidationResponseDto
                {
                    IsValid = false,
                    Errors = validationResult.Errors.Select(e => new EntityAggregateValidationErrorDto
                    {
                        PropertyPath = e.PropertyPath,
                        Message = LocalizeValidationError(loc, lang, e)
                    }).ToList()
                }));
            }
        })
        .WithName("ValidateEntityAggregate")
        .WithSummary("Validate entity aggregate")
        .WithDescription("Validate entity definition and sub entities without saving")
        .Produces<SuccessResponse<EntityAggregateValidationResponseDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        // Delete sub entity
        group.MapDelete("/sub-entities/{id:guid}", async (
            Guid id,
            [FromServices] EntityDefinitionAggregateService service,
            CancellationToken cancellationToken) =>
        {
            await service.DeleteSubEntityAsync(id, cancellationToken);
            return Results.Ok(ApiResponseExtensions.SuccessResponse());
        })
        .WithName("DeleteSubEntity")
        .WithSummary("Delete sub entity")
        .WithDescription("Delete the specified sub entity and its fields")
        .Produces<SuccessResponse>(StatusCodes.Status200OK);

        // Generate metadata preview
        group.MapGet("/{id:guid}/metadata-preview", async (
            Guid id,
            [FromServices] EntityDefinitionAggregateService service,
            [FromServices] IAggregateMetadataPublisher publisher,
            CancellationToken cancellationToken) =>
        {
            var aggregate = await service.LoadAggregateAsync(id, cancellationToken);
            if (aggregate == null)
            {
                return Results.NotFound(new ErrorResponse("Not found", "ENTITY_NOT_FOUND"));
            }

            var metadataJson = publisher.GenerateMetadataJson(aggregate);
            using var doc = JsonDocument.Parse(metadataJson);
            return Results.Ok(new SuccessResponse<JsonElement>(doc.RootElement.Clone()));
        })
        .WithName("PreviewAggregateMetadata")
        .WithSummary("Preview aggregate metadata")
        .WithDescription("Generate and preview aggregate JSON metadata")
        .Produces<SuccessResponse<JsonElement>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // Generate code preview
        group.MapGet("/{id:guid}/code-preview", async (
            Guid id,
            [FromServices] EntityDefinitionAggregateService service,
            [FromServices] ISubEntityCodeGenerator codeGenerator,
            CancellationToken cancellationToken) =>
        {
            var aggregate = await service.LoadAggregateAsync(id, cancellationToken);
            if (aggregate == null)
            {
                return Results.NotFound(new ErrorResponse("Not found", "ENTITY_NOT_FOUND"));
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

            return Results.Ok(new SuccessResponse<Dictionary<string, string>>(codePreview));
        })
        .WithName("PreviewGeneratedCode")
        .WithSummary("Preview generated code")
        .WithDescription("Preview generated C# code for sub entities")
        .Produces<SuccessResponse<Dictionary<string, string>>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

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
                throw new InvalidOperationException($"Entity definition {request.Id} not found");
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

    private static EntityAggregateDto MapToAggregateDto(EntityDefinitionAggregate aggregate)
    {
        return new EntityAggregateDto
        {
            Master = new EntityAggregateMasterDto
            {
                Id = aggregate.Root.Id,
                Namespace = aggregate.Root.Namespace,
                EntityName = aggregate.Root.EntityName,
                DisplayName = aggregate.Root.DisplayName,
                Description = aggregate.Root.Description,
                Status = aggregate.Root.Status,
                CreatedAt = aggregate.Root.CreatedAt,
                UpdatedAt = aggregate.Root.UpdatedAt
            },
            SubEntities = aggregate.SubEntities.Select(s => new EntityAggregateSubEntityDto
            {
                Id = s.Id,
                Code = s.Code,
                DisplayName = s.DisplayName,
                Description = s.Description,
                SortOrder = s.SortOrder,
                DefaultSortField = s.DefaultSortField,
                IsDescending = s.IsDescending,
                ForeignKeyField = s.ForeignKeyField,
                CollectionPropertyName = s.CollectionPropertyName,
                CascadeDeleteBehavior = s.CascadeDeleteBehavior,
                Fields = s.Fields.Select(f => new EntityAggregateFieldDto
                {
                    Id = f.Id,
                    PropertyName = f.PropertyName,
                    DisplayName = f.DisplayName,
                    DataType = f.DataType,
                    Length = f.Length,
                    Precision = f.Precision,
                    Scale = f.Scale,
                    IsRequired = f.IsRequired,
                    DefaultValue = f.DefaultValue,
                    ValidationRules = f.ValidationRules,
                    SortOrder = f.SortOrder
                }).ToList()
            }).ToList()
        };
    }

    private static string LocalizeValidationError(ILocalization loc, string lang, ValidationError error)
    {
        var template = loc.T(error.MessageKey, lang);
        if (error.Args.Length == 0)
        {
            return template;
        }

        try
        {
            return string.Format(template, error.Args);
        }
        catch
        {
            return template;
        }
    }

    private static string LocalizeDomainException(ILocalization loc, string lang, DomainException exception)
    {
        var template = loc.T(exception.MessageKey, lang);
        if (exception.Args.Length == 0)
        {
            return template;
        }

        try
        {
            return string.Format(template, exception.Args);
        }
        catch
        {
            return template;
        }
    }
}
