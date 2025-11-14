using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Domain.Models;

/// <summary>
/// 业务领域档案，用于对实体分类并支持多语言显示
/// </summary>
public class EntityDomain
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(64)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 多语言名称（jsonb）
    /// </summary>
    public Dictionary<string, string?> Name { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public int SortOrder { get; set; } = 100;

    public bool IsSystem { get; set; } = true;

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

