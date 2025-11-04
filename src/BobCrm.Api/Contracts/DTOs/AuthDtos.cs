namespace BobCrm.Api.Contracts.DTOs;

public record RegisterDto(string Username, string Password, string Email);
public record LoginDto(string Username, string Password);
public record RefreshDto(string RefreshToken);
public record LogoutDto(string RefreshToken);
// ActivateDto 已删除：激活接口使用 query 参数 (userId, code) 而非 DTO

