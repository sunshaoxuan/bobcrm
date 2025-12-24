using System.Linq.Expressions;

namespace BobCrm.Api.Core.Persistence;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id, CancellationToken ct = default);
    IQueryable<T> Query(Expression<Func<T, bool>>? predicate = null);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}
