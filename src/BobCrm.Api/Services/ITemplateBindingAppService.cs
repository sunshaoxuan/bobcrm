using BobCrm.Api.Contracts.DTOs.Template;
using BobCrm.Api.Contracts.Responses.Template;
using BobCrm.Api.Contracts.DTOs;

namespace BobCrm.Api.Services;

public interface ITemplateBindingAppService
{
    Task<List<MenuTemplateIntersectionDto>> GetMenuTemplateIntersectionsAsync(
        string uid,
        string? targetLang,
        string viewState,
        CancellationToken ct = default);
}
