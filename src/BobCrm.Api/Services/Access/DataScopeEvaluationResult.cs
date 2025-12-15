using System;
using System.Collections.Generic;
using System.Linq;

namespace BobCrm.Api.Services;

public record DataScopeEvaluationResult(bool HasFullAccess, IReadOnlyList<ScopeBinding> Scopes)
{
    public IReadOnlyList<Guid?> OrganizationFilter =>
        Scopes.Select(sb => sb.OrganizationId)
            .Where(id => id.HasValue)
            .Distinct()
            .ToList();
}
