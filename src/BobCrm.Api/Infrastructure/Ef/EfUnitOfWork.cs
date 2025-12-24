using BobCrm.Api.Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Infrastructure.Ef;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly DbContext _db;
    public EfUnitOfWork(DbContext db) => _db = db;
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
