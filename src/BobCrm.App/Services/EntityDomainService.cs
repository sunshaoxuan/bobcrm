using System.Net.Http.Json;
using BobCrm.App.Models;

namespace BobCrm.App.Services;

public class EntityDomainService
{
    private readonly AuthService _auth;

    public EntityDomainService(AuthService auth)
    {
        _auth = auth;
    }

    public async Task<List<EntityDomainDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var domains = await http.GetFromJsonAsync<List<EntityDomainDto>>("/api/entity-domains", cancellationToken: cancellationToken);
        if (domains == null || domains.Count == 0)
        {
            return new List<EntityDomainDto>();
        }

        foreach (var domain in domains)
        {
            domain.Name ??= new MultilingualTextDto();
        }

        return domains;
    }
}

