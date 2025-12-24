namespace BobCrm.App.Models;

public class RoleFunctionDto
{
    public Guid RoleId { get; set; }
    public Guid FunctionId { get; set; }
    public FunctionMenuNode? Function { get; set; }
    public int? TemplateBindingId { get; set; }
}
