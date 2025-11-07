using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Domain.Models;

/// <summary>
/// 实体接口 - 通过勾选快速添加常用字段
/// </summary>
public class EntityInterface
{
    /// <summary>
    /// 实体接口ID
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 所属实体定义ID
    /// </summary>
    [Required]
    public Guid EntityDefinitionId { get; set; }

    /// <summary>
    /// 接口类型（Base、Archive、Audit、Version、TimeVersion）
    /// </summary>
    [Required, MaxLength(50)]
    public string InterfaceType { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 是否已被引用锁定（锁定后不可删除）
    /// </summary>
    public bool IsLocked { get; set; } = false;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 所属实体定义（导航属性）
    /// </summary>
    public EntityDefinition? EntityDefinition { get; set; }
}

/// <summary>
/// 接口类型枚举
/// </summary>
public static class InterfaceType
{
    /// <summary>
    /// 基础信息接口 - 提供 Id 主键
    /// </summary>
    public const string Base = "Base";

    /// <summary>
    /// 档案信息接口 - 提供 Code、Name 字段
    /// </summary>
    public const string Archive = "Archive";

    /// <summary>
    /// 审计信息接口 - 提供 CreatedAt、CreatedBy、UpdatedAt、UpdatedBy 字段
    /// </summary>
    public const string Audit = "Audit";

    /// <summary>
    /// 版本管理接口 - 提供 Version 字段（乐观锁）
    /// </summary>
    public const string Version = "Version";

    /// <summary>
    /// 时间版本接口 - 提供 ValidFrom、ValidTo、VersionNo 字段
    /// </summary>
    public const string TimeVersion = "TimeVersion";
}

/// <summary>
/// 接口字段映射 - 每种接口对应的字段定义
/// </summary>
public static class InterfaceFieldMapping
{
    /// <summary>
    /// 获取接口类型对应的字段列表
    /// </summary>
    public static List<InterfaceFieldDefinition> GetFields(string interfaceType)
    {
        return interfaceType switch
        {
            InterfaceType.Base => new List<InterfaceFieldDefinition>
            {
                new() { PropertyName = "Id", DataType = FieldDataType.Guid, IsRequired = true, IsPrimaryKey = true }
            },
            InterfaceType.Archive => new List<InterfaceFieldDefinition>
            {
                new() { PropertyName = "Code", DataType = FieldDataType.String, Length = 50, IsRequired = true },
                new() { PropertyName = "Name", DataType = FieldDataType.String, Length = 255, IsRequired = true }
            },
            InterfaceType.Audit => new List<InterfaceFieldDefinition>
            {
                new() { PropertyName = "CreatedAt", DataType = FieldDataType.DateTime, IsRequired = true },
                new() { PropertyName = "CreatedBy", DataType = FieldDataType.String, Length = 100, IsRequired = false },
                new() { PropertyName = "UpdatedAt", DataType = FieldDataType.DateTime, IsRequired = true },
                new() { PropertyName = "UpdatedBy", DataType = FieldDataType.String, Length = 100, IsRequired = false }
            },
            InterfaceType.Version => new List<InterfaceFieldDefinition>
            {
                new() { PropertyName = "Version", DataType = FieldDataType.Int32, IsRequired = true, DefaultValue = "1" }
            },
            InterfaceType.TimeVersion => new List<InterfaceFieldDefinition>
            {
                new() { PropertyName = "ValidFrom", DataType = FieldDataType.DateTime, IsRequired = true },
                new() { PropertyName = "ValidTo", DataType = FieldDataType.DateTime, IsRequired = true },
                new() { PropertyName = "VersionNo", DataType = FieldDataType.Int32, IsRequired = true, DefaultValue = "1" }
            },
            _ => new List<InterfaceFieldDefinition>()
        };
    }
}

/// <summary>
/// 接口字段定义
/// </summary>
public class InterfaceFieldDefinition
{
    public string PropertyName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int? Length { get; set; }
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsPrimaryKey { get; set; }
}
