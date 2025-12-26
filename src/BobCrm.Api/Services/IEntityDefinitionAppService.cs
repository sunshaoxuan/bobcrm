using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.Requests.Entity;
using BobCrm.Api.Contracts.Responses.Entity;
using Microsoft.Extensions.Localization;

namespace BobCrm.Api.Services;

public interface IEntityDefinitionAppService
{
    Task<EntityDefinitionDto> CreateEntityDefinitionAsync(string uid, string? lang, CreateEntityDefinitionDto dto, CancellationToken ct = default);
    Task<EntityDefinitionDto> UpdateEntityDefinitionAsync(Guid id, string uid, string? lang, UpdateEntityDefinitionDto dto, CancellationToken ct = default);
}
