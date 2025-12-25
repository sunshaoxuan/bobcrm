namespace BobCrm.Api.Contracts.Responses.System;

public sealed class BackgroundJobDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Running / Completed / Failed / Canceled / CancelRequested
    /// </summary>
    public string Status { get; set; } = "Running";

    public int ProgressPercent { get; set; }

    public bool CanCancel { get; set; }

    public bool CancelRequested { get; set; }

    public string? ActorId { get; set; }

    public string? ActorName { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateTime? FinishedAtUtc { get; set; }

    public string? ErrorMessage { get; set; }
}
