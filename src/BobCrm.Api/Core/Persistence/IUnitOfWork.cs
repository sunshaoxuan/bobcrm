namespace BobCrm.Api.Core.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
