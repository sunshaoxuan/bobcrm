using System.Collections.Concurrent;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.Responses.System;

namespace BobCrm.Api.Services.BackgroundJobs;

public sealed class InMemoryBackgroundJobClient : IBackgroundJobClient
{
    private const int MaxLogsPerJob = 2000;
    private static readonly TimeSpan DefaultRetention = TimeSpan.FromDays(7);

    private readonly ConcurrentDictionary<Guid, JobState> _jobs = new();

    public Task<PagedResponse<BackgroundJobDto>> GetRecentJobsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        CleanupExpired();

        var items = _jobs.Values
            .OrderByDescending(x => x.StartedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.ToDto())
            .ToList();

        var total = _jobs.Count;
        return Task.FromResult(new PagedResponse<BackgroundJobDto>(items, page, pageSize, total));
    }

    public Task<BackgroundJobDto?> GetJobAsync(Guid id, CancellationToken ct = default)
    {
        CleanupExpired();
        return Task.FromResult(_jobs.TryGetValue(id, out var job) ? job.ToDto() : null);
    }

    public Task<IReadOnlyList<BackgroundJobLogDto>> GetJobLogsAsync(Guid id, int limit = 500, CancellationToken ct = default)
    {
        CleanupExpired();
        if (!_jobs.TryGetValue(id, out var job))
        {
            return Task.FromResult<IReadOnlyList<BackgroundJobLogDto>>(Array.Empty<BackgroundJobLogDto>());
        }

        limit = Math.Clamp(limit, 1, MaxLogsPerJob);
        var list = job.LogsSnapshot(limit);
        return Task.FromResult<IReadOnlyList<BackgroundJobLogDto>>(list);
    }

    public Task<bool> RequestCancelAsync(Guid id, CancellationToken ct = default)
    {
        CleanupExpired();
        if (!_jobs.TryGetValue(id, out var job))
        {
            return Task.FromResult(false);
        }

        if (!job.CanCancel || !string.Equals(job.Status, "Running", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(false);
        }

        job.MarkCancelRequested();
        return Task.FromResult(true);
    }

    public Guid StartJob(string name, string category, string? actorId, string? actorName, bool canCancel)
    {
        CleanupExpired();
        var id = Guid.NewGuid();
        var job = new JobState(id, name, category, actorId, actorName, canCancel);
        _jobs[id] = job;
        job.AppendLog("INFO", $"Job started: {name}");
        return id;
    }

    public void AppendLog(Guid jobId, string level, string message)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.AppendLog(level, message);
        }
    }

    public void SetProgress(Guid jobId, int progressPercent)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.SetProgress(progressPercent);
        }
    }

    public void Complete(Guid jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Complete();
        }
    }

    public void Fail(Guid jobId, string errorMessage)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Fail(errorMessage);
        }
    }

    private void CleanupExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _jobs)
        {
            if (kvp.Value.IsExpired(now, DefaultRetention))
            {
                _jobs.TryRemove(kvp.Key, out _);
            }
        }
    }

    private sealed class JobState
    {
        private readonly object _lock = new();
        private readonly List<BackgroundJobLogDto> _logs = new();

        public JobState(Guid id, string name, string category, string? actorId, string? actorName, bool canCancel)
        {
            Id = id;
            Name = name;
            Category = category;
            ActorId = actorId;
            ActorName = actorName;
            CanCancel = canCancel;

            var now = DateTime.UtcNow;
            StartedAtUtc = now;
            UpdatedAtUtc = now;
        }

        public Guid Id { get; }
        public string Name { get; }
        public string Category { get; }
        public string Status { get; private set; } = "Running";
        public int ProgressPercent { get; private set; }
        public bool CanCancel { get; }
        public bool CancelRequested { get; private set; }
        public string? ActorId { get; }
        public string? ActorName { get; }
        public DateTime StartedAtUtc { get; }
        public DateTime UpdatedAtUtc { get; private set; }
        public DateTime? FinishedAtUtc { get; private set; }
        public string? ErrorMessage { get; private set; }

        public BackgroundJobDto ToDto()
        {
            lock (_lock)
            {
                return new BackgroundJobDto
                {
                    Id = Id,
                    Name = Name,
                    Category = Category,
                    Status = Status,
                    ProgressPercent = ProgressPercent,
                    CanCancel = CanCancel,
                    CancelRequested = CancelRequested,
                    ActorId = ActorId,
                    ActorName = ActorName,
                    StartedAtUtc = StartedAtUtc,
                    UpdatedAtUtc = UpdatedAtUtc,
                    FinishedAtUtc = FinishedAtUtc,
                    ErrorMessage = ErrorMessage
                };
            }
        }

        public List<BackgroundJobLogDto> LogsSnapshot(int limit)
        {
            lock (_lock)
            {
                return _logs
                    .OrderByDescending(x => x.TimestampUtc)
                    .Take(limit)
                    .OrderBy(x => x.TimestampUtc)
                    .ToList();
            }
        }

        public void AppendLog(string level, string message)
        {
            lock (_lock)
            {
                AppendLogUnsafe(level, message);
            }
        }

        public void SetProgress(int progressPercent)
        {
            lock (_lock)
            {
                ProgressPercent = Math.Clamp(progressPercent, 0, 100);
                UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        public void MarkCancelRequested()
        {
            lock (_lock)
            {
                if (!CancelRequested)
                {
                    CancelRequested = true;
                    Status = "CancelRequested";
                    UpdatedAtUtc = DateTime.UtcNow;
                    AppendLogUnsafe("WARN", "Cancel requested.");
                }
            }
        }

        public void Complete()
        {
            lock (_lock)
            {
                if (FinishedAtUtc.HasValue) return;
                Status = "Completed";
                ProgressPercent = 100;
                FinishedAtUtc = DateTime.UtcNow;
                UpdatedAtUtc = FinishedAtUtc.Value;
                AppendLogUnsafe("INFO", "Job completed.");
            }
        }

        public void Fail(string errorMessage)
        {
            lock (_lock)
            {
                if (FinishedAtUtc.HasValue) return;
                Status = "Failed";
                ErrorMessage = errorMessage;
                FinishedAtUtc = DateTime.UtcNow;
                UpdatedAtUtc = FinishedAtUtc.Value;
                AppendLogUnsafe("ERROR", errorMessage);
            }
        }

        public bool IsExpired(DateTime now, TimeSpan retention)
        {
            lock (_lock)
            {
                if (!FinishedAtUtc.HasValue) return false;
                return now - FinishedAtUtc.Value > retention;
            }
        }

        private void AppendLogUnsafe(string level, string message)
        {
            if (_logs.Count >= MaxLogsPerJob)
            {
                _logs.RemoveRange(0, Math.Min(200, _logs.Count));
            }

            _logs.Add(new BackgroundJobLogDto
            {
                TimestampUtc = DateTime.UtcNow,
                Level = string.IsNullOrWhiteSpace(level) ? "INFO" : level.Trim().ToUpperInvariant(),
                Message = message ?? string.Empty
            });

            UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}
