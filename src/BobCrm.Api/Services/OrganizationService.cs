using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.Requests.Organization;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

public class OrganizationService
{
    private readonly AppDbContext _db;

    public OrganizationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<OrganizationNodeDto>> GetTreeAsync(CancellationToken ct = default)
    {
        var nodes = await _db.OrganizationNodes
            .OrderBy(o => o.PathCode)
            .AsNoTracking()
            .ToListAsync(ct);

        var lookup = nodes.ToDictionary(x => x.Id, x => ToDto(x));
        List<OrganizationNodeDto> roots = new();

        foreach (var node in nodes)
        {
            var dto = lookup[node.Id];
            if (node.ParentId.HasValue && lookup.TryGetValue(node.ParentId.Value, out var parent))
            {
                parent.Children.Add(dto);
            }
            else
            {
                roots.Add(dto);
            }
        }

        return roots;
    }

    public async Task<OrganizationNodeDto> CreateAsync(CreateOrganizationRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new InvalidOperationException("Code is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Name is required.");
        }

        if (request.ParentId == null)
        {
            var hasRoot = await _db.OrganizationNodes.AnyAsync(x => x.ParentId == null, ct);
            if (hasRoot)
            {
                throw new InvalidOperationException("Root organization already exists.");
            }
        }

        await EnsureCodeUniqueAsync(request.ParentId, request.Code, Guid.Empty, ct);

        var parent = request.ParentId.HasValue
            ? await _db.OrganizationNodes.FirstOrDefaultAsync(x => x.Id == request.ParentId.Value, ct)
            : null;

        if (request.ParentId.HasValue && parent == null)
        {
            throw new InvalidOperationException("Parent not found.");
        }

        var entity = new OrganizationNode
        {
            Id = Guid.NewGuid(),
            ParentId = request.ParentId,
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Level = parent?.Level + 1 ?? 0,
            SortOrder = await GetNextSortOrderAsync(request.ParentId, ct),
            PathCode = await GeneratePathCodeAsync(parent, ct)
        };

        _db.OrganizationNodes.Add(entity);
        await _db.SaveChangesAsync(ct);

        return ToDto(entity);
    }

    public async Task<OrganizationNodeDto> UpdateAsync(Guid id, UpdateOrganizationRequest request, CancellationToken ct = default)
    {
        var entity = await _db.OrganizationNodes.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("Organization not found.");

        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Code and Name are required.");
        }

        await EnsureCodeUniqueAsync(entity.ParentId, request.Code, id, ct);

        entity.Code = request.Code.Trim();
        entity.Name = request.Name.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var hasChild = await _db.OrganizationNodes.AnyAsync(x => x.ParentId == id, ct);
        if (hasChild)
        {
            throw new InvalidOperationException("Cannot delete organization that has children.");
        }

        var entity = await _db.OrganizationNodes.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("Organization not found.");

        if (entity.ParentId == null)
        {
            throw new InvalidOperationException("Root organization cannot be deleted.");
        }

        _db.OrganizationNodes.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    private async Task EnsureCodeUniqueAsync(Guid? parentId, string code, Guid currentId, CancellationToken ct)
    {
        var exists = await _db.OrganizationNodes
            .AnyAsync(x => x.ParentId == parentId && x.Code == code && x.Id != currentId, ct);
        if (exists)
        {
            throw new InvalidOperationException("Code already exists for the selected parent.");
        }
    }

    private async Task<string> GeneratePathCodeAsync(OrganizationNode? parent, CancellationToken ct)
    {
        var parentId = parent?.Id;
        var siblings = await _db.OrganizationNodes
            .Where(x => x.ParentId == parentId)
            .Select(x => x.PathCode)
            .ToListAsync(ct);

        var nextSegment = CalculateNextSegment(siblings);
        return parent == null ? nextSegment : $"{parent.PathCode}.{nextSegment}";
    }

    private static string CalculateNextSegment(IEnumerable<string> siblingPathCodes)
    {
        var lastSegment = siblingPathCodes
            .Select(path => path?.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? path)
            .Select(segment => int.TryParse(segment, out var value) ? value : 0)
            .DefaultIfEmpty(0)
            .Max();
        return (lastSegment + 1).ToString("D2");
    }

    private async Task<int> GetNextSortOrderAsync(Guid? parentId, CancellationToken ct)
    {
        var maxSort = await _db.OrganizationNodes
            .Where(x => x.ParentId == parentId)
            .OrderByDescending(x => x.SortOrder)
            .Select(x => (int?)x.SortOrder)
            .FirstOrDefaultAsync(ct);
        return (maxSort ?? 0) + 10;
    }

    private static OrganizationNodeDto ToDto(OrganizationNode node) =>
        new()
        {
            Id = node.Id,
            ParentId = node.ParentId,
            Code = node.Code,
            Name = node.Name,
            PathCode = node.PathCode,
            Level = node.Level,
            SortOrder = node.SortOrder,
            Children = new List<OrganizationNodeDto>()
        };
}
