using System;
using System.Collections.Generic;

namespace BobCrm.Api.Services.EntityLocking;

/// <summary>
/// 实体锁定信息
/// </summary>
public class EntityLockInfo
{
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = "";
    public bool IsLocked { get; set; }
    public List<string> Reasons { get; set; } = new();
}
