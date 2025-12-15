using System.Collections.Generic;

namespace BobCrm.Api.Services.EntityLocking;

/// <summary>
/// 实体锁定验证结果
/// </summary>
public class EntityLockValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
