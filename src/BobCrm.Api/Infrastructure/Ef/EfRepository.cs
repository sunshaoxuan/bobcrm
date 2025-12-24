using BobCrm.Api.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BobCrm.Api.Infrastructure.Ef;

public class EfRepository<T> : IRepository<T> where T : class
{
    private readonly DbContext _db;
    private readonly DbSet<T> _set;

    public EfRepository(DbContext db)
    {
        _db = db;
        _set = _db.Set<T>();
    }

    public async Task<T?> GetByIdAsync(object id, CancellationToken ct = default)
        => await _set.FindAsync(new[] { id }, ct);

    public IQueryable<T> Query(Expression<Func<T, bool>>? predicate = null)
        => predicate is null ? _set.AsQueryable() : _set.Where(predicate);

    public Task AddAsync(T entity, CancellationToken ct = default)
        => _set.AddAsync(entity, ct).AsTask();

    public void Update(T entity) => _set.Update(entity);

    public void Remove(T entity) => _set.Remove(entity);
}

