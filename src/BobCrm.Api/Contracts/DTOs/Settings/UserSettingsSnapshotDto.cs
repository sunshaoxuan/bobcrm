namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// 用户设置快照（系统+生效+覆盖）
/// </summary>
public record UserSettingsSnapshotDto(
    SystemSettingsDto System,
    UserSettingsDto Effective,
    UserSettingsDto? Overrides);
