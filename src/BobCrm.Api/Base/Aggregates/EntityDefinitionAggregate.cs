namespace BobCrm.Api.Base.Aggregates;

using BobCrm.Api.Base.Models;

/// <summary>
/// 实体定义聚合根
/// 管理主实体及其子实体的完整生命周期，确保业务规则的一致性
/// </summary>
public class EntityDefinitionAggregate
{
    private readonly EntityDefinition _root;
    private readonly List<SubEntityDefinition> _subEntities;

    /// <summary>
    /// 聚合根实体（主实体）
    /// </summary>
    public EntityDefinition Root => _root;

    /// <summary>
    /// 子实体集合（只读）
    /// </summary>
    public IReadOnlyList<SubEntityDefinition> SubEntities => _subEntities.AsReadOnly();

    /// <summary>
    /// 构造函数（用于新建聚合）
    /// </summary>
    /// <param name="root">主实体定义</param>
    public EntityDefinitionAggregate(EntityDefinition root)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        _subEntities = new List<SubEntityDefinition>();
    }

    /// <summary>
    /// 构造函数（用于加载现有聚合）
    /// </summary>
    /// <param name="root">主实体定义</param>
    /// <param name="subEntities">子实体定义列表</param>
    public EntityDefinitionAggregate(EntityDefinition root, List<SubEntityDefinition> subEntities)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        _subEntities = subEntities ?? new List<SubEntityDefinition>();
    }

    // ============ 子实体管理 ============

    /// <summary>
    /// 添加子实体
    /// </summary>
    /// <param name="code">子实体编码</param>
    /// <param name="displayName">显示名称（多语言）</param>
    /// <param name="description">描述（多语言，可选）</param>
    /// <param name="sortOrder">排序顺序</param>
    /// <returns>新创建的子实体</returns>
    /// <exception cref="DomainException">当子实体编码已存在时</exception>
    public SubEntityDefinition AddSubEntity(
        string code,
        Dictionary<string, string?> displayName,
        Dictionary<string, string?>? description = null,
        int sortOrder = 0)
    {
        // 验证子实体编码唯一性
        if (_subEntities.Any(s => s.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException($"子实体编码 '{code}' 已存在");
        }

        // 验证编码格式
        if (!IsValidCode(code))
        {
            throw new DomainException($"子实体编码 '{code}' 格式无效。必须以大写字母开头，只能包含字母和数字");
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

    /// <summary>
    /// 更新子实体
    /// </summary>
    /// <param name="subEntityId">子实体ID</param>
    /// <param name="displayName">显示名称</param>
    /// <param name="description">描述</param>
    /// <param name="sortOrder">排序顺序</param>
    /// <exception cref="DomainException">当子实体不存在时</exception>
    public void UpdateSubEntity(
        Guid subEntityId,
        Dictionary<string, string?> displayName,
        Dictionary<string, string?>? description = null,
        int? sortOrder = null)
    {
        var subEntity = _subEntities.FirstOrDefault(s => s.Id == subEntityId);
        if (subEntity == null)
        {
            throw new DomainException($"子实体 ID {subEntityId} 不存在");
        }

        subEntity.DisplayName = displayName;
        subEntity.Description = description;
        if (sortOrder.HasValue)
        {
            subEntity.SortOrder = sortOrder.Value;
        }
        subEntity.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 移除子实体
    /// </summary>
    /// <param name="subEntityId">子实体ID</param>
    public void RemoveSubEntity(Guid subEntityId)
    {
        var subEntity = _subEntities.FirstOrDefault(s => s.Id == subEntityId);
        if (subEntity != null)
        {
            _subEntities.Remove(subEntity);
        }
    }

    /// <summary>
    /// 获取子实体
    /// </summary>
    /// <param name="subEntityId">子实体ID</param>
    /// <returns>子实体，如果不存在返回null</returns>
    public SubEntityDefinition? GetSubEntity(Guid subEntityId)
    {
        return _subEntities.FirstOrDefault(s => s.Id == subEntityId);
    }

    // ============ 字段管理 ============

    /// <summary>
    /// 为子实体添加字段
    /// </summary>
    /// <param name="subEntityId">子实体ID</param>
    /// <param name="propertyName">字段属性名</param>
    /// <param name="displayName">显示名称（多语言）</param>
    /// <param name="dataType">数据类型</param>
    /// <param name="isRequired">是否必填</param>
    /// <param name="length">长度（可选）</param>
    /// <param name="sortOrder">排序顺序</param>
    /// <returns>新创建的字段</returns>
    /// <exception cref="DomainException">当子实体不存在或字段名重复时</exception>
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
            throw new DomainException($"子实体 ID {subEntityId} 不存在");
        }

        // 验证字段名唯一性（同一子实体内）
        if (subEntity.Fields.Any(f => f.PropertyName.Equals(propertyName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException($"子实体 '{subEntity.Code}' 中字段 '{propertyName}' 已存在");
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

    /// <summary>
    /// 更新子实体的字段
    /// </summary>
    /// <param name="subEntityId">子实体ID</param>
    /// <param name="fieldId">字段ID</param>
    /// <param name="propertyName">字段属性名</param>
    /// <param name="displayName">显示名称</param>
    /// <param name="dataType">数据类型</param>
    /// <param name="isRequired">是否必填</param>
    /// <param name="length">长度</param>
    /// <exception cref="DomainException">当子实体或字段不存在时</exception>
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
            throw new DomainException($"子实体 ID {subEntityId} 不存在");
        }

        var field = subEntity.Fields.FirstOrDefault(f => f.Id == fieldId);
        if (field == null)
        {
            throw new DomainException($"字段 ID {fieldId} 不存在");
        }

        // 如果字段名变更，检查唯一性
        if (!field.PropertyName.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
        {
            if (subEntity.Fields.Any(f => f.Id != fieldId && f.PropertyName.Equals(propertyName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new DomainException($"子实体 '{subEntity.Code}' 中字段 '{propertyName}' 已存在");
            }
        }

        field.PropertyName = propertyName;
        field.DisplayName = displayName;
        field.DataType = dataType;
        field.IsRequired = isRequired;
        field.Length = length;
        field.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 从子实体移除字段
    /// </summary>
    /// <param name="subEntityId">子实体ID</param>
    /// <param name="fieldId">字段ID</param>
    public void RemoveFieldFromSubEntity(Guid subEntityId, Guid fieldId)
    {
        var subEntity = _subEntities.FirstOrDefault(s => s.Id == subEntityId);
        if (subEntity != null)
        {
            var field = subEntity.Fields.FirstOrDefault(f => f.Id == fieldId);
            if (field != null)
            {
                subEntity.Fields.Remove(field);
            }
        }
    }

    // ============ 验证 ============

    /// <summary>
    /// 验证聚合完整性
    /// </summary>
    /// <returns>验证结果</returns>
    public ValidationResult Validate()
    {
        var errors = new List<ValidationError>();

        // 验证主实体
        ValidateRootEntity(errors);

        // 验证子实体
        foreach (var subEntity in _subEntities)
        {
            ValidateSubEntity(subEntity, errors);
        }

        // 验证引用一致性
        ValidateReferenceConsistency(errors);

        return new ValidationResult(errors);
    }

    private void ValidateRootEntity(List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(_root.EntityName))
        {
            errors.Add(new ValidationError("Root.EntityName", "实体名称不能为空"));
        }

        if (_root.DisplayName == null || !_root.DisplayName.Any())
        {
            errors.Add(new ValidationError("Root.DisplayName", "显示名称不能为空"));
        }

        if (string.IsNullOrWhiteSpace(_root.Namespace))
        {
            errors.Add(new ValidationError("Root.Namespace", "命名空间不能为空"));
        }
    }

    private void ValidateSubEntity(SubEntityDefinition subEntity, List<ValidationError> errors)
    {
        var context = $"SubEntity[{subEntity.Code}]";

        if (string.IsNullOrWhiteSpace(subEntity.Code))
        {
            errors.Add(new ValidationError($"{context}.Code", "子实体编码不能为空"));
        }
        else if (!IsValidCode(subEntity.Code))
        {
            errors.Add(new ValidationError($"{context}.Code", "子实体编码格式无效。必须以大写字母开头，只能包含字母和数字"));
        }

        if (subEntity.DisplayName == null || !subEntity.DisplayName.Any())
        {
            errors.Add(new ValidationError($"{context}.DisplayName", "子实体显示名称不能为空"));
        }

        // 验证字段唯一性
        var duplicateFields = subEntity.Fields
            .GroupBy(f => f.PropertyName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateFields.Any())
        {
            errors.Add(new ValidationError(
                $"{context}.Fields",
                $"字段名重复: {string.Join(", ", duplicateFields)}"));
        }

        // 验证必填字段
        foreach (var field in subEntity.Fields.Where(f => f.IsRequired))
        {
            if (string.IsNullOrWhiteSpace(field.DataType))
            {
                errors.Add(new ValidationError(
                    $"{context}.Field[{field.PropertyName}].DataType",
                    "必填字段的数据类型不能为空"));
            }

            if (string.IsNullOrWhiteSpace(field.PropertyName))
            {
                errors.Add(new ValidationError(
                    $"{context}.Field.PropertyName",
                    "必填字段的属性名不能为空"));
            }
        }

        // 验证字段属性名
        foreach (var field in subEntity.Fields)
        {
            if (string.IsNullOrWhiteSpace(field.PropertyName))
            {
                errors.Add(new ValidationError(
                    $"{context}.Field.PropertyName",
                    "字段属性名不能为空"));
            }
        }
    }

    private void ValidateReferenceConsistency(List<ValidationError> errors)
    {
        // 验证所有子实体的EntityDefinitionId与聚合根ID一致
        foreach (var subEntity in _subEntities)
        {
            if (subEntity.EntityDefinitionId != _root.Id)
            {
                errors.Add(new ValidationError(
                    $"SubEntity[{subEntity.Code}].EntityDefinitionId",
                    "子实体的EntityDefinitionId与聚合根不一致"));
            }

            // 验证所有字段的EntityDefinitionId与聚合根ID一致
            foreach (var field in subEntity.Fields)
            {
                if (field.EntityDefinitionId != _root.Id)
                {
                    errors.Add(new ValidationError(
                        $"SubEntity[{subEntity.Code}].Field[{field.PropertyName}].EntityDefinitionId",
                        "字段的EntityDefinitionId与聚合根不一致"));
                }

                if (field.SubEntityDefinitionId != subEntity.Id)
                {
                    errors.Add(new ValidationError(
                        $"SubEntity[{subEntity.Code}].Field[{field.PropertyName}].SubEntityDefinitionId",
                        "字段的SubEntityDefinitionId与所属子实体不一致"));
                }
            }
        }
    }

    private static bool IsValidCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        // 必须以大写字母开头
        if (!char.IsUpper(code[0]))
            return false;

        // 只能包含字母和数字
        return code.All(c => char.IsLetterOrDigit(c));
    }
}
