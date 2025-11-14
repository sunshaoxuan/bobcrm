using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Endpoints;

public static class DomainEndpoints
{
    public static IEndpointRouteBuilder MapDomainEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/entity-domains")
            .RequireAuthorization()
            .WithTags("Entity Domains")
            .WithOpenApi();

        group.MapGet("/", async (AppDbContext db) =>
        {
            var domains = await db.EntityDomains
                .AsNoTracking()
                .Where(d => d.IsEnabled)
                .OrderBy(d => d.SortOrder)
                .ThenBy(d => d.Code)
                .Select(d => new EntityDomainResponse
                {
                    Id = d.Id,
                    Code = d.Code,
                    Name = d.Name,
                    SortOrder = d.SortOrder,
                    IsSystem = d.IsSystem
                })
                .ToListAsync();

            return Results.Json(domains);
        })
        .WithName("GetEntityDomains")
        .WithSummary("获取实体领域列表")
        .WithDescription("返回可用的实体领域信息，包含多语言名称。");

        return app;
    }

    private sealed class EntityDomainResponse
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public Dictionary<string, string?>? Name { get; set; }
        public int SortOrder { get; set; }
        public bool IsSystem { get; set; }
    }
}

