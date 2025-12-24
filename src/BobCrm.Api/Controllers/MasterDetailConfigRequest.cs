using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Controllers;

/// <summary>
/// 主子表配置请求
/// </summary>
public class MasterDetailConfigRequest
{
    /// <summary>结构类型</summary>
    public string StructureType { get; set; } = EntityStructureType.Single;

    /// <summary>子实体配置列表</summary>
    public List<ChildEntityConfig>? Children { get; set; }
}
