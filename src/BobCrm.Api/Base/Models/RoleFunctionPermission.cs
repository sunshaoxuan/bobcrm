namespace BobCrm.Api.Base.Models;

/// <summary>
/// 角色功能对照
/// </summary>
public class RoleFunctionPermission
{
    public Guid RoleId { get; set; }
    public RoleProfile? Role { get; set; }

    public Guid FunctionId { get; set; }
    public FunctionNode? Function { get; set; }

    public int? TemplateBindingId { get; set; }
    public TemplateBinding? TemplateBinding { get; set; }
}
