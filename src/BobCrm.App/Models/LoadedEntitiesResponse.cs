namespace BobCrm.App.Models;

/// <summary>
/// 已加载实体响应
/// </summary>
public class LoadedEntitiesResponse
{
    public int Count { get; set; }
    public List<string> Entities { get; set; } = new();
}
