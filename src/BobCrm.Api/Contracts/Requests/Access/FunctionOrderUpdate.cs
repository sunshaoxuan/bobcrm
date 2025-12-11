namespace BobCrm.Api.Contracts.Requests.Access;

/// <summary>
/// 功能排序更新请求
/// </summary>
public record FunctionOrderUpdate(Guid Id, Guid? ParentId, int SortOrder);
