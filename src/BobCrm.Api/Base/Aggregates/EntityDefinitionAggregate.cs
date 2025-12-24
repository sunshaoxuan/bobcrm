namespace BobCrm.Api.Base.Aggregates;

using BobCrm.Api.Base.Models;

public class EntityDefinitionAggregate
{
    private readonly EntityDefinition _root;
    private readonly List<SubEntityDefinition> _subEntities;

    public EntityDefinition Root => _root;

    public IReadOnlyList<SubEntityDefinition> SubEntities => _subEntities.AsReadOnly();

    public EntityDefinitionAggregate(EntityDefinition root)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        _subEntities = new List<SubEntityDefinition>();
    }

    public EntityDefinitionAggregate(EntityDefinition root, List<SubEntityDefinition> subEntities)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        _subEntities = subEntities ?? new List<SubEntityDefinition>();
    }

    public SubEntityDefinition AddSubEntity(
        string code,
        Dictionary<string, string?> displayName,
        Dictionary<string, string?>? description = null,
        int sortOrder = 0)
    {
        if (_subEntities.Any(s => s.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
        {
            ThrowDomain("ERR_SUBENTITY_CODE_EXISTS", code);
        }

        if (!IsValidCode(code))
        {
            ThrowDomain("ERR_SUBENTITY_CODE_INVALID", code);
        }

        var subEntity = new SubEntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = _root.Id,
            Code = code,
            DisplayName = displayName,
            Description = description,
            SortOrder = sortOrder,
            Fields = new List<FieldMetadata>(),
            CreatedAt = DateTime.UtcNow
        };

        _subEntities.Add(subEntity);
        return subEntity;
    }

    public void UpdateSubEntity(
        Guid subEntityId,
        Dictionary<string, string?> displayName,
        Dictionary<string, string?>? description = null,
        int? sortOrder = null)
    {
        var subEntity = _subEntities.FirstOrDefault(s => s.Id == subEntityId);
        if (subEntity == null)
        {
            ThrowDomain("ERR_SUBENTITY_NOT_FOUND", subEntityId);
        }

        subEntity.DisplayName = displayName;
        subEntity.Description = description;
        if (sortOrder.HasValue)
        {
            subEntity.SortOrder = sortOrder.Value;
        }
        subEntity.UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveSubEntity(Guid subEntityId)
    {
        var subEntity = _subEntities.FirstOrDefault(s => s.Id == subEntityId);
        if (subEntity != null)
        {
            _subEntities.Remove(subEntity);
        }
    }

    public SubEntityDefinition? GetSubEntity(Guid subEntityId)
        => _subEntities.FirstOrDefault(s => s.Id == subEntityId);

    public FieldMetadata AddFieldToSubEntity(
        Guid subEntityId,
        string propertyName,
        Dictionary<string, string?> displayName,
        string dataType,
        bool isRequired = false,
        int? length = null,
        int sortOrder = 0)
    {
        var subEntity = _subEntities.FirstOrDefault(s => s.Id == subEntityId);
        if (subEntity == null)
        {
            ThrowDomain("ERR_SUBENTITY_NOT_FOUND", subEntityId);
        }

        if (subEntity.Fields.Any(f => f.PropertyName.Equals(propertyName, StringComparison.OrdinalIgnoreCase)))
        {
            ThrowDomain("ERR_SUBENTITY_FIELD_EXISTS", subEntity.Code, propertyName);
        }

        var field = new FieldMetadata
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = _root.Id,
            SubEntityDefinitionId = subEntityId,
            PropertyName = propertyName,
            DisplayName = displayName,
            DataType = dataType,
            IsRequired = isRequired,
            Length = length,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        subEntity.Fields.Add(field);
        return field;
    }

    public void UpdateFieldInSubEntity(
        Guid subEntityId,
        Guid fieldId,
        string propertyName,
        Dictionary<string, string?> displayName,
        string dataType,
        bool isRequired,
        int? length = null)
    {
        var subEntity = _subEntities.FirstOrDefault(s => s.Id == subEntityId);
        if (subEntity == null)
        {
            ThrowDomain("ERR_SUBENTITY_NOT_FOUND", subEntityId);
        }

        var field = subEntity.Fields.FirstOrDefault(f => f.Id == fieldId);
        if (field == null)
        {
            ThrowDomain("ERR_FIELD_NOT_FOUND", fieldId);
        }

        if (!field.PropertyName.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
        {
            if (subEntity.Fields.Any(f => f.Id != fieldId && f.PropertyName.Equals(propertyName, StringComparison.OrdinalIgnoreCase)))
            {
                ThrowDomain("ERR_SUBENTITY_FIELD_EXISTS", subEntity.Code, propertyName);
            }
        }

        field.PropertyName = propertyName;
        field.DisplayName = displayName;
        field.DataType = dataType;
        field.IsRequired = isRequired;
        field.Length = length;
        field.UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveFieldFromSubEntity(Guid subEntityId, Guid fieldId)
    {
        var subEntity = _subEntities.FirstOrDefault(s => s.Id == subEntityId);
        if (subEntity == null)
        {
            return;
        }

        var field = subEntity.Fields.FirstOrDefault(f => f.Id == fieldId);
        if (field != null)
        {
            subEntity.Fields.Remove(field);
        }
    }

    public ValidationResult Validate()
    {
        var errors = new List<ValidationError>();

        ValidateRootEntity(errors);

        foreach (var subEntity in _subEntities)
        {
            ValidateSubEntity(subEntity, errors);
        }

        ValidateReferenceConsistency(errors);

        return new ValidationResult(errors);
    }

    private void ValidateRootEntity(List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(_root.EntityName))
        {
            errors.Add(new ValidationError("Root.EntityName", "ERR_ENTITY_NAME_REQUIRED"));
        }

        if (_root.DisplayName == null || !_root.DisplayName.Any())
        {
            errors.Add(new ValidationError("Root.DisplayName", "ERR_DISPLAY_NAME_REQUIRED"));
        }

        if (string.IsNullOrWhiteSpace(_root.Namespace))
        {
            errors.Add(new ValidationError("Root.Namespace", "ERR_NAMESPACE_REQUIRED"));
        }
    }

    private void ValidateSubEntity(SubEntityDefinition subEntity, List<ValidationError> errors)
    {
        var context = $"SubEntity[{subEntity.Code}]";

        if (string.IsNullOrWhiteSpace(subEntity.Code))
        {
            errors.Add(new ValidationError($"{context}.Code", "ERR_SUBENTITY_CODE_REQUIRED"));
        }
        else if (!IsValidCode(subEntity.Code))
        {
            errors.Add(new ValidationError($"{context}.Code", "ERR_SUBENTITY_CODE_INVALID", subEntity.Code));
        }

        if (subEntity.DisplayName == null || !subEntity.DisplayName.Any())
        {
            errors.Add(new ValidationError($"{context}.DisplayName", "ERR_SUBENTITY_DISPLAY_NAME_REQUIRED"));
        }

        var duplicateFields = subEntity.Fields
            .Where(f => !string.IsNullOrWhiteSpace(f.PropertyName))
            .GroupBy(f => f.PropertyName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateFields.Any())
        {
            errors.Add(new ValidationError(
                $"{context}.Fields",
                "ERR_DUPLICATE_FIELDS",
                string.Join(", ", duplicateFields)));
        }

        foreach (var field in subEntity.Fields.Where(f => f.IsRequired))
        {
            if (string.IsNullOrWhiteSpace(field.DataType))
            {
                errors.Add(new ValidationError(
                    $"{context}.Field[{field.PropertyName}].DataType",
                    "ERR_REQUIRED_FIELD_DATATYPE_REQUIRED",
                    field.PropertyName));
            }

            if (string.IsNullOrWhiteSpace(field.PropertyName))
            {
                errors.Add(new ValidationError(
                    $"{context}.Field.PropertyName",
                    "ERR_REQUIRED_FIELD_PROPERTY_REQUIRED"));
            }
        }

        foreach (var field in subEntity.Fields)
        {
            if (string.IsNullOrWhiteSpace(field.PropertyName))
            {
                errors.Add(new ValidationError(
                    $"{context}.Field.PropertyName",
                    "ERR_FIELD_PROPERTY_REQUIRED"));
            }
        }
    }

    private void ValidateReferenceConsistency(List<ValidationError> errors)
    {
        foreach (var subEntity in _subEntities)
        {
            if (subEntity.EntityDefinitionId != _root.Id)
            {
                errors.Add(new ValidationError(
                    $"SubEntity[{subEntity.Code}].EntityDefinitionId",
                    "ERR_SUBENTITY_ENTITYDEF_ID_MISMATCH"));
            }

            foreach (var field in subEntity.Fields)
            {
                if (field.EntityDefinitionId != _root.Id)
                {
                    errors.Add(new ValidationError(
                        $"SubEntity[{subEntity.Code}].Field[{field.PropertyName}].EntityDefinitionId",
                        "ERR_FIELD_ENTITYDEF_ID_MISMATCH"));
                }

                if (field.SubEntityDefinitionId != subEntity.Id)
                {
                    errors.Add(new ValidationError(
                        $"SubEntity[{subEntity.Code}].Field[{field.PropertyName}].SubEntityDefinitionId",
                        "ERR_FIELD_SUBENTITY_ID_MISMATCH"));
                }
            }
        }
    }

    private static void ThrowDomain(string messageKey, params object[] args)
        => throw new DomainException(messageKey, args);

    private static bool IsValidCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        if (!char.IsUpper(code[0]))
        {
            return false;
        }

        return code.All(c => char.IsLetterOrDigit(c));
    }
}
