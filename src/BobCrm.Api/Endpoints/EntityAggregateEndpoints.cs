using BobCrm.Api.Base.Aggregates;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Services;
using BobCrm.Api.Base;
using BobCrm.Api.Contracts;
using BobCrm.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

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
            return Results.Ok(dto);
        })
        .WithName("GetEntityAggregate")
        .WithSummary("Get entity aggregate")
        .WithDescription("Get entity definition and all sub entities");

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
            catch (DomainException ex)
            {
                return Results.BadRequest(new
                {
                    message = LocalizeDomainException(loc, lang, ex),
                    code = ex.MessageKey
                });
            }
            catch (ValidationException ex)
            {
                return Results.BadRequest(new
                {
                    message = loc.T("ERR_AGGREGATE_VALIDATION_FAILED", lang),
                    errors = ex.Errors.Select(e => new
                    {
                        propertyPath = e.PropertyPath,
                        message = LocalizeValidationError(loc, lang, e)
                    })
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: loc.T("ERR_AGGREGATE_SAVE_FAILED", lang),
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("SaveEntityAggregate")
        .WithSummary("Save entity aggregate")
        .WithDescription("Save entity definition and sub entities, generate code and metadata");

        // Validate aggregate (no save)
        group.MapPost("/validate", async (
            [FromBody] SaveEntityDefinitionAggregateRequest request,
            [FromServices] EntityDefinitionAggregateService aggregateService,
            ILocalization loc,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var lang = LangHelper.GetLang(http);
            try
            {
                var aggregate = await BuildAggregateFromRequest(request, aggregateService, cancellationToken);
                var validationResult = aggregateService.ValidateAggregate(aggregate);

                if (validationResult.IsValid)
                {
                    return Results.Ok(new { isValid = true, message = loc.T("MSG_AGGREGATE_VALID", lang) });
                }
                else
                {
                    return Results.Ok(new
                    {
                        isValid = false,
                        errors = validationResult.Errors.Select(e => new
                        {
                            propertyPath = e.PropertyPath,
                            message = LocalizeValidationError(loc, lang, e)
                        })
                    });
                }
            }
            catch (DomainException ex)
            {
                return Results.BadRequest(new
                {
                    message = LocalizeDomainException(loc, lang, ex),
                    code = ex.MessageKey
                });
            }
            catch (ValidationException ex)
            {
                return Results.Ok(new
                {
                    isValid = false,
                    errors = ex.Errors.Select(e => new
                    {
                        propertyPath = e.PropertyPath,
                        message = LocalizeValidationError(loc, lang, e)
                    })
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: loc.T("ERR_AGGREGATE_VALIDATION_FAILED", lang),
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("ValidateEntityAggregate")
        .WithSummary("Validate entity aggregate")
        .WithDescription("Validate entity definition and sub entities without saving");

        // Delete sub entity
        group.MapDelete("/sub-entities/{id:guid}", async (
            Guid id,
            [FromServices] EntityDefinitionAggregateService service,
            CancellationToken cancellationToken) =>
        {
            await service.DeleteSubEntityAsync(id, cancellationToken);
            return Results.NoContent();
        })
        .WithName("DeleteSubEntity")
        .WithSummary("Delete sub entity")
        .WithDescription("Delete the specified sub entity and its fields");

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
                return Results.NotFound();
            }

            var metadataJson = publisher.GenerateMetadataJson(aggregate);
            return Results.Content(metadataJson, "application/json");
        })
        .WithName("PreviewAggregateMetadata")
        .WithSummary("Preview aggregate metadata")
        .WithDescription("Generate and preview aggregate JSON metadata");

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
        .WithSummary("Preview generated code")
        .WithDescription("Preview generated C# code for sub entities");

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
