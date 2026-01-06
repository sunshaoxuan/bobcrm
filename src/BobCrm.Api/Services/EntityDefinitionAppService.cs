using System.Security.Claims;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Core.DomainCommon;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.Requests.Entity;
using BobCrm.Api.Contracts.Responses.Entity;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Utils;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

public class EntityDefinitionAppService : IEntityDefinitionAppService
{
    private readonly AppDbContext _db;
    private readonly ILocalization _loc;
    private readonly IFieldMetadataCache _fieldMetadataCache;
    private readonly ILogger<EntityDefinitionAppService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EntityDefinitionAppService(
        AppDbContext db,
        ILocalization loc,
        IFieldMetadataCache fieldMetadataCache,
        ILogger<EntityDefinitionAppService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _loc = loc;
        _fieldMetadataCache = fieldMetadataCache;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<EntityDefinitionDto> CreateEntityDefinitionAsync(
        string uid,
        string? lang,
        CreateEntityDefinitionDto dto,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[EntityDefinition] Creating new entity: {Namespace}.{EntityName}", dto.Namespace, dto.EntityName);

        var uiLang = ResolveLang(lang);

        if (string.IsNullOrWhiteSpace(dto.Namespace))
        {
            throw new ServiceException(_loc.T("ERR_NAMESPACE_REQUIRED", uiLang), "VALIDATION_ERROR");
        }

        if (string.IsNullOrWhiteSpace(dto.EntityName))
        {
            throw new ServiceException(_loc.T("ERR_ENTITY_NAME_REQUIRED", uiLang), "VALIDATION_ERROR");
        }

        if (dto.DisplayName == null || !dto.DisplayName.Any() || !dto.DisplayName.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
        {
            throw new ServiceException(_loc.T("ERR_DISPLAY_NAME_REQUIRED", uiLang), "VALIDATION_ERROR");
        }

        var exists = await _db.EntityDefinitions
            .AnyAsync(ed => ed.Namespace == dto.Namespace && ed.EntityName == dto.EntityName, ct);

        if (exists)
        {
            _logger.LogWarning("[EntityDefinition] Entity already exists: {Namespace}.{EntityName}", dto.Namespace, dto.EntityName);
            throw new ServiceException(_loc.T("ERR_ENTITY_EXISTS", uiLang), "ENTITY_EXISTS");
        }

        var definition = new EntityDefinition
        {
            Namespace = dto.Namespace,
            EntityName = dto.EntityName,
            DisplayName = dto.DisplayName,
            Description = dto.Description?.Any(kvp => !string.IsNullOrWhiteSpace(kvp.Value)) == true ? dto.Description : null,
            StructureType = string.IsNullOrWhiteSpace(dto.StructureType) ? EntityStructureType.Single : dto.StructureType,
            Status = EntityStatus.Draft,
            CreatedBy = uid,
            UpdatedBy = uid
        };

        if (dto.Fields != null)
        {
            foreach (var fieldDto in dto.Fields)
            {
                if (fieldDto.DisplayName == null || !fieldDto.DisplayName.Any() || !fieldDto.DisplayName.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
                {
                    var message = string.Format(_loc.T("ERR_FIELD_DISPLAY_NAME_REQUIRED", uiLang), fieldDto.PropertyName);
                    throw new ServiceException(message, "VALIDATION_ERROR");
                }

                definition.Fields.Add(new FieldMetadata
                {
                    PropertyName = fieldDto.PropertyName,
                    DisplayName = fieldDto.DisplayName,
                    DataType = fieldDto.DataType,
                    Length = fieldDto.Length,
                    Precision = fieldDto.Precision,
                    Scale = fieldDto.Scale,
                    IsRequired = fieldDto.IsRequired,
                    IsEntityRef = fieldDto.IsEntityRef,
                    ReferencedEntityId = fieldDto.ReferencedEntityId,
                    LookupEntityName = fieldDto.LookupEntityName,
                    LookupDisplayField = fieldDto.LookupDisplayField,
                    ForeignKeyAction = fieldDto.ForeignKeyAction,
                    SortOrder = fieldDto.SortOrder,
                    DefaultValue = fieldDto.DefaultValue,
                    ValidationRules = fieldDto.ValidationRules,
                    EnumDefinitionId = fieldDto.EnumDefinitionId,
                    IsMultiSelect = fieldDto.IsMultiSelect
                });
            }
        }

        if (dto.Interfaces != null)
        {
            ApplyInterfaces(definition, dto.Interfaces);
        }

        _db.EntityDefinitions.Add(definition);
        await _db.SaveChangesAsync(ct);

        var createdType = ResolveFullTypeNameForCache(definition);
        if (!string.IsNullOrWhiteSpace(createdType))
        {
            _fieldMetadataCache.Invalidate(createdType);
        }

        _logger.LogInformation("[EntityDefinition] Entity created successfully: {Id}", definition.Id);
        return BuildResponseDto(definition);
    }

    public async Task<EntityDefinitionDto> UpdateEntityDefinitionAsync(
        Guid id,
        string uid,
        string? lang,
        UpdateEntityDefinitionDto dto,
        CancellationToken ct = default)
    {
        var uiLang = ResolveLang(lang);

        var definition = await _db.EntityDefinitions
            .Include(ed => ed.Fields)
            .Include(ed => ed.Interfaces)
            .FirstOrDefaultAsync(ed => ed.Id == id, ct);

        if (definition == null)
        {
            throw new KeyNotFoundException(_loc.T("ERR_ENTITY_NOT_FOUND", uiLang));
        }

        _logger.LogInformation("[EntityDefinition] Updating entity: {Id}, IsLocked={IsLocked}", id, definition.IsLocked);

        if (definition.IsLocked)
        {
            if (dto.Namespace != null && dto.Namespace != definition.Namespace)
            {
                throw new ServiceException(_loc.T("ERR_ENTITY_LOCKED_NAMESPACE", uiLang), "ENTITY_LOCKED");
            }

            if (dto.EntityName != null && dto.EntityName != definition.EntityName)
            {
                throw new ServiceException(_loc.T("ERR_ENTITY_LOCKED_NAME", uiLang), "ENTITY_LOCKED");
            }

            if (dto.Fields != null)
            {
                var existingFieldIds = definition.Fields.Select(f => f.Id).ToHashSet();
                var newFieldIds = dto.Fields.Where(f => f.Id.HasValue).Select(f => f.Id!.Value).ToHashSet();
                var deletedFieldIds = existingFieldIds.Except(newFieldIds).ToList();

                if (deletedFieldIds.Count > 0)
                {
                    _logger.LogWarning("[EntityDefinition] Cannot delete fields when entity is locked: {FieldIds}", string.Join(", ", deletedFieldIds));
                    throw new ServiceException(_loc.T("ERR_ENTITY_LOCKED_DELETE_FIELD", uiLang), "ENTITY_LOCKED");
                }

                foreach (var fieldDto in dto.Fields.Where(f => f.Id.HasValue))
                {
                    var existingField = definition.Fields.FirstOrDefault(f => f.Id == fieldDto.Id!.Value);
                    if (existingField == null)
                    {
                        continue;
                    }

                    if (fieldDto.DataType != null && fieldDto.DataType != existingField.DataType)
                    {
                        throw new ServiceException(string.Format(_loc.T("ERR_FIELD_LOCKED_TYPE_CHANGE", uiLang), existingField.PropertyName), "ENTITY_LOCKED");
                    }

                    if (fieldDto.Length.HasValue && fieldDto.Length < existingField.Length)
                    {
                        throw new ServiceException(string.Format(_loc.T("ERR_FIELD_LOCKED_LENGTH_DECREASE", uiLang), existingField.PropertyName), "ENTITY_LOCKED");
                    }
                }
            }

            if (dto.Interfaces != null)
            {
                var lockedInterfaceTypes = definition.Interfaces.Where(i => i.IsLocked).Select(i => i.InterfaceType).ToHashSet();
                var removedInterfaces = lockedInterfaceTypes.Except(dto.Interfaces).ToList();
                if (removedInterfaces.Count > 0)
                {
                    _logger.LogWarning("[EntityDefinition] Cannot remove locked interfaces: {Interfaces}", string.Join(", ", removedInterfaces));
                    throw new ServiceException(_loc.T("ERR_ENTITY_LOCKED_DELETE_INTERFACE", uiLang), "ENTITY_LOCKED");
                }
            }
        }

        if (dto.Namespace != null)
        {
            definition.Namespace = dto.Namespace;
        }

        if (dto.EntityName != null)
        {
            definition.EntityName = dto.EntityName;
        }

        if (dto.StructureType != null)
        {
            definition.StructureType = dto.StructureType;
        }

        if (dto.DisplayName != null && dto.DisplayName.Any() && dto.DisplayName.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
        {
            definition.DisplayName = dto.DisplayName;
        }

        if (dto.Description != null && dto.Description.Any() && dto.Description.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
        {
            definition.Description = dto.Description;
        }

        definition.UpdatedAt = DateTime.UtcNow;
        definition.UpdatedBy = uid;

        if (definition.Status == EntityStatus.Published)
        {
            definition.Status = EntityStatus.Modified;
        }

        if (dto.Fields != null)
        {
            var incomingFieldIds = dto.Fields.Where(f => f.Id.HasValue).Select(f => f.Id!.Value).ToHashSet();

            var fieldsToRemove = definition.Fields.Where(f => !incomingFieldIds.Contains(f.Id) && !f.IsDeleted).ToList();
            var protectedToRemove = fieldsToRemove
                .Where(f =>
                    string.Equals(f.Source, FieldSource.System, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(f.Source, FieldSource.Interface, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.PropertyName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();
            if (protectedToRemove.Count > 0)
            {
                var msg = string.Format(_loc.T("ERR_FIELD_PROTECTED_BY_SOURCE", uiLang), string.Join(", ", protectedToRemove));
                throw new ServiceException(msg, ErrorCodes.FieldProtectedBySource);
            }

            foreach (var field in fieldsToRemove)
            {
                field.IsDeleted = true;
                field.UpdatedAt = DateTime.UtcNow;
            }

            foreach (var fieldDto in dto.Fields)
            {
                if (fieldDto.Id.HasValue)
                {
                    var existingField = definition.Fields.FirstOrDefault(f => f.Id == fieldDto.Id.Value);
                    if (existingField != null)
                    {
                        var source = existingField.Source;
                        if (string.Equals(source, FieldSource.System, StringComparison.OrdinalIgnoreCase))
                        {
                            if (fieldDto.DisplayName != null) existingField.DisplayName = fieldDto.DisplayName;
                            if (fieldDto.SortOrder.HasValue) existingField.SortOrder = fieldDto.SortOrder.Value;
                            existingField.UpdatedAt = DateTime.UtcNow;
                            existingField.IsDeleted = false;
                            continue;
                        }

                        if (string.Equals(source, FieldSource.Interface, StringComparison.OrdinalIgnoreCase))
                        {
                            if (fieldDto.DisplayName != null) existingField.DisplayName = fieldDto.DisplayName;
                            if (fieldDto.SortOrder.HasValue) existingField.SortOrder = fieldDto.SortOrder.Value;
                            existingField.DefaultValue = fieldDto.DefaultValue;
                            existingField.UpdatedAt = DateTime.UtcNow;
                            existingField.IsDeleted = false;
                            continue;
                        }

                        if (fieldDto.PropertyName != null) existingField.PropertyName = fieldDto.PropertyName;
                        if (fieldDto.DisplayName != null) existingField.DisplayName = fieldDto.DisplayName;
                        if (fieldDto.DataType != null) existingField.DataType = fieldDto.DataType;
                        existingField.Length = fieldDto.Length;
                        existingField.Precision = fieldDto.Precision;
                        existingField.Scale = fieldDto.Scale;
                        if (fieldDto.IsRequired.HasValue) existingField.IsRequired = fieldDto.IsRequired.Value;
                        if (fieldDto.IsEntityRef.HasValue) existingField.IsEntityRef = fieldDto.IsEntityRef.Value;
                        existingField.ReferencedEntityId = fieldDto.ReferencedEntityId;
                        if (fieldDto.LookupEntityName != null) existingField.LookupEntityName = fieldDto.LookupEntityName;
                        if (fieldDto.LookupDisplayField != null) existingField.LookupDisplayField = fieldDto.LookupDisplayField;
                        if (fieldDto.ForeignKeyAction.HasValue) existingField.ForeignKeyAction = fieldDto.ForeignKeyAction.Value;
                        if (fieldDto.SortOrder.HasValue) existingField.SortOrder = fieldDto.SortOrder.Value;
                        existingField.DefaultValue = fieldDto.DefaultValue;
                        existingField.ValidationRules = fieldDto.ValidationRules;
                        ApplyEnumConfig(existingField, fieldDto.DataType, fieldDto.EnumDefinitionId, fieldDto.IsMultiSelect);
                        existingField.UpdatedAt = DateTime.UtcNow;
                        existingField.IsDeleted = false;
                    }
                }
                else
                {
                    var newField = new FieldMetadata
                    {
                        EntityDefinitionId = definition.Id,
                        PropertyName = fieldDto.PropertyName ?? string.Empty,
                        DisplayName = fieldDto.DisplayName,
                        DataType = fieldDto.DataType ?? FieldDataType.String,
                        Length = fieldDto.Length,
                        Precision = fieldDto.Precision,
                        Scale = fieldDto.Scale,
                        IsRequired = fieldDto.IsRequired ?? false,
                        IsEntityRef = fieldDto.IsEntityRef ?? false,
                        ReferencedEntityId = fieldDto.ReferencedEntityId,
                        LookupEntityName = fieldDto.LookupEntityName,
                        LookupDisplayField = fieldDto.LookupDisplayField,
                        ForeignKeyAction = fieldDto.ForeignKeyAction ?? ForeignKeyAction.Restrict,
                        SortOrder = fieldDto.SortOrder ?? 0,
                        DefaultValue = fieldDto.DefaultValue,
                        ValidationRules = fieldDto.ValidationRules,
                        EnumDefinitionId = fieldDto.EnumDefinitionId,
                        IsMultiSelect = fieldDto.IsMultiSelect ?? false,
                        Source = FieldSource.Custom,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    definition.Fields.Add(newField);
                }
            }
        }

        if (dto.Interfaces != null)
        {
            SyncInterfaces(definition, dto.Interfaces);
        }

        var beforeType = ResolveFullTypeNameForCache(definition);

        await _db.SaveChangesAsync(ct);

        var afterType = ResolveFullTypeNameForCache(definition);
        if (!string.IsNullOrWhiteSpace(beforeType) && !string.Equals(beforeType, afterType, StringComparison.Ordinal))
        {
            _fieldMetadataCache.Invalidate(beforeType);
        }

        if (!string.IsNullOrWhiteSpace(afterType))
        {
            _fieldMetadataCache.Invalidate(afterType);
        }

        _logger.LogInformation("[EntityDefinition] Entity updated successfully: {Id}", id);
        return BuildResponseDto(definition);
    }

    private static string ResolveFullTypeNameForCache(EntityDefinition definition)
    {
        if (!string.IsNullOrWhiteSpace(definition.FullTypeName))
        {
            return definition.FullTypeName.Trim();
        }

        if (string.IsNullOrWhiteSpace(definition.Namespace) || string.IsNullOrWhiteSpace(definition.EntityName))
        {
            return string.Empty;
        }

        return $"{definition.Namespace.Trim()}.{definition.EntityName.Trim()}";
    }

    private string ResolveLang(string? lang)
    {
        if (!string.IsNullOrWhiteSpace(lang))
        {
            return lang;
        }

        var http = _httpContextAccessor.HttpContext;
        return http == null ? string.Empty : LangHelper.GetLang(http);
    }

    private static EntityDefinitionDto BuildResponseDto(EntityDefinition definition)
    {
        return new EntityDefinitionDto
        {
            Id = definition.Id,
            Namespace = definition.Namespace,
            EntityName = definition.EntityName,
            FullTypeName = definition.FullTypeName,
            EntityRoute = definition.EntityRoute,
            DisplayNameTranslations = definition.DisplayName != null ? new MultilingualText(definition.DisplayName) : new MultilingualText(),
            DescriptionTranslations = definition.Description != null ? new MultilingualText(definition.Description) : null,
            ApiEndpoint = definition.ApiEndpoint,
            StructureType = definition.StructureType,
            Status = definition.Status,
            Source = definition.Source,
            IsLocked = definition.IsLocked,
            IsRootEntity = definition.IsRootEntity,
            IsEnabled = definition.IsEnabled,
            Order = definition.Order,
            Icon = definition.Icon,
            Category = definition.Category,
            CreatedAt = definition.CreatedAt,
            UpdatedAt = definition.UpdatedAt,
            CreatedBy = definition.CreatedBy,
            UpdatedBy = definition.UpdatedBy
        };
    }

    private static void ApplyInterfaces(EntityDefinition definition, IEnumerable<string> interfaceTypes)
    {
        foreach (var interfaceType in interfaceTypes)
        {
            definition.Interfaces.Add(new EntityInterface
            {
                InterfaceType = interfaceType,
                IsEnabled = true
            });

            var interfaceFields = InterfaceFieldMapping.GetFields(interfaceType);
            foreach (var ifField in interfaceFields)
            {
                if (!definition.Fields.Any(f => f.PropertyName == ifField.PropertyName))
                {
                    definition.Fields.Add(new FieldMetadata
                    {
                        PropertyName = ifField.PropertyName,
                        DisplayName = null,
                        DataType = ifField.DataType,
                        Length = ifField.Length,
                        IsRequired = ifField.IsRequired,
                        IsEntityRef = ifField.IsEntityRef,
                        TableName = ifField.ReferenceTable,
                        DefaultValue = ifField.DefaultValue,
                        SortOrder = 0,
                        Source = FieldSource.Interface
                    });
                }
            }
        }
    }

    private static void SyncInterfaces(EntityDefinition definition, List<string> interfaceTypes)
    {
        var existingInterfaces = definition.Interfaces.Select(i => i.InterfaceType).ToHashSet();
        var incomingInterfaces = interfaceTypes.ToHashSet();

        var interfacesToRemove = definition.Interfaces
            .Where(i => !incomingInterfaces.Contains(i.InterfaceType))
            .ToList();
        foreach (var iface in interfacesToRemove)
        {
            definition.Interfaces.Remove(iface);
        }

        var newInterfaceTypes = incomingInterfaces.Except(existingInterfaces).ToList();
        ApplyInterfaces(definition, newInterfaceTypes);
    }

    private static void ApplyEnumConfig(
        FieldMetadata field,
        string? newDataType,
        Guid? enumDefinitionId,
        bool? isMultiSelect)
    {
        if (newDataType != null &&
            !string.Equals(newDataType, FieldDataType.Enum, StringComparison.OrdinalIgnoreCase))
        {
            field.EnumDefinitionId = null;
            field.IsMultiSelect = false;
            return;
        }

        if (!string.Equals(field.DataType, FieldDataType.Enum, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (enumDefinitionId.HasValue)
        {
            field.EnumDefinitionId = enumDefinitionId;
        }

        if (isMultiSelect.HasValue)
        {
            field.IsMultiSelect = isMultiSelect.Value;
        }
    }
}
