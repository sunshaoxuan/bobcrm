using BobCrm.Api.Abstractions;
using BobCrm.Api.Infrastructure;

namespace BobCrm.Api.Domain.Models;

/// <summary>
/// System user wrapper (metadata driven).
/// </summary>
public class SystemUser : IBizEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; }

    public static EntityDefinition GetInitialDefinition()
    {
        return EntityDefinitionArchive.Load("system-user");
    }
}
