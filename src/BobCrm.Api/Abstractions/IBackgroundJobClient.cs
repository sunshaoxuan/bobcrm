using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.Responses.System;

namespace BobCrm.Api.Abstractions;

public interface IBackgroundJobClient
{
    Task<PagedResponse<BackgroundJobDto>> GetRecentJobsAsync(int page, int pageSize, CancellationToken ct = default);

    Task<BackgroundJobDto?> GetJobAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<BackgroundJobLogDto>> GetJobLogsAsync(Guid id, int limit = 500, CancellationToken ct = default);

    Task<bool> RequestCancelAsync(Guid id, CancellationToken ct = default);

    Guid StartJob(string name, string category, string? actorId, string? actorName, bool canCancel);

    void AppendLog(Guid jobId, string level, string message);

    void SetProgress(Guid jobId, int progressPercent);

    void Complete(Guid jobId);

    void Fail(Guid jobId, string errorMessage);
}

