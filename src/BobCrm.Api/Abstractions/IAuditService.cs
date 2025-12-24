using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;

namespace BobCrm.Api.Abstractions;

/// <summary>
/// 审计日志持久化服务，用于异步写入系统审计轨迹。
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// 将一批审计日志附加到当前工作单元（由 DbContext 统一提交）。
    /// </summary>
    Task AttachAsync(AppDbContext db, IReadOnlyCollection<AuditLog> logs, CancellationToken ct = default);
}
