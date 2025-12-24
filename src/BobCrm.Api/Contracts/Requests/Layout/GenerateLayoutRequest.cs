namespace BobCrm.Api.Contracts.Requests.Layout;

/// <summary>
/// 从标签生成布局请求。
/// </summary>
public record GenerateLayoutRequest(string[] Tags, string? Mode, bool? Save, string? Scope);

